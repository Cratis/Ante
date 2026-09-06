// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, vi } from 'vitest';
import { getInvitationToken } from '../invitationToken';

const stubLocation = (pathname: string, search: string) => {
    vi.stubGlobal('window', { location: { pathname, search } });
};

describe('when reading the current url', () => {
    afterEach(() => {
        vi.unstubAllGlobals();
    });

    it('should read the token from the /invite/ path segment', () => {
        stubLocation('/invite/abc123', '');
        getInvitationToken()!.should.equal('abc123');
    });

    it('should read the token from the query string when there is no path segment', () => {
        stubLocation('/', '?token=xyz789');
        getInvitationToken()!.should.equal('xyz789');
    });

    it('should prefer the path segment over the query string', () => {
        stubLocation('/invite/from-path', '?token=from-query');
        getInvitationToken()!.should.equal('from-path');
    });

    it('should decode a url-encoded token', () => {
        stubLocation('/', `?token=${encodeURIComponent('a.b.c+d')}`);
        getInvitationToken()!.should.equal('a.b.c+d');
    });

    it('should return null when neither is present', () => {
        stubLocation('/', '');
        (getInvitationToken() === null).should.be.true;
    });
});
