// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Picks the currently-active step index from each panel's `hidden` state.
 *
 * `CommandStepper`/`CommandStepperContent` (`@cratis/components`) keep exactly one panel's `hidden`
 * attribute unset regardless of how the person navigated - Next, Previous, or a header click - which
 * makes this the one reliable, navigation-method-agnostic signal for "which step is showing now".
 * `Cratis/Components` does not otherwise expose a step-changed callback that fires for Next/Previous
 * (only for header clicks), which is why this app observes the DOM directly instead - see
 * `useAccessibleStepper.ts`.
 * @param hiddenFlags Each panel's `hidden` state, in step order.
 * @returns The index of the first panel that is not hidden, or `0` if every panel is hidden (should not happen; a safe, deterministic fallback rather than `-1`).
 */
export const pickCurrentStepIndex = (hiddenFlags: readonly boolean[]): number => {
    const index = hiddenFlags.findIndex(hidden => !hidden);
    return index === -1 ? 0 : index;
};
