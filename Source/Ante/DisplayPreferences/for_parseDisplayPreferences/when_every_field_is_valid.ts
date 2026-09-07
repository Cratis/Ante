// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DISPLAY_PREFERENCES_SCHEMA_VERSION, DisplayPreferences } from '../DisplayPreferences';
import { parseDisplayPreferences, serializeDisplayPreferences } from '../displayPreferencesSchema';

describe('when parsing display preferences and every field is valid', () => {
    const stored: DisplayPreferences = {
        schemaVersion: DISPLAY_PREFERENCES_SCHEMA_VERSION,
        textSize: 'largest',
        contrast: 'high',
        spacing: 'relaxed',
        controlSize: 'large',
        motion: 'reduced'
    };

    it('should round-trip through serialize/parse unchanged', () =>
        parseDisplayPreferences(serializeDisplayPreferences(stored)).should.deep.equal(stored));
});
