// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ICommandResult } from '@cratis/arc/commands';

/** The fields needed to pre-flight a partially completed onboarding command. */
export interface OrganizationNameCommand {
    organizationName: string;
    firstName: string;
    lastName: string;
    acceptedLegalTerms: boolean;
    acceptedLegalVersion: string;
    validateClientSide(): ICommandResult<unknown>;
    validate(): Promise<ICommandResult<unknown>>;
}
