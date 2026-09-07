// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DEFAULT_DISPLAY_PREFERENCES } from '../DisplayPreferences';
import { computeEffectiveDisplayPreferences } from '../effectiveDisplayPreferences';

describe('when computing effective display preferences and neither the person nor the operating system requests reduced motion', () => {
    const effective = computeEffectiveDisplayPreferences({ ...DEFAULT_DISPLAY_PREFERENCES, motion: 'system' }, false);

    it('should not reduce motion', () => effective.reducedMotion.should.be.false);
    it('should carry the other preferences through unchanged', () => {
        effective.textSize.should.equal(DEFAULT_DISPLAY_PREFERENCES.textSize);
        effective.contrast.should.equal(DEFAULT_DISPLAY_PREFERENCES.contrast);
        effective.spacing.should.equal(DEFAULT_DISPLAY_PREFERENCES.spacing);
        effective.controlSize.should.equal(DEFAULT_DISPLAY_PREFERENCES.controlSize);
    });
});
