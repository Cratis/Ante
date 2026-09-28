// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Guid } from '@cratis/fundamentals';
import { OrganizationSetupAcceptanceStatus } from './OrganizationSetupAcceptanceStatus';

/** Keeps registration ids out of invited-flow status requests, and vice versa. */
export const organizationStatusIds = (id: Guid, isRegistration: boolean) => ({
    invitationId: isRegistration ? Guid.empty : id,
    registrationId: isRegistration ? id : Guid.empty,
});

/** Recheck only after submission or while recovering an existing pointer, within the recovery window. */
export const shouldRecheckRegistrationStatus = (
    isRegistration: boolean,
    isSubmittedOrRecovering: boolean,
    recoveryExpired: boolean,
    hasData: boolean,
    status?: OrganizationSetupAcceptanceStatus) =>
    isRegistration && isSubmittedOrRecovering && !recoveryExpired &&
    (!hasData || status !== OrganizationSetupAcceptanceStatus.accepted);
