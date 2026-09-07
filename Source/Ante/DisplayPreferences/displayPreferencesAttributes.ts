// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { EffectiveDisplayPreferences } from './effectiveDisplayPreferences';

/** The `data-*` attributes {@link toDisplayAttributes} writes to the document root, keyed by the preference they carry. */
export const DISPLAY_PREFERENCE_ATTRIBUTES = Object.freeze({
    textSize: 'data-display-text-size',
    contrast: 'data-display-contrast',
    spacing: 'data-display-spacing',
    controlSize: 'data-display-control-size',
    motion: 'data-display-motion'
});

/**
 * Maps effective preferences to the `data-*` attributes `displayPreferences.css` selects on. A `null`
 * value means "remove the attribute" rather than "write the word default" - the default state carries
 * no attribute at all, so the CSS only ever needs to target the non-default cases and an unrecognized
 * or not-yet-applied document is indistinguishable from an explicit default.
 * @param effective The preferences to render as attributes.
 * @returns A map of attribute name to the value to set, or `null` to remove that attribute.
 */
export const toDisplayAttributes = (effective: EffectiveDisplayPreferences): Record<string, string | null> => ({
    [DISPLAY_PREFERENCE_ATTRIBUTES.textSize]: effective.textSize === 'default' ? null : effective.textSize,
    [DISPLAY_PREFERENCE_ATTRIBUTES.contrast]: effective.contrast === 'default' ? null : effective.contrast,
    [DISPLAY_PREFERENCE_ATTRIBUTES.spacing]: effective.spacing === 'default' ? null : effective.spacing,
    [DISPLAY_PREFERENCE_ATTRIBUTES.controlSize]: effective.controlSize === 'default' ? null : effective.controlSize,
    [DISPLAY_PREFERENCE_ATTRIBUTES.motion]: effective.reducedMotion ? 'reduced' : null
});
