// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { resolveLocale } from '../resolveLocale';

describe('when the requested locale carries a region subtag', () => {
    it('should resolve by the primary language subtag alone', () => resolveLocale('en-GB').should.equal('en'));

    it('should also match an underscore-separated tag', () => resolveLocale('en_GB').should.equal('en'));
});
