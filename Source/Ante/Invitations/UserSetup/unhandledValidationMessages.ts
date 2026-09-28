// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ValidationResult } from '@cratis/arc/validation';

const editableFields = ['firstname', 'middlename', 'lastname', 'acceptedlegalterms'];

/** Field errors belong in the still-editable form; only other failures need a page-level message. */
export const unhandledValidationMessages = (results: ValidationResult[]): string[] => results.filter(result =>
    !result.members?.some(member => editableFields.includes(member.toLowerCase()))
).map(result => result.message).filter(message => !!message);
