// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo, useRef } from 'react';
import { Button } from '@cratis/components/Common';
import { ProgressSpinner } from '@cratis/components/Display';
import { InputTextField } from '@cratis/components/CommandForm';
import { CommandStepper, StepperPanel } from '@cratis/components/CommandDialog';
import { useIdentity } from '@cratis/arc.react/identity';
import { Guid } from '@cratis/fundamentals';
import { SetupOrganization } from './OrganizationSetup';
import { useOrganizationSetupHandoff } from './useOrganizationSetupHandoff';
import { Current as LegalDocumentsCurrent } from '../../Legal/LegalDocuments';
import { OrganizationSetupFrame } from './OrganizationSetupFrame';
import { InvitationIdentityDetails } from '../Accepting/Accepting';
import { getInvitationIdFromToken } from '../Accepting/invitationToken';
import { LegalAcceptanceField } from '../../Legal/LegalAcceptanceField';
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
    const [legalStatus] = LegalDocumentsCurrent.use();

    const handoff = useOrganizationSetupHandoff({
        invitationId: resolvedInvitationId,
        hostAppUnavailableMessage: strings.organizationSetup.hostAppUrlUnavailable,
    });

    // Memoized so an unrelated re-render - opening the terms dialog, a status poll tick - does not
    // recreate this object: CommandForm reasserts initialValues/currentValues onto the command whenever
    // their identity changes, and a fresh literal on every render would otherwise silently uncheck the
    // acceptance box or blank the version as often as the page re-renders.
    const initialValues = useMemo(() => ({ invitationId: resolvedInvitationId }), [resolvedInvitationId]);

    // The legal version comes from a query, so it arrives after mount - it has to be a reactive overlay
    // rather than part of the synchronous baseline, or the command would submit an empty version and be
    // rejected. Memoized on the configured/version pair rather than recreated every render, so it is
    // only reapplied when the document the host presents actually changes - at which point
    // acceptedLegalTerms is deliberately reset to false too, renewing review: a version bump the user
    // has not seen forces a fresh acceptance instead of silently carrying the old one forward. Only
    // these two legal fields are ever included here, so a version change never touches the name fields
    // the user has already filled in. If the source stops being configured entirely, this becomes
    // undefined and any previously accepted value is left as-is on the command; that submission is
    // rejected server-side as unsolicited acceptance rather than silently recorded or silently dropped.
    const currentValues = useMemo(
        () => (legalStatus.data?.isConfigured
            ? { acceptedLegalTerms: false, acceptedLegalVersion: legalStatus.data.version }
            : undefined),
        [legalStatus.data?.isConfigured, legalStatus.data?.version]);

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

    return (
        <OrganizationSetupFrame>
            <div className='organization-setup-card__content organization-setup-card__content--stepper' ref={stepperContainerRef}>
                <CommandStepper<SetupOrganization>
                    command={SetupOrganization}
                    validateOnInit
                    okLabel={strings.organizationSetup.setupOrganization}
                    initialValues={initialValues}
                    currentValues={currentValues}
                    onBeforeExecute={(values) => {
                        handoff.captureOrganizationName(values.organizationName ?? '');
                        return values;
                    }}
                    onSuccess={async () => { handoff.markSubmitted(); }}
                >
                    <StepperPanel header={strings.organizationSetup.stepOrganization}>
                        <InputTextField<SetupOrganization>
                            value={c => c.organizationName}
                            title={strings.organizationSetup.organizationName}
                            placeholder={strings.organizationSetup.organizationNamePlaceholder}
                            pt={{ root: { autoComplete: 'organization' } }}
                        />
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
                            <LegalAcceptanceField<SetupOrganization> value={c => c.acceptedLegalTerms} onShowDocument={legalDocuments.showDocument} />
                        </StepperPanel>
                    )}
                </CommandStepper>
                {legalDocuments.dialog}
            </div>
            <LiveRegion message={announcement} />
        </OrganizationSetupFrame>
    );
};
