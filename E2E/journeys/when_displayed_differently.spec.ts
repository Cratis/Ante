// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect, Page, test } from '@playwright/test';
import { expectNoSeriousViolations } from '../support/accessibility';
import { completeSteps, expectCompletedOnce, expectHandedOverToHost, journeys, newValues, start } from '../support/journeys';
import { tabTo } from '../support/keyboard';
import { english } from '../support/strings';

/** Expects nothing on the page to need scrolling sideways - content reflows instead of spilling out. */
const expectNoHorizontalScrolling = async (page: Page, where: string): Promise<void> => {
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
    expect(overflow, `horizontal overflow on ${where}`).toBeLessThanOrEqual(0);
};

/** Animations and transitions still running for longer than a blink. */
const longAnimations = (page: Page): Promise<string[]> => page.evaluate(() => document.getAnimations()
    .filter(animation => {
        const duration = Number(animation.effect?.getComputedTiming().duration ?? 0);
        return animation.playState === 'running' && duration > 100;
    })
    .map(animation => `${(animation as CSSAnimation).animationName ?? (animation as CSSTransition).transitionProperty} on ${(animation.effect as KeyframeEffect | null)?.target?.className ?? '?'}`));

const viewports = [
    // 1280 CSS pixels at 200% zoom.
    { name: '200% zoom', viewport: { width: 640, height: 800 } },
    // WCAG reflow: 1280 CSS pixels at 400% zoom, or a small phone.
    { name: 'a 320 pixel wide screen', viewport: { width: 320, height: 640 } }
];

for (const journey of journeys) {
    for (const { name, viewport } of viewports) {
        test.describe(() => {
            test.use({ viewport });

            test(`${journey.name} reflows at ${name} without scrolling sideways`, async ({ page, context }, testInfo) => {
                const s = english.strings;
                const opened = await journey.open(context, 'legal');
                const steps = journey.steps(s, 'legal', newValues());
                const eventSourceId = await start(page, journey, opened, steps[0]);

                await completeSteps(page, s, journey, steps, async step => {
                    await expectNoHorizontalScrolling(page, step.header);
                    await expectNoSeriousViolations(page, testInfo, `${step.header} at ${name}`);
                });

                await expectHandedOverToHost(page);
                await expectCompletedOnce('legal', journey, eventSourceId);
            });
        });
    }

    test.describe(() => {
        test.use({ reducedMotion: 'reduce' });

        test(`${journey.name} does not animate when reduced motion is requested`, async ({ page, context }) => {
            const s = english.strings;
            const opened = await journey.open(context, 'legal');
            const steps = journey.steps(s, 'legal', newValues());
            const eventSourceId = await start(page, journey, opened, steps[0]);

            await completeSteps(page, s, journey, steps, async step => {
                expect(await longAnimations(page), `animations on ${step.header}`).toEqual([]);
            });

            await expectHandedOverToHost(page);
            await expectCompletedOnce('legal', journey, eventSourceId);
        });
    });

    test.describe(() => {
        test.use({ forcedColors: 'active', contrast: 'more' });

        test(`${journey.name} stays usable with forced colors and high contrast, focus included`, async ({ page, context }, testInfo) => {
            const s = english.strings;
            const opened = await journey.open(context, 'legal');
            const steps = journey.steps(s, 'legal', newValues());
            const eventSourceId = await start(page, journey, opened, steps[0]);

            await completeSteps(page, s, journey, steps, async step => {
                // Forced colors drop box shadows, so the focus indicator has to survive as an outline; tabTo checks every stop.
                const isLast = steps.indexOf(step) === steps.length - 1;
                await tabTo(page, page.getByRole('button', { name: isLast ? journey.submitLabel(s) : s.components.stepper.next, exact: true }));
                await expectNoSeriousViolations(page, testInfo, `${step.header} with forced colors`);
            });

            await expectHandedOverToHost(page);
            await expectCompletedOnce('legal', journey, eventSourceId);
        });
    });
}
