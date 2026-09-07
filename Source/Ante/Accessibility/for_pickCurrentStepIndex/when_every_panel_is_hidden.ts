// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { pickCurrentStepIndex } from '../stepperStepIndex';

describe('when picking the current step index and every panel is hidden', () => {
    it('should fall back to the first step rather than an invalid -1 index', () =>
        pickCurrentStepIndex([true, true, true]).should.equal(0));
});
