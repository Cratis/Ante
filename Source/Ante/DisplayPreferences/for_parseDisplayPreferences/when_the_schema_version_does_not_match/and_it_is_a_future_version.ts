// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DEFAULT_DISPLAY_PREFERENCES } from '../../DisplayPreferences';
import { parseDisplayPreferences } from '../../displayPreferencesSchema';

describe('when parsing display preferences and the schema version does not match and it is a future version', () => {
    it('should fall back to the defaults entirely rather than reading fields whose meaning it does not know', () =>
        parseDisplayPreferences(JSON.stringify({ schemaVersion: 99, textSize: 'largest', someNewField: true })).should.deep.equal(DEFAULT_DISPLAY_PREFERENCES));
});
