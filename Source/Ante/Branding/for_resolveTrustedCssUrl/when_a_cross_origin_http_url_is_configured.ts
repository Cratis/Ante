// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { resolveTrustedCssUrl } from '../resolveTrustedCssUrl';

const DOCUMENT_URL = 'https://lobby.example.com/';

describe('when a cross-origin plain-http url is configured', () => {
    it('should refuse it rather than load a third party unencrypted', () =>
        (resolveTrustedCssUrl('http://cdn.example.net/theme.css', DOCUMENT_URL) === null).should.be.true);
});
