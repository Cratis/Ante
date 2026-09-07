// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { applyStepperPanelAria, buildStepperAriaIds } from '../stepperAria';
import { FakeAriaElement } from '../for_applyStepperHeaderAria/FakeAriaElement';

describe('when applying ARIA to a stepper panel', () => {
    let panel: FakeAriaElement;

    beforeEach(() => {
        panel = new FakeAriaElement();
        applyStepperPanelAria(panel, buildStepperAriaIds('user-setup', 2));
    });

    it('should give it the tabpanel role', () => panel.get('role')!.should.equal('tabpanel'));
    it('should label it by the paired header id', () => panel.get('aria-labelledby')!.should.equal('user-setup-stepper-header-2'));
    it('should make it a valid focus target for step-transition focus', () => panel.get('tabindex')!.should.equal('-1'));
});
