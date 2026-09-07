// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useMemo, useState } from 'react';
import { Button } from '@cratis/components/Common';
import { Message, ProgressSpinner } from '@cratis/components/Display';
import { InputTextField } from '@cratis/components/CommandForm';
import { CommandStepper, StepperPanel } from '@cratis/components/CommandDialog';
import { useIdentity } from '@cratis/arc.react/identity';
import { Guid } from '@cratis/fundamentals';
import { AcceptInvitation, StatusForInvitation } from './UserSetup';
import { UserSetupAcceptanceStatus } from './UserSetupAcceptanceStatus';
import { useOnboardingRecovery } from '../useOnboardingRecovery';
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
    const [errorMessages, setErrorMessages] = useState<string[]>([]);
    const resolvedInvitationId = invitationId ?? Guid.empty;
    const [statusResult] = StatusForInvitation.use({ invitationId: resolvedInvitationId });
    const [hostUrlResult] = HostUrl.use();
    const [legalStatus] = LegalDocumentsCurrent.use();

    const isRecorded = statusResult.hasData && statusResult.data.status !== UserSetupAcceptanceStatus.pending;
    const isAccepted = statusResult.hasData && statusResult.data.status === UserSetupAcceptanceStatus.accepted;
    const recovery = useOnboardingRecovery(isRecorded, isAccepted);

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

    useEffect(() => {
        if (!recovery.isAccepted) return;

        if (!hostUrlResult.isSuccess) {
            setErrorMessages([strings.userSetup.hostAppUrlUnavailable]);
            return;
        }

        try {
            // The provider-specific sign-in path challenges the provider the person just used, so
            // entering the host application is a silent round trip instead of a second provider
            // selection.
            const config = hostUrlResult.data;
            window.location.href = config.signInPath && config.signInPath !== '/'
                ? new URL(config.signInPath, new URL(config.hostAppUrl, window.location.origin)).toString()
                : config.hostAppUrl;
        } catch {
            // A malformed host configuration must present as a recoverable destination failure, not a
            // crashed page - acceptance already published, so the person's information was not lost.
            setErrorMessages([strings.userSetup.hostAppUrlUnavailable]);
        }
    }, [recovery.isAccepted, hostUrlResult]);

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

    // Identity/invitation resolution and the first durable status read both have to complete before it
    // is safe to decide between the form and the waiting phase - showing the form even briefly beforehand
    // would let a recovered, already-submitted acceptance flash a form it must never resubmit.
    if ((!invitationId && !identity.isSet) || !statusResult.hasData) {
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

    if (recovery.phase === 'timedOut') {
        return (
            <UserSetupFrame>
                <div className='user-setup-card__content'>
                    <div className='user-setup-waiting'>
                        <p className='user-setup-waiting__message'>{strings.onboarding.notYetConfirmed}</p>
                        <Button label={strings.onboarding.checkAgain} onClick={recovery.checkAgain} />
                        <p className='user-setup-waiting__message'>{strings.onboarding.contactSupport}</p>
                    </div>
                </div>
            </UserSetupFrame>
        );
    }

    if (recovery.phase === 'waiting') {
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
                    initialValues={initialValues}
                    currentValues={currentValues}
                    onSuccess={async () => { recovery.markSubmitted(); }}
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
