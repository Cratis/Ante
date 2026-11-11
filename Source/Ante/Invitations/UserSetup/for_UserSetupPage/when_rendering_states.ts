// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it, vi } from 'vitest';
import { createElement, type ReactNode } from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { UserSetupPage } from '../UserSetupPage';
import strings from 'Strings';

// The CI runner shares its module cache across files; reload the page against this file's mocks.
vi.hoisted(() => { vi.resetModules(); });

type DocumentsState = { isConfigured: boolean; version: string; termsAndConditions: string; privacyPolicy: string } | undefined;
type ButtonProps = { label: string; variant?: string; onClick: () => void };

const invitationId = 'f61541c5-0ee8-429a-b29a-a37e8434d1b9';
const state = vi.hoisted(() => ({
    identitySet: true,
    hasStatus: true,
    phase: 'form',
    accepted: true,
    gate: 'keepWaiting',
    outcome: { status: 2, reasonCode: '', requiresNewInvitation: false },
    documents: undefined as unknown as DocumentsState,
    lastDocuments: undefined as unknown as DocumentsState,
    checking: false,
    hostUrl: { isSuccess: true, data: { hostAppUrl: 'https://host.example/app', signInPath: '/' } } as { isSuccess: boolean; data?: { hostAppUrl: string; signInPath: string } },
    buttons: [] as { label: string; variant?: string; onClick: () => void }[],
    stepperProps: undefined as Record<string, unknown> | undefined,
    recoveryCalls: [] as string[],
    outcomeChecks: 0
}));

vi.mock('../UserSetupFrame', () => ({ UserSetupFrame: ({ children }: { children: ReactNode }) => createElement('div', { className: 'frame' }, children) }));
vi.mock('../../useInvitationContent', () => ({ useInvitationContent: () => undefined }));
vi.mock('../../../Configuration/Configuration', () => ({ HostUrl: { use: () => [state.hostUrl] } }));
vi.mock('@cratis/arc.react/identity', () => ({
    useIdentity: () => ({ isSet: state.identitySet, details: state.identitySet ? { invitationId } : undefined })
}));
vi.mock('../UserSetup', () => ({
    AcceptInvitation: class {},
    StatusForInvitation: { use: () => [state.hasStatus ? { hasData: true, data: { status: state.accepted ? 1 : 0, invitationId } } : { hasData: false }] }
}));
vi.mock('../../useOnboardingRecovery', () => ({
    useOnboardingRecovery: () => ({
        phase: state.phase, isAccepted: state.accepted,
        markSubmitted: () => state.recoveryCalls.push('markSubmitted'),
        checkAgain: () => state.recoveryCalls.push('checkAgain')
    })
}));
vi.mock('../../useHostOutcome', () => ({
    useHostOutcome: () => ({ ...state.outcome, isConfigured: true, isRead: true, checkAgain: () => { state.outcomeChecks++; } })
}));
vi.mock('../../HostOutcomeGate', () => ({ resolveHostOutcomeGate: () => state.gate }));
vi.mock('../../../Legal/useFreshLegalDocuments', () => ({
    useFreshLegalDocuments: () => ({ documents: state.documents, lastAvailableDocuments: state.lastDocuments, isChecking: state.checking, refresh: vi.fn() })
}));
vi.mock('../../../Legal/useLegalDocumentViewer', () => ({
    useLegalDocumentViewer: () => ({ showDocument: vi.fn(), dialog: createElement('aside', null, 'viewer') })
}));
vi.mock('../../../Legal/LegalAcceptanceField', () => ({ LegalAcceptanceField: () => createElement('input', { name: 'acceptedLegalTerms' }) }));
vi.mock('../../../Legal/LegalVersionValues', () => ({ LegalVersionValues: ({ version }: { version: string }) => createElement('i', null, version) }));
vi.mock('../../../Legal/LegalDocumentsUnavailable', () => ({ LegalDocumentsUnavailable: () => createElement('p', null, 'legal unavailable') }));
vi.mock('../../InitialNameErrors', () => ({ InitialNameErrors: () => null }));
vi.mock('../../../Accessibility/useAccessibleStepper', () => ({ useAccessibleStepper: () => ({ announcement: 'step announced', containerRef: null }) }));
vi.mock('@cratis/components/Common', () => ({
    Button: (props: ButtonProps) => {
        state.buttons.push(props);
        return createElement('button', { 'data-variant': props.variant }, props.label);
    }
}));
vi.mock('@cratis/components/Display', () => ({
    ProgressSpinner: (props: { className?: string; 'aria-label'?: string }) => createElement('span', { className: props.className, 'aria-label': props['aria-label'] }, 'spinner')
}));
vi.mock('@cratis/components/CommandForm', () => ({
    InputTextField: ({ title }: { title: string }) => createElement('input', { 'aria-label': title })
}));
vi.mock('@cratis/components/CommandDialog', () => ({
    CommandStepper: (props: { children: ReactNode; okLabel: string }) => {
        state.stepperProps = props;
        return createElement('form', null, props.children);
    },
    StepperPanel: ({ children, header }: { children: ReactNode; header: string }) => createElement('section', { 'aria-label': header }, children)
}));

