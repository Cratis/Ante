// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DEFAULT_DISPLAY_PREFERENCES } from '../DisplayPreferences';
import { computeEffectiveDisplayPreferences } from '../effectiveDisplayPreferences';

describe('when computing effective display preferences and the operating system prefers reduced motion', () => {
    const effective = computeEffectiveDisplayPreferences({ ...DEFAULT_DISPLAY_PREFERENCES, motion: 'system' }, true);

    it('should reduce motion even though no explicit in-app preference was set', () => effective.reducedMotion.should.be.true);
});
