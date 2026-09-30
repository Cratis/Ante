// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryResultWithState } from '@cratis/arc/queries';
import { Guid } from '@cratis/fundamentals';
import { ForAttempt, HostOutcomeView } from '../HostOutcome/HostOutcome';
import { hasReadHostOutcome } from '../hasReadHostOutcome';

const attempt = Guid.parse('3f1d7c2e-5b0a-4d8e-9f6a-1c2b3d4e5f60');
const settled = (attemptId: Guid) => ({ hasData: true, isSuccess: true, isPerforming: false, data: { attemptId, isConfigured: true } });

describe('when checking whether a host outcome has been read', () => {
    it('should not count the result Arc starts the query with', () =>
        hasReadHostOutcome(QueryResultWithState.initial<HostOutcomeView>(new ForAttempt().defaultValue), attempt).should.be.false);
    it('should not count the previous lookup made before the attempt was accepted', () => hasReadHostOutcome(settled(Guid.empty), attempt).should.be.false);
    it('should count the lookup for the attempt', () => hasReadHostOutcome(settled(attempt), attempt).should.be.true);
    it('should count a failed lookup, which reads as not configured', () =>
        hasReadHostOutcome({ hasData: true, isSuccess: false, isPerforming: false, data: {} }, attempt).should.be.true);
});
