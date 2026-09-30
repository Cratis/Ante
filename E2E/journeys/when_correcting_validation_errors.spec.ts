// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect, test } from '@playwright/test';
import { expectNoSeriousViolations } from '../support/accessibility';
import { advance, completeSteps, expectCompletedOnce, expectHandedOverToHost, fillStep, joinByInvitation, journeys, newValues, start, stepRegion } from '../support/journeys';
import { english } from '../support/strings';

// A text-direction override: allowed by the form's own checks, refused by the server's name rules.
const directionOverride = '\u202E';

for (const journey of journeys) {
    test(`${journey.name} shows a missing required field as an error that clears once it is filled in`, async ({ page, context }, testInfo) => {
        const s = english.strings;
        const opened = await journey.open(context, 'plain');
        const steps = journey.steps(s, 'plain', newValues());
        await start(page, journey, opened, steps[0]);

        const field = stepRegion(page, steps[0]).getByRole('textbox', { name: steps[0].fields[0].label, exact: true });
        const next = page.getByRole('button', { name: steps.length > 1 ? s.components.stepper.next : journey.submitLabel(s), exact: true });
        await field.fill('x');
        await field.fill('');
        const required = s.nameValidation.required.replace('{field}', steps[0].fields[0].label);
        await expect(field).toHaveAttribute('aria-invalid', 'true');
        await expect(stepRegion(page, steps[0]).getByText(required).first()).toBeVisible();
        // The field is described by its own error element; the message drawn under it is for the eye only, so a
        // screen reader does not come across it a second time.
        await expect(field).toHaveAccessibleDescription(new RegExp(required.replace('.', '\\.')));
        await expect(stepRegion(page, steps[0]).locator('small[aria-hidden="true"]').filter({ hasText: required })).toHaveCount(1);
        await expect(next).toBeDisabled();
        await expectNoSeriousViolations(page, testInfo, `${steps[0].header} with a missing field`);

        await field.fill(steps[0].fields[0].value);
        await expect(field).not.toHaveAttribute('aria-invalid', 'true');
        await expect(stepRegion(page, steps[0]).getByText(required)).toHaveCount(0);
    });
}

for (const journey of journeys.filter(journey => journey.namesOrganization)) {
    test(`${journey.name} refuses an organization name the host cannot use until it is corrected`, async ({ page, context }, testInfo) => {
        const s = english.strings;
        const opened = await journey.open(context, 'plain');
        const values = newValues();
        const steps = journey.steps(s, 'plain', values);
        const eventSourceId = await start(page, journey, opened, steps[0]);

        const name = stepRegion(page, steps[0]).getByRole('textbox', { name: steps[0].fields[0].label, exact: true });
        await name.fill(`${values.organizationName} Corp`);

        // The server's verdict is shown next to the field and announced, and holds the wizard on this step.
        const refusal = /cannot contain spaces/;
        await expect(stepRegion(page, steps[0]).getByText(refusal).first()).toBeVisible();
        await expect(page.getByRole('status').filter({ hasText: refusal })).toHaveCount(1);
        await expect(page.getByRole('button', { name: s.components.stepper.next, exact: true })).toBeDisabled();
        await expectNoSeriousViolations(page, testInfo, `${steps[0].header} with a refused name`);

        await name.fill(values.organizationName);
        await expect(stepRegion(page, steps[0]).getByText(refusal)).toHaveCount(0);
        await advance(page, s, journey, false);
        await completeSteps(page, s, journey, steps.slice(1));

        await expectHandedOverToHost(page);
        await expectCompletedOnce('plain', journey, eventSourceId);
    });
}

test(`${joinByInvitation.name} keeps what was entered when the server refuses a name, and succeeds once it is corrected`, async ({ page, context }, testInfo) => {
    const s = english.strings;
    const journey = joinByInvitation;
    const opened = await journey.open(context, 'plain');
    const values = newValues();
    const steps = journey.steps(s, 'plain', { ...values, lastName: `${values.lastName}${directionOverride}` });
    const eventSourceId = await start(page, journey, opened, steps[0]);

    await fillStep(page, steps[0]);
    await advance(page, s, journey, true);

    // The refusal is summarised in an alert that takes focus, above the form that still holds every value.
    // The summary is the one alert: its messages are plain text, not alerts nested inside it.
    const summary = page.getByRole('alert').filter({ hasText: /control or text-direction-override/ });
    await expect(summary).toHaveCount(1);
    await expect(summary).toBeVisible();
    await expect(summary).toBeFocused();
    const firstName = stepRegion(page, steps[0]).getByRole('textbox', { name: s.userSetup.firstName, exact: true });
    const lastName = stepRegion(page, steps[0]).getByRole('textbox', { name: s.userSetup.lastName, exact: true });
    await expect(firstName).toHaveValue(values.firstName);
    await expectNoSeriousViolations(page, testInfo, 'the join form refused by the server');

    await lastName.fill(values.lastName);
    await expect(summary).toBeHidden();

    // The server's verdict on the field is rechecked when the person leaves it.
    await page.keyboard.press('Tab');
    await advance(page, s, journey, true);

    await expectHandedOverToHost(page);
    await expectCompletedOnce('plain', journey, eventSourceId);
});
