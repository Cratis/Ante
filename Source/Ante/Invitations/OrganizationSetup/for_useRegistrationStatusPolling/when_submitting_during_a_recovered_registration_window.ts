// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, describe, it, vi } from 'vitest';
import { Guid } from '@cratis/fundamentals';
import { useRegistrationStatusPolling } from '../useRegistrationStatusPolling';

// The CI runner shares its module cache across files; reload the hook after installing this file's mocks.
vi.hoisted(() => { vi.resetModules(); });

// Drive the real hook's state/effects with a small synchronous dispatcher: the project's node test
// environment has no DOM renderer. Effects and their cleanup run on dependency changes as in React.
const harness = vi.hoisted(() => {
    const slots: unknown[] = [];
    const cleanups: Array<(() => void) | undefined> = [];
    let index = 0;
    let dirty = false;
    const reset = () => { cleanups.forEach(cleanup => cleanup?.()); slots.length = 0; cleanups.length = 0; index = 0; dirty = false; };
    const begin = () => { index = 0; dirty = false; };
    const changed = (before: unknown[], after: unknown[]) => before.length !== after.length || before.some((value, i) => !Object.is(value, after[i]));
    return { slots, cleanups, next: () => index++, begin, needsRender: () => dirty, changed, reset, markDirty: () => { dirty = true; } };
});

vi.mock('react', async importOriginal => ({
    ...await importOriginal<typeof import('react')>(),
    useRef: (initial: unknown) => {
        const i = harness.next();
        return (harness.slots[i] ??= { current: initial });
    },
    useState: (initial: unknown) => {
        const i = harness.next();
        if (!(i in harness.slots)) harness.slots[i] = initial;
        return [harness.slots[i], (value: unknown) => { harness.slots[i] = value; harness.markDirty(); }];
    },
    useEffect: (effect: () => void | (() => void), dependencies?: unknown[]) => {
        const i = harness.next();
        const previous = harness.slots[i] as unknown[] | undefined;
        if (!previous || !dependencies || harness.changed(previous, dependencies)) {
            harness.cleanups[i]?.();
            harness.slots[i] = dependencies;
            harness.cleanups[i] = effect() || undefined;
        }
    },
}));

const invitationId = Guid.parse('f61541c5-0ee8-429a-b29a-a37e8434d1b9');
const registrationStatus = { hasData: true, isPerforming: false, data: { status: 0 } };
const render = (submitted: boolean) => {
    let result!: ReturnType<typeof useRegistrationStatusPolling>;
    for (let pass = 0; pass < 15; pass++) {
        harness.begin();
        result = useRegistrationStatusPolling({
            invitationId, isRegistration: true, submitted, recoveringRegistration: true, registrationStatus,
            refreshRegistration: async () => {}, isAccepted: false, recoveryPhase: 'waiting'
        });
        if (!harness.needsRender()) return result;
    }
    throw new Error('The hook did not settle');
};

describe('when submitting during the polling window of a recovered registration', () => {
    afterEach(() => { harness.reset(); vi.useRealTimers(); });

    it('should restart the window so it has not expired 25 seconds after the start', async () => {
        vi.useFakeTimers();
        render(false);
        await vi.advanceTimersByTimeAsync(10000);
        render(true);
        await vi.advanceTimersByTimeAsync(15000);
        render(true).pollWindowExpired.should.be.false;
    });

    it('should expire the window 20 seconds after the submission', async () => {
        vi.useFakeTimers();
        render(false);
        await vi.advanceTimersByTimeAsync(10000);
        render(true);
        await vi.advanceTimersByTimeAsync(20000);
        render(true).pollWindowExpired.should.be.true;
    });
});
