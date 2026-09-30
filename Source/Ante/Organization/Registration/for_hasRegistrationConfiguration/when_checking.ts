// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryResultWithState } from '@cratis/arc/queries';
import { Registration, RegistrationConfiguration } from '../../../Configuration/Configuration';
import { hasRegistrationConfiguration } from '../hasRegistrationConfiguration';

describe('when checking whether the registration configuration has arrived', () => {
    it('should not count the result Arc starts the query with', () =>
        hasRegistrationConfiguration(QueryResultWithState.initial<RegistrationConfiguration>(new Registration().defaultValue)).should.be.false);
    it('should count an open registration', () => hasRegistrationConfiguration({ hasData: true, data: { isEnabled: true } }).should.be.true);
    it('should count a closed registration', () => hasRegistrationConfiguration({ hasData: true, data: { isEnabled: false } }).should.be.true);
});
