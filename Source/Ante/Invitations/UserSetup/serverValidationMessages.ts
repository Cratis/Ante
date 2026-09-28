// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ValidationResult } from '@cratis/arc/validation';

/** Keep server-only field errors visible even when their field belongs to a hidden wizard step. */
export const serverValidationMessages = (results: ValidationResult[]): string[] =>
    results.map(result => result.message).filter(message => !!message);
