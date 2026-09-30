// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { BrowserContext, expect, Page } from '@playwright/test';
import { invite, LobbyName, lobbyUrl, newOrganizationName, newSubject, received, signIn } from './harness';
import { Strings } from './strings';

/** The values a person enters in a wizard. */
export interface Values {
    organizationName: string;
    firstName: string;
    lastName: string;
}

/** One wizard step: its header, the text fields on it, and whether it asks for legal consent. */
export interface Step {
    header: string;
    fields: { label: string; value: string }[];
    consent: boolean;
}

/** A journey opened for one person: where it starts and who is signed in. */
export interface Opened {
    url: string;
    subject: string;
    invitationId?: string;
}

/** One of the lobby's three onboarding journeys. */
export interface Journey {
    name: string;

    /** The event the host receives exactly once when the journey completes. */
    completedEvent: string;

    /** Whether the journey's wizard checks the organization name with the server as it is typed. */
    namesOrganization: boolean;

    /**
     * Prepares the journey for one person - an invitation for the invited journeys - and signs the context in.
     * @param context The browser context.
     * @param lobby The lobby.
     * @returns Where the journey starts.
     */
    open(context: BrowserContext, lobby: LobbyName): Promise<Opened>;

    /**
     * The wizard's steps for a lobby.
     * @param strings The translations.
     * @param lobby The lobby; the legal lobby adds a terms step.
     * @param values What the person enters.
     * @returns The steps, in order.
     */
    steps(strings: Strings, lobby: LobbyName, values: Values): Step[];

    /** The label of the final step's submit button. */
    submitLabel(strings: Strings): string;

    /** The invitation or registration the host receives the journey's events for. */
    eventSourceId(page: Page, opened: Opened): Promise<string>;
}

const legalStep = (header: string): Step => ({ header, fields: [], consent: true });

const invited = (kind: 'join' | 'create') => async (context: BrowserContext, lobby: LobbyName): Promise<Opened> => {
    const invitation = await invite(lobby, kind);
    await signIn(context, invitation.subject);
    return { url: invitation.link, subject: invitation.subject, invitationId: invitation.invitationId };
};

export const joinByInvitation: Journey = {
    name: 'join by invitation',
    completedEvent: 'InvitationToJoinTenantAccepted',
    namesOrganization: false,
    open: invited('join'),
    steps: (s, lobby, values) => [
        {
            header: s.userSetup.stepUserInformation,
            fields: [{ label: s.userSetup.firstName, value: values.firstName }, { label: s.userSetup.lastName, value: values.lastName }],
            consent: false
        },
        ...(lobby === 'legal' ? [legalStep(s.userSetup.stepTermsConditions)] : [])
    ],
    submitLabel: s => s.userSetup.acceptInvitation,
    eventSourceId: async (_page, opened) => opened.invitationId!
};

const organizationSteps = (s: Strings, lobby: LobbyName, values: Values, organizationLabel: string): Step[] => [
    { header: s.organizationSetup.stepOrganization, fields: [{ label: organizationLabel, value: values.organizationName }], consent: false },
    {
        header: s.organizationSetup.stepUserInformation,
        fields: [{ label: s.organizationSetup.firstName, value: values.firstName }, { label: s.organizationSetup.lastName, value: values.lastName }],
        consent: false
    },
    ...(lobby === 'legal' ? [legalStep(s.organizationSetup.stepTermsConditions)] : [])
];

export const invitedOrganizationSetup: Journey = {
    name: 'invited organization setup',
    completedEvent: 'InvitationToCreateTenantAccepted',
    namesOrganization: true,
    open: invited('create'),
    steps: (s, lobby, values) => organizationSteps(s, lobby, values, s.organizationSetup.organizationName),
    submitLabel: s => s.organizationSetup.setupOrganization,
    eventSourceId: async (_page, opened) => opened.invitationId!
};

