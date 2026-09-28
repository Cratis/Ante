// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, beforeEach, describe, it } from 'vitest';
import { selectStrings } from '../../Locales/Strings';
import { validateChangedName } from '../NameFieldValidation';

const command = { firstName: '', middleName: '', lastName: '' };

describe('when a required first name is cleared and then corrected', () => {
    let error: string | undefined;

    beforeEach(() => {
        selectStrings('nb-NO');
        error = validateChangedName(command, 'firstName', 'Ada', '');
    });
    afterEach(() => selectStrings('en'));

    it('should show a localized field error that gates the name step', () => {
        (error ?? '').should.equal('Fornavn er påkrevd.');
    });
    it('should clear the field error after the visitor corrects it', () => {
        error = validateChangedName(command, 'firstName', '', 'Ada');
        (error === undefined).should.be.true;
    });
});

describe('when a required last name exceeds the limit and then is corrected', () => {
    let error: string | undefined;

    beforeEach(() => {
        selectStrings('nb-NO');
        error = validateChangedName(command, 'lastName', 'Ada', 'a'.repeat(101));
    });
    afterEach(() => selectStrings('en'));

    it('should show the localized length error', () => {
        (error ?? '').should.equal('Etternavn kan ikke være lengre enn 100 tegn.');
    });
    it('should release the field after correction', () => {
        error = validateChangedName(command, 'lastName', 'a'.repeat(101), 'Lovelace');
        (error === undefined).should.be.true;
    });
});
