// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { applyStepperHeaderAria, buildStepperAriaIds } from '../stepperAria';
import { FakeAriaElement } from './FakeAriaElement';

describe('when applying ARIA to the selected stepper header', () => {
    let header: FakeAriaElement;

    beforeEach(() => {
        header = new FakeAriaElement();
        applyStepperHeaderAria(header, buildStepperAriaIds('organization-setup', 0), true);
    });

    it('should mark it as a selected tab', () => header.get('aria-selected')!.should.equal('true'));
    it('should give it the tab role', () => header.get('role')!.should.equal('tab'));
    it('should point aria-controls at the paired panel id', () => header.get('aria-controls')!.should.equal('organization-setup-stepper-panel-0'));
});
