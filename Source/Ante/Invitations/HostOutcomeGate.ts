// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Which of the two accepted-state behaviors a wizard should render: redirect to the host automatically -
 * today's unconditional behavior - or pause on a completion screen that displays the optional
 * host-reported outcome and lets the person continue on their own action instead. `keepWaiting` covers
 * every phase before acceptance, where neither applies yet.
 */
export type HostOutcomeGateDecision = 'redirectAutomatically' | 'showHostOutcome' | 'keepWaiting';

/**
 * Decides which of the two accepted-state behaviors an invitation-bound wizard should render, purely
 * from whether Ante's own onboarding has published and whether this deployment has a host outcome
 * backchannel configured. Kept pure and framework-free - the same discipline OnboardingRecoveryState
 * follows - so the branching a wizard renders from can be exercised directly, without mounting a
 * component or a query.
 *
 * Configuring the host outcome adapter never changes *when* onboarding is considered done - published
 * onboarding and the optional host outcome stay distinct: `isAccepted` is unaffected either way, this
 * only decides what the person sees once it is true.
 * @param isAccepted Whether durable evidence confirms Ante's own onboarding facts have published.
 * @param isHostOutcomeConfigured Whether this deployment has a host outcome backchannel configured.
 * @returns The decision the wizard should render from.
 */
export const resolveHostOutcomeGate = (isAccepted: boolean, isHostOutcomeConfigured: boolean): HostOutcomeGateDecision => {
    if (!isAccepted) return 'keepWaiting';
    return isHostOutcomeConfigured ? 'showHostOutcome' : 'redirectAutomatically';
};
