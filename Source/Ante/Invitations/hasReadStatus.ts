// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Guid } from '@cratis/fundamentals';

/** The part of an onboarding status query result {@link hasReadStatus} looks at. */
export interface OnboardingStatusResult {
    hasData: boolean;
    data?: { status?: unknown; invitationId?: unknown } | null;
}

/**
 * Whether an identifier a query result carries is the one that was asked for.
 * @param value The identifier in the result.
 * @param id The identifier the query was made for.
 * @returns True when they are the same identifier.
 */
export const isResultFor = (value: unknown, id: Guid): boolean =>
    value !== undefined && value !== null && String(value).toLowerCase() === id.toString().toLowerCase();

/**
 * Whether an onboarding status query has actually delivered a status for the invitation or registration it
 * was asked about. Arc seeds a query's result with its default value - `{}` in the generated proxies - and
 * `hasData` counts that seeded object as data, so a missing status would read as "not pending" and report a
 * brand-new invitation or registration as already recorded, which `OnboardingRecoveryState` deliberately
 * never takes back (Cratis/Ante#146). Arc also keeps the previous result while a query re-runs for new
 * arguments, so a status is only read once it is about the identifier asked for - never the previous
 * query's, such as the `Guid.empty` lookup made before identity resolves the invitation.
 * @param result The status query result.
 * @param id The invitation or registration the query was made for.
 * @returns True once a status for that identifier has arrived.
 */
export const hasReadStatus = (result: OnboardingStatusResult, id: Guid): boolean =>
    result.hasData && typeof result.data?.status === 'number' && isResultFor(result.data?.invitationId, id);
