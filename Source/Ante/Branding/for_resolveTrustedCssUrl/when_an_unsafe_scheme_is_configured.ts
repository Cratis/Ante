// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { resolveTrustedCssUrl } from '../resolveTrustedCssUrl';

const DOCUMENT_URL = 'https://lobby.example.com/';

describe('when an unsafe scheme is configured', () => {
    it('should refuse a javascript: value', () => (resolveTrustedCssUrl('javascript:alert(1)', DOCUMENT_URL) === null).should.be.true);

    it('should refuse a data: value', () => (resolveTrustedCssUrl('data:text/css,body{}', DOCUMENT_URL) === null).should.be.true);

    it('should refuse a file: value', () => (resolveTrustedCssUrl('file:///etc/passwd', DOCUMENT_URL) === null).should.be.true);
});
