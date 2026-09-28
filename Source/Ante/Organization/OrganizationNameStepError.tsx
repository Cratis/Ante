// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useSyncExternalStore } from 'react';
import { useCommandFormContext } from '@cratis/arc.react/commands';
import type { OrganizationNameStepValidation } from './OrganizationNameStepValidation';
import type { OrganizationNameCommand } from './OrganizationNameCommand';

interface OrganizationNameStepErrorProps {
    validation: Pick<OrganizationNameStepValidation<OrganizationNameCommand>, 'subscribe' | 'getSnapshot'>;
}

/** Feed the pre-flight's result into the existing CommandForm field and step error UI. */
export const OrganizationNameStepError = ({ validation }: OrganizationNameStepErrorProps) => {
    const { setCustomFieldError } = useCommandFormContext();
    const { error } = useSyncExternalStore(validation.subscribe, validation.getSnapshot);
    useEffect(() => {
        setCustomFieldError('organizationName', error);
    }, [setCustomFieldError, error]);
    return null;
};
