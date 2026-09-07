// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { toDisplayAttributes } from '../displayPreferencesAttributes';

describe('when converting effective display preferences to attributes and every preference is default', () => {
    const attributes = toDisplayAttributes({
        textSize: 'default',
        contrast: 'default',
        spacing: 'default',
        controlSize: 'default',
        reducedMotion: false
    });

    it('should produce no attributes to set - the default state carries none', () =>
        Object.values(attributes).every(value => value === null).should.be.true);
});