export const selfServiceRegistration: Journey = {
    name: 'self-service registration',
    completedEvent: 'OrganizationRegistrationCompleted',
    namesOrganization: true,
    open: async (context, lobby) => {
        const subject = newSubject('visitor');
        await signIn(context, subject);
        return { url: `${lobbyUrl(lobby)}/register`, subject };
    },
    steps: (s, lobby, values) => organizationSteps(s, lobby, values, s.registration.organizationName),
    submitLabel: s => s.registration.register,

    // The tab keeps its registration id in session storage, so a reload resumes the same registration.
    eventSourceId: async page => {
        const stored = await page.evaluate(() => sessionStorage.getItem('ante.registration.operation'));
        expect(stored, 'the registration this tab started').toBeTruthy();
        return (JSON.parse(stored!) as { id: string }).id;
    }
};

export const journeys = [joinByInvitation, invitedOrganizationSetup, selfServiceRegistration];

export const newValues = (): Values => ({ organizationName: newOrganizationName(), firstName: 'Ada', lastName: 'Lovelace' });

/** The wizard step with this header, once it is the one shown - a tab panel labelled by its step header. */
export const stepRegion = (page: Page, step: Step) => page.getByRole('tabpanel', { name: step.header });

/**
 * Opens a journey in a page and waits for its first step.
 * @param page The page.
 * @param journey The journey.
 * @param opened Where the journey starts.
 * @param firstStep The first step.
 * @returns The invitation or registration the host receives the journey's events for.
 */
export const start = async (page: Page, journey: Journey, opened: Opened, firstStep: Step): Promise<string> => {
    await page.goto(opened.url);
    await expect(stepRegion(page, firstStep)).toBeVisible({ timeout: 30_000 });
    return await journey.eventSourceId(page, opened);
};

/**
 * Fills a step's fields and, on a terms step, gives consent - with the mouse and keyboard defaults Playwright uses.
 * @param page The page.
 * @param step The step.
 */
export const fillStep = async (page: Page, step: Step): Promise<void> => {
    const region = stepRegion(page, step);
    await expect(region).toBeVisible();
    for (const field of step.fields) {
        await region.getByRole('textbox', { name: field.label, exact: true }).fill(field.value);
    }

    if (step.consent) {
        await region.getByRole('checkbox').check();
    }
};

/**
 * Moves on from a step: Next on every step but the last, the journey's submit button on the last.
 * @param page The page.
 * @param strings The translations.
 * @param journey The journey.
 * @param isLast Whether this is the last step.
 */
export const advance = async (page: Page, strings: Strings, journey: Journey, isLast: boolean): Promise<void> => {
    const button = page.getByRole('button', { name: isLast ? journey.submitLabel(strings) : strings.components.stepper.next, exact: true });
    await expect(button).toBeEnabled();
    await button.click();
};

/**
 * Completes every step of a wizard, running a check on each step before moving on.
 * @param page The page.
 * @param strings The translations.
 * @param journey The journey.
 * @param steps The steps.
 * @param onStep Runs on each step once it is filled in, before moving on.
 */
export const completeSteps = async (
    page: Page,
    strings: Strings,
    journey: Journey,
    steps: Step[],
    onStep?: (step: Step, index: number) => Promise<void>): Promise<void> => {
    for (const [index, step] of steps.entries()) {
        await fillStep(page, step);
        await onStep?.(step, index);
        await advance(page, strings, journey, index === steps.length - 1);
    }
};

/** Waits until the lobby has handed the person over to the host application. */
export const expectHandedOverToHost = async (page: Page): Promise<void> => {
    await expect(page.getByRole('heading', { name: 'Host application', level: 1 })).toBeVisible({ timeout: 30_000 });
};

/**
 * Expects the host to have received the journey's completion exactly once, and nothing like it again after a while.
 * @param lobby The lobby.
 * @param journey The journey.
 * @param eventSourceId The invitation or registration.
 */
export const expectCompletedOnce = async (lobby: LobbyName, journey: Journey, eventSourceId: string): Promise<void> => {
    const completions = async () => (await received(lobby, eventSourceId)).filter(name => name === journey.completedEvent).length;
    await expect.poll(completions, { message: `${journey.completedEvent} at the host`, timeout: 30_000 }).toBeGreaterThan(0);

    // A duplicate submission would follow within moments of the first; give it time to show up.
    await new Promise(resolve => setTimeout(resolve, 2_000));
    expect(await completions(), `${journey.completedEvent} deliveries to the host`).toBe(1);
};
