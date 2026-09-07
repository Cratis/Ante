// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DEFAULT_DISPLAY_PREFERENCES } from '../DisplayPreferences';
import { parseDisplayPreferences } from '../displayPreferencesSchema';

describe('when parsing display preferences and the stored JSON is corrupt', () => {
    it('should return the defaults rather than throwing', () =>
        parseDisplayPreferences('{not json').should.deep.equal(DEFAULT_DISPLAY_PREFERENCES));
});
