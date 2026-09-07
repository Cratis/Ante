// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DEFAULT_DISPLAY_PREFERENCES } from '../DisplayPreferences';
import { computeEffectiveDisplayPreferences } from '../effectiveDisplayPreferences';

describe('when computing effective display preferences and motion is reduced explicitly', () => {
    const effective = computeEffectiveDisplayPreferences({ ...DEFAULT_DISPLAY_PREFERENCES, motion: 'reduced' }, false);

    it('should reduce motion even though the operating system does not request it', () => effective.reducedMotion.should.be.true);
});
