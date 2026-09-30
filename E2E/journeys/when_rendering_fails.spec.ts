// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect, test } from '@playwright/test';
import { expectNoSeriousViolations } from '../support/accessibility';
import { completeSteps, expectCompletedOnce, expectHandedOverToHost, journeys, newValues, selfServiceRegistration, start, stepRegion } from '../support/journeys';
import { tabTo } from '../support/keyboard';
import { english, locales } from '../support/strings';

const recovered = 'ante-e2e-render-recovered';

/**
 * Breaks number formatting until the tab is marked recovered - the wizard formats the step count as its form appears,
 * so the first render of every journey's form fails underneath the lobby's recovery boundary.
 */
const breakRenderingUntilRecovered = (flag: string) => {
    if (sessionStorage.getItem(flag)) return;
    Intl.NumberFormat = function () {
        throw new Error('E2E: number formatting is broken on purpose.');
    } as unknown as typeof Intl.NumberFormat;
};

for (const locale of locales) {
    for (const journey of journeys) {
        test.describe(() => {
            test.use({ locale: locale.browserLocale });

            test(`${journey.name} recovers from a render failure in ${locale.browserLocale}, with the keyboard`, async ({ page, context }, testInfo) => {
                const s = locale.strings;
                const opened = await journey.open(context, 'plain');
                const steps = journey.steps(s, 'plain', newValues());
                await page.addInitScript(breakRenderingUntilRecovered, recovered);
                await page.goto(opened.url);

                // A neutral, translated notice - never the error itself - with one way forward.
                const notice = page.getByRole('alert').filter({ has: page.getByRole('heading', { name: s.renderRecovery.title }) });
                await expect(notice).toBeVisible({ timeout: 30_000 });
                await expect(notice).toContainText(s.renderRecovery.message);
                await expect(notice).not.toContainText('E2E: number formatting');
                await expectNoSeriousViolations(page, testInfo, `the render recovery notice in ${locale.browserLocale}`);

                const reload = notice.getByRole('button', { name: s.renderRecovery.reload, exact: true });
                await tabTo(page, reload);
                await page.evaluate(flag => sessionStorage.setItem(flag, 'true'), recovered);
                await page.keyboard.press('Enter');

                await expect(stepRegion(page, steps[0])).toBeVisible({ timeout: 30_000 });
                const eventSourceId = await journey.eventSourceId(page, opened);
                await completeSteps(page, s, journey, steps);
                await expectHandedOverToHost(page);
                await expectCompletedOnce('plain', journey, eventSourceId);
            });
        });
    }
}

test('a registration reloaded out of a render failure resumes the same registration, not a new one', async ({ page, context }) => {
    const journey = selfServiceRegistration;
    const s = english.strings;
    const opened = await journey.open(context, 'plain');
    const steps = journey.steps(s, 'plain', newValues());
    const before = await start(page, journey, opened, steps[0]);
    await page.addInitScript(breakRenderingUntilRecovered, recovered);
    await page.reload();
    await expect(page.getByRole('heading', { name: s.renderRecovery.title })).toBeVisible({ timeout: 30_000 });

    await page.evaluate(flag => sessionStorage.setItem(flag, 'true'), recovered);
    await page.getByRole('button', { name: s.renderRecovery.reload, exact: true }).click();
    await expect(stepRegion(page, steps[0])).toBeVisible({ timeout: 30_000 });
    expect(await journey.eventSourceId(page, opened)).toEqual(before);
});
