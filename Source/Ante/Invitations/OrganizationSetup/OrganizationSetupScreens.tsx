// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ReactNode } from 'react';
import { Button } from '@cratis/components/Common';
import { ProgressSpinner } from '@cratis/components/Display';
import { OrganizationSetupFrame } from './OrganizationSetupFrame';
import { HostOutcomeCompletion, type HostOutcomeCompletionProps } from './HostOutcomeCompletion';
import type { InvitationPageContent } from '../../Configuration/Configuration';
import { LegalDocumentsUnavailable } from '../../Legal/LegalDocumentsUnavailable';
import { ErrorSummary } from '../../Accessibility/ErrorSummary';
import strings from 'Strings';

/** What the frame shows above the card: the invited journey's content or the registration subtitle. */
export type OrganizationSetupFrameContext = {
    content?: InvitationPageContent;
    subtitle?: string;
};

type ScreenProps = OrganizationSetupFrameContext & { children: ReactNode };

/** The framed card every non-form organization setup and registration screen renders inside. */
export const OrganizationSetupScreen = ({ children, ...frame }: ScreenProps) => (
    <OrganizationSetupFrame {...frame}>
        <div className='organization-setup-card__content'>
            {children}
        </div>
    </OrganizationSetupFrame>
);

/** Setup published but the host destination could not be resolved or reached. */
export const OrganizationSetupErrorScreen = ({ messages, ...frame }: OrganizationSetupFrameContext & { messages: string[] }) => (
    <OrganizationSetupScreen {...frame}>
        <ErrorSummary
            messages={messages}
            className='organization-setup-errors'
            itemClassName='organization-setup-errors__item'
        />
    </OrganizationSetupScreen>
);

/** The bare spinner shown until identity and the first durable status read resolve. */
export const OrganizationSetupResolvingScreen = (frame: OrganizationSetupFrameContext) => (
    <OrganizationSetupScreen {...frame}>
        <div className='organization-setup-waiting'>
            <ProgressSpinner className='organization-setup-waiting__spinner' />
        </div>
    </OrganizationSetupScreen>
);

/** The hand-off gave up waiting for confirmation; `extraAction` adds a screen-specific action. */
export const OrganizationSetupTimedOutScreen = ({ onCheckAgain, extraAction, ...frame }: OrganizationSetupFrameContext & { onCheckAgain: () => void; extraAction?: ReactNode }) => (
    <OrganizationSetupScreen {...frame}>
        <div className='organization-setup-waiting' role='status' aria-live='polite'>
            <p className='organization-setup-waiting__message'>{strings.onboarding.notYetConfirmed}</p>
            <Button label={strings.onboarding.checkAgain} onClick={onCheckAgain} />
            <p className='organization-setup-waiting__message'>{strings.onboarding.contactSupport}</p>
            {extraAction}
        </div>
    </OrganizationSetupScreen>
);

/** The optional host-outcome completion screen. */
export const OrganizationSetupHostOutcomeScreen = ({ handoff, succeededMessage, ...frame }: OrganizationSetupFrameContext & HostOutcomeCompletionProps) => (
    <OrganizationSetupScreen {...frame}>
        <HostOutcomeCompletion handoff={handoff} succeededMessage={succeededMessage} />
    </OrganizationSetupScreen>
);

/** Setup recorded, waiting for the redirect. */
export const OrganizationSetupSettingUpScreen = ({ message, ...frame }: OrganizationSetupFrameContext & { message: string }) => (
    <OrganizationSetupScreen {...frame}>
        <div className='organization-setup-waiting'>
            <ProgressSpinner className='organization-setup-waiting__spinner' aria-label={strings.organizationSetup.settingUp} />
            <p className='organization-setup-waiting__message'>{message}</p>
        </div>
    </OrganizationSetupScreen>
);

/** The first legal document check is still running. */
export const OrganizationSetupLegalCheckingScreen = (frame: OrganizationSetupFrameContext) => (
    <OrganizationSetupScreen {...frame}>
        <ProgressSpinner aria-label={strings.onboarding.legalDocumentsChecking} />
    </OrganizationSetupScreen>
);

/** No legal documents were ever available. */
export const OrganizationSetupLegalUnavailableScreen = ({ onRetry, ...frame }: OrganizationSetupFrameContext & { onRetry: () => void }) => (
    <OrganizationSetupScreen {...frame}>
        <LegalDocumentsUnavailable onRetry={onRetry} />
    </OrganizationSetupScreen>
);
