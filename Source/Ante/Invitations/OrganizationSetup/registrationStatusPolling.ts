// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** Polls serially, without superseding the initial status query while it is still running. */
export const startRegistrationStatusPolling = (
    refresh: () => Promise<void>,
    isInitialQueryPerforming: () => boolean,
    intervalMs = 1500): (() => void) => {
    let active = true;
    let timer: ReturnType<typeof setTimeout>;
    const schedule = () => {
        timer = globalThis.setTimeout(() => { void tick(); }, intervalMs);
    };
    const tick = async () => {
        if (!active) return;
        if (!isInitialQueryPerforming()) await refresh();
        if (active) schedule();
    };
    schedule();
    return () => {
        active = false;
        globalThis.clearTimeout(timer);
    };
};
