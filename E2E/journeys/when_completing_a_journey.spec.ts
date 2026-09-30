// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect, test } from '@playwright/test';
import { expectNoSeriousViolations } from '../support/accessibility';
import { LobbyName } from '../support/harness';
import { completeSteps, expectCompletedOnce, expectHandedOverToHost, journeys, newValues, start } from '../support/journeys';
import { english, stepAnnouncement } from '../support/strings';

const lobbies: LobbyName[] = ['plain', 'legal'];

for (const journey of journeys) {
    for (const lobby of lobbies) {
        test(`${journey.name} completes ${lobby === 'legal' ? 'with legal consent' : 'without legal documents'}, accessibly and announced`, async ({ page, context }, testInfo) => {
            const s = english.strings;
            const opened = await journey.open(context, lobby);
            const steps = journey.steps(s, lobby, newValues());
            const eventSourceId = await start(page, journey, opened, steps[0]);

            await completeSteps(page, s, journey, steps, async (step, index) => {
                if (index > 0) {
                    // Moving to a step is announced politely, since the step's own content has no heading to read.
                    await expect(page.getByRole('status').filter({ hasText: stepAnnouncement(s, index + 1, steps.length, step.header) })).toHaveCount(1);
                }

                if (step.consent) {
                    // The documents open in a dialog that takes focus and gives it back to the link that opened it.
                    const link = page.getByRole('button', { name: s.legal.documents.termsAndConditions, exact: true });
                    await link.click();
                    const dialog = page.getByRole('dialog');
                    await expect(dialog).toBeVisible();
                    await expectNoSeriousViolations(page, testInfo, `${step.header} - terms and conditions`);
                    await page.keyboard.press('Escape');
                    await expect(dialog).toBeHidden();
                    await expect(link).toBeFocused();
                }

                await expectNoSeriousViolations(page, testInfo, step.header);
            });

            await expectHandedOverToHost(page);
            await expectCompletedOnce(lobby, journey, eventSourceId);
        });
    }
}
