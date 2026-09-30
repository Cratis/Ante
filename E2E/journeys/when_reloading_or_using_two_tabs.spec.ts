// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect, test } from '@playwright/test';
import { received } from '../support/harness';
import {
    advance, completeSteps, expectCompletedOnce, expectHandedOverToHost, fillStep, invitedOrganizationSetup, joinByInvitation, journeys,
    newValues, selfServiceRegistration, start, stepRegion
} from '../support/journeys';
import { english } from '../support/strings';

for (const journey of journeys) {
    test(`${journey.name} starts over safely when reloaded part way through`, async ({ page, context }) => {
        const s = english.strings;
        const opened = await journey.open(context, 'legal');
        const steps = journey.steps(s, 'legal', newValues());
        const eventSourceId = await start(page, journey, opened, steps[0]);

        await fillStep(page, steps[0]);
        await advance(page, s, journey, false);
        await expect(stepRegion(page, steps[1])).toBeVisible();
        await page.reload();

        // Nothing was submitted, so the wizard is back at its first step and the host has heard nothing.
        await expect(stepRegion(page, steps[0])).toBeVisible();
        expect(await received('legal', eventSourceId)).not.toContain(journey.completedEvent);
        await completeSteps(page, s, journey, steps);

        await expectHandedOverToHost(page);
        await expectCompletedOnce('legal', journey, eventSourceId);
    });

    test(`${journey.name} is not submitted twice when reloaded right after submitting`, async ({ page, context }) => {
        const s = english.strings;
        const opened = await journey.open(context, 'plain');
        const steps = journey.steps(s, 'plain', newValues());
        const eventSourceId = await start(page, journey, opened, steps[0]);

        const submitted = page.waitForResponse(response => response.request().method() === 'POST' && response.ok() &&
            !response.url().includes('/validate') && !response.url().includes('/start'));
        await completeSteps(page, s, journey, steps);
        await submitted;
        await page.reload();

        // The durable status tells the reloaded page the work is done; it never offers the form again.
        await expectHandedOverToHost(page);
        await expectCompletedOnce('plain', journey, eventSourceId);
    });
}

for (const journey of [joinByInvitation, invitedOrganizationSetup]) {
    test(`${journey.name} finishes in a second tab too when the first tab submits, without a second submission`, async ({ page, context }) => {
        const s = english.strings;
        const opened = await journey.open(context, 'plain');
        const steps = journey.steps(s, 'plain', newValues());
        const eventSourceId = await start(page, journey, opened, steps[0]);
        const second = await context.newPage();
        await start(second, journey, opened, steps[0]);
        await fillStep(second, steps[0]);

        await completeSteps(page, s, journey, steps);

        // The second tab learns of the success from the live status and hands over as well - a late success, not a retry.
        await expectHandedOverToHost(page);
        await expectHandedOverToHost(second);
        await expectCompletedOnce('plain', journey, eventSourceId);
    });
}

test(`${selfServiceRegistration.name} in two tabs registers two organizations and refuses the second tab the first one's name`, async ({ page, context }) => {
    const s = english.strings;
    const journey = selfServiceRegistration;
    const opened = await journey.open(context, 'plain');
    const first = newValues();
    const firstSteps = journey.steps(s, 'plain', first);
    const firstRegistration = await start(page, journey, opened, firstSteps[0]);
    const second = await context.newPage();
    const secondValues = newValues();
    const secondSteps = journey.steps(s, 'plain', secondValues);
    const secondRegistration = await start(second, journey, opened, secondSteps[0]);

    // Each tab keeps its own registration, so two tabs are two registrations - never one registration sent twice.
    expect(secondRegistration).not.toEqual(firstRegistration);
    await completeSteps(page, s, journey, firstSteps);
    await expectHandedOverToHost(page);

    const name = stepRegion(second, secondSteps[0]).getByRole('textbox', { name: secondSteps[0].fields[0].label, exact: true });
    await name.fill(first.organizationName);
    await expect(stepRegion(second, secondSteps[0]).getByText(/already in use|already exists/).first()).toBeVisible();
    await expect(second.getByRole('button', { name: s.components.stepper.next, exact: true })).toBeDisabled();

    await completeSteps(second, s, journey, secondSteps);
    await expectHandedOverToHost(second);
    await expectCompletedOnce('plain', journey, firstRegistration);
    await expectCompletedOnce('plain', journey, secondRegistration);
});
