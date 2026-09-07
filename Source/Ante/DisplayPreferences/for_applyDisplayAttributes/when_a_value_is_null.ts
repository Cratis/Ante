// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { applyDisplayAttributes } from '../applyDisplayPreferencesToDocument';
import { FakeAttributeTarget } from './FakeAttributeTarget';

describe('when applying display attributes and a value is null', () => {
    let target: FakeAttributeTarget;

    beforeEach(() => {
        target = new FakeAttributeTarget();
        target.setAttribute('data-display-text-size', 'large');
        applyDisplayAttributes(target, { 'data-display-text-size': null });
    });

    it('should remove a previously-set attribute rather than writing a literal "null"', () =>
        target.has('data-display-text-size').should.be.false);
});
