// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Guid } from '@cratis/fundamentals';
import { isResultFor } from './hasReadStatus';

/** The part of a host outcome query result {@link hasReadHostOutcome} looks at. */
export interface HostOutcomeResult {
    hasData: boolean;
    isSuccess: boolean;
    isPerforming: boolean;
    data?: { attemptId?: unknown; isConfigured?: unknown } | null;
}

/**
 * Whether a host outcome lookup has settled for the attempt it was made for. Arc starts the query with the
 * proxy's default value (`{}`, which `hasData` counts) and keeps the previous attempt's result while it
 * re-runs, and reading either as "not configured" would redirect straight past the host outcome screen. A
 * lookup that failed has settled too: it reads as not configured, so the person is still taken to the host.
 * @param result The host outcome query result.
 * @param attemptId The attempt the lookup was made for.
 * @returns True once the lookup has answered for that attempt, or failed.
 */
export const hasReadHostOutcome = (result: HostOutcomeResult, attemptId: Guid): boolean =>
    (!result.isPerforming && !result.isSuccess) ||
    (result.hasData && typeof result.data?.isConfigured === 'boolean' && isResultFor(result.data?.attemptId, attemptId));
