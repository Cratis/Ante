// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useEffect, useRef, useState } from 'react';
import { Guid } from '@cratis/fundamentals';
import { StatusForInvitation } from './OrganizationSetup';
import { OrganizationSetupAcceptanceStatus } from './OrganizationSetupAcceptanceStatus';
import { HostUrl } from '../../Configuration/Configuration';
import { resolveHostAppRedirectUrl } from '../../Configuration/hostAppRedirect';
import { useOnboardingRecovery } from '../useOnboardingRecovery';
import { useHostOutcome } from '../useHostOutcome';
import { resolveHostOutcomeGate } from '../HostOutcomeGate';
import { OnboardingRecoveryPhase } from '../OnboardingRecoveryPhase';
import { HostOutcomeStatus } from '../HostOutcome/HostOutcomeStatus';

export type OrganizationSetupHandoffOptions = {
    /** The invitation or registration identifier this setup is for. */
    invitationId: Guid;
    /** Message shown when setup published but the host destination could not be resolved. */
    hostAppUnavailableMessage: string;
    /**
     * Opts into the optional host-outcome completion screen once accepted, in place of today's
     * unconditional automatic redirect - only when this deployment also has a host outcome backchannel
     * configured (`Ante:HostOutcomeUrl`). Defaults to `false`.
     *
     * Only an invitation-bound wizard can safely pass `true`: the host outcome lookup is authenticated
     * through `ISignedInIdentity.IsVerifiedOwnerOf`, which requires a genuine accepted-invitation
     * session. Self-service registration (`RegistrationPage`) has no such session for its
     * client-generated registration id - `IsVerifiedOwnerOf` would never verify it - so it must never
     * pass `true` here: doing so would not show a host outcome, it would just replace registration's
     * always-reliable automatic redirect with a screen that can never leave its pending state.
     */
    supportsHostOutcome?: boolean;
};

/** The phases `OrganizationSetupPage` and `RegistrationPage` render from, once `hasStatus` is true. */
export type OrganizationSetupHandoffPhase = OnboardingRecoveryPhase | 'hostOutcome';

export type OrganizationSetupHandoffState = {
    /** Whether the durable status for this operation has been read at least once. */
    hasStatus: boolean;
    /** The phase to render: the wizard form, a waiting spinner, the optional host-outcome completion screen, or a recoverable timed-out state. */
    phase: OrganizationSetupHandoffPhase;
    /** Set once setup published but the redirect destination could not be resolved or reached. */
    errorMessages: string[];
    /** Records the organization name at submit time, for the redirect once the host is ready. */
    captureOrganizationName: (organizationName: string) => void;
    /** Call once the wizard's command has succeeded, to switch to the waiting phase immediately. */
    markSubmitted: () => void;
    /** Resets the timeout window without creating a new operation or losing durable status observation. */
    checkAgain: () => void;
    /** The host-reported outcome. Only meaningful while `phase` is `'hostOutcome'`. */
    hostOutcomeStatus: HostOutcomeStatus;
    /** A stable, low-cardinality reason code accompanying a terminal host outcome; empty otherwise. */
    hostOutcomeReasonCode: string;
    /** Re-runs the host outcome lookup, for a host that has not yet reported a terminal result. */
    checkHostOutcomeAgain: () => void;
    /** Navigates to the host now - published onboarding never depends on host outcome, so this is always safe to call regardless of what (if anything) the host has reported. */
    continueToHost: () => void;
};

/**
 * Shared status/hand-off coordination for the two wizards that create an organization -
 * `OrganizationSetupPage` (invited) and `RegistrationPage` (self-service) - which poll the exact same
 * durable status query and redirect the exact same way once it publishes. Extracted here because the two
 * pages already needed byte-identical logic for this, not as a general-purpose abstraction over the
 * three onboarding journeys.
 * @param options The invitation/registration id to track, the message to show on a destination failure, and whether this wizard supports the optional host-outcome screen.
 * @returns The current phase and the actions the page's `CommandStepper` and completion screen drive it with.
 */
export const useOrganizationSetupHandoff = ({ invitationId, hostAppUnavailableMessage, supportsHostOutcome = false }: OrganizationSetupHandoffOptions): OrganizationSetupHandoffState => {
    const [statusResult] = StatusForInvitation.use({ invitationId });
    const [hostUrlResult] = HostUrl.use();
    const [errorMessages, setErrorMessages] = useState<string[]>([]);
    const organizationNameRef = useRef('');

    const isRecorded = statusResult.hasData && statusResult.data.status !== OrganizationSetupAcceptanceStatus.pending;
    const isAccepted = statusResult.hasData && statusResult.data.status === OrganizationSetupAcceptanceStatus.accepted;
    const recovery = useOnboardingRecovery(isRecorded, isAccepted);

    // Never looked up before Ante's own onboarding has actually published - a host has nothing to report
    // on an attempt it has not been notified of yet - and never looked up at all for a caller that has
    // not opted in, so a registration id (which IsVerifiedOwnerOf can never verify) is never even sent.
    const hostOutcome = useHostOutcome(supportsHostOutcome && recovery.isAccepted ? invitationId : Guid.empty);
    const gate = resolveHostOutcomeGate(recovery.isAccepted, supportsHostOutcome && hostOutcome.isConfigured);

    const navigateToHost = useCallback(() => {
        if (!hostUrlResult.isSuccess) {
            setErrorMessages([hostAppUnavailableMessage]);
            return;
        }

        const organizationName = statusResult.data.organizationName || organizationNameRef.current;
        if (!organizationName) return;

        try {
            const config = hostUrlResult.data;
            window.location.href = resolveHostAppRedirectUrl(config.hostAppUrl, organizationName, config.signInPath);
        } catch {
            // A malformed host configuration must present as a recoverable destination failure, not a
            // crashed page - setup already published, so the person's information was not lost.
            setErrorMessages([hostAppUnavailableMessage]);
        }
    }, [hostUrlResult, statusResult.data.organizationName, hostAppUnavailableMessage]);

    useEffect(() => {
        // Unchanged automatic redirect when the host-outcome screen was never opted into or configured -
        // 'showHostOutcome' instead lets the completion screen's own Continue action call navigateToHost,
        // and 'keepWaiting' means onboarding has not published yet, so there is nowhere to go.
        if (gate !== 'redirectAutomatically') return;
        navigateToHost();
    }, [gate, navigateToHost]);

    return {
        hasStatus: statusResult.hasData,
        phase: gate === 'showHostOutcome' ? 'hostOutcome' : recovery.phase,
        errorMessages,
        captureOrganizationName: (organizationName: string) => { organizationNameRef.current = organizationName; },
        markSubmitted: () => recovery.markSubmitted(),
        checkAgain: () => recovery.checkAgain(),
        hostOutcomeStatus: hostOutcome.status,
        hostOutcomeReasonCode: hostOutcome.reasonCode,
        checkHostOutcomeAgain: hostOutcome.checkAgain,
        continueToHost: navigateToHost,
    };
};
