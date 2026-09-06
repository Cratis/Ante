// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useMemo, useState } from 'react';
import { Message, ProgressSpinner } from '@cratis/components/Display';
import { InputTextField } from '@cratis/components/CommandForm';
import { CommandStepper, StepperPanel } from '@cratis/components/CommandDialog';
import { useIdentity } from '@cratis/arc.react/identity';
import { Guid } from '@cratis/fundamentals';
import { AcceptInvitation, StatusForInvitation } from './UserSetup';
import { UserSetupAcceptanceStatus } from './UserSetupAcceptanceStatus';
import { HostUrl } from '../../Configuration/Configuration';
import { Current as LegalDocumentsCurrent } from '../../Legal/LegalDocuments';
import { UserSetupFrame } from './UserSetupFrame';
import { InvitationIdentityDetails } from '../Accepting/Accepting';
import { getInvitationIdFromToken } from '../Accepting/invitationToken';
import { LegalAcceptanceField } from '../../Legal/LegalAcceptanceField';
import { useLegalDocumentViewer } from '../../Legal/useLegalDocumentViewer';
import strings from 'Strings';

type UserSetupPageProps = {
    invitationToken?: string | null;
};

export const UserSetupPage = ({ invitationToken }: UserSetupPageProps) => {
    const identity = useIdentity(InvitationIdentityDetails);
    const invitationIdentityDetails = identity.details as unknown as InvitationIdentityDetails | undefined;
    const invitationId = useMemo(() => {
        const invitationIdFromToken = getInvitationIdFromToken(invitationToken);
        if (invitationIdFromToken) return invitationIdFromToken;

        if (!identity.isSet || !identity.details) return null;
        const id = invitationIdentityDetails?.invitationId;
        if (!id) return null;
        const str = id.toString();
        return Guid.isGuid(str) ? Guid.parse(str) : null;
    }, [identity.isSet, invitationIdentityDetails, invitationToken]);
    const [isWaitingForAcceptance, setIsWaitingForAcceptance] = useState(false);
    const [errorMessages, setErrorMessages] = useState<string[]>([]);
    const resolvedInvitationId = invitationId ?? Guid.empty;
    const [statusResult] = StatusForInvitation.use({ invitationId: resolvedInvitationId });
    const [hostUrlResult] = HostUrl.use();
    const [legalStatus] = LegalDocumentsCurrent.use();
    const legalDocuments = useLegalDocumentViewer({
        termsAndConditions: legalStatus.data?.termsAndConditions ?? '',
        privacyPolicy: legalStatus.data?.privacyPolicy ?? ''
    });

    useEffect(() => {
        if (!isWaitingForAcceptance || !statusResult.hasData) {
            return;
        }

        switch (statusResult.data.status) {
            case UserSetupAcceptanceStatus.accepted:
                if (!hostUrlResult.isSuccess) {
                    setIsWaitingForAcceptance(false);
                    setErrorMessages([strings.userSetup.hostAppUrlUnavailable]);
                    return;
                }

                {
                    // The provider-specific sign-in path challenges the provider the person just used, so
                    // entering the host application is a silent round trip instead of a second provider
                    // selection.
                    const config = hostUrlResult.data;
                    window.location.href = config.signInPath && config.signInPath !== '/'
                        ? new URL(config.signInPath, new URL(config.hostAppUrl, window.location.origin)).toString()
                        : config.hostAppUrl;
                }
                return;

            case UserSetupAcceptanceStatus.timedOut:
                setIsWaitingForAcceptance(false);
                setErrorMessages([strings.userSetup.acceptanceTimedOut]);
                return;
        }
    }, [isWaitingForAcceptance, statusResult, hostUrlResult]);

    if (isWaitingForAcceptance) {
        return (
            <UserSetupFrame>
                <div className='user-setup-card__content'>
                    <div className='user-setup-waiting'>
                        <ProgressSpinner className='user-setup-waiting__spinner' />
                        <p className='user-setup-waiting__message'>{strings.userSetup.setting}</p>
                    </div>
                </div>
            </UserSetupFrame>
        );
    }

    if (errorMessages.length > 0) {
        return (
            <UserSetupFrame>
                <div className='user-setup-card__content'>
                    <div className='user-setup-errors'>
                        {errorMessages.map((msg, i) => (
                            <Message key={i} severity='error' text={msg} className='user-setup-errors__item' />
                        ))}
                    </div>
                </div>
            </UserSetupFrame>
        );
    }

    if (!invitationId && !identity.isSet) {
        return (
            <UserSetupFrame>
                <div className='user-setup-card__content'>
                    <div className='user-setup-waiting'>
                        <ProgressSpinner className='user-setup-waiting__spinner' />
                    </div>
                </div>
            </UserSetupFrame>
        );
    }

    const userInformationPanel = (
        <StepperPanel header={strings.userSetup.stepUserInformation}>
            <InputTextField<AcceptInvitation>
                value={c => c.firstName}
                title={strings.userSetup.firstName}
                placeholder={strings.userSetup.firstNamePlaceholder}
            />
            <InputTextField<AcceptInvitation>
                value={c => c.middleName}
                title={strings.userSetup.middleName}
                placeholder={strings.userSetup.middleNamePlaceholder}
            />
            <InputTextField<AcceptInvitation>
                value={c => c.lastName}
                title={strings.userSetup.lastName}
                placeholder={strings.userSetup.lastNamePlaceholder}
            />
        </StepperPanel>
    );

    return (
        <UserSetupFrame>
            <div className='user-setup-card__content user-setup-card__content--stepper'>
                <CommandStepper<AcceptInvitation>
                    command={AcceptInvitation}
                    validateOnInit
                    okLabel={strings.userSetup.acceptInvitation}
                    initialValues={{ invitationId: resolvedInvitationId, acceptedLegalTerms: false, acceptedLegalVersion: '' }}
                    // The legal version comes from a query, so it arrives after mount - it has to be a reactive
                    // overlay rather than part of the synchronous baseline, or the command would submit an empty
                    // version and be rejected.
                    currentValues={legalStatus.data?.isConfigured ? { acceptedLegalVersion: legalStatus.data.version } : undefined}
                    onSuccess={async () => { setIsWaitingForAcceptance(true); }}
                    onValidationFailure={(validationResults) => {
                        // Acceptance can be rejected for reasons no form field can express - most importantly when the
                        // login already belongs to a user in the organization. Surface those messages rather than
                        // leaving the user on a form that silently refuses to submit.
                        const messages = validationResults.map(result => result.message).filter(message => !!message);
                        setErrorMessages(messages.length > 0 ? messages : [strings.userSetup.acceptanceFailed]);
                    }}
                >
                    {/* Every child here has to be a StepperPanel. A component that renders one is not one:
                        the stepper counts it as a step but never displays it, which would push the terms step
                        into the position the wizard treats as the last one — so Next would submit the
                        invitation with the terms still unaccepted instead of showing them. */}
                    {userInformationPanel}
                    {legalStatus.data?.isConfigured && (
                        <StepperPanel header={strings.userSetup.stepTermsConditions}>
                            <LegalAcceptanceField<AcceptInvitation> value={c => c.acceptedLegalTerms} onShowDocument={legalDocuments.showDocument} />
                        </StepperPanel>
                    )}
                </CommandStepper>
                {legalDocuments.dialog}
            </div>
        </UserSetupFrame>
    );
};
