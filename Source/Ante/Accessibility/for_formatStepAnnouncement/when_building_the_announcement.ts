// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { formatStepAnnouncement } from '../stepAnnouncement';

describe('when building a step-transition announcement', () => {
    it('should substitute the current step, total, and label into the template', () =>
        formatStepAnnouncement('Step {current} of {total}: {label}', 2, 3, 'Your Information', 'en')
            .should.equal('Step 2 of 3: Your Information'));

    it('should tolerate a template that reorders the placeholders', () =>
        formatStepAnnouncement('{label} ({current}/{total})', 1, 2, 'Organization', 'en')
            .should.equal('Organization (1/2)'));
});
