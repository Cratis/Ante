// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, vi } from 'vitest';
import { prefersReducedMotion } from '../systemPreferences';

describe('when reading the reduced-motion system preference and the operating system reports a preference', () => {
    afterEach(() => vi.unstubAllGlobals());

    it('should reflect a match', () => {
        vi.stubGlobal('matchMedia', () => ({ matches: true }));
        prefersReducedMotion().should.be.true;
    });

    it('should reflect no match', () => {
        vi.stubGlobal('matchMedia', () => ({ matches: false }));
        prefersReducedMotion().should.be.false;
    });
});
