// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {
    DEFAULT_DISPLAY_PREFERENCES,
    DISPLAY_PREFERENCES_SCHEMA_VERSION,
    DisplayContrast,
    DisplayControlSize,
    DisplayMotion,
    DisplayPreferences,
    DisplaySpacing,
    DisplayTextSize
} from './DisplayPreferences';

const TEXT_SIZES: readonly DisplayTextSize[] = ['default', 'large', 'largest'];
const CONTRASTS: readonly DisplayContrast[] = ['default', 'high'];
const SPACINGS: readonly DisplaySpacing[] = ['default', 'relaxed'];
const CONTROL_SIZES: readonly DisplayControlSize[] = ['default', 'large'];
const MOTIONS: readonly DisplayMotion[] = ['system', 'reduced'];

const isOneOf = <T extends string>(value: unknown, allowed: readonly T[]): value is T =>
    typeof value === 'string' && (allowed as readonly string[]).includes(value);

/**
 * Parses a stored display-preferences payload, enforcing the closed schema described by
 * `Cratis/Ante#20`.
 *
 * Deterministic and total - every input, however malformed, produces a valid
 * {@link DisplayPreferences}, never an exception and never a partially-applied render:
 * - `null`/`undefined`/empty input falls back to {@link DEFAULT_DISPLAY_PREFERENCES}.
 * - Invalid JSON falls back to {@link DEFAULT_DISPLAY_PREFERENCES}.
 * - A `schemaVersion` other than {@link DISPLAY_PREFERENCES_SCHEMA_VERSION} (older or newer/future)
 *   falls back to {@link DEFAULT_DISPLAY_PREFERENCES} entirely, rather than guessing how to migrate
 *   fields whose meaning under a different version is unknown.
 * - Within a matching schema version, each field is validated independently against its closed set
 *   of allowed values; an unknown or missing value for one field falls back to that field's default
 *   without discarding the other, valid fields.
 * @param raw The raw stored string (e.g. from `localStorage`), or `null`/`undefined` when nothing is stored.
 * @returns A valid {@link DisplayPreferences}, never throws.
 */
export const parseDisplayPreferences = (raw: string | null | undefined): DisplayPreferences => {
    if (!raw) return DEFAULT_DISPLAY_PREFERENCES;

    try {
        const parsed = JSON.parse(raw) as Partial<DisplayPreferences> | null;
        if (!parsed || typeof parsed !== 'object' || parsed.schemaVersion !== DISPLAY_PREFERENCES_SCHEMA_VERSION) {
            return DEFAULT_DISPLAY_PREFERENCES;
        }

        return {
            schemaVersion: DISPLAY_PREFERENCES_SCHEMA_VERSION,
            textSize: isOneOf(parsed.textSize, TEXT_SIZES) ? parsed.textSize : DEFAULT_DISPLAY_PREFERENCES.textSize,
            contrast: isOneOf(parsed.contrast, CONTRASTS) ? parsed.contrast : DEFAULT_DISPLAY_PREFERENCES.contrast,
            spacing: isOneOf(parsed.spacing, SPACINGS) ? parsed.spacing : DEFAULT_DISPLAY_PREFERENCES.spacing,
            controlSize: isOneOf(parsed.controlSize, CONTROL_SIZES) ? parsed.controlSize : DEFAULT_DISPLAY_PREFERENCES.controlSize,
            motion: isOneOf(parsed.motion, MOTIONS) ? parsed.motion : DEFAULT_DISPLAY_PREFERENCES.motion
        };
    } catch {
        return DEFAULT_DISPLAY_PREFERENCES;
    }
};

/** Serializes preferences for storage. The inverse of {@link parseDisplayPreferences}. */
export const serializeDisplayPreferences = (preferences: DisplayPreferences): string => JSON.stringify(preferences);
