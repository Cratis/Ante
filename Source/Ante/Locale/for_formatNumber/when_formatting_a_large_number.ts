// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { formatNumber } from '../formatNumber';

// Onboarding never realistically shows a four-digit step count, but this is what proves the
// implementation genuinely goes through Intl.NumberFormat rather than a naive String(value) that
// would happen to look identical for every step count the lobby actually displays.
describe('when formatting a number large enough to be grouped', () => {
    it('should apply the locale\'s digit grouping', () => formatNumber(12345, 'en').should.equal('12,345'));
});
