// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IdentityProvider, IIdentity } from '@cratis/arc/identity';
import sinon from 'sinon';
import { afterEach, beforeEach, describe, it, vi } from 'vitest';

for (const cookieIdentity of ['admin', 'previous-visitor']) {
    for (const status of [200, 401, 403]) {
        describe(`when a ${cookieIdentity} legacy cookie disagrees with the identity endpoint returning ${status}`, () => {
            let result: IIdentity;
            let fetch: sinon.SinonStub;

            beforeEach(async () => {
                IdentityProvider.clearCache();
                const cookie = btoa(JSON.stringify({ id: cookieIdentity, name: cookieIdentity, roles: ['admin'], details: { invitationId: 'forged-invitation', flowType: 1 } }));
                vi.stubGlobal('document', { cookie: `.cratis-identity=${cookie}`, location: { origin: 'https://ante.test' } });
                fetch = sinon.stub().resolves({
                    status,
                    ok: status === 200,
                    json: async () => ({ id: 'visitor', name: 'Visitor', roles: ['authenticated'], details: {} })
                });
                vi.stubGlobal('fetch', fetch);
                result = await IdentityProvider.getCurrent();
            });

            afterEach(() => {
                IdentityProvider.clearCache();
                vi.unstubAllGlobals();
            });

            it('should request the server identity instead of reading the cookie first', () => fetch.calledOnceWithExactly(sinon.match((url: unknown) => url instanceof URL && url.href === 'https://ante.test/.cratis/me'), { method: 'GET', headers: {} }).should.be.true);
            it('should use only the identity authenticated by the server', () => result.id.should.equal(status === 200 ? 'visitor' : ''));
            it('should remain anonymous when the server rejects the request', () => result.isSet.should.equal(status === 200));
            it('should not accept cookie roles', () => result.isInRole('admin').should.be.false);
            it('should not accept cookie invitation details', () => result.details.should.deep.equal({}));
        });
    }
}
