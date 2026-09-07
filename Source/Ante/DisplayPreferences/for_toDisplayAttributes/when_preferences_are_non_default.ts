// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DISPLAY_PREFERENCE_ATTRIBUTES, toDisplayAttributes } from '../displayPreferencesAttributes';

describe('when converting effective display preferences to attributes and preferences are non-default', () => {
    const attributes = toDisplayAttributes({
        textSize: 'large',
        contrast: 'high',
        spacing: 'relaxed',
        controlSize: 'large',
        reducedMotion: true
    });

    it('should carry each non-default value under its attribute name', () => {
        attributes[DISPLAY_PREFERENCE_ATTRIBUTES.textSize]!.should.equal('large');
        attributes[DISPLAY_PREFERENCE_ATTRIBUTES.contrast]!.should.equal('high');
        attributes[DISPLAY_PREFERENCE_ATTRIBUTES.spacing]!.should.equal('relaxed');
        attributes[DISPLAY_PREFERENCE_ATTRIBUTES.controlSize]!.should.equal('large');
    });

    it('should represent reduced motion as the literal "reduced"', () =>
        attributes[DISPLAY_PREFERENCE_ATTRIBUTES.motion]!.should.equal('reduced'));
});
