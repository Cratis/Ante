// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, describe, it, vi } from 'vitest';
import { Guid } from '@cratis/fundamentals';
import { useOrganizationSetupHandoff } from '../useOrganizationSetupHandoff';

// Drive the real hook's state/effects with a small synchronous dispatcher: the project's node test
// environment has no DOM renderer. Effects and their cleanup run on dependency changes as in React.
const harness = vi.hoisted(() => {
    const slots: unknown[] = [];
    const cleanups: Array<(() => void) | undefined> = [];
    let index = 0;
    let dirty = false;
    let status = { hasData: true, isPerforming: false, data: { status: 0, organizationName: '' } };
    const refresh = vi.fn(async () => {});
    const reset = () => { cleanups.forEach(cleanup => cleanup?.()); slots.length = 0; cleanups.length = 0; index = 0; dirty = false; refresh.mockClear(); status = { hasData: true, isPerforming: false, data: { status: 0, organizationName: '' } }; };
    const begin = () => { index = 0; dirty = false; };
    const needsRender = () => dirty;
    const next = () => index++;
    const changed = (before: unknown[], after: unknown[]) => before.length !== after.length || before.some((value, i) => !Object.is(value, after[i]));
    return { slots, cleanups, next, begin, needsRender, changed, reset, refresh, get status() { return status; }, set status(value: typeof status) { status = value; }, markDirty: () => { dirty = true; } };
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
    useReducer: (_: unknown, initial: unknown) => {
        const i = harness.next();
        if (!(i in harness.slots)) harness.slots[i] = initial;
        return [harness.slots[i], () => { harness.markDirty(); }];
    },
    useCallback: (callback: unknown) => callback,
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

vi.mock('../OrganizationSetup', () => ({
    StatusForInvitation: { when: () => ({ use: () => [{ hasData: false, data: {} }] }) },
    StatusForRegistration: { when: () => ({ use: () => [harness.status, harness.refresh] }) },
}));
vi.mock('../../../Configuration/Configuration', () => ({ HostUrl: { use: () => [{ isSuccess: false }] } }));
vi.mock('../../useHostOutcome', () => ({ useHostOutcome: () => ({ status: 0, reasonCode: '', isConfigured: false, checkAgain: () => {} }) }));
vi.mock('../../HostOutcomeGate', () => ({ resolveHostOutcomeGate: (accepted: boolean) => accepted ? 'showHostOutcome' : 'keepWaiting' }));

const invitationId = Guid.parse('f61541c5-0ee8-429a-b29a-a37e8434d1b9');
const render = () => {
    let result!: ReturnType<typeof useOrganizationSetupHandoff>;
    for (let pass = 0; pass < 15; pass++) {
        harness.begin();
        result = useOrganizationSetupHandoff({ invitationId, hostAppUnavailableMessage: 'Unavailable', isRegistration: true, recoveringRegistration: true });
        if (!harness.needsRender()) return result;
    }
    throw new Error('The hook did not settle');
};

describe('when a recovered registration is submitted after its initial polling window expires', () => {
    afterEach(() => { harness.reset(); vi.useRealTimers(); });

    it('should restart polling and observe publication without Check Again', async () => {
        vi.useFakeTimers();
        render();
        harness.status = { hasData: true, isPerforming: false, data: { status: 0, organizationName: '' } };
        render();
        await vi.advanceTimersByTimeAsync(21000);
        const expired = render();
        const oldRefreshCount = harness.refresh.mock.calls.length;
        expired.markSubmitted();
        render();
        await vi.advanceTimersByTimeAsync(1600);
        harness.refresh.mock.calls.length.should.be.greaterThan(oldRefreshCount);
        harness.status = { hasData: true, isPerforming: false, data: { status: 2, organizationName: 'Acme' } };
        render().phase.should.equal('hostOutcome');
    });
});
