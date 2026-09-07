// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DEFAULT_DISPLAY_PREFERENCES, DisplayPreferences } from './DisplayPreferences';

/**
 * Resets the purely visual preferences (text size, contrast, spacing, control size) back to their
 * defaults while preserving `motion` exactly as it was.
 *
 * `Cratis/Ante#20` is explicit that a reset must "preserve explicit reduced motion" - a person who
 * turned reduced motion on deliberately should not have that safety-relevant choice silently
 * discarded by a generic "reset display settings" action. Visual preferences carry no such safety
 * concern, so they reset unconditionally.
 * @param preferences The current preferences.
 * @returns Preferences with every field but `motion` restored to default.
 */
export const resetVisualDisplayPreferences = (preferences: DisplayPreferences): DisplayPreferences => ({
    ...DEFAULT_DISPLAY_PREFERENCES,
    motion: preferences.motion
});