const configuredDocuments = { isConfigured: true, version: 'legal-v1', termsAndConditions: 'terms', privacyPolicy: 'privacy' };
const the = (value: string) => renderToStaticMarkup(createElement('span', null, value)).slice(6, -7);
const render = () => renderToStaticMarkup(createElement(UserSetupPage));
const press = (label: string) => state.buttons.find(button => button.label === label)!.onClick();

beforeEach(() => {
    state.identitySet = true;
    state.hasStatus = true;
    state.phase = 'form';
    state.accepted = false;
    state.gate = 'keepWaiting';
    state.outcome = { status: 2, reasonCode: '', requiresNewInvitation: false };
    state.documents = configuredDocuments;
    state.lastDocuments = undefined;
    state.checking = false;
    state.hostUrl = { isSuccess: true, data: { hostAppUrl: 'https://host.example/app', signInPath: '/' } };
    state.buttons = [];
    state.stepperProps = undefined;
    state.recoveryCalls = [];
    state.outcomeChecks = 0;
    (globalThis as { window?: unknown }).window = { location: { href: 'https://ante.example/', origin: 'https://ante.example' } };
});

describe('when the identity and the first status read have not resolved', () => {
    beforeEach(() => { state.identitySet = false; });
    it('should render only the bare waiting spinner', () =>
        render().should.equal("<div class=\"frame\"><div class=\"user-setup-card__content\"><div class=\"user-setup-waiting\"><span class=\"user-setup-waiting__spinner\">spinner</span></div></div></div>"));
});

describe('when the status has not been read yet', () => {
    beforeEach(() => { state.hasStatus = false; });
    it('should render the bare waiting spinner instead of the form', () => {
        const html = render();
        html.should.include('user-setup-waiting__spinner');
        html.should.not.include('<form');
    });
});

describe('when recovery timed out', () => {
    beforeEach(() => { state.phase = 'timedOut'; });
    it('should render the not-yet-confirmed status with check again and support text', () =>
        render().should.equal(`<div class="frame"><div class="user-setup-card__content"><div class="user-setup-waiting" role="status" aria-live="polite"><p class="user-setup-waiting__message">${the(strings.onboarding.notYetConfirmed)}</p><button>${the(strings.onboarding.checkAgain)}</button><p class="user-setup-waiting__message">${the(strings.onboarding.contactSupport)}</p></div></div></div>`));
    it('should check again through recovery', () => {
        render();
        press(strings.onboarding.checkAgain);
        state.recoveryCalls.should.deep.equal(['checkAgain']);
    });
});

describe('when the host outcome screen is shown', () => {
    beforeEach(() => { state.gate = 'showHostOutcome'; state.phase = 'waiting'; state.accepted = true; });

    it('should render a successful outcome with the continue action only', () => {
        state.outcome = { status: 2, reasonCode: 'IGNORED', requiresNewInvitation: false };
        render().should.equal(`<div class="frame"><div class="user-setup-card__content"><div class="user-setup-waiting" role="status" aria-live="polite"><p class="user-setup-waiting__message">${the(strings.userSetup.hostOutcomeSucceeded)}</p><div class="user-setup-host-outcome__actions"><button>${the(strings.onboarding.continueToHost)}</button></div></div></div></div>`);
    });

    it('should render a failed outcome with its reason reference and a ghost retry', () => {
        state.outcome = { status: 3, reasonCode: 'E42', requiresNewInvitation: false };
        const html = render();
        html.should.include(the(strings.userSetup.hostOutcomeFailed));
        html.should.include(the(strings.onboarding.hostOutcomeReference.replace('{reasonCode}', 'E42')));
        html.should.include(`<button data-variant="ghost">${the(strings.onboarding.checkAgain)}</button>`);
        html.should.not.include(the(strings.onboarding.continueToHost));
    });

    it('should omit the reference for a failed outcome without a reason', () => {
        state.outcome = { status: 3, reasonCode: '', requiresNewInvitation: false };
        render().should.not.include(the(strings.onboarding.hostOutcomeReference.replace('{reasonCode}', '')));
    });

    it('should ask for a new invitation when the host requires one', () => {
        state.outcome = { status: 3, reasonCode: '', requiresNewInvitation: true };
        render().should.include(the(strings.userSetup.hostOutcomeRequiresNewInvitation));
    });

    it('should render the pending message for a pending outcome', () => {
        state.outcome = { status: 0, reasonCode: '', requiresNewInvitation: false };
        render().should.include(the(strings.userSetup.hostOutcomePending));
    });

    it('should re-run the host lookup on retry', () => {
        state.outcome = { status: 0, reasonCode: '', requiresNewInvitation: false };
        render();
        press(strings.onboarding.checkAgain);
        state.outcomeChecks.should.equal(1);
    });

    describe('and Continue is pressed', () => {
        beforeEach(() => { state.outcome = { status: 2, reasonCode: '', requiresNewInvitation: false }; state.gate = 'showHostOutcome'; });
        const hostHref = () => (globalThis as unknown as { window: { location: { href: string } } }).window.location.href;

        it('should not navigate while the gate is not redirecting', () => {
            render();
            press(strings.onboarding.continueToHost);
            hostHref().should.equal('https://ante.example/');
        });
    });
});

