// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useMemo, useRef, useState } from 'react';
import { Message, ProgressSpinner } from '@cratis/components/Display';
import { InputTextField } from '@cratis/components/CommandForm';
import { CommandStepper, StepperPanel } from '@cratis/components/CommandDialog';
import { useIdentity } from '@cratis/arc.react/identity';
import { Guid } from '@cratis/fundamentals';
import { SetupOrganization, StatusForInvitation } from './OrganizationSetup';
import { OrganizationSetupAcceptanceStatus } from './OrganizationSetupAcceptanceStatus';
import { HostUrl } from '../../Configuration/Configuration';
import { resolveHostAppRedirectUrl } from '../../Configuration/hostAppRedirect';
import { Current as LegalDocumentsCurrent } from '../../Legal/LegalDocuments';
import { OrganizationSetupFrame } from './OrganizationSetupFrame';
import { InvitationIdentityDetails } from '../Accepting/Accepting';
import { getInvitationIdFromToken } from '../Accepting/invitationToken';
import { LegalAcceptanceField } from '../../Legal/LegalAcceptanceField';
import { useLegalDocumentViewer } from '../../Legal/useLegalDocumentViewer';
import strings from 'Strings';

type OrganizationSetupPageProps = {
    invitationToken?: string | null;
};

export const OrganizationSetupPage = ({ invitationToken }: OrganizationSetupPageProps) => {
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
    const orgNameRef = useRef('');
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

        if (statusResult.data.status !== OrganizationSetupAcceptanceStatus.accepted) {
            return;
        }

        if (!hostUrlResult.isSuccess) {
            setIsWaitingForAcceptance(false);
            setErrorMessages([strings.organizationSetup.hostAppUrlUnavailable]);
            return;
        }

        const organizationName = statusResult.data.organizationName || orgNameRef.current;
        if (!organizationName) {
            return;
        }

        const config = hostUrlResult.data;
        window.location.href = resolveHostAppRedirectUrl(config.hostAppUrl, organizationName, config.signInPath);
    }, [isWaitingForAcceptance, statusResult, hostUrlResult]);

    if (isWaitingForAcceptance) {
        return (
            <OrganizationSetupFrame>
                <div className='organization-setup-card__content'>
                    <div className='organization-setup-waiting'>
                        <ProgressSpinner className='organization-setup-waiting__spinner' />
                        <p className='organization-setup-waiting__message'>{strings.organizationSetup.settingUp}</p>
                    </div>
                </div>
            </OrganizationSetupFrame>
        );
    }

    if (errorMessages.length > 0) {
        return (
            <OrganizationSetupFrame>
                <div className='organization-setup-card__content'>
                    <div className='organization-setup-errors'>
                        {errorMessages.map((msg, i) => (
                            <Message key={i} severity='error' text={msg} className='organization-setup-errors__item' />
                        ))}
                    </div>
                </div>
            </OrganizationSetupFrame>
        );
    }

    if (!invitationId && !identity.isSet) {
        return (
            <OrganizationSetupFrame>
                <div className='organization-setup-card__content'>
                    <div className='organization-setup-waiting'>
                        <ProgressSpinner className='organization-setup-waiting__spinner' />
                    </div>
                </div>
            </OrganizationSetupFrame>
        );
    }

    return (
        <OrganizationSetupFrame>
            <div className='organization-setup-card__content organization-setup-card__content--stepper'>
                <CommandStepper<SetupOrganization>
                    command={SetupOrganization}
                    validateOnInit
                    okLabel={strings.organizationSetup.setupOrganization}
                    initialValues={{ invitationId: resolvedInvitationId, acceptedLegalTerms: false, acceptedLegalVersion: '' }}
                    // The legal version comes from a query, so it arrives after mount - it has to be a reactive
                    // overlay rather than part of the synchronous baseline, or the command would submit an empty
                    // version and be rejected.
                    currentValues={legalStatus.data?.isConfigured ? { acceptedLegalVersion: legalStatus.data.version } : undefined}
                    onBeforeExecute={(values) => {
                        orgNameRef.current = values.organizationName ?? '';
                        return values;
                    }}
                    onSuccess={async () => { setIsWaitingForAcceptance(true); }}
                >
                    <StepperPanel header={strings.organizationSetup.stepOrganization}>
                        <InputTextField<SetupOrganization>
                            value={c => c.organizationName}
                            title={strings.organizationSetup.organizationName}
                            placeholder={strings.organizationSetup.organizationNamePlaceholder}
                        />
                    </StepperPanel>
                    <StepperPanel header={strings.organizationSetup.stepUserInformation}>
                        <InputTextField<SetupOrganization>
                            value={c => c.firstName}
                            title={strings.organizationSetup.firstName}
                            placeholder={strings.organizationSetup.firstNamePlaceholder}
                        />
                        <InputTextField<SetupOrganization>
                            value={c => c.middleName}
                            title={strings.organizationSetup.middleName}
                            placeholder={strings.organizationSetup.middleNamePlaceholder}
                        />
                        <InputTextField<SetupOrganization>
                            value={c => c.lastName}
                            title={strings.organizationSetup.lastName}
                            placeholder={strings.organizationSetup.lastNamePlaceholder}
                        />
                    </StepperPanel>
                    {legalStatus.data?.isConfigured && (
                        <StepperPanel header={strings.organizationSetup.stepTermsConditions}>
                            <LegalAcceptanceField<SetupOrganization> value={c => c.acceptedLegalTerms} onShowDocument={legalDocuments.showDocument} />
                        </StepperPanel>
                    )}
                </CommandStepper>
                {legalDocuments.dialog}
            </div>
        </OrganizationSetupFrame>
    );
};
