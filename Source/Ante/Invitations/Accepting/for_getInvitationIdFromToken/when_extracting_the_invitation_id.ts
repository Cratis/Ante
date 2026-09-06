// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Guid } from '@cratis/fundamentals';
import { getInvitationIdFromToken } from '../invitationToken';

const encodeBase64Url = (value: string): string =>
    btoa(value).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');

const tokenWithPayload = (payload: unknown): string =>
    `header.${encodeBase64Url(JSON.stringify(payload))}.signature`;

describe('when extracting the invitation id', () => {
    it('should return null when there is no token', () => {
        (getInvitationIdFromToken(null) === null).should.be.true;
    });

    it('should return null when the jti claim is missing', () => {
        const token = tokenWithPayload({});
        (getInvitationIdFromToken(token) === null).should.be.true;
    });

    it('should return null when the jti claim is not a valid guid', () => {
        const token = tokenWithPayload({ jti: 'not-a-guid' });
        (getInvitationIdFromToken(token) === null).should.be.true;
    });

    it('should parse a valid guid from the jti claim', () => {
        const id = Guid.create();
        const token = tokenWithPayload({ jti: id.toString() });
        getInvitationIdFromToken(token)!.toString().should.equal(id.toString());
    });
});
