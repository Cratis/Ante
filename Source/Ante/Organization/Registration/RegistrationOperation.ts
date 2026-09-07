// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Guid } from '@cratis/fundamentals';

const STORAGE_KEY = 'ante.registration.operation';
const STORAGE_VERSION = 1;

type StoredRegistrationOperation = {
    v: number;
    id: string;
};

/**
 * Reads the browser-persisted registration operation pointer for this tab, or creates and persists a new
 * one. The pointer is a versioned, opaque registration id only - never a profile draft, identity claim,
 * legal content, or token - so a reload or a return later resumes polling the same durable registration
 * instead of losing track of what was already submitted and risking a duplicate. Storage is per-tab
 * (`sessionStorage`), so two tabs opened to `/register` each get their own registration rather than
 * silently sharing - or colliding on - one.
 * @returns The registration id to use for this tab's registration attempt.
 */
export const getOrCreateRegistrationId = (): Guid => {
    const stored = readStoredOperation();
    if (stored) return Guid.parse(stored.id);

    const id = Guid.create();
    writeStoredOperation(id);
    return id;
};

/**
 * Clears the persisted registration operation pointer, so the next call to
 * {@link getOrCreateRegistrationId} starts a fresh registration - used when a person deliberately wants to
 * register a different organization rather than resume the one already tracked in this tab.
 */
export const clearRegistrationOperation = (): void => {
    try {
        globalThis.sessionStorage?.removeItem(STORAGE_KEY);
    } catch {
        // Storage unavailable (private browsing, disabled storage) - nothing was persisted, so nothing to clear.
    }
};

const readStoredOperation = (): StoredRegistrationOperation | null => {
    try {
        const raw = globalThis.sessionStorage?.getItem(STORAGE_KEY);
        if (!raw) return null;

        const parsed = JSON.parse(raw) as Partial<StoredRegistrationOperation>;
        if (parsed.v !== STORAGE_VERSION || typeof parsed.id !== 'string' || !Guid.isGuid(parsed.id)) return null;

        return parsed as StoredRegistrationOperation;
    } catch {
        return null;
    }
};

const writeStoredOperation = (id: Guid): void => {
    try {
        const operation: StoredRegistrationOperation = { v: STORAGE_VERSION, id: id.toString() };
        globalThis.sessionStorage?.setItem(STORAGE_KEY, JSON.stringify(operation));
    } catch {
        // Storage unavailable - the registration still works for this render, it just cannot resume a reload.
    }
};
