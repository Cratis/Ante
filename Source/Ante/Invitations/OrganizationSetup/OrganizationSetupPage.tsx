// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useMemo, useRef, useSyncExternalStore } from 'react';
import { Button } from '@cratis/components/Common';
import { ProgressSpinner } from '@cratis/components/Display';
import { InputTextField } from '@cratis/components/CommandForm';
import { CommandStepper, StepperPanel } from '@cratis/components/CommandDialog';
import { useIdentity } from '@cratis/arc.react/identity';
import { Guid } from '@cratis/fundamentals';
import { SetupOrganization } from './OrganizationSetup';
import { useOrganizationSetupHandoff } from './useOrganizationSetupHandoff';
import { OrganizationNameStepValidation } from '../../Organization/OrganizationNameStepValidation';
import { OrganizationNameStepError } from '../../Organization/OrganizationNameStepError';
import { InitialNameErrors } from '../InitialNameErrors';
import { initialOrganizationSetupValues } from '../initialOnboardingValues';
import { validateChangedName } from '../NameFieldValidation';
import { localeHttpHeaders } from '../../Locale/localeHttpHeaders';
import { useLocale } from '../../Locale/LocaleContext';
import { HostOutcomeStatus } from '../HostOutcome/HostOutcomeStatus';
import { Current as LegalDocumentsCurrent } from '../../Legal/LegalDocuments';
import { OrganizationSetupFrame } from './OrganizationSetupFrame';
import { InvitationIdentityDetails } from '../Accepting/Accepting';
import { getInvitationIdFromToken } from '../Accepting/invitationToken';
import { LegalAcceptanceField } from '../../Legal/LegalAcceptanceField';
import { LegalVersionValues } from '../../Legal/LegalVersionValues';
import { useLegalDocumentViewer } from '../../Legal/useLegalDocumentViewer';
import { ErrorSummary } from '../../Accessibility/ErrorSummary';
import { LiveRegion } from '../../Accessibility/LiveRegion';
import { useAccessibleStepper } from '../../Accessibility/useAccessibleStepper';
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
    const resolvedInvitationId = invitationId ?? Guid.empty;
    const invitationIdText = resolvedInvitationId.toString();
    const [legalStatus] = LegalDocumentsCurrent.use();
    const locale = useLocale();

    const handoff = useOrganizationSetupHandoff({
        invitationId: resolvedInvitationId,
        hostAppUnavailableMessage: strings.organizationSetup.hostAppUrlUnavailable,
        supportsHostOutcome: true,
    });
    const nameValidation = useMemo(() => new OrganizationNameStepValidation(
        () => {
            const probe = new SetupOrganization();
            probe.invitationId = Guid.parse(invitationIdText);
            probe.setHttpHeadersCallback(localeHttpHeaders);
            return probe;
        },
        () => strings.organizationSetup.nameValidationUnavailable
    ), [invitationIdText]);
    const { isValidating: isNameValidating, error: nameValidationError } = useSyncExternalStore(nameValidation.subscribe, nameValidation.getSnapshot);
    useEffect(() => () => nameValidation.dispose(), [nameValidation]);
    useEffect(() => nameValidation.onLocaleChange(), [nameValidation, locale]);

    // Seed editable fields once; later legal document updates must not replay these defaults.
    const initialValues = useMemo(() => initialOrganizationSetupValues(resolvedInvitationId), [resolvedInvitationId]);

    const legalDocuments = useLegalDocumentViewer({
        termsAndConditions: legalStatus.data?.termsAndConditions ?? '',
        privacyPolicy: legalStatus.data?.privacyPolicy ?? ''
    });

    const stepperContainerRef = useRef<HTMLDivElement>(null);
    const { announcement } = useAccessibleStepper(stepperContainerRef, {
        idPrefix: 'organization-setup',
        announcementTemplate: strings.accessibility.stepAnnouncement
    });

    if (handoff.errorMessages.length > 0) {
        return (
            <OrganizationSetupFrame>
                <div className='organization-setup-card__content'>
                    <ErrorSummary
                        messages={handoff.errorMessages}
                        className='organization-setup-errors'
                        itemClassName='organization-setup-errors__item'
                    />
                </div>
            </OrganizationSetupFrame>
        );
    }

    // Identity/invitation resolution and the first durable status read both have to complete before it
    // is safe to decide between the form and the waiting phase - showing the form even briefly beforehand
    // would let a recovered, already-submitted operation flash a form it must never resubmit.
    if ((!invitationId && !identity.isSet) || !handoff.hasStatus) {
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

    if (handoff.phase === 'timedOut') {
        return (
            <OrganizationSetupFrame>
                <div className='organization-setup-card__content'>
                    <div className='organization-setup-waiting' role='status' aria-live='polite'>
                        <p className='organization-setup-waiting__message'>{strings.onboarding.notYetConfirmed}</p>
                        <Button label={strings.onboarding.checkAgain} onClick={handoff.checkAgain} />
                        <p className='organization-setup-waiting__message'>{strings.onboarding.contactSupport}</p>
                    </div>
                </div>
            </OrganizationSetupFrame>
        );
    }

    if (handoff.phase === 'waiting') {
        return (
            <OrganizationSetupFrame>
                <div className='organization-setup-card__content'>
                    <div className='organization-setup-waiting'>
                        <ProgressSpinner className='organization-setup-waiting__spinner' aria-label={strings.organizationSetup.settingUp} />
                        <p className='organization-setup-waiting__message'>{strings.organizationSetup.settingUp}</p>
                    </div>
                </div>
            </OrganizationSetupFrame>
        );
    }

    // Only reached when this deployment has a host outcome backchannel configured - see
    // useOrganizationSetupHandoff's supportsHostOutcome option. Organization setup already published by
    // this point regardless of what (if anything) is shown here; a failed or still-pending host outcome
    // never blocks the Continue action below.
    if (handoff.phase === 'hostOutcome') {
        const message = handoff.hostOutcomeStatus === HostOutcomeStatus.succeeded
            ? strings.organizationSetup.hostOutcomeSucceeded
            : handoff.hostOutcomeStatus === HostOutcomeStatus.failed
                ? strings.organizationSetup.hostOutcomeFailed
                : strings.organizationSetup.hostOutcomePending;

        return (
            <OrganizationSetupFrame>
                <div className='organization-setup-card__content'>
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
                </div>
            </OrganizationSetupFrame>
        );
    }

    return (
        <OrganizationSetupFrame>
            <div className='organization-setup-card__content organization-setup-card__content--stepper' ref={stepperContainerRef}>
                <CommandStepper<SetupOrganization>
                    key={invitationIdText}
                    command={SetupOrganization}
                    validateOnInit
                    onFieldValidate={validateChangedName}
                    isBusy={isNameValidating}
                    onFieldChange={nameValidation.onFieldChange}
                    okLabel={strings.organizationSetup.setupOrganization}
                    initialValues={initialValues}
                    onBeforeExecute={(values) => {
                        handoff.captureOrganizationName(values.organizationName ?? '');
                        return values;
                    }}
                    onSuccess={async () => { handoff.markSubmitted(); }}
                >
                    <StepperPanel header={strings.organizationSetup.stepOrganization}>
                        <InitialNameErrors includeOrganization />
                        <InputTextField<SetupOrganization>
                            value={c => c.organizationName}
                            title={strings.organizationSetup.organizationName}
                            placeholder={strings.organizationSetup.organizationNamePlaceholder}
                            pt={{ root: { autoComplete: 'organization' } }}
                        />
                        <OrganizationNameStepError validation={nameValidation} />
                    </StepperPanel>
                    <StepperPanel header={strings.organizationSetup.stepUserInformation}>
                        <InputTextField<SetupOrganization>
                            value={c => c.firstName}
                            title={strings.organizationSetup.firstName}
                            placeholder={strings.organizationSetup.firstNamePlaceholder}
                            pt={{ root: { autoComplete: 'given-name' } }}
                        />
                        <InputTextField<SetupOrganization>
                            value={c => c.middleName}
                            title={strings.organizationSetup.middleName}
                            placeholder={strings.organizationSetup.middleNamePlaceholder}
                            pt={{ root: { autoComplete: 'additional-name' } }}
                        />
                        <InputTextField<SetupOrganization>
                            value={c => c.lastName}
                            title={strings.organizationSetup.lastName}
                            placeholder={strings.organizationSetup.lastNamePlaceholder}
                            pt={{ root: { autoComplete: 'family-name' } }}
                        />
                    </StepperPanel>
                    {legalStatus.data?.isConfigured && (
                        <StepperPanel header={strings.organizationSetup.stepTermsConditions}>
                            <LegalVersionValues version={legalStatus.data.version} />
                            <LegalAcceptanceField<SetupOrganization> value={c => c.acceptedLegalTerms} onShowDocument={legalDocuments.showDocument} />
                        </StepperPanel>
                    )}
                </CommandStepper>
                {legalDocuments.dialog}
            </div>
            <LiveRegion message={announcement} />
            <LiveRegion message={nameValidationError ?? ''} />
        </OrganizationSetupFrame>
    );
};
