// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Guid } from '@cratis/fundamentals';
import { BeginRegistration } from './Start/BeginningRegistration';

type RegistrationStartCommand = { registrationId: Guid; execute: () => Promise<{ isSuccess: boolean }> };

/** Start an operation before rendering the wizard; a failed command must never unlock submission. */
export const startRegistration = async (
    registrationId: Guid,
    createCommand: () => RegistrationStartCommand = () => new BeginRegistration()
): Promise<boolean> => {
    const command = createCommand();
    command.registrationId = registrationId;
    try {
        return (await command.execute()).isSuccess;
    } catch {
        return false;
    }
};
