// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { applyDisplayAttributes } from '../applyDisplayPreferencesToDocument';
import { FakeAttributeTarget } from './FakeAttributeTarget';

describe('when applying display attributes and a value is provided', () => {
    let target: FakeAttributeTarget;

    beforeEach(() => {
        target = new FakeAttributeTarget();
        applyDisplayAttributes(target, { 'data-display-text-size': 'large' });
    });

    it('should set the attribute to that value', () => target.get('data-display-text-size')!.should.equal('large'));
});
