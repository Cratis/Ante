// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { shouldShowBrandLogoImage } from '../shouldShowBrandLogoImage';

describe('when no logo is configured', () => {
    it('should not show the image', () => shouldShowBrandLogoImage(undefined, false).should.be.false);

    it('should not show the image for an empty configured value', () => shouldShowBrandLogoImage('', false).should.be.false);
});
