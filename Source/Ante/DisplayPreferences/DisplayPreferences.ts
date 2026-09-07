// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** The current version of the {@link DisplayPreferences} schema - bump this, never reshape a field in place, whenever the shape changes. */
export const DISPLAY_PREFERENCES_SCHEMA_VERSION = 1;

/** Relative text size, scaling the document root font size. */
export type DisplayTextSize = 'default' | 'large' | 'largest';

/** Contrast level between text/controls and their surfaces. */
export type DisplayContrast = 'default' | 'high';

/** Spacing between and within onboarding controls. */
export type DisplaySpacing = 'default' | 'relaxed';

/** Minimum size of interactive controls (buttons, inputs, stepper headers). */
export type DisplayControlSize = 'default' | 'large';

/**
 * Motion preference. `system` continues honoring the operating system's `prefers-reduced-motion`
 * setting; `reduced` is an explicit, in-app override that keeps motion reduced even if the person
 * later resets the other, purely visual preferences.
 */
export type DisplayMotion = 'system' | 'reduced';

/**
 * The small, versioned, closed schema of onboarding display preferences described by
 * `Cratis/Ante#20` - only these fields and only these values are ever recognized. Anything else
 * (an unknown field, an unknown value, a future schema version) is treated as absent rather than
 * guessed at, so a corrupt or newer-than-this-build stored value can never produce undefined
 * behavior - see {@link parseDisplayPreferences}.
 *
 * This is deliberately small and neutral: it never stores a personal draft, a token, or anything
 * that identifies who is onboarding - only how the onboarding UI should render for them.
 */
export interface DisplayPreferences {
    readonly schemaVersion: typeof DISPLAY_PREFERENCES_SCHEMA_VERSION;
    readonly textSize: DisplayTextSize;
    readonly contrast: DisplayContrast;
    readonly spacing: DisplaySpacing;
    readonly controlSize: DisplayControlSize;
    readonly motion: DisplayMotion;
}

/** The preferences used when nothing is stored yet, storage is unavailable, or a stored value cannot be trusted. */
export const DEFAULT_DISPLAY_PREFERENCES: DisplayPreferences = Object.freeze({
    schemaVersion: DISPLAY_PREFERENCES_SCHEMA_VERSION,
    textSize: 'default',
    contrast: 'default',
    spacing: 'default',
    controlSize: 'default',
    motion: 'system'
});
