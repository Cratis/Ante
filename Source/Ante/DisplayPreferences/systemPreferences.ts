// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const REDUCED_MOTION_QUERY = '(prefers-reduced-motion: reduce)';

/** No subscription was made (matchMedia unavailable/throwing) - unsubscribing is a safe, deliberate no-op. */
const noUnsubscribe = (): void => undefined;

/**
 * Reads whether the operating system currently requests reduced motion, defensively - `matchMedia`
 * is unavailable during server-side rendering and in some restricted embeddings, so its absence (or
 * throwing) is treated as "no preference expressed" rather than a hard failure.
 * @returns Whether the OS currently prefers reduced motion.
 */
export const prefersReducedMotion = (): boolean => {
    try {
        return globalThis.matchMedia?.(REDUCED_MOTION_QUERY).matches ?? false;
    } catch {
        return false;
    }
};

/**
 * Subscribes to live changes in the operating system's reduced-motion preference (e.g. the person
 * changes it in their OS settings while onboarding is open, without reloading).
 * @param callback Invoked with the new match state whenever the OS preference changes.
 * @returns An unsubscribe function; always safe to call, even when no subscription was made.
 */
export const subscribeToReducedMotionChange = (callback: (matches: boolean) => void): (() => void) => {
    try {
        const query = globalThis.matchMedia?.(REDUCED_MOTION_QUERY);
        if (!query) return noUnsubscribe;

        const listener = (event: MediaQueryListEvent) => callback(event.matches);
        query.addEventListener('change', listener);
        return () => query.removeEventListener('change', listener);
    } catch {
        return noUnsubscribe;
    }
};
