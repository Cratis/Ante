// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { applyDisplayAttributes } from './applyDisplayPreferencesToDocument';
import { toDisplayAttributes } from './displayPreferencesAttributes';
import { computeEffectiveDisplayPreferences } from './effectiveDisplayPreferences';
import { loadDisplayPreferences } from './displayPreferencesStorage';
import { prefersReducedMotion } from './systemPreferences';

/**
 * Applies stored display preferences to the document root synchronously, before React renders
 * anything. Called once from `index.tsx`, ahead of `ReactDOM.createRoot(...).render(...)`.
 *
 * This is what `Cratis/Ante#20` means by "deterministic pre-render application" and "no unwanted
 * first-paint flash": the `data-display-*` attributes `displayPreferences.css` selects on are already
 * on `<html>` by the time the first frame paints, so onboarding never renders once at the wrong text
 * size/contrast/spacing and then visibly snaps to the stored preference a moment later.
 */
export const applyInitialDisplayPreferences = (): void => {
    const preferences = loadDisplayPreferences();
    const effective = computeEffectiveDisplayPreferences(preferences, prefersReducedMotion());
    applyDisplayAttributes(document.documentElement, toDisplayAttributes(effective));
};
