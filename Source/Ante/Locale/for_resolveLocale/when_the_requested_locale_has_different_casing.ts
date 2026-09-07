// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { resolveLocale } from '../resolveLocale';

describe('when the requested locale is cased differently than the supported entry', () => {
    it('should resolve case-insensitively', () => resolveLocale('EN-US').should.equal('en'));
});
