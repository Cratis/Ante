// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { buildStepperAriaIds } from '../stepperAria';

describe('when building stepper ARIA ids for an index', () => {
    const ids = buildStepperAriaIds('registration', 3);

    it('should scope both ids under the given prefix', () => {
        ids.headerId.should.equal('registration-stepper-header-3');
        ids.panelId.should.equal('registration-stepper-panel-3');
    });

    it('should never produce equal header and panel ids', () => ids.headerId.should.not.equal(ids.panelId));
});
