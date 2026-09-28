// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import sinon from 'sinon';
import { Guid } from '@cratis/fundamentals';
import { startRegistration } from '../../startRegistration';

describe('when preparing the wizard and the start is rejected', () => {
    it('should not unlock the wizard after a command rejection', async () => {
        const command = { registrationId: Guid.empty, execute: sinon.stub().resolves({ isSuccess: false, validationResults: [] }) };
        (await startRegistration(Guid.create(), () => command)).should.equal('failed');
    });

    it('should not unlock the wizard after a transport error', async () => {
        const command = { registrationId: Guid.empty, execute: sinon.stub().rejects(new Error('offline')) };
        (await startRegistration(Guid.create(), () => command)).should.equal('failed');
    });
});
