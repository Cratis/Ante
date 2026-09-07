// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { resolveTrustedCssUrl } from '../resolveTrustedCssUrl';

const DOCUMENT_URL = 'https://lobby.example.com/';

describe('when nothing is configured', () => {
    it('should resolve undefined to null', () => (resolveTrustedCssUrl(undefined, DOCUMENT_URL) === null).should.be.true);

    it('should resolve an empty string to null', () => (resolveTrustedCssUrl('', DOCUMENT_URL) === null).should.be.true);

    it('should resolve whitespace only to null', () => (resolveTrustedCssUrl('   ', DOCUMENT_URL) === null).should.be.true);
});
