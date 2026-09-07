// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { formatNumber } from '../formatNumber';

describe('when formatting a small number for the default locale', () => {
    it('should render its plain digits', () => formatNumber(2, 'en').should.equal('2'));
});
