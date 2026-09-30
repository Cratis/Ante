// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

/** The lobby's own translations, so the specs find controls by the text a person actually sees. */
export type Strings = typeof import('../../Source/Ante/Locales/en/translation.json');

const load = (folder: string): Strings =>
    JSON.parse(readFileSync(fileURLToPath(new URL(`../../Source/Ante/Locales/${folder}/translation.json`, import.meta.url)), 'utf8')) as Strings;

export interface Locale {
    /** What the browser asks for (`navigator.language`, `Accept-Language`). */
    browserLocale: string;

    /** The document language the lobby sets. */
    documentLanguage: string;
    strings: Strings;
}

export const english: Locale = { browserLocale: 'en-US', documentLanguage: 'en-US', strings: load('en') };
export const bokmal: Locale = { browserLocale: 'nb-NO', documentLanguage: 'nb-NO', strings: load('nb') };
export const locales = [english, bokmal];

/**
 * Formats the stepper's step announcement the way the lobby does.
 * @param strings The translations.
 * @param current The one-based step number.
 * @param total The number of steps.
 * @param label The step's header.
 * @returns The announcement.
 */
export const stepAnnouncement = (strings: Strings, current: number, total: number, label: string): string =>
    strings.accessibility.stepAnnouncement
        .replace('{current}', String(current))
        .replace('{total}', String(total))
        .replace('{label}', label);
