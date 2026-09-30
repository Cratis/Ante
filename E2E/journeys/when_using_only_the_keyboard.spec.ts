// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect, Page, test } from '@playwright/test';
import { expectNoSeriousViolations } from '../support/accessibility';
import { expectCompletedOnce, expectHandedOverToHost, Journey, journeys, newValues, selfServiceRegistration, start, Step, stepRegion } from '../support/journeys';
import { expectFocusIndicated, tabTo } from '../support/keyboard';
import { english, Strings } from '../support/strings';

/**
 * Completes a wizard with Tab, typing, Space and Enter only, expecting focus to move through each step in reading order -
 * never wrapping back to the top of the page before the step's own controls are done - and to land on each new step.
 */
const completeWithKeyboard = async (page: Page, s: Strings, journey: Journey, steps: Step[]): Promise<void> => {
    for (const [index, step] of steps.entries()) {
        const region = stepRegion(page, step);
        if (index > 0) {
            // Moving on puts focus on the new step, so the next Tab starts inside it.
            await expect(region).toBeFocused();
        }

        const visited: string[] = [];
        let reachedStep = 0;
        for (const field of step.fields) {
            await tabTo(page, region.getByRole('textbox', { name: field.label, exact: true }), visited);
            await page.keyboard.type(field.value);
            reachedStep ||= visited.length;
        }

        if (step.consent) {
            await tabTo(page, region.getByRole('checkbox'), visited);
            reachedStep ||= visited.length;
            await page.keyboard.press('Space');
            await expect(region.getByRole('checkbox')).toBeChecked();
        }

        const isLast = index === steps.length - 1;
        const button = page.getByRole('button', { name: isLast ? journey.submitLabel(s) : s.components.stepper.next, exact: true });
        await expect(button).toBeEnabled();
        await tabTo(page, button, visited);
        // Past the step's first control, Tab must not wrap back to the top of the page before reaching the button.
        expect(visited.slice(reachedStep).filter(stop => stop.includes(s.displayPreferences.trigger)), `focus order on ${step.header}: ${visited.join(', ')}`).toEqual([]);
        await page.keyboard.press('Enter');
    }
};

for (const journey of journeys) {
    test(`${journey.name} can be completed with the keyboard alone, with focus always visible`, async ({ page, context }) => {
        const s = english.strings;
        const opened = await journey.open(context, 'legal');
        const steps = journey.steps(s, 'legal', newValues());
        const eventSourceId = await start(page, journey, opened, steps[0]);

        await completeWithKeyboard(page, s, journey, steps);

        await expectHandedOverToHost(page);
        await expectCompletedOnce('legal', journey, eventSourceId);
    });
}

test('the terms and conditions open, read and close with the keyboard, giving focus back', async ({ page, context }, testInfo) => {
    const s = english.strings;
    const journey = selfServiceRegistration;
    const opened = await journey.open(context, 'legal');
    const steps = journey.steps(s, 'legal', newValues());
    await start(page, journey, opened, steps[0]);
    const terms = steps[steps.length - 1];
    for (const step of steps.slice(0, -1)) {
        const region = stepRegion(page, step);
        for (const field of step.fields) await region.getByRole('textbox', { name: field.label, exact: true }).fill(field.value);
        await page.getByRole('button', { name: s.components.stepper.next, exact: true }).click();
    }

    const link = stepRegion(page, terms).getByRole('button', { name: s.legal.documents.termsAndConditions, exact: true });
    await tabTo(page, link);
    await page.keyboard.press('Enter');
    const dialog = page.getByRole('dialog');
    await expect(dialog).toBeVisible();
    await expect.poll(() => dialog.evaluate(element => element.contains(document.activeElement)), { message: 'focus inside the dialog' }).toBe(true);
    await expectFocusIndicated(page);
    await expectNoSeriousViolations(page, testInfo, 'terms and conditions opened with the keyboard');
    await page.keyboard.press('Escape');
    await expect(dialog).toBeHidden();
    await expect(link).toBeFocused();
});

test('the display preferences open and close with the keyboard, giving focus back', async ({ page, context }, testInfo) => {
    const s = english.strings;
    const journey = selfServiceRegistration;
    const opened = await journey.open(context, 'plain');
    await start(page, journey, opened, journey.steps(s, 'plain', newValues())[0]);

    const trigger = page.getByRole('button', { name: s.displayPreferences.trigger, exact: true });
    await tabTo(page, trigger);
    await page.keyboard.press('Enter');
    const dialog = page.getByRole('dialog', { name: s.displayPreferences.title });
    await expect(dialog).toBeVisible();
    await expect.poll(() => dialog.evaluate(element => element.contains(document.activeElement)), { message: 'focus inside the dialog' }).toBe(true);
    await expectNoSeriousViolations(page, testInfo, 'display preferences');
    await page.keyboard.press('Escape');
    await expect(dialog).toBeHidden();
    await expect(trigger).toBeFocused();
});
