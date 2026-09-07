// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, vi } from 'vitest';
import { prefersReducedMotion } from '../systemPreferences';

describe('when reading the reduced-motion system preference and matchMedia is unavailable', () => {
    beforeEach(() => vi.stubGlobal('matchMedia', undefined));
    afterEach(() => vi.unstubAllGlobals());

    it('should report no preference rather than throwing', () => prefersReducedMotion().should.be.false);
});
