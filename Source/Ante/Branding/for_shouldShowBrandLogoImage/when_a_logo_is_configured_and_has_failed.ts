// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { shouldShowBrandLogoImage } from '../shouldShowBrandLogoImage';

describe('when a logo is configured but has already failed to load', () => {
    it('should fall back to the neutral text wordmark instead of the image', () =>
        shouldShowBrandLogoImage('https://example.com/logo.svg', true).should.be.false);
});
