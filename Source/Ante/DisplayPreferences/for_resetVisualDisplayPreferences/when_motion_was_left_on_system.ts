// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DISPLAY_PREFERENCES_SCHEMA_VERSION } from '../DisplayPreferences';
import { resetVisualDisplayPreferences } from '../resetDisplayPreferences';

describe('when resetting visual display preferences and motion was left on system', () => {
    const reset = resetVisualDisplayPreferences({
        schemaVersion: DISPLAY_PREFERENCES_SCHEMA_VERSION,
        textSize: 'large',
        contrast: 'default',
        spacing: 'default',
        controlSize: 'default',
        motion: 'system'
    });

    it('should keep motion following the operating system', () => reset.motion.should.equal('system'));
});
