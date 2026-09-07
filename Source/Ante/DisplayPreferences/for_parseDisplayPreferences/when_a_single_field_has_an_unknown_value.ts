// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DISPLAY_PREFERENCES_SCHEMA_VERSION } from '../DisplayPreferences';
import { parseDisplayPreferences } from '../displayPreferencesSchema';

describe('when parsing display preferences and a single field has an unknown value', () => {
    const result = parseDisplayPreferences(JSON.stringify({
        schemaVersion: DISPLAY_PREFERENCES_SCHEMA_VERSION,
        textSize: 'gigantic',
        contrast: 'high',
        spacing: 'relaxed',
        controlSize: 'large',
        motion: 'reduced'
    }));

    it('should fall back only that field to its default', () => result.textSize.should.equal('default'));
    it('should keep the other, valid fields as stored', () => {
        result.contrast.should.equal('high');
        result.spacing.should.equal('relaxed');
        result.controlSize.should.equal('large');
        result.motion.should.equal('reduced');
    });
});
