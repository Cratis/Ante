// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DisplayContrast, DisplayControlSize, DisplayPreferences, DisplaySpacing, DisplayTextSize } from './DisplayPreferences';

/**
 * The preferences actually in effect after reconciling the stored, explicit {@link DisplayPreferences}
 * with the operating system's live `prefers-reduced-motion` signal. This is what gets applied to the
 * document - never the raw stored preferences alone - so the OS setting is always honored even when
 * the person has not opened the in-app preference menu at all.
 */
export interface EffectiveDisplayPreferences {
    readonly textSize: DisplayTextSize;
    readonly contrast: DisplayContrast;
    readonly spacing: DisplaySpacing;
    readonly controlSize: DisplayControlSize;
    readonly reducedMotion: boolean;
}

/**
 * Reconciles stored preferences with the live operating-system reduced-motion signal.
 *
 * Motion is the one preference that is never purely an app setting: `reducedMotion` is `true`
 * whenever the person explicitly turned it on in-app (`motion: 'reduced'`) OR the operating system
 * currently requests reduced motion - whichever asks for less motion wins. This is what "continue
 * honoring OS settings" and "preserve explicit reduced motion when resetting visual preferences"
 * (`Cratis/Ante#20`) both come down to at the point of use.
 * @param preferences The stored, explicit preferences.
 * @param systemPrefersReducedMotion The live `(prefers-reduced-motion: reduce)` match, read by the caller.
 * @returns The preferences that should actually be applied to the document.
 */
export const computeEffectiveDisplayPreferences = (
    preferences: DisplayPreferences,
    systemPrefersReducedMotion: boolean
): EffectiveDisplayPreferences => ({
    textSize: preferences.textSize,
    contrast: preferences.contrast,
    spacing: preferences.spacing,
    controlSize: preferences.controlSize,
    reducedMotion: preferences.motion === 'reduced' || systemPrefersReducedMotion
});
