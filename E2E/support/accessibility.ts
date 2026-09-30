// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import AxeBuilder from '@axe-core/playwright';
import { expect, Page, TestInfo } from '@playwright/test';

/** The WCAG levels the lobby is held to. */
const tags = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'];

/** A serious or critical violation the lobby already has, tracked by an open issue. */
interface KnownViolation {
    rule: string;

    /** The axe target of the element. */
    target: RegExp;

    /** Where the page is, when the element alone is too generic to tell this violation from a new one. */
    where?: RegExp;
    issue: string;
}

/**
 * Violations the lobby already has, each tracked by an open issue. A listed violation is still attached to the test and
 * only stops failing the run; remove its entry together with the fix. Nothing may be added here without an issue.
 * There are none at the moment: the color contrast failures tracked by Cratis/Ante#150 are fixed.
 */
const knownViolations: KnownViolation[] = [];

const trackedBy = (rule: string, target: string, where: string): string | undefined =>
    knownViolations.find(known => known.rule === rule && known.target.test(target) && (!known.where || known.where.test(where)))?.issue;

/**
 * Runs axe on the page as it is now and fails on any serious or critical violation not already tracked.
 * @param page The page.
 * @param testInfo The running test, which gets every violation attached for the report.
 * @param where What the page is showing, for the failure message and the attachment.
 */
export const expectNoSeriousViolations = async (page: Page, testInfo: TestInfo, where: string): Promise<void> => {
    // A dialog fades in, and axe measures whatever is painted at that instant - text and surface part way between the
    // page and their own colors. What people read is the settled page, so let every finite animation finish first.
    await page.evaluate(() => Promise.allSettled(document.getAnimations()
        .filter(animation => animation.effect?.getComputedTiming().iterations !== Infinity)
        .map(animation => animation.finished)));
    const results = await new AxeBuilder({ page }).withTags(tags).analyze();
    // One entry per element, so a new element breaking an already-known rule is still caught.
    const violations = results.violations.flatMap(violation => violation.nodes.map(node => {
        const target = node.target.join(' ');
        return {
            rule: violation.id,
            impact: violation.impact ?? 'unknown',
            help: violation.help,
            target,
            summary: node.failureSummary ?? '',
            trackedBy: trackedBy(violation.id, target, where)
        };
    }));
    if (violations.length > 0) {
        await testInfo.attach(`axe - ${where}`, { body: JSON.stringify(violations, undefined, 2), contentType: 'application/json' });
    }

    const blocking = violations
        .filter(violation => (violation.impact === 'serious' || violation.impact === 'critical') && !violation.trackedBy)
        .map(({ rule, impact, target, summary }) => ({ rule, impact, target, summary }));
    expect(blocking, `serious or critical accessibility violations on ${where}`).toEqual([]);
};
