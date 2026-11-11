// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it, vi } from 'vitest';
import { createElement, type ReactNode } from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { OrganizationSetupPage } from '../OrganizationSetupPage';
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
    errors: [] as string[],
    outcomeStatus: 2,
    documents: undefined as unknown as DocumentsState,
    lastDocuments: undefined as unknown as DocumentsState,
    checking: false,
    content: undefined as unknown,
    buttons: [] as { label: string; variant?: string; onClick: () => void }[],
    stepperProps: undefined as Record<string, unknown> | undefined,
    calls: [] as string[],
    validation: { isValidating: false, error: undefined as string | undefined }
}));

vi.mock('react', async importOriginal => ({
    ...await importOriginal<typeof import('react')>(),
    useSyncExternalStore: () => state.validation
}));
vi.mock('../OrganizationSetupFrame', () => ({ OrganizationSetupFrame: ({ children }: { children: ReactNode }) => createElement('div', { className: 'frame' }, children) }));
vi.mock('../../useInvitationContent', () => ({ useInvitationContent: () => state.content }));
vi.mock('@cratis/arc.react/identity', () => ({
    useIdentity: () => ({ isSet: state.identitySet, details: state.identitySet ? { invitationId } : undefined })
}));
vi.mock('../OrganizationSetup', () => ({ SetupOrganization: class { setHttpHeadersCallback() {} } }));
vi.mock('../useOrganizationSetupHandoff', () => ({
    useOrganizationSetupHandoff: () => ({
        hasStatus: state.hasStatus, phase: state.phase, errorMessages: state.errors,
        hostOutcomeStatus: state.outcomeStatus, hostOutcomeReasonCode: '', hostOutcomeRequiresNewInvitation: false,
        checkHostOutcomeAgain: () => state.calls.push('checkHostOutcomeAgain'),
        continueToHost: () => state.calls.push('continueToHost'),
        checkAgain: () => state.calls.push('checkAgain'),
        captureOrganizationName: (name: string) => state.calls.push(`capture:${name}`),
        markSubmitted: () => state.calls.push('markSubmitted')
    })
}));
vi.mock('../../../Organization/OrganizationNameStepValidation', () => ({
    OrganizationNameStepValidation: class {
        subscribe = () => () => {};
        getSnapshot = () => state.validation;
        dispose = () => {};
        onLocaleChange = () => () => {};
        onFieldChange = () => {};
    }
}));
vi.mock('../../../Organization/OrganizationNameStepError', () => ({ OrganizationNameStepError: () => createElement('em', null, 'name error') }));
vi.mock('../../../Locale/LocaleContext', () => ({ useLocale: () => 'en' }));
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
const render = () => renderToStaticMarkup(createElement(OrganizationSetupPage));
const press = (label: string) => state.buttons.find(button => button.label === label)!.onClick();

beforeEach(() => {
    state.identitySet = true;
    state.hasStatus = true;
    state.phase = 'form';
    state.errors = [];
    state.outcomeStatus = 2;
    state.documents = configuredDocuments;
    state.lastDocuments = undefined;
    state.checking = false;
    state.content = undefined;
    state.buttons = [];
    state.stepperProps = undefined;
    state.calls = [];
    state.validation = { isValidating: false, error: undefined };
});

describe('when the hand-off reports a destination failure', () => {
    beforeEach(() => { state.errors = ['Host unavailable']; state.phase = 'waiting'; });
    it('should render only the error summary', () => {
        const html = render();
        html.should.include('organization-setup-errors');
        html.should.include('Host unavailable');
        html.should.not.include('<form');
    });
});

describe('when the identity has not resolved', () => {
    beforeEach(() => { state.identitySet = false; });
    it('should render only the bare waiting spinner', () =>
        render().should.equal('<div class="frame"><div class="organization-setup-card__content"><div class="organization-setup-waiting"><span class="organization-setup-waiting__spinner">spinner</span></div></div></div>'));
});

describe('when the first status read has not completed', () => {
    beforeEach(() => { state.hasStatus = false; });
    it('should render the bare waiting spinner instead of the form', () => {
        const html = render();
        html.should.include('organization-setup-waiting__spinner');
        html.should.not.include('<form');
    });
});

describe('when the hand-off timed out', () => {
    beforeEach(() => { state.phase = 'timedOut'; });
    it('should render the not-yet-confirmed status with check again and support text', () =>
        render().should.equal(`<div class="frame"><div class="organization-setup-card__content"><div class="organization-setup-waiting" role="status" aria-live="polite"><p class="organization-setup-waiting__message">${the(strings.onboarding.notYetConfirmed)}</p><button>${the(strings.onboarding.checkAgain)}</button><p class="organization-setup-waiting__message">${the(strings.onboarding.contactSupport)}</p></div></div></div>`));
    it('should check again through the hand-off', () => {
        render();
        press(strings.onboarding.checkAgain);
        state.calls.should.deep.equal(['checkAgain']);
    });
});

