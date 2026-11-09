// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Guid } from '@cratis/fundamentals';
import { HostOutcomeStatus } from './HostOutcome/HostOutcomeStatus';
import { HostProvisioningStatus } from '../Onboarding/Provisioning/HostProvisioningStatus';
import { isResultFor } from './hasReadStatus';

/** The part of a durable host provisioning query result {@link hasReadHostProvisioning} looks at. */
export interface HostProvisioningResult {
    hasData: boolean;
    isSuccess: boolean;
    isPerforming: boolean;
    data?: { operationId?: unknown; isRequired?: unknown } | null;
}

/** The advisory host outcome, as the host outcome backchannel reports it. */
export type AdvisoryHostOutcome = {
    isConfigured: boolean;
    isRead: boolean;
    status: HostOutcomeStatus;
    reasonCode: string;
};

/** The durable host provisioning status Ante recorded from host-signed outcomes. */
export type DurableHostOutcome = {
    isRead: boolean;
    isRequired: boolean;
    status: HostProvisioningStatus;
    reasonCode: string;
};

/** The host outcome a completion screen renders from. */
export type CombinedHostOutcome = AdvisoryHostOutcome & {
    /** Whether the host reported that the person needs a new invitation; `status` is then `failed`. */
    requiresNewInvitation: boolean;
};

/**
 * Whether the durable host provisioning lookup has settled for the operation it was made for. Mirrors
 * {@link hasReadHostOutcome}: Arc's default value and a previous operation's result do not count, and a
 * failed lookup has settled and reads as not required.
 * @param result The durable host provisioning query result.
 * @param operationId The operation the lookup was made for.
 * @returns True once the lookup has answered for that operation, or failed.
 */
export const hasReadHostProvisioning = (result: HostProvisioningResult, operationId: Guid): boolean =>
    (!result.isPerforming && !result.isSuccess) ||
    (result.hasData && typeof result.data?.isRequired === 'boolean' && isResultFor(result.data?.operationId, operationId));

/**
 * Combines the advisory host outcome with the durable, host-signed one. When the deployment does not require
 * durable outcomes the advisory outcome is used unchanged. When it does, only the durable status decides: the
 * advisory lookup can never report success or failure on its own, and a host that has not answered stays pending
 * however long it takes - waiting never turns into a failure.
 * @param advisory The advisory host outcome.
 * @param durable The durable host provisioning status.
 * @returns The outcome to render from.
 */
export const combineHostOutcomes = (advisory: AdvisoryHostOutcome, durable: DurableHostOutcome): CombinedHostOutcome => {
    if (!durable.isRead) return { ...advisory, isRead: false, requiresNewInvitation: false };
    if (!durable.isRequired) return { ...advisory, requiresNewInvitation: false };

    const durableOutcome = (status: HostOutcomeStatus, reasonCode = '', requiresNewInvitation = false): CombinedHostOutcome =>
        ({ isConfigured: true, isRead: true, status, reasonCode, requiresNewInvitation });
    switch (durable.status) {
        case HostProvisioningStatus.succeeded: return durableOutcome(HostOutcomeStatus.succeeded);
        case HostProvisioningStatus.failed: return durableOutcome(HostOutcomeStatus.failed, durable.reasonCode);
        case HostProvisioningStatus.requiresNewInvitation: return durableOutcome(HostOutcomeStatus.failed, durable.reasonCode, true);
        default: return durableOutcome(HostOutcomeStatus.pending);
    }
};
