// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import sinon from 'sinon';
import { Guid } from '@cratis/fundamentals';
import { startRegistration } from '../../startRegistration';

describe('when preparing the wizard and the start succeeds', () => {
    const id = Guid.create();
    const execute = sinon.stub().resolves({ isSuccess: true });
    const command = { registrationId: Guid.empty, execute };
    let succeeded: boolean;

    beforeEach(async () => {
        execute.resetHistory();
        succeeded = await startRegistration(id, () => command);
    });

    it('should record the current operation id before executing', () => command.registrationId.toString().should.equal(id.toString()));
    it('should execute once', () => execute.calledOnce.should.be.true);
    it('should unlock the wizard', () => succeeded.should.be.true);
});
