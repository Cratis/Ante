// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useEffect, useState } from 'react';
import { Guid } from '@cratis/fundamentals';
import { ForAttempt } from './HostOutcome/HostOutcome';
import { HostOutcomeStatus } from './HostOutcome/HostOutcomeStatus';
import { hasReadHostOutcome } from './hasReadHostOutcome';

/** How often the host outcome is looked up again while the host has not reported a result. */
export const HOST_OUTCOME_POLL_INTERVAL_MS = 2000;

/** How long the lookup is repeated before the person is handed the manual actions instead. */
export const HOST_OUTCOME_GIVE_UP_AFTER_MS = 60000;

export type HostOutcomeState = {
    /** Whether this deployment has a host outcome backchannel configured at all. */
    isConfigured: boolean;
    /**
     * Whether the lookup for this attempt has settled, so `isConfigured` can be trusted. Not until the
     * lookup has answered for this very attempt - Arc starts with its default value and keeps the previous
     * attempt's result while re-running (Cratis/Ante#146) - unless it failed, which reads as not configured.
     */
    isRead: boolean;
    /** The host-reported outcome, defaulted to unknown until the first read arrives. */
    status: HostOutcomeStatus;
    /** A stable, low-cardinality reason code accompanying a terminal result; empty otherwise. */
    reasonCode: string;
    /** Whether the lookup has been repeated for {@link HOST_OUTCOME_GIVE_UP_AFTER_MS} without the host reporting a result. */
    hasGivenUp: boolean;
    /** Re-runs the lookup - for a host that has not yet reported a terminal result. */
    checkAgain: () => void;
};

/**
 * React binding for the `HostOutcomeView.ForAttempt` query - a thin wrapper with no logic of its own
 * beyond exposing a `checkAgain` action, the same thin-binding shape useOnboardingRecovery keeps over
 * OnboardingRecoveryState. The query is authenticated and attempt-bound server-side
 * (`ISignedInIdentity.IsVerifiedOwnerOf`), so passing `Guid.empty` - as callers that have not opted into
 * the host-outcome completion screen do - resolves to nothing rather than a real attempt's outcome.
 * @param attemptId The invitation or registration identifier to look up the outcome for.
 * @returns The current host outcome state and a `checkAgain` action.
 */
export const useHostOutcome = (attemptId: Guid): HostOutcomeState => {
    const [result, performQuery] = ForAttempt.use({ attemptId });

    const checkAgain = useCallback(() => performQuery({ attemptId }), [performQuery, attemptId]);
    const [hasGivenUp, setHasGivenUp] = useState(false);

    const isRead = hasReadHostOutcome(result, attemptId);
    const isConfigured = result.data?.isConfigured ?? false;
    const status = result.data?.status ?? HostOutcomeStatus.unknown;
    const isLookingUp = attemptId.toString() !== Guid.empty.toString();
    const isAwaitingHost = isLookingUp && isRead && isConfigured && status !== HostOutcomeStatus.succeeded && status !== HostOutcomeStatus.failed;

    // A host reports its outcome some moments after onboarding publishes, so keep asking rather than
    // leaving the person to - and give up only after a bounded time, when the manual actions are shown.
    useEffect(() => {
        if (!isAwaitingHost || hasGivenUp) return;
        const poll = globalThis.setInterval(() => { void checkAgain(); }, HOST_OUTCOME_POLL_INTERVAL_MS);
        const giveUp = globalThis.setTimeout(() => setHasGivenUp(true), HOST_OUTCOME_GIVE_UP_AFTER_MS);
        return () => {
            globalThis.clearInterval(poll);
            globalThis.clearTimeout(giveUp);
        };
    }, [isAwaitingHost, hasGivenUp, checkAgain]);

    // "Check again" on the manual screen starts a fresh window.
    const checkAgainAndResume = useCallback(() => {
        setHasGivenUp(false);
        void checkAgain();
    }, [checkAgain]);

    return {
        isConfigured,
        isRead,
        status,
        reasonCode: result.data?.reasonCode ?? '',
        hasGivenUp,
        checkAgain: checkAgainAndResume,
    };
};
