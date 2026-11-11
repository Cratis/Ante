// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it, vi } from 'vitest';
import { createElement, type ReactNode } from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { RegistrationPage } from '../RegistrationPage';
import strings from 'Strings';

// The CI runner shares its module cache across files; reload the page against this file's mocks.
vi.hoisted(() => { vi.resetModules(); });

type DocumentsState = { isConfigured: boolean; version: string; termsAndConditions: string; privacyPolicy: string } | undefined;
type ButtonProps = { label: string; variant?: string; onClick: () => void };

const state = vi.hoisted(() => ({
    configuration: { hasData: true, data: { isEnabled: true, closedUrl: '', contextKeys: ['campaign'], content: undefined as unknown } } as { hasData: boolean; data?: { isEnabled?: boolean; closedUrl: string; contextKeys: string[]; content: unknown } },
    hasStatus: true,
    phase: 'form',
    errors: [] as string[],
    startState: 'started' as 'pending' | 'started' | 'failed',
    documents: undefined as unknown as DocumentsState,
    lastDocuments: undefined as unknown as DocumentsState,
    checking: false,
    buttons: [] as { label: string; variant?: string; onClick: () => void }[],
    startFailed: undefined as { onRetry: () => void; onStartNew: () => void } | undefined,
    stepperProps: undefined as Record<string, unknown> | undefined,
    calls: [] as string[],
    validation: { isValidating: false, error: undefined as string | undefined },
    useStateOverride: undefined as string | undefined
}));

