// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { applyStepperHeaderAria, buildStepperAriaIds } from '../stepperAria';
import { FakeAriaElement } from './FakeAriaElement';

describe('when applying ARIA to an unselected stepper header', () => {
    let header: FakeAriaElement;

    beforeEach(() => {
        header = new FakeAriaElement();
        applyStepperHeaderAria(header, buildStepperAriaIds('organization-setup', 1), false);
    });

    it('should mark it as not selected', () => header.get('aria-selected')!.should.equal('false'));
});
