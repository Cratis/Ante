// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DISPLAY_PREFERENCES_SCHEMA_VERSION } from '../DisplayPreferences';
import { resetVisualDisplayPreferences } from '../resetDisplayPreferences';

describe('when resetting visual display preferences and motion was set to reduced', () => {
    const reset = resetVisualDisplayPreferences({
        schemaVersion: DISPLAY_PREFERENCES_SCHEMA_VERSION,
        textSize: 'largest',
        contrast: 'high',
        spacing: 'relaxed',
        controlSize: 'large',
        motion: 'reduced'
    });

    it('should preserve the explicit reduced-motion choice', () => reset.motion.should.equal('reduced'));
    it('should reset every visual preference to default', () => {
        reset.textSize.should.equal('default');
        reset.contrast.should.equal('default');
        reset.spacing.should.equal('default');
        reset.controlSize.should.equal('default');
    });
});
