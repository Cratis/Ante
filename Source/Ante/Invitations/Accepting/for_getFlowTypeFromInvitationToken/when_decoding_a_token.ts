// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { InvitationFlowType } from '../../../Contracts/Invitations/InvitationFlowType';
import { getFlowTypeFromInvitationToken } from '../invitationToken';

const encodeBase64Url = (value: string): string =>
    btoa(value).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');

const tokenWithPayload = (payload: unknown): string =>
    `header.${encodeBase64Url(JSON.stringify(payload))}.signature`;

describe('when decoding a token', () => {
    it('should return null when there is no token', () => {
        (getFlowTypeFromInvitationToken(null) === null).should.be.true;
        (getFlowTypeFromInvitationToken(undefined) === null).should.be.true;
    });

    it('should return null when the token has no payload segment', () => {
        (getFlowTypeFromInvitationToken('not-a-jwt') === null).should.be.true;
    });

    it('should return null when the payload is not valid JSON', () => {
        const malformed = `header.${encodeBase64Url('not json')}.signature`;
        (getFlowTypeFromInvitationToken(malformed) === null).should.be.true;
    });

    it('should extract the createTenant flow type from the invite_type claim', () => {
        const token = tokenWithPayload({ invite_type: 'createTenant' });
        getFlowTypeFromInvitationToken(token)!.should.equal(InvitationFlowType.createTenant);
    });

    it('should extract the joinTenant flow type from a numeric invite_type claim', () => {
        const token = tokenWithPayload({ invite_type: InvitationFlowType.joinTenant });
        getFlowTypeFromInvitationToken(token)!.should.equal(InvitationFlowType.joinTenant);
    });

    it('should return null when the claim names no recognized flow type', () => {
        const token = tokenWithPayload({ invite_type: 'somethingElse' });
        (getFlowTypeFromInvitationToken(token) === null).should.be.true;
    });
});
