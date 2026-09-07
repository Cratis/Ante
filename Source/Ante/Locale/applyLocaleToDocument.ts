// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { LOCALE_DIRECTIONS, LOCALE_TAGS, SupportedLocale } from './Locale';

/**
 * The minimal surface {@link applyLocaleToDocument} needs from a target element - narrowed down from
 * `Element` (rather than typed as `HTMLElement` directly) so specs can pass a plain fake instead of
 * requiring a real DOM, the same reasoning `applyDisplayPreferencesToDocument.ts`'s `AttributeTarget` applies.
 */
export interface LocaleAttributeTarget {
    setAttribute(name: string, value: string): void;
}

/**
 * Applies the resolved locale's language tag and text direction to a target element - normally
 * `document.documentElement` - covering `Cratis/Ante#21`'s "document language/direction" requirement.
 * Idempotent: calling this again with the same locale simply re-asserts the same two attributes.
 * @param target The element to write `lang`/`dir` to.
 * @param locale The resolved locale to apply.
 */
export const applyLocaleToDocument = (target: LocaleAttributeTarget, locale: SupportedLocale): void => {
    target.setAttribute('lang', LOCALE_TAGS[locale]);
    target.setAttribute('dir', LOCALE_DIRECTIONS[locale]);
};
