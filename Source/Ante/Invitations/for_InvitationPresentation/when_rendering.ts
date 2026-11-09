// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it, vi } from 'vitest';
import { createElement, type ReactNode } from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { Guid } from '@cratis/fundamentals';
import type { InvitationConfiguration } from '../../Configuration/Configuration';
import { UserSetupPage } from '../UserSetup/UserSetupPage';
import { OrganizationSetupPage } from '../OrganizationSetup/OrganizationSetupPage';
import { OrganizationSetupFrame } from '../OrganizationSetup/OrganizationSetupFrame';
import strings from 'Strings';

// The CI runner shares its module cache across files; reload pages and hooks against this file's mocks.
vi.hoisted(() => { vi.resetModules(); });

const state = vi.hoisted(() => ({
    content: undefined as InvitationConfiguration | undefined,
    contentAvailable: true,
    phase: 'form',
    gate: 'keepWaiting',
    outcome: 2,
    commandProps: undefined as Record<string, unknown> | undefined
}));
const invitationId = 'f61541c5-0ee8-429a-b29a-a37e8434d1b9';

vi.mock('../../Configuration/Configuration', () => ({
    GetInvitationContent: { use: () => [{ isSuccess: state.contentAvailable, data: state.content }, vi.fn()] },
    GetConfiguration: { use: () => [{ isSuccess: true, data: { pageTitle: 'Deployment', logoUrl: '' } }] },
    HostUrl: { use: () => [{ isSuccess: false }] }
}));
vi.mock('../../DisplayPreferences/DisplayPreferencesMenu', () => ({ DisplayPreferencesMenu: () => null }));
// The wizard is client-rendered; its external store has no SSR snapshot. Read the same snapshot in this presentation harness.
vi.mock('react', async importOriginal => ({
    ...await importOriginal<typeof import('react')>(),
    useSyncExternalStore: (_subscribe: unknown, snapshot: () => unknown) => snapshot()
}));
vi.mock('../InitialNameErrors', () => ({ InitialNameErrors: () => null }));
vi.mock('../../Organization/OrganizationNameStepError', () => ({ OrganizationNameStepError: () => null }));
vi.mock('@cratis/arc.react/identity', () => ({
    useIdentity: () => ({ isSet: true, details: { invitationId: 'f61541c5-0ee8-429a-b29a-a37e8434d1b9' } })
}));
vi.mock('../UserSetup/UserSetup', () => ({
    AcceptInvitation: class {},
    StatusForInvitation: { use: () => [{ hasData: true, data: { status: 0, invitationId: 'f61541c5-0ee8-429a-b29a-a37e8434d1b9' } }] }
}));
vi.mock('../OrganizationSetup/OrganizationSetup', () => ({ SetupOrganization: class {} }));
vi.mock('../useOnboardingRecovery', () => ({
    useOnboardingRecovery: () => ({ phase: state.phase, isAccepted: true, markSubmitted: vi.fn(), checkAgain: vi.fn() })
}));
vi.mock('../useHostOutcome', () => ({
    useHostOutcome: () => ({ status: state.outcome, reasonCode: '', isConfigured: true, isRead: true, checkAgain: vi.fn() })
}));
vi.mock('../HostOutcomeGate', () => ({ resolveHostOutcomeGate: () => state.gate }));
vi.mock('../OrganizationSetup/useOrganizationSetupHandoff', () => ({
    useOrganizationSetupHandoff: () => ({
        hasStatus: true, phase: state.phase, errorMessages: [], hostOutcomeStatus: state.outcome, hostOutcomeReasonCode: '',
        checkHostOutcomeAgain: vi.fn(), continueToHost: vi.fn(), checkAgain: vi.fn(),
        captureOrganizationName: vi.fn(), markSubmitted: vi.fn()
    })
}));
vi.mock('../../Legal/useFreshLegalDocuments', () => ({
    useFreshLegalDocuments: () => ({
        documents: { isConfigured: true, isUnavailable: false, version: 'legal-v1', termsAndConditions: 'HOST LEGAL TERMS', privacyPolicy: 'HOST PRIVACY POLICY' },
        isChecking: false, refresh: vi.fn()
    })
}));
vi.mock('../../Legal/useLegalDocumentViewer', () => ({
    useLegalDocumentViewer: () => ({ openTermsAndConditions: vi.fn(), openPrivacyPolicy: vi.fn(), dialog: null })
}));
vi.mock('../../Legal/LegalAcceptanceField', () => ({
    LegalAcceptanceField: () => createElement('input', { type: 'checkbox', name: 'acceptedLegalTerms', 'aria-label': 'Legal acceptance' })
}));
vi.mock('../../Legal/LegalVersionValues', () => ({ LegalVersionValues: () => null }));
vi.mock('../../Accessibility/useAccessibleStepper', () => ({ useAccessibleStepper: () => ({ announcement: '', containerRef: null }) }));
vi.mock('@cratis/components/Common', () => ({ Button: ({ label }: { label: string }) => createElement('button', null, label) }));
vi.mock('@cratis/components/Display', () => ({ ProgressSpinner: () => createElement('span', null, 'spinner') }));
vi.mock('@cratis/components/CommandForm', () => ({
    InputTextField: ({ name, title }: { name: string; title: string }) => createElement('input', { name, 'aria-label': title })
}));
vi.mock('@cratis/components/CommandDialog', () => ({
    CommandStepper: (props: { children: ReactNode; okLabel: string }) => {
        state.commandProps = props;
        return createElement('form', null, props.children, createElement('button', null, props.okLabel));
    },
    StepperPanel: ({ children, header }: { children: ReactNode; header: string }) => createElement('section', { 'aria-label': header }, children)
}));

