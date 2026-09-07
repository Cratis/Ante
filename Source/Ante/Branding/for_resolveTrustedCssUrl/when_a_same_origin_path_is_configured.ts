// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { resolveTrustedCssUrl } from '../resolveTrustedCssUrl';

const DOCUMENT_URL = 'https://lobby.example.com/';

describe('when a same-origin root-relative path is configured', () => {
    it('should resolve to the absolute same-origin URL', () =>
        resolveTrustedCssUrl('/custom/theme.css', DOCUMENT_URL)!.should.equal('https://lobby.example.com/custom/theme.css'));
});

// A file mounted directly into Ante's own wwwroot (Documentation/configuration.md: "Overridable by
// mounting a file into the container") is served over whatever scheme the document itself uses - even
// plain http: in a local/dev deployment - because it carries exactly the same trust as the app itself.
describe('when a same-origin path is configured on a plain-http deployment', () => {
    it('should still resolve, since same-origin is as trusted as the document', () =>
        resolveTrustedCssUrl('/custom/theme.css', 'http://localhost:5173/')!.should.equal('http://localhost:5173/custom/theme.css'));
});
