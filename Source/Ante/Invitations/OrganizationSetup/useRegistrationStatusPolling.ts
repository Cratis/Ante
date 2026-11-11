// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useRef, useState } from 'react';
import { Guid } from '@cratis/fundamentals';
import { OrganizationSetupAcceptanceStatus } from './OrganizationSetupAcceptanceStatus';
import { isDecidedByReview, shouldRecheckRegistrationStatus } from './organizationStatusAccess';
import { startRegistrationStatusPolling } from './registrationStatusPolling';

const pollWindowMilliseconds = 20000;

type RegistrationStatusResult = {
    isPerforming: boolean;
    hasData: boolean;
    data?: { status?: OrganizationSetupAcceptanceStatus };
};

export type RegistrationStatusPollingOptions = {
    /** The registration identifier being tracked. */
    invitationId: Guid;
    /** Whether this handoff tracks a self-service registration at all. */
    isRegistration: boolean;
    /** True once the command succeeded or a recovered registration id may already be an operation. */
    isOperation: boolean;
    /** The registration status query result. */
    registrationStatus: RegistrationStatusResult;
    /** Re-runs the registration status query. */
    refreshRegistration: (parameters: { registrationId: Guid }) => Promise<void>;
    /** Whether onboarding already published. */
    isAccepted: boolean;
    /** The recorded acceptance status, when read. */
    reviewStatus?: OrganizationSetupAcceptanceStatus;
    /** The current recovery phase. */
    recoveryPhase: string;
};

/**
 * Keeps a self-service registration's durable status fresh: bounds the polling window and refreshes the
 * status query until a decision, acceptance or the window's end.
 * @param options The registration being tracked and the state the polling decisions depend on.
 * @returns Whether the polling window expired, and the action that starts a fresh window.
 */
export const useRegistrationStatusPolling = ({ invitationId, isRegistration, isOperation, registrationStatus, refreshRegistration, isAccepted, reviewStatus, recoveryPhase }: RegistrationStatusPollingOptions) => {
    const [pollWindowExpired, setPollWindowExpired] = useState(false);
    const firstRegistrationResultRef = useRef({ id: invitationId.toString(), result: registrationStatus });
    if (firstRegistrationResultRef.current.id !== invitationId.toString()) {
        firstRegistrationResultRef.current = { id: invitationId.toString(), result: registrationStatus };
    }
    // A cached snapshot starts with isPerforming=false even while Arc's automatic initial fetch is
    // in flight. Wait for that fetch to settle into a new result before starting any refresh.
    const initialQueryPending = registrationStatus === firstRegistrationResultRef.current.result || registrationStatus.isPerforming;
    const pollRef = useRef({ refreshRegistration, initialQueryPending });
    pollRef.current = { refreshRegistration, initialQueryPending };

    // A fresh registration id is not an operation yet: never poll it until the command succeeds.
    // A pointer recovered after reload may already have an owner, so recheck its initial snapshot.
    // Stop after the recovery window; Check Again starts a fresh window for a pending operation.
    useEffect(() => {
        if (!isRegistration || !isOperation || pollWindowExpired || isAccepted || isDecidedByReview(reviewStatus)) return;
        const timeout = globalThis.setTimeout(() => setPollWindowExpired(true), pollWindowMilliseconds);
        return () => globalThis.clearTimeout(timeout);
    }, [isRegistration, isOperation, pollWindowExpired, isAccepted, reviewStatus]);

    useEffect(() => {
        if (!shouldRecheckRegistrationStatus(
            isRegistration, isOperation,
            pollWindowExpired || recoveryPhase === 'timedOut', registrationStatus.hasData, registrationStatus.data?.status)) return;
        return startRegistrationStatusPolling(
            () => pollRef.current.refreshRegistration({ registrationId: invitationId }),
            () => pollRef.current.initialQueryPending);
    }, [isRegistration, isOperation, pollWindowExpired, recoveryPhase, registrationStatus.hasData, registrationStatus.data?.status, invitationId]);

    return { pollWindowExpired, resetPollWindow: () => setPollWindowExpired(false) };
};
