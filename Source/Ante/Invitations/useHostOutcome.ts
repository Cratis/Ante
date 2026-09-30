// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback } from 'react';
import { Guid } from '@cratis/fundamentals';
import { ForAttempt } from './HostOutcome/HostOutcome';
import { HostOutcomeStatus } from './HostOutcome/HostOutcomeStatus';
import { hasReadHostOutcome } from './hasReadHostOutcome';

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

    return {
        isConfigured: result.data?.isConfigured ?? false,
        isRead: hasReadHostOutcome(result, attemptId),
        status: result.data?.status ?? HostOutcomeStatus.unknown,
        reasonCode: result.data?.reasonCode ?? '',
        checkAgain,
    };
};
