// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SignupContextEntry } from '../../Contracts/Organization/SignupContextEntry';

const STORAGE_KEY = 'ante.registration.context';
const MAXIMUM_LENGTH = 200;

/**
 * Captures the signup context the deployment allows from the registration link's query string, and keeps it
 * for this tab so a reload - or a round trip through sign-in that dropped the query - still carries it.
 * A link that carries any allowed key replaces what was kept; a link without one keeps the earlier context.
 * The server filters again: this only decides what the browser offers, never what is trusted.
 * @param allowedKeys The query-string keys this deployment keeps.
 * @param search The query string of the current location.
 * @returns The context to submit with the registration.
 */
export const captureSignupContext = (allowedKeys: readonly string[], search: string): SignupContextEntry[] => {
    const parameters = new URLSearchParams(search);
    const captured: SignupContextEntry[] = [];
    for (const key of allowedKeys) {
        const value = parameters.get(key)?.trim();
        if (value && value.length <= MAXIMUM_LENGTH) {
            captured.push(entry(key, value));
        }
    }

    if (captured.length > 0) {
        write(captured);
        return captured;
    }

    return read().filter(stored => allowedKeys.includes(stored.key));
};

/** Forgets the kept signup context, for when a person deliberately starts a new registration. */
export const clearSignupContext = (): void => {
    try {
        globalThis.sessionStorage?.removeItem(STORAGE_KEY);
    } catch {
        // Storage unavailable - nothing was kept.
    }
};

const entry = (key: string, value: string): SignupContextEntry => {
    const result = new SignupContextEntry();
    result.key = key;
    result.value = value;
    return result;
};

const read = (): SignupContextEntry[] => {
    try {
        const raw = globalThis.sessionStorage?.getItem(STORAGE_KEY);
        if (!raw) return [];
        const parsed: unknown = JSON.parse(raw);
        if (!Array.isArray(parsed)) return [];
        return parsed
            .filter((item): item is { key: string; value: string } =>
                typeof item?.key === 'string' && typeof item?.value === 'string' && item.value.length <= MAXIMUM_LENGTH)
            .map(item => entry(item.key, item.value));
    } catch {
        return [];
    }
};

const write = (context: SignupContextEntry[]): void => {
    try {
        globalThis.sessionStorage?.setItem(STORAGE_KEY, JSON.stringify(context.map(item => ({ key: item.key, value: item.value }))));
    } catch {
        // Storage unavailable - the context still travels with this render's submission.
    }
};
