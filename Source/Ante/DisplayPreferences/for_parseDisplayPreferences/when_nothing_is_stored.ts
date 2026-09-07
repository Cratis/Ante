// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DEFAULT_DISPLAY_PREFERENCES } from '../DisplayPreferences';
import { parseDisplayPreferences } from '../displayPreferencesSchema';

describe('when parsing display preferences and nothing is stored', () => {
    it('should return the defaults for null', () => parseDisplayPreferences(null).should.deep.equal(DEFAULT_DISPLAY_PREFERENCES));
    it('should return the defaults for undefined', () => parseDisplayPreferences(undefined).should.deep.equal(DEFAULT_DISPLAY_PREFERENCES));
    it('should return the defaults for an empty string', () => parseDisplayPreferences('').should.deep.equal(DEFAULT_DISPLAY_PREFERENCES));
});
