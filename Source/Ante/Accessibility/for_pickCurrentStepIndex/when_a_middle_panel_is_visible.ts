// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { pickCurrentStepIndex } from '../stepperStepIndex';

describe('when picking the current step index and a middle panel is visible', () => {
    it('should return the index of the one panel that is not hidden regardless of navigation method', () =>
        pickCurrentStepIndex([true, false, true]).should.equal(1));
});
