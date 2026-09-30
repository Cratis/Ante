// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** The part of an onboarding status query result {@link hasReadStatus} looks at. */
export interface OnboardingStatusResult {
    hasData: boolean;
    data?: { status?: unknown } | null;
}

/**
 * Whether an onboarding status query has actually delivered a status. Arc seeds an observable query's
 * result with the query's default value - `{}` in the generated proxies - and `hasData` counts that seeded
 * object as data. Reading its missing status as "not pending" would report a brand-new invitation or
 * registration as already recorded, which `OnboardingRecoveryState` deliberately never takes back, so the
 * wizard would never show its form (Cratis/Ante#146).
 * @param result The status query result.
 * @returns True once a status value has arrived.
 */
export const hasReadStatus = (result: OnboardingStatusResult): boolean =>
    result.hasData && typeof result.data?.status === 'number';
