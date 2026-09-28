// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useRef, useSyncExternalStore } from 'react';
import { useCommandFormContext } from '@cratis/arc.react/commands';
import type { OrganizationNameStepValidation } from './OrganizationNameStepValidation';
import type { OrganizationNameCommand } from './OrganizationNameCommand';
import { withOrganizationNameError } from './organizationNameCommandResult';

interface OrganizationNameStepErrorProps {
    validation: Pick<OrganizationNameStepValidation<OrganizationNameCommand>, 'subscribe' | 'getSnapshot'>;
}

/** Feed the pre-flight's result into the existing CommandForm field and step error UI. */
export const OrganizationNameStepError = ({ validation }: OrganizationNameStepErrorProps) => {
    const { commandResult, setCommandResult } = useCommandFormContext();
    const { error } = useSyncExternalStore(validation.subscribe, validation.getSnapshot);
    const previousError = useRef<string | undefined>(undefined);
    useEffect(() => {
        const updatedResult = withOrganizationNameError(commandResult, previousError.current, error);
        previousError.current = error;
        if (updatedResult && updatedResult !== commandResult) setCommandResult(updatedResult);
    }, [commandResult, error, setCommandResult]);
    return null;
};
