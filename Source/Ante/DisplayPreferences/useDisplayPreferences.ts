// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useMemo, useState } from 'react';
import { applyDisplayAttributes } from './applyDisplayPreferencesToDocument';
import { toDisplayAttributes } from './displayPreferencesAttributes';
import {
    DisplayContrast,
    DisplayControlSize,
    DisplayMotion,
    DisplayPreferences,
    DisplaySpacing,
    DisplayTextSize
} from './DisplayPreferences';
import { DISPLAY_PREFERENCES_STORAGE_KEY, loadDisplayPreferences, saveDisplayPreferences } from './displayPreferencesStorage';
import { computeEffectiveDisplayPreferences, EffectiveDisplayPreferences } from './effectiveDisplayPreferences';
import { resetVisualDisplayPreferences } from './resetDisplayPreferences';
import { prefersReducedMotion, subscribeToReducedMotionChange } from './systemPreferences';

/** The reactive display-preferences surface a settings UI consumes. */
export interface UseDisplayPreferencesResult {
    /** The stored, explicit preferences. */
    readonly preferences: DisplayPreferences;

    /** The preferences actually in effect, after reconciling with the live OS reduced-motion signal. */
    readonly effective: EffectiveDisplayPreferences;

    readonly setTextSize: (value: DisplayTextSize) => void;
    readonly setContrast: (value: DisplayContrast) => void;
    readonly setSpacing: (value: DisplaySpacing) => void;
    readonly setControlSize: (value: DisplayControlSize) => void;
    readonly setMotion: (value: DisplayMotion) => void;

    /** Resets every visual preference to default, preserving an explicit reduced-motion choice. */
    readonly reset: () => void;
}

/**
 * Owns the onboarding display-preferences lifecycle described by `Cratis/Ante#20`: reads the stored
 * preferences, keeps the document's `data-display-*` attributes (see `displayPreferencesAttributes.ts`)
 * in sync with them and with the live OS reduced-motion signal, persists every change, and stays in
 * sync with the same preferences changing in another same-origin tab.
 *
 * Applying to the document happens here (not only in `applyInitialDisplayPreferences`) so a change
 * made through the in-app menu, or arriving from another tab, takes effect immediately without a
 * reload.
 */
export const useDisplayPreferences = (): UseDisplayPreferencesResult => {
    const [preferences, setPreferences] = useState<DisplayPreferences>(loadDisplayPreferences);
    const [systemReducedMotion, setSystemReducedMotion] = useState<boolean>(prefersReducedMotion);

    const effective = useMemo(
        () => computeEffectiveDisplayPreferences(preferences, systemReducedMotion),
        [preferences, systemReducedMotion]);

    useEffect(() => {
        applyDisplayAttributes(document.documentElement, toDisplayAttributes(effective));
    }, [effective]);

    useEffect(() => subscribeToReducedMotionChange(setSystemReducedMotion), []);

    useEffect(() => {
        const onStorageChange = (event: StorageEvent) => {
            // A `key` of `null` means the whole store was cleared (e.g. `localStorage.clear()`); anything
            // else that isn't this preference's key belongs to unrelated state and is not our concern.
            if (event.key !== null && event.key !== DISPLAY_PREFERENCES_STORAGE_KEY) return;
            setPreferences(loadDisplayPreferences());
        };

        globalThis.addEventListener?.('storage', onStorageChange);
        return () => globalThis.removeEventListener?.('storage', onStorageChange);
    }, []);

    const update = (patch: Partial<DisplayPreferences>) => setPreferences(current => {
        const next = { ...current, ...patch };
        saveDisplayPreferences(next);
        return next;
    });

    return {
        preferences,
        effective,
        setTextSize: value => update({ textSize: value }),
        setContrast: value => update({ contrast: value }),
        setSpacing: value => update({ spacing: value }),
        setControlSize: value => update({ controlSize: value }),
        setMotion: value => update({ motion: value }),
        reset: () => setPreferences(current => {
            const next = resetVisualDisplayPreferences(current);
            saveDisplayPreferences(next);
            return next;
        })
    };
};
