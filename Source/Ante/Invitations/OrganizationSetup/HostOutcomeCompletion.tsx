// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Button } from '@cratis/components/Common';
import { HostOutcomeStatus } from '../HostOutcome/HostOutcomeStatus';
import { OrganizationSetupHandoffState } from './useOrganizationSetupHandoff';
import strings from 'Strings';

export type HostOutcomeCompletionProps = {
    /** The hand-off state of the wizard that published onboarding. */
    handoff: OrganizationSetupHandoffState;
    /** Replaces the default "succeeded" message, for example with host-authored registration copy. */
    succeededMessage?: string;
};

/**
 * The completion screen shown once onboarding has published and the deployment has a host outcome
 * backchannel configured. Onboarding already published by this point regardless of what (if anything) is
 * shown here; a failed or still-pending host outcome never blocks the Continue action.
 * @param props The hand-off state and optional success message.
 * @returns The completion content.
 */
export const HostOutcomeCompletion = ({ handoff, succeededMessage }: HostOutcomeCompletionProps) => {
    const message = handoff.hostOutcomeStatus === HostOutcomeStatus.succeeded
        ? (succeededMessage || strings.organizationSetup.hostOutcomeSucceeded)
        : handoff.hostOutcomeStatus === HostOutcomeStatus.failed
            ? strings.organizationSetup.hostOutcomeFailed
            : strings.organizationSetup.hostOutcomePending;

    return (
        <div className='organization-setup-waiting' role='status' aria-live='polite'>
            <p className='organization-setup-waiting__message'>{message}</p>
            {handoff.hostOutcomeStatus === HostOutcomeStatus.failed && handoff.hostOutcomeReasonCode && (
                <p className='organization-setup-waiting__message'>
                    {strings.onboarding.hostOutcomeReference.replace('{reasonCode}', handoff.hostOutcomeReasonCode)}
                </p>
            )}
            <div className='organization-setup-host-outcome__actions'>
                <Button label={strings.onboarding.continueToHost} onClick={handoff.continueToHost} />
                {handoff.hostOutcomeStatus !== HostOutcomeStatus.succeeded && (
                    <Button label={strings.onboarding.checkAgain} variant='ghost' onClick={handoff.checkHostOutcomeAgain} />
                )}
            </div>
        </div>
    );
};
