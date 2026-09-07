// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { formatStepAnnouncement } from '../stepAnnouncement';

// No real wizard has a thousand steps, but this is what proves current/total are genuinely formatted
// through Intl.NumberFormat (Cratis/Ante#21's locale-aware number formatting) rather than a plain
// String(...) that would happen to look identical for every step count the lobby actually shows.
describe('when the step numbers are large enough to be grouped', () => {
    it('should format them using the locale\'s own digit grouping', () =>
        formatStepAnnouncement('Step {current} of {total}: {label}', 1234, 5678, 'Details', 'en')
            .should.equal('Step 1,234 of 5,678: Details'));
});
