// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, vi } from 'vitest';
import { DEFAULT_DISPLAY_PREFERENCES } from '../DisplayPreferences';
import { loadDisplayPreferences } from '../displayPreferencesStorage';
import { ThrowingStorage } from './FakeLocalStorage';

describe('when loading display preferences and storage throws', () => {
    beforeEach(() => vi.stubGlobal('localStorage', new ThrowingStorage()));
    afterEach(() => vi.unstubAllGlobals());

    it('should return the defaults rather than propagating the failure', () =>
        loadDisplayPreferences().should.deep.equal(DEFAULT_DISPLAY_PREFERENCES));
});
