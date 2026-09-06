// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useMemo, useRef, useState } from 'react';
import { Message, ProgressSpinner } from '@cratis/components/Display';
import { InputTextField } from '@cratis/components/CommandForm';
import { CommandStepper, StepperPanel } from '@cratis/components/CommandDialog';
import { Guid } from '@cratis/fundamentals';
import { RegisterOrganization } from './Registration';
import { StatusForInvitation } from '../../Invitations/OrganizationSetup/OrganizationSetup';
import { OrganizationSetupAcceptanceStatus } from '../../Invitations/OrganizationSetup/OrganizationSetupAcceptanceStatus';
import { OrganizationSetupFrame } from '../../Invitations/OrganizationSetup/OrganizationSetupFrame';
import { HostUrl } from '../../Configuration/Configuration';
import { resolveHostAppRedirectUrl } from '../../Configuration/hostAppRedirect';
import { Current as LegalDocumentsCurrent } from '../../Legal/LegalDocuments';
import { LegalAcceptanceField } from '../../Legal/LegalAcceptanceField';
import { useLegalDocumentViewer } from '../../Legal/useLegalDocumentViewer';
import strings from 'Strings';

export const RegistrationPage = () => {
    const registrationId = useMemo(() => Guid.create(), []);
    const [isWaitingForAcceptance, setIsWaitingForAcceptance] = useState(false);
    const [errorMessages, setErrorMessages] = useState<string[]>([]);
    const orgNameRef = useRef('');
    const [statusResult] = StatusForInvitation.use({ invitationId: registrationId });
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
            setErrorMessages([strings.registration.hostAppUrlUnavailable]);
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
            <OrganizationSetupFrame subtitle={strings.registration.subtitle}>
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
            <OrganizationSetupFrame subtitle={strings.registration.subtitle}>
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

    return (
        <OrganizationSetupFrame subtitle={strings.registration.subtitle}>
            <div className='organization-setup-card__content organization-setup-card__content--stepper'>
                <CommandStepper<RegisterOrganization>
                    command={RegisterOrganization}
                    validateOnInit
                    okLabel={strings.registration.register}
                    initialValues={{ registrationId, acceptedLegalTerms: false, acceptedLegalVersion: '' }}
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
                        <InputTextField<RegisterOrganization>
                            value={c => c.organizationName}
                            title={strings.registration.organizationName}
                            placeholder={strings.registration.organizationNamePlaceholder}
                        />
                    </StepperPanel>
                    <StepperPanel header={strings.organizationSetup.stepUserInformation}>
                        <InputTextField<RegisterOrganization>
                            value={c => c.firstName}
                            title={strings.organizationSetup.firstName}
                            placeholder={strings.organizationSetup.firstNamePlaceholder}
                        />
                        <InputTextField<RegisterOrganization>
                            value={c => c.middleName}
                            title={strings.organizationSetup.middleName}
                            placeholder={strings.organizationSetup.middleNamePlaceholder}
                        />
                        <InputTextField<RegisterOrganization>
                            value={c => c.lastName}
                            title={strings.organizationSetup.lastName}
                            placeholder={strings.organizationSetup.lastNamePlaceholder}
                        />
                    </StepperPanel>
                    {legalStatus.data?.isConfigured && (
                        <StepperPanel header={strings.organizationSetup.stepTermsConditions}>
                            <LegalAcceptanceField<RegisterOrganization> value={c => c.acceptedLegalTerms} onShowDocument={legalDocuments.showDocument} />
                        </StepperPanel>
                    )}
                </CommandStepper>
                {legalDocuments.dialog}
            </div>
        </OrganizationSetupFrame>
    );
};
