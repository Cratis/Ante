// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { resolveTrustedCssUrl } from '../resolveTrustedCssUrl';

describe('when the configured value cannot be parsed as a url at all', () => {
    it('should resolve to null rather than throwing', () =>
        (resolveTrustedCssUrl('http://[::1', 'https://lobby.example.com/') === null).should.be.true);
});
