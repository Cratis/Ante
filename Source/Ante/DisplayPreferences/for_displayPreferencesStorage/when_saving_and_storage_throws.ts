// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, vi } from 'vitest';
import { DEFAULT_DISPLAY_PREFERENCES } from '../DisplayPreferences';
import { saveDisplayPreferences } from '../displayPreferencesStorage';
import { ThrowingStorage } from './FakeLocalStorage';

describe('when saving display preferences and storage throws', () => {
    beforeEach(() => vi.stubGlobal('localStorage', new ThrowingStorage()));
    afterEach(() => vi.unstubAllGlobals());

    it('should not throw - the preference still applies for this render, it just cannot persist', () =>
        (() => saveDisplayPreferences(DEFAULT_DISPLAY_PREFERENCES)).should.not.throw());
});
