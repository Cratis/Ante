// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { resolveHostOutcomeGate } from '../HostOutcomeGate';

describe('when resolving the host outcome gate', () => {
    it('should keep waiting when onboarding has not been accepted yet, regardless of configuration', () => {
        resolveHostOutcomeGate(false, true).should.equal('keepWaiting');
        resolveHostOutcomeGate(false, false).should.equal('keepWaiting');
    });

    it('should redirect automatically once accepted when no host outcome adapter is configured', () =>
        resolveHostOutcomeGate(true, false).should.equal('redirectAutomatically'));

    it('should show the host outcome once accepted when the adapter is configured', () =>
        resolveHostOutcomeGate(true, true).should.equal('showHostOutcome'));
});
