// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DEFAULT_DISPLAY_PREFERENCES, DisplayPreferences } from './DisplayPreferences';
import { parseDisplayPreferences, serializeDisplayPreferences } from './displayPreferencesSchema';

/** The `localStorage` key display preferences are persisted under - same-origin, so a change in one tab is visible to every other tab open to the same origin via the `storage` event. */
export const DISPLAY_PREFERENCES_STORAGE_KEY = 'ante.displayPreferences';

/**
 * Loads the persisted display preferences, defensively.
 *
 * `localStorage` itself can throw (private/incognito modes in some browsers, storage disabled by
 * policy, quota exceeded) as much as its contents can be corrupt - both are covered by the same
 * `try`/`catch`, both fall back to {@link DEFAULT_DISPLAY_PREFERENCES}, and neither ever blocks
 * onboarding from rendering.
 * @returns The stored preferences, or the defaults when nothing usable is stored.
 */
export const loadDisplayPreferences = (): DisplayPreferences => {
    try {
        const raw = globalThis.localStorage?.getItem(DISPLAY_PREFERENCES_STORAGE_KEY) ?? null;
        return parseDisplayPreferences(raw);
    } catch {
        return DEFAULT_DISPLAY_PREFERENCES;
    }
};

/**
 * Persists display preferences, defensively.
 *
 * A denied or throwing `localStorage` still lets preferences apply for the current render via
 * in-memory state - they simply cannot survive a reload or reach another tab, which is a strictly
 * smaller failure than blocking onboarding on a storage write.
 * @param preferences The preferences to persist.
 */
export const saveDisplayPreferences = (preferences: DisplayPreferences): void => {
    try {
        globalThis.localStorage?.setItem(DISPLAY_PREFERENCES_STORAGE_KEY, serializeDisplayPreferences(preferences));
    } catch {
        // Storage unavailable or denied - nothing further to do; see the doc comment above.
    }
};
