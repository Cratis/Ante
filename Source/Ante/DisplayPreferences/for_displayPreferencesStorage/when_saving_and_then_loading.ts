// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, vi } from 'vitest';
import { DEFAULT_DISPLAY_PREFERENCES } from '../DisplayPreferences';
import { loadDisplayPreferences, saveDisplayPreferences } from '../displayPreferencesStorage';
import { FakeLocalStorage } from './FakeLocalStorage';

describe('when saving display preferences and then loading them back', () => {
    const preferences = { ...DEFAULT_DISPLAY_PREFERENCES, textSize: 'large' as const, motion: 'reduced' as const };

    beforeEach(() => vi.stubGlobal('localStorage', new FakeLocalStorage()));
    afterEach(() => vi.unstubAllGlobals());

    it('should round-trip the saved preferences', () => {
        saveDisplayPreferences(preferences);
        loadDisplayPreferences().should.deep.equal(preferences);
    });
});
