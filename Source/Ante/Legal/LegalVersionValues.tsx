// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect } from 'react';
import { useCommandFormContext } from '@cratis/arc.react/commands';
import type { ICommandResult } from '@cratis/arc/commands';

/** Apply a newly presented version once, without replaying name defaults or revoking acceptance on re-render. */
export const LegalVersionValues = ({ version }: { version: string }) => {
    const { commandInstance, setCommandValues, setCommandResult, beginSilentValidation, setSilentValidationResult } =
        useCommandFormContext<{ acceptedLegalTerms: boolean; acceptedLegalVersion: string }>();

    useEffect(() => {
        const issue = beginSilentValidation();
        setCommandValues({ acceptedLegalTerms: false, acceptedLegalVersion: version });
        const command = commandInstance as typeof commandInstance & { validateClientSide: () => ICommandResult<unknown> };
        const result = command.validateClientSide();
        setSilentValidationResult(result, issue);
        setCommandResult(result);
    }, [beginSilentValidation, commandInstance, setCommandResult, setCommandValues, setSilentValidationResult, version]);

    return null;
};