vi.mock('react', async importOriginal => {
    const actual = await importOriginal<typeof import('react')>();
    return {
        ...actual,
        useSyncExternalStore: () => state.validation,
        useState: (initial: unknown) => initial === 'pending' && state.useStateOverride ? [state.useStateOverride, () => {}] : actual.useState(initial)
    };
});
vi.mock('../../../Invitations/OrganizationSetup/OrganizationSetupFrame', () => ({
    OrganizationSetupFrame: ({ children, subtitle }: { children: ReactNode; subtitle?: string }) => createElement('div', { className: 'frame', 'data-subtitle': subtitle }, children)
}));
vi.mock('../../../Invitations/OrganizationSetup/HostOutcomeCompletion', () => ({
    HostOutcomeCompletion: ({ succeededMessage }: { succeededMessage?: string }) => createElement('p', { className: 'host-outcome' }, succeededMessage ?? 'default outcome')
}));
vi.mock('../../../Configuration/Configuration', () => ({
    Registration: { use: () => [state.configuration] },
    RegistrationConfiguration: class {}
}));
vi.mock('../Registration', () => ({ RegisterOrganization: class { setHttpHeadersCallback() {} } }));
vi.mock('../startRegistration', () => ({ startRegistration: () => new Promise(() => {}) }));
vi.mock('../RegistrationOperation', () => ({
    getOrCreateRegistrationOperation: () => ({ id: 'registration-1', isRecovered: false }),
    clearRegistrationOperation: () => state.calls.push('clearOperation')
}));
vi.mock('../signupContext', () => ({
    captureSignupContext: () => [{ key: 'campaign', value: 'spring' }],
    clearSignupContext: () => state.calls.push('clearSignupContext')
}));
vi.mock('../RegistrationIntro', () => ({ RegistrationIntro: () => createElement('header', null, 'intro') }));
vi.mock('../RegistrationClosed', () => ({
    RegistrationClosed: ({ closedUrl }: { closedUrl: string }) => createElement('div', { className: 'closed' }, closedUrl)
}));
vi.mock('../RegistrationStartFailed', () => ({
    RegistrationStartFailed: (props: { onRetry: () => void; onStartNew: () => void }) => {
        state.startFailed = props;
        return createElement('div', { className: 'start-failed' });
    }
}));
vi.mock('../../../Invitations/OrganizationSetup/useOrganizationSetupHandoff', () => ({
    useOrganizationSetupHandoff: () => ({
        hasStatus: state.hasStatus, phase: state.phase, errorMessages: state.errors,
        hostOutcomeStatus: 2, hostOutcomeReasonCode: '', hostOutcomeRequiresNewInvitation: false,
        checkAgain: () => state.calls.push('checkAgain'),
        captureOrganizationName: (name: string) => state.calls.push(`capture:${name}`),
        markSubmitted: () => state.calls.push('markSubmitted')
    })
}));
vi.mock('../../OrganizationNameStepValidation', () => ({
    OrganizationNameStepValidation: class {
        subscribe = () => () => {};
        getSnapshot = () => state.validation;
        dispose = () => {};
        onLocaleChange = () => () => {};
        onFieldChange = () => {};
    }
}));
vi.mock('../../OrganizationNameStepError', () => ({ OrganizationNameStepError: () => createElement('em', null, 'name error') }));
vi.mock('../../../Locale/LocaleContext', () => ({ useLocale: () => 'en' }));
vi.mock('../../../Locale/localeHttpHeaders', () => ({ localeHttpHeaders: () => ({}) }));
vi.mock('../../../Legal/useFreshLegalDocuments', () => ({
    useFreshLegalDocuments: () => ({ documents: state.documents, lastAvailableDocuments: state.lastDocuments, isChecking: state.checking, refresh: vi.fn() })
}));
vi.mock('../../../Legal/useLegalDocumentViewer', () => ({
    useLegalDocumentViewer: () => ({ showDocument: vi.fn(), dialog: createElement('aside', null, 'viewer') })
}));
vi.mock('../../../Legal/LegalAcceptanceField', () => ({ LegalAcceptanceField: () => createElement('input', { name: 'acceptedLegalTerms' }) }));
vi.mock('../../../Legal/LegalVersionValues', () => ({ LegalVersionValues: ({ version }: { version: string }) => createElement('i', null, version) }));
vi.mock('../../../Legal/LegalDocumentsUnavailable', () => ({ LegalDocumentsUnavailable: () => createElement('p', null, 'legal unavailable') }));
vi.mock('../../../Invitations/InitialNameErrors', () => ({ InitialNameErrors: () => null }));
vi.mock('../../../Accessibility/useAccessibleStepper', () => ({ useAccessibleStepper: () => ({ announcement: 'step announced', containerRef: null }) }));
vi.mock('@cratis/components/Common', () => ({
    Button: (props: ButtonProps) => {
        state.buttons.push(props);
        return createElement('button', { 'data-variant': props.variant }, props.label);
    }
}));
vi.mock('@cratis/components/Display', () => ({
    Message: ({ text }: { text: string }) => createElement('div', null, text),
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
const render = () => renderToStaticMarkup(createElement(RegistrationPage));
const press = (label: string) => state.buttons.find(button => button.label === label)!.onClick();
const openConfiguration = (content?: unknown) => ({ hasData: true, data: { isEnabled: true, closedUrl: '', contextKeys: ['campaign'], content } });

beforeEach(() => {
    state.configuration = openConfiguration();
    state.hasStatus = true;
    state.phase = 'form';
    state.errors = [];
    state.documents = configuredDocuments;
    state.lastDocuments = undefined;
    state.checking = false;
    state.buttons = [];
    state.startFailed = undefined;
    state.stepperProps = undefined;
    state.calls = [];
    state.validation = { isValidating: false, error: undefined };
    state.useStateOverride = undefined;
    (globalThis as { location?: unknown }).location = { search: '?campaign=spring' };
});

describe('when the registration configuration has not arrived', () => {
    beforeEach(() => { state.configuration = { hasData: true, data: { closedUrl: '', contextKeys: [], content: undefined } }; });
    it('should render the loading spinner under the default subtitle', () =>
        render().should.equal(`<div class="frame" data-subtitle="${the(strings.registration.subtitle)}"><div class="organization-setup-card__content"><span aria-label="${the(strings.registration.loading)}">spinner</span></div></div>`));
});

describe('when registration is not enabled', () => {
    beforeEach(() => { state.configuration = { hasData: true, data: { isEnabled: false, closedUrl: 'https://help.example', contextKeys: [], content: undefined } }; });
    it('should render the closed notice with the configured link', () =>
        render().should.equal(`<div class="frame" data-subtitle="${the(strings.registration.subtitle)}"><div class="organization-setup-card__content"><div class="closed">https://help.example</div></div></div>`));
});

describe('when the hand-off reports a failure', () => {
    beforeEach(() => { state.errors = ['Host unavailable']; });
    it('should render only the error summary', () => {
        const html = render();
        html.should.include('organization-setup-errors');
        html.should.include('Host unavailable');
        html.should.not.include('<form');
    });
});

describe('when the first status read has not completed', () => {
    beforeEach(() => { state.hasStatus = false; });
    it('should render the bare waiting spinner', () =>
        render().should.equal(`<div class="frame" data-subtitle="${the(strings.registration.subtitle)}"><div class="organization-setup-card__content"><div class="organization-setup-waiting"><span class="organization-setup-waiting__spinner">spinner</span></div></div></div>`));
});

describe('when the host configured a title', () => {
    beforeEach(() => { state.hasStatus = false; state.configuration = openConfiguration({ title: 'Join Acme', completionMessage: 'Welcome aboard' }); });
    it('should use it as the subtitle', () => render().should.include('data-subtitle="Join Acme"'));
});

describe('when the hand-off timed out', () => {
    beforeEach(() => { state.phase = 'timedOut'; });
    it('should render check again, support text and a ghost start-new action', () =>
        render().should.equal(`<div class="frame" data-subtitle="${the(strings.registration.subtitle)}"><div class="organization-setup-card__content"><div class="organization-setup-waiting" role="status" aria-live="polite"><p class="organization-setup-waiting__message">${the(strings.onboarding.notYetConfirmed)}</p><button>${the(strings.onboarding.checkAgain)}</button><p class="organization-setup-waiting__message">${the(strings.onboarding.contactSupport)}</p><button data-variant="ghost">${the(strings.registration.startNewRegistration)}</button></div></div></div>`));
    it('should check again through the hand-off', () => {
        render();
        press(strings.onboarding.checkAgain);
        state.calls.should.deep.equal(['checkAgain']);
    });
});

describe('when the sign-up is held for review or declined', () => {
    it('should say it is waiting for review', () => {
        state.phase = 'heldForReview';
        render().should.include(`<p class="organization-setup-waiting__message">${the(strings.registration.waitingForReview)}</p>`);
    });
    it('should say it was declined', () => {
        state.phase = 'declined';
        render().should.include(`<p class="organization-setup-waiting__message">${the(strings.registration.declined)}</p>`);
    });
});

describe('when the host outcome screen is shown', () => {
    beforeEach(() => { state.phase = 'hostOutcome'; });
    it('should pass the host-authored completion message', () => {
        state.configuration = openConfiguration({ completionMessage: 'Welcome aboard' });
        render().should.include('<p class="host-outcome">Welcome aboard</p>');
    });
    it('should pass no message when the host authored none', () =>
        render().should.include('<p class="host-outcome">default outcome</p>'));
});

describe('when setup is published and the page is waiting', () => {
    beforeEach(() => { state.phase = 'waiting'; });
    it('should render the shipped setting-up message', () =>
        render().should.include(`<span class="organization-setup-waiting__spinner" aria-label="${the(strings.organizationSetup.settingUp)}">spinner</span><p class="organization-setup-waiting__message">${the(strings.organizationSetup.settingUp)}</p>`));
    it('should prefer the host-authored completion message', () => {
        state.configuration = openConfiguration({ completionMessage: 'Welcome aboard' });
        render().should.include('<p class="organization-setup-waiting__message">Welcome aboard</p>');
    });
});

describe('when the registration start is pending', () => {
    beforeEach(() => { state.useStateOverride = 'pending'; });
    it('should render the starting spinner', () =>
        render().should.equal(`<div class="frame" data-subtitle="${the(strings.registration.subtitle)}"><div class="organization-setup-card__content"><span aria-label="${the(strings.registration.starting)}">spinner</span></div></div>`));
});

describe('when legal documents are being checked for the first time', () => {
    beforeEach(() => { state.useStateOverride = 'started'; state.documents = undefined; state.checking = true; });
    it('should render the checking spinner only', () =>
        render().should.equal(`<div class="frame" data-subtitle="${the(strings.registration.subtitle)}"><div class="organization-setup-card__content"><span aria-label="${the(strings.onboarding.legalDocumentsChecking)}">spinner</span></div></div>`));
});

describe('when no legal documents were ever available', () => {
    beforeEach(() => { state.useStateOverride = 'started'; state.documents = undefined; });
    it('should render the unavailable notice only', () =>
        render().should.equal(`<div class="frame" data-subtitle="${the(strings.registration.subtitle)}"><div class="organization-setup-card__content"><p>legal unavailable</p></div></div>`));
});

describe('when the registration start failed', () => {
    beforeEach(() => { state.useStateOverride = 'failed'; });
    it('should render the start-failed notice', () =>
        render().should.include('<div class="start-failed"></div>'));
    it('should start a new registration by clearing the operation and signup context', () => {
        (globalThis as { window?: unknown }).window = { location: { reload: () => state.calls.push('reload') } };
        render();
        state.startFailed!.onStartNew();
        state.calls.should.deep.equal(['clearOperation', 'clearSignupContext', 'reload']);
    });
});

describe('when the wizard form is shown', () => {
    // The start effect does not run in static markup, so the page stays in its starting state; the
    // form is reached by reporting the started state through the state hook.
    beforeEach(() => { state.useStateOverride = undefined; });
    const renderStarted = () => {
        state.useStateOverride = 'started';
        return render();
    };

    it('should render all three steps, the intro, the viewer and the live regions', () => {
        state.validation = { isValidating: false, error: 'Name taken' };
        const html = renderStarted();
        html.should.include('<header>intro</header>');
        html.should.include(`<section aria-label="${the(strings.organizationSetup.stepOrganization)}"><input aria-label="${the(strings.registration.organizationName)}"/><em>name error</em></section>`);
        html.should.include(`<section aria-label="${the(strings.organizationSetup.stepUserInformation)}"><input aria-label="${the(strings.organizationSetup.firstName)}"/><input aria-label="${the(strings.organizationSetup.middleName)}"/><input aria-label="${the(strings.organizationSetup.lastName)}"/></section>`);
        html.should.include(`<section aria-label="${the(strings.organizationSetup.stepTermsConditions)}"><i>legal-v1</i><input name="acceptedLegalTerms"/></section>`);
        html.should.include('<aside>viewer</aside>');
        html.should.include('organization-setup-card__content organization-setup-card__content--stepper');
        html.should.include('step announced');
        html.should.include('Name taken');
    });

    it('should omit the terms step when legal documents are not configured', () => {
        state.documents = { ...configuredDocuments, isConfigured: false };
        renderStarted().should.not.include(the(strings.organizationSetup.stepTermsConditions));
    });

    it('should wire the stepper to the register command', () => {
        renderStarted();
        state.stepperProps!.okLabel!.should.equal(strings.registration.register);
        (state.stepperProps!.validateOnInit as boolean).should.be.true;
        (state.stepperProps!.isBusy as boolean).should.be.false;
    });

    it('should capture the organization name and the signup context before execution', () => {
        renderStarted();
        const values: { organizationName?: string; signupContext?: unknown } = { organizationName: 'Acme' };
        ((state.stepperProps!.onBeforeExecute as (v: unknown) => unknown)(values) === values).should.be.true;
        state.calls.should.deep.equal(['capture:Acme']);
        values.signupContext!.should.deep.equal([{ key: 'campaign', value: 'spring' }]);
    });

    it('should mark the hand-off submitted when the command succeeds', async () => {
        renderStarted();
        await (state.stepperProps!.onSuccess as () => Promise<void>)();
        state.calls.should.deep.equal(['markSubmitted']);
    });

    it('should hide the form while only previously seen documents are known', () => {
        state.documents = undefined;
        state.lastDocuments = configuredDocuments;
        const html = renderStarted();
        html.should.include('legal unavailable');
        html.should.include('hidden=""');
        html.should.not.include('<aside>viewer</aside>');
    });

    it('should show the checking spinner above the hidden form while refreshing stale documents', () => {
        state.documents = undefined;
        state.lastDocuments = configuredDocuments;
        state.checking = true;
        const html = renderStarted();
        html.should.include(`aria-label="${the(strings.onboarding.legalDocumentsChecking)}"`);
        html.should.not.include('legal unavailable');
    });
});
