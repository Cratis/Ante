// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import { Guid } from '@cratis/fundamentals';
import { StatusForInvitation, StatusForRegistration } from './OrganizationSetup';
import { OrganizationSetupAcceptanceStatus } from './OrganizationSetupAcceptanceStatus';
import { organizationStatusIds } from './organizationStatusAccess';
import { useRegistrationStatusPolling } from './useRegistrationStatusPolling';
import { useHostRedirect } from './useHostRedirect';
import { useOnboardingRecovery } from '../useOnboardingRecovery';
import { hasReadStatus } from '../hasReadStatus';
import { useHostOutcome } from '../useHostOutcome';
import { resolveHostOutcomeGate } from '../HostOutcomeGate';
import { OnboardingRecoveryPhase } from '../OnboardingRecoveryPhase';
import { HostOutcomeStatus } from '../HostOutcome/HostOutcomeStatus';

export type OrganizationSetupHandoffOptions = {
    /** The invitation or registration identifier this setup is for. */
    invitationId: Guid;
    /** Message shown when setup published but the host destination could not be resolved. */
    hostAppUnavailableMessage: string;
    /** Uses the separately owner-verified registration status instead of invitation status. */
    isRegistration?: boolean;
    /** True when this registration id was persisted before the current page load. */
    recoveringRegistration?: boolean;
    /**
     * Opts into the optional host-outcome completion screen once accepted, in place of the unconditional
     * automatic redirect - only when this deployment also has a host outcome backchannel configured
     * (`Ante:HostOutcomeUrl`). Defaults to `false`. The server only answers the verified owner of the
     * attempt: the invitation session for an invited wizard, or the owner recorded with a self-service
     * registration.
     */
    supportsHostOutcome?: boolean;
};

/** The phases `OrganizationSetupPage` and `RegistrationPage` render from, once `hasStatus` is true. */
export type OrganizationSetupHandoffPhase = OnboardingRecoveryPhase | 'hostOutcome' | 'heldForReview' | 'declined';

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
    /** Whether the host reported, with a signed outcome, that the person needs a new invitation; `hostOutcomeStatus` is then `failed`. */
    hostOutcomeRequiresNewInvitation: boolean;
    /** Re-runs the host outcome lookup, for a host that has not yet reported a terminal result. */
    checkHostOutcomeAgain: () => void;
    /** Navigates to the host only when the outcome gate permits handoff. */
    continueToHost: () => void;
};

const reviewPhase = (status?: OrganizationSetupAcceptanceStatus): OrganizationSetupHandoffPhase | undefined => {
    if (status === OrganizationSetupAcceptanceStatus.held) return 'heldForReview';
    if (status === OrganizationSetupAcceptanceStatus.declined) return 'declined';
    return undefined;
};

/**
 * Shared status/hand-off coordination for the two wizards that create an organization -
 * `OrganizationSetupPage` (invited) and `RegistrationPage` (self-service) - which use distinct
 * owner-verified status queries and redirect the same way once setup publishes. Extracted here because the two
 * pages already needed byte-identical logic for this, not as a general-purpose abstraction over the
 * three onboarding journeys.
 * @param options The invitation/registration id to track, the message to show on a destination failure, and whether this wizard supports the optional host-outcome screen.
 * @returns The current phase and the actions the page's `CommandStepper` and completion screen drive it with.
 */
export const useOrganizationSetupHandoff = ({ invitationId, hostAppUnavailableMessage, supportsHostOutcome = false, isRegistration = false, recoveringRegistration = false }: OrganizationSetupHandoffOptions): OrganizationSetupHandoffState => {
    const statusIds = organizationStatusIds(invitationId, isRegistration);
    const [invitationStatus] = StatusForInvitation.when(!isRegistration).use({ invitationId: statusIds.invitationId });
    const [registrationStatus, refreshRegistration] = StatusForRegistration.when(isRegistration).use({ registrationId: statusIds.registrationId });
    const statusResult = isRegistration ? registrationStatus : invitationStatus;

    const hasStatus = hasReadStatus(statusResult, invitationId);
    const isRecorded = hasStatus && statusResult.data.status !== OrganizationSetupAcceptanceStatus.pending;
    const isAccepted = hasStatus && statusResult.data.status === OrganizationSetupAcceptanceStatus.accepted;
    const reviewStatus = hasStatus ? statusResult.data.status : undefined;
    const recovery = useOnboardingRecovery(isRecorded, isAccepted);
    const [submitted, setSubmitted] = useState(false);
    const { resetPollWindow } = useRegistrationStatusPolling({
        invitationId, isRegistration, isOperation: submitted || recoveringRegistration,
        registrationStatus, refreshRegistration, isAccepted, reviewStatus, recoveryPhase: recovery.phase
    });

    // Never looked up before Ante's own onboarding has actually published - a host has nothing to report
    // on an attempt it has not been notified of yet - and never looked up at all for a caller that has
    // not opted in.
    const hostOutcome = useHostOutcome(supportsHostOutcome && recovery.isAccepted ? invitationId : Guid.empty);
    const gate = resolveHostOutcomeGate(recovery.isAccepted, supportsHostOutcome && hostOutcome.isConfigured, !supportsHostOutcome || hostOutcome.isRead, hostOutcome.status, hostOutcome.hasGivenUp);

    const { errorMessages, navigateToHost, captureOrganizationName } = useHostRedirect({
        gate, statusOrganizationName: statusResult.data.organizationName, hostAppUnavailableMessage
    });

    return {
        hasStatus,
        phase: reviewPhase(reviewStatus) ?? (gate === 'showHostOutcome' ? 'hostOutcome' : recovery.phase),
        errorMessages,
        captureOrganizationName,
        markSubmitted: () => { resetPollWindow(); setSubmitted(true); recovery.checkAgain(); recovery.markSubmitted(); },
        checkAgain: () => { resetPollWindow(); recovery.checkAgain(); },
        hostOutcomeStatus: hostOutcome.status,
        hostOutcomeReasonCode: hostOutcome.reasonCode,
        hostOutcomeRequiresNewInvitation: hostOutcome.requiresNewInvitation,
        checkHostOutcomeAgain: hostOutcome.checkAgain,
        continueToHost: navigateToHost,
    };
};
