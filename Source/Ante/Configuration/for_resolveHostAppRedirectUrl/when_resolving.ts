// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, vi } from 'vitest';
import { resolveHostAppRedirectUrl } from '../hostAppRedirect';

const stubLocation = (origin: string, port: string) => {
    vi.stubGlobal('window', { location: { origin, port } });
};

describe('when resolving the host app redirect url', () => {
    afterEach(() => {
        vi.unstubAllGlobals();
    });

    it('should substitute the tenant placeholder with the organization name', () => {
        stubLocation('https://ante.example.com', '');
        resolveHostAppRedirectUrl('https://{tenant}.example.com/', 'acme').should.equal('https://acme.example.com/');
    });

    it('should carry the current port over to a localhost subdomain', () => {
        stubLocation('http://ante.localhost:9002', '9002');
        resolveHostAppRedirectUrl('http://{tenant}.localhost/', 'acme').should.equal('http://acme.localhost:9002/');
    });

    it('should not add a port for a non-localhost host', () => {
        stubLocation('https://ante.example.com', '9002');
        resolveHostAppRedirectUrl('https://{tenant}.example.com/', 'acme').should.equal('https://acme.example.com/');
    });

    it('should append the sign-in path when one is given', () => {
        stubLocation('https://ante.example.com', '');
        resolveHostAppRedirectUrl('https://{tenant}.example.com/', 'acme', '/.cratis/login/entra-id?returnUrl=%2F')
            .should.equal('https://acme.example.com/.cratis/login/entra-id?returnUrl=%2F');
    });

    it('should ignore a root sign-in path', () => {
        stubLocation('https://ante.example.com', '');
        resolveHostAppRedirectUrl('https://{tenant}.example.com/', 'acme', '/').should.equal('https://acme.example.com/');
    });

    it('should throw rather than silently produce an unusable url for a malformed host configuration', () => {
        stubLocation('https://ante.example.com', '');
        // A misconfigured HostAppUrl (an incomplete scheme, stray whitespace inside the authority, ...)
        // must surface as a thrown error the caller can turn into a recoverable "destination failure"
        // message - never as window.location.href silently being set to something that does not
        // navigate anywhere useful.
        (() => resolveHostAppRedirectUrl('http://{tenant} example.com/', 'acme')).should.throw();
    });
});
