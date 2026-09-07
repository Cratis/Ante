// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Which phase an onboarding wizard should render: the command form, a waiting spinner while durable
 * publication is still in flight, or a recoverable "still processing" state once a local timeout window
 * has elapsed without confirmation.
 */
export type OnboardingRecoveryPhase = 'form' | 'waiting' | 'timedOut';
