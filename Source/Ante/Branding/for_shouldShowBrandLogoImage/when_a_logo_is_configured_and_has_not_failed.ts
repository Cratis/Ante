// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { shouldShowBrandLogoImage } from '../shouldShowBrandLogoImage';

describe('when a logo is configured and has not failed to load', () => {
    it('should show the image', () => shouldShowBrandLogoImage('https://example.com/logo.svg', false).should.be.true);
});
