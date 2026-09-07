// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { resolveLocale } from '../resolveLocale';

describe('when the requested locale is exactly a supported one', () => {
    it('should resolve to that locale', () => resolveLocale('en').should.equal('en'));
});