const configureCopy = () => {
    state.content = {
        userSetup: { title: 'User title', intro: '<b>User intro</b>', completionMessage: 'User completion', supportUrl: 'https://example.com/user' },
        organizationSetup: { title: 'Organization title', intro: '<b>Organization intro</b>', completionMessage: 'Organization completion', supportUrl: 'https://example.com/organization' }
    };
};

beforeEach(() => {
    state.content = undefined;
    state.contentAvailable = true;
    state.phase = 'form';
    state.gate = 'keepWaiting';
    state.outcome = 2;
    state.commandProps = undefined;
});

const journeys = [
    { key: 'userSetup' as const, name: 'User', otherName: 'Organization', component: UserSetupPage, support: 'user', messages: strings.userSetup, submit: strings.userSetup.acceptInvitation },
    { key: 'organizationSetup' as const, name: 'Organization', otherName: 'User', component: OrganizationSetupPage, support: 'organization', messages: strings.organizationSetup, submit: strings.organizationSetup.setupOrganization }
];

const renderedText = (text: string) => renderToStaticMarkup(createElement('span', null, text)).slice(6, -7);

for (const journey of journeys) {
    const subtitle = journey.key === 'userSetup' ? strings.userSetup.subtitle : strings.organizationSetup.subtitle;
    let html: string;
    const render = () => { html = renderToStaticMarkup(createElement(journey.component)); };

    describe(`when ${journey.key} copy is absent`, () => {
        beforeEach(render);
        it('should retain the shipped localized subtitle', () => html.should.include(subtitle));
        it('should retain the legal acceptance control', () => html.should.include('name="acceptedLegalTerms"'));
    });

    describe(`when ${journey.key} copy is configured`, () => {
        beforeEach(() => { configureCopy(); render(); });
        it('should replace only its own subtitle', () => html.should.include(`${journey.name} title`));
        it('should not show the other journeys copy', () => html.should.not.include(`${journey.otherName} title`));
        it('should preserve deployment branding', () => html.should.include(strings.branding.wordmark));
        it('should display markup as escaped plain text', () => html.should.include(`&lt;b&gt;${journey.name} intro&lt;/b&gt;`));
        it('should never interpret markup', () => html.should.not.include(`<b>${journey.name} intro</b>`));
        it('should use safe support link attributes', () => html.should.include(`href="https://example.com/${journey.support}" target="_blank" rel="noopener noreferrer"`));
        it('should preserve the legal acceptance control', () => html.should.include('name="acceptedLegalTerms"'));
        it('should preserve the submit button label', () => html.should.include(journey.submit));
        it('should preserve command payload defaults without adding presentation fields', () => {
            const expected = { invitationId: Guid.parse(invitationId), firstName: '', middleName: '', lastName: '', acceptedLegalTerms: false };
            state.commandProps!.initialValues!.should.deep.equal(journey.key === 'organizationSetup' ? { ...expected, organizationName: '' } : expected);
        });
    });

    describe(`when ${journey.key} copy cannot be loaded`, () => {
        beforeEach(() => { configureCopy(); state.contentAvailable = false; render(); });
        it('should retain the shipped localized subtitle', () => html.should.include(subtitle));
    });

    describe(`when ${journey.key} has partial copy`, () => {
        beforeEach(() => {
            configureCopy();
            state.content![journey.key] = { title: '', intro: 'Only introduction', completionMessage: '', supportUrl: '' };
            render();
        });
        it('should retain the default subtitle', () => html.should.include(subtitle));
        it('should display the configured introduction', () => html.should.include('Only introduction'));
        it('should omit the absent support link', () => html.should.not.include(`https://example.com/${journey.support}`));
    });

    describe(`when ${journey.key} succeeds with configured completion copy`, () => {
        beforeEach(() => { configureCopy(); state.gate = 'showHostOutcome'; state.phase = 'hostOutcome'; render(); });
        it('should display the configured successful completion', () => html.should.include(`${journey.name} completion`));
        it('should retain the continue action', () => html.should.include(renderedText(strings.onboarding.continueToHost)));
    });

    describe(`when ${journey.key} succeeds without completion copy`, () => {
        beforeEach(() => { state.gate = 'showHostOutcome'; state.phase = 'hostOutcome'; render(); });
        it('should retain the shipped success message', () => html.should.include(renderedText(journey.messages.hostOutcomeSucceeded)));
    });

    for (const outcome of [0, 1, 3]) {
        describe(`when ${journey.key} has non-successful host outcome ${outcome}`, () => {
            beforeEach(() => { configureCopy(); state.gate = 'showHostOutcome'; state.phase = 'hostOutcome'; state.outcome = outcome; render(); });
            it('should not display the configured completion', () => html.should.not.include(`${journey.name} completion`));
            it('should retain the shipped outcome message', () => html.should.include(renderedText(outcome === 3 ? journey.messages.hostOutcomeFailed : journey.messages.hostOutcomePending)));
            it('should retain the retry action', () => html.should.include(renderedText(strings.onboarding.checkAgain)));
            it('should not offer manual handoff after failure, timeout or an unknown lookup', () => html.should.not.include(renderedText(strings.onboarding.continueToHost)));
        });
    }
}

describe('when registration uses the shared organization setup frame', () => {
    let html: string;
    beforeEach(() => {
        configureCopy();
        const props = { subtitle: 'Registration subtitle', children: 'Registration form' };
        html = renderToStaticMarkup(createElement(OrganizationSetupFrame, props));
    });
    it('should retain the registration subtitle', () => html.should.include('Registration subtitle'));
    it('should never apply organization invitation copy', () => html.should.not.include('Organization title'));
});
