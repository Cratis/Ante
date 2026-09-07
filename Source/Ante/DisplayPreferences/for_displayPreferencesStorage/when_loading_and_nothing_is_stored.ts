// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, vi } from 'vitest';
import { DEFAULT_DISPLAY_PREFERENCES } from '../DisplayPreferences';
import { loadDisplayPreferences } from '../displayPreferencesStorage';
import { FakeLocalStorage } from './FakeLocalStorage';

describe('when loading display preferences and nothing is stored', () => {
    beforeEach(() => vi.stubGlobal('localStorage', new FakeLocalStorage()));
    afterEach(() => vi.unstubAllGlobals());

    it('should return the defaults', () => loadDisplayPreferences().should.deep.equal(DEFAULT_DISPLAY_PREFERENCES));
});
