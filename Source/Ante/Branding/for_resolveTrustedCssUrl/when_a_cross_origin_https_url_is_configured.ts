// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { resolveTrustedCssUrl } from '../resolveTrustedCssUrl';

const DOCUMENT_URL = 'https://lobby.example.com/';

describe('when a cross-origin https url is configured', () => {
    it('should resolve to that url', () =>
        resolveTrustedCssUrl('https://cdn.example.net/theme.css', DOCUMENT_URL)!.should.equal('https://cdn.example.net/theme.css'));
});
