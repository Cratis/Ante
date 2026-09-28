// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect } from 'react';
import { useCommandFormContext } from '@cratis/arc.react/commands';
import { validateNameField } from './NameFieldValidation';
import { useLocale } from '../Locale/LocaleContext';

/** Seed per-field form errors for empty names, so Next cannot leave an untouched required step. */
export const InitialNameErrors = ({ includeOrganization = false }: { includeOrganization?: boolean }) => {
    const { commandInstance, setCustomFieldError } = useCommandFormContext<{ firstName: string; middleName?: string; lastName: string; organizationName?: string }>();
    const locale = useLocale();
    useEffect(() => {
        const fields = includeOrganization ? ['organizationName', 'firstName', 'middleName', 'lastName'] : ['firstName', 'middleName', 'lastName'];
        fields.forEach(field => setCustomFieldError(field, validateNameField(field, commandInstance[field as keyof typeof commandInstance])));
    }, [commandInstance, includeOrganization, locale, setCustomFieldError]);
    return null;
};
