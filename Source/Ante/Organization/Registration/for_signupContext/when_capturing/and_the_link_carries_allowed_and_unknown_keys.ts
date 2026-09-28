// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, vi } from 'vitest';
import { captureSignupContext } from '../../signupContext';
import { FakeSessionStorage } from '../../for_RegistrationOperation/FakeSessionStorage';

describe('when capturing signup context and the link carries allowed and unknown keys', () => {
    let captured: { key: string; value: string }[];

    beforeEach(() => {
        vi.stubGlobal('sessionStorage', new FakeSessionStorage());
        captured = captureSignupContext(['offer', 'utm_source'], '?offer=trial&utm_source=cratis.studio&plan=enterprise')
            .map(entry => ({ key: entry.key, value: entry.value }));
    });

    afterEach(() => vi.unstubAllGlobals());

    it('should keep only the allowed keys', () => captured.should.deep.equal([
        { key: 'offer', value: 'trial' },
        { key: 'utm_source', value: 'cratis.studio' },
    ]));
});
