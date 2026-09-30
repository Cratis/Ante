// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { hasReadStatus } from '../hasReadStatus';

describe('when checking whether an onboarding status has been read', () => {
    it('should not count the default value Arc seeds the query with', () => hasReadStatus({ hasData: true, data: {} }).should.be.false);
    it('should not count a result without data', () => hasReadStatus({ hasData: false, data: undefined }).should.be.false);
    it('should count a pending status', () => hasReadStatus({ hasData: true, data: { status: 0 } }).should.be.true);
    it('should count an accepted status', () => hasReadStatus({ hasData: true, data: { status: 2 } }).should.be.true);
});
