// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect, test } from '@playwright/test';
import { expectNoSeriousViolations } from '../support/accessibility';
import { completeSteps, expectCompletedOnce, expectHandedOverToHost, fillStep, journeys, newValues, selfServiceRegistration, start, stepRegion } from '../support/journeys';
import { bokmal, english, locales, stepAnnouncement } from '../support/strings';

for (const locale of locales) {
    for (const journey of journeys) {
        test.describe(() => {
            test.use({ locale: locale.browserLocale });

            test(`${journey.name} is presented and announced in ${locale.browserLocale} when the browser asks for it`, async ({ page, context }, testInfo) => {
                const s = locale.strings;
                const opened = await journey.open(context, 'legal');
                const steps = journey.steps(s, 'legal', newValues());
                const eventSourceId = await start(page, journey, opened, steps[0]);
                await expect(page.locator('html')).toHaveAttribute('lang', locale.documentLanguage);

                await completeSteps(page, s, journey, steps, async (step, index) => {
                    await expect(page.getByRole('status').filter({ hasText: stepAnnouncement(s, index + 1, steps.length, step.header) })).toHaveCount(1);
                    await expectNoSeriousViolations(page, testInfo, `${step.header} in ${locale.browserLocale}`);
                });

                await expectHandedOverToHost(page);
                await expectCompletedOnce('legal', journey, eventSourceId);
            });
        });
    }
}

test('switching language part way through keeps what was entered and changes the whole page', async ({ page, context }, testInfo) => {
    const journey = selfServiceRegistration;
    const opened = await journey.open(context, 'legal');
    const values = newValues();
    const englishSteps = journey.steps(english.strings, 'legal', values);
    const bokmalSteps = journey.steps(bokmal.strings, 'legal', values);
    const eventSourceId = await start(page, journey, opened, englishSteps[0]);
    await fillStep(page, englishSteps[0]);

    await page.getByRole('button', { name: english.strings.displayPreferences.trigger, exact: true }).click();
    await page.getByRole('radio', { name: english.strings.displayPreferences.bokmal, exact: true }).check();
    await page.getByRole('button', { name: bokmal.strings.displayPreferences.close, exact: true }).click();

    await expect(page.locator('html')).toHaveAttribute('lang', bokmal.documentLanguage);
    const name = stepRegion(page, bokmalSteps[0]).getByRole('textbox', { name: bokmalSteps[0].fields[0].label, exact: true });
    await expect(name).toHaveValue(values.organizationName);
    await expectNoSeriousViolations(page, testInfo, 'registration after switching to Bokmål');

    await completeSteps(page, bokmal.strings, journey, bokmalSteps);
    await expectHandedOverToHost(page);
    await expectCompletedOnce('legal', journey, eventSourceId);
});