describe('when setup is published and the page is waiting', () => {
    beforeEach(() => { state.phase = 'waiting'; });
    it('should render the setting-up wait with the spinner label', () =>
        render().should.equal(`<div class="frame"><div class="organization-setup-card__content"><div class="organization-setup-waiting"><span class="organization-setup-waiting__spinner" aria-label="${the(strings.organizationSetup.settingUp)}">spinner</span><p class="organization-setup-waiting__message">${the(strings.organizationSetup.settingUp)}</p></div></div></div>`));
});

describe('when the host outcome screen is shown', () => {
    beforeEach(() => { state.phase = 'hostOutcome'; });
    it('should render the shipped success message and the continue action', () => {
        const html = render();
        html.should.include(the(strings.organizationSetup.hostOutcomeSucceeded));
        html.should.include(the(strings.onboarding.continueToHost));
    });
    it('should prefer the host-authored completion message', () => {
        state.content = { completionMessage: 'Custom done' };
        render().should.include('Custom done');
    });
    it('should continue to the host through the hand-off', () => {
        render();
        press(strings.onboarding.continueToHost);
        state.calls.should.deep.equal(['continueToHost']);
    });
    it('should offer a retry for a pending outcome', () => {
        state.outcomeStatus = 0;
        render();
        press(strings.onboarding.checkAgain);
        state.calls.should.deep.equal(['checkHostOutcomeAgain']);
    });
});

describe('when legal documents are being checked for the first time', () => {
    beforeEach(() => { state.documents = undefined; state.checking = true; });
    it('should render the checking spinner only', () =>
        render().should.equal(`<div class="frame"><div class="organization-setup-card__content"><span aria-label="${the(strings.onboarding.legalDocumentsChecking)}">spinner</span></div></div>`));
});

describe('when no legal documents were ever available', () => {
    beforeEach(() => { state.documents = undefined; });
    it('should render the unavailable notice only', () =>
        render().should.equal('<div class="frame"><div class="organization-setup-card__content"><p>legal unavailable</p></div></div>'));
});

describe('when the wizard form is shown', () => {
    it('should render all three steps, the viewer and the live regions', () => {
        state.validation = { isValidating: false, error: 'Name taken' };
        const html = render();
        html.should.include(`<section aria-label="${the(strings.organizationSetup.stepOrganization)}"><input aria-label="${the(strings.organizationSetup.organizationName)}"/><em>name error</em></section>`);
        html.should.include(`<section aria-label="${the(strings.organizationSetup.stepUserInformation)}"><input aria-label="${the(strings.organizationSetup.firstName)}"/><input aria-label="${the(strings.organizationSetup.middleName)}"/><input aria-label="${the(strings.organizationSetup.lastName)}"/></section>`);
        html.should.include(`<section aria-label="${the(strings.organizationSetup.stepTermsConditions)}"><i>legal-v1</i><input name="acceptedLegalTerms"/></section>`);
        html.should.include('<aside>viewer</aside>');
        html.should.include('organization-setup-card__content organization-setup-card__content--stepper');
        html.should.include('step announced');
        html.should.include('Name taken');
    });

    it('should omit the terms step when legal documents are not configured', () => {
        state.documents = { ...configuredDocuments, isConfigured: false };
        const html = render();
        html.should.include(the(strings.organizationSetup.stepUserInformation));
        html.should.not.include(the(strings.organizationSetup.stepTermsConditions));
    });

    it('should wire the stepper to the setup command', () => {
        render();
        state.stepperProps!.okLabel!.should.equal(strings.organizationSetup.setupOrganization);
        (state.stepperProps!.validateOnInit as boolean).should.be.true;
        (state.stepperProps!.isBusy as boolean).should.be.false;
    });

    it('should report the busy state while the name is validated', () => {
        state.validation = { isValidating: true, error: undefined };
        render();
        (state.stepperProps!.isBusy as boolean).should.be.true;
    });

    it('should capture the organization name before execution and keep the values', () => {
        render();
        const values = { organizationName: 'Acme' };
        (state.stepperProps!.onBeforeExecute as (v: unknown) => unknown)(values).should.equal(values);
        state.calls.should.deep.equal(['capture:Acme']);
    });

    it('should capture an empty name when none was entered', () => {
        render();
        (state.stepperProps!.onBeforeExecute as (v: unknown) => unknown)({});
        state.calls.should.deep.equal(['capture:']);
    });

    it('should mark the hand-off submitted when the command succeeds', async () => {
        render();
        await (state.stepperProps!.onSuccess as () => Promise<void>)();
        state.calls.should.deep.equal(['markSubmitted']);
    });

    it('should hide the form while only previously seen documents are known', () => {
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
});
