// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it, vi } from 'vitest';
import { useInvitationContent } from '../useInvitationContent';

// The CI runner shares its module cache across files; reload the hook after installing this file's mocks.
vi.hoisted(() => { vi.resetModules(); });

const harness = vi.hoisted(() => ({
    locale: 'en-US',
    reference: undefined as { current: unknown } | undefined,
    refresh: vi.fn(),
    content: {
        userSetup: { title: 'User copy', intro: '', completionMessage: '', supportUrl: '' },
        organizationSetup: { title: 'Organization copy', intro: '', completionMessage: '', supportUrl: '' }
    }
}));

// Drive just the hook's persistent reference and locale-change effect without a DOM renderer.
vi.mock('react', async importOriginal => ({
    ...await importOriginal<typeof import('react')>(),
    useRef: (initial: unknown) => (harness.reference ??= { current: initial }),
    useEffect: (effect: () => void) => effect()
}));
vi.mock('../../Locale/LocaleContext', () => ({ useLocale: () => harness.locale }));
vi.mock('../../Configuration/Configuration', () => ({
    GetInvitationContent: { use: () => [{ isSuccess: true, data: harness.content }, harness.refresh] }
}));

beforeEach(() => {
    harness.reference = undefined;
    harness.locale = 'en-US';
    harness.refresh.mockClear();
});

describe('when invitation content is first read', () => {
    let result: ReturnType<typeof useInvitationContent>;
    beforeEach(() => { result = useInvitationContent('userSetup'); });
    it('should use only the requested journeys copy', () => result!.title.should.equal('User copy'));
    it('should not duplicate the initial query', () => harness.refresh.mock.calls.should.have.lengthOf(0));
});

describe('when the visitor changes language', () => {
    let result: ReturnType<typeof useInvitationContent>;
    beforeEach(() => {
        useInvitationContent('organizationSetup');
        harness.locale = 'nb-NO';
        result = useInvitationContent('organizationSetup');
        useInvitationContent('organizationSetup');
    });
    it('should refresh the server-resolved invitation copy once', () => harness.refresh.mock.calls.should.have.lengthOf(1));
    it('should continue reading only the requested journey', () => result!.title.should.equal('Organization copy'));
});
