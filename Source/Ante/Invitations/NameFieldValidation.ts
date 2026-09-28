// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import strings from 'Strings';

export type NameFields = {
    firstName: string;
    middleName?: string;
    lastName: string;
    organizationName?: string;
};

type NameField = keyof NameFields;

/** Keep required/length feedback in the command form; the server still owns the full name rules. */
export const validateNameField = (field: string, value: unknown): string | undefined => {
    if (!['firstName', 'middleName', 'lastName', 'organizationName'].includes(field)) return undefined;
    const nameField = field as NameField;
    const text = typeof value === 'string' ? value : '';
    const label = nameField === 'organizationName' ? strings.organizationSetup.organizationName : strings.organizationSetup[nameField];
    if (nameField !== 'middleName' && !text.trim()) return strings.nameValidation.required.replace('{field}', label);
    const maximum = nameField === 'organizationName' ? 50 : 100;
    if (text.length > maximum) return strings.nameValidation.length.replace('{field}', label).replace('{maximum}', String(maximum));
    return undefined;
};

/** Arc's onFieldValidate signature; a corrected field clears its previous form error. */
export const validateChangedName = (_command: NameFields, field: string, _oldValue: unknown, value: unknown): string | undefined =>
    validateNameField(field, value);