describe('when the gate redirects automatically', () => {
    beforeEach(() => { state.gate = 'redirectAutomatically'; state.phase = 'waiting'; state.accepted = true; });
    it('should render the setting-up wait with the spinner label', () =>
        render().should.equal(`<div class="frame"><div class="user-setup-card__content"><div class="user-setup-waiting"><span class="user-setup-waiting__spinner" aria-label="${the(strings.userSetup.setting)}">spinner</span><p class="user-setup-waiting__message">${the(strings.userSetup.setting)}</p></div></div></div>`));
});

describe('when acceptance is recorded but not yet confirmed', () => {
    beforeEach(() => { state.phase = 'waiting'; });
    it('should render the setting-up wait', () => render().should.include('user-setup-waiting__message'));
});

describe('when legal documents are being checked for the first time', () => {
    beforeEach(() => { state.documents = undefined; state.checking = true; });
    it('should render the checking spinner only', () =>
        render().should.equal(`<div class="frame"><div class="user-setup-card__content"><span aria-label="${the(strings.onboarding.legalDocumentsChecking)}">spinner</span></div></div>`));
});

describe('when no legal documents were ever available', () => {
    beforeEach(() => { state.documents = undefined; });
    it('should render the unavailable notice only', () =>
        render().should.equal('<div class="frame"><div class="user-setup-card__content"><p>legal unavailable</p></div></div>'));
});

describe('when the wizard form is shown', () => {
    it('should render both steps, the version, the legal field and the viewer', () => {
        const html = render();
        html.should.include(`<section aria-label="${the(strings.userSetup.stepUserInformation)}"><input aria-label="${the(strings.userSetup.firstName)}"/><input aria-label="${the(strings.userSetup.middleName)}"/><input aria-label="${the(strings.userSetup.lastName)}"/></section>`);
        html.should.include(`<section aria-label="${the(strings.userSetup.stepTermsConditions)}"><i>legal-v1</i><input name="acceptedLegalTerms"/></section>`);
        html.should.include('<aside>viewer</aside>');
        html.should.include('user-setup-card__content user-setup-card__content--stepper');
    });

    it('should omit the terms step when legal documents are not configured', () => {
        state.documents = { ...configuredDocuments, isConfigured: false };
        const html = render();
        html.should.include(the(strings.userSetup.stepUserInformation));
        html.should.not.include(the(strings.userSetup.stepTermsConditions));
    });

    it('should wire the stepper to the accept command defaults and labels', () => {
        render();
        state.stepperProps!.okLabel!.should.equal(strings.userSetup.acceptInvitation);
        (state.stepperProps!.validateOnInit as boolean).should.be.true;
    });

    it('should mark the recovery submitted when the command succeeds', async () => {
        render();
        await (state.stepperProps!.onSuccess as () => Promise<void>)();
        state.recoveryCalls.should.deep.equal(['markSubmitted']);
    });

    it('should hide and disable the form while only previously seen documents are known', () => {
        state.documents = undefined;
        state.lastDocuments = configuredDocuments;
        const html = render();
        html.should.include('legal unavailable');
        html.should.include('hidden=""');
        html.should.not.include('<aside>viewer</aside>');
    });

    it('should show the checking spinner above the hidden form while refreshing stale documents', () => {
        state.documents = undefined;
        state.lastDocuments = configuredDocuments;
        state.checking = true;
        const html = render();
        html.should.include(`aria-label="${the(strings.onboarding.legalDocumentsChecking)}"`);
        html.should.not.include('legal unavailable');
    });

    it('should expose a field change reset and a validation failure handler', () => {
        render();
        (state.stepperProps!.onFieldChange as () => void)();
        (state.stepperProps!.onValidationFailure as (results: unknown) => void).should.be.a('function');
    });
});
