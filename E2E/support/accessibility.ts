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
 */
const knownViolations: KnownViolation[] = [
    { rule: 'color-contrast', target: /cratis-command-stepper__title/, issue: 'Cratis/Ante#150' },
    // Field validation messages (`p-error`); axe names them by position or, alone on a form, just `small`.
    { rule: 'color-contrast', target: /^(.*\.w-full( > |:nth-child\(\d+\) > ))?small$/, issue: 'Cratis/Ante#150' },
    { rule: 'color-contrast', target: /^#react-aria\d+-_r_\w+_$|\.cratis-dialog__button > span$/, where: /terms and conditions/, issue: 'Cratis/Ante#150' },
    { rule: 'color-contrast', target: /\.display-preferences-menu__description$/, issue: 'Cratis/Ante#150' },
    { rule: 'color-contrast', target: /^button$/, where: /render recovery/, issue: 'Cratis/Ante#150' },
    { rule: 'color-contrast', target: /^p$|legal-acceptance__link|cratis-primereact-button__label/, where: /forced colors/, issue: 'Cratis/Ante#150' }
];

const trackedBy = (rule: string, target: string, where: string): string | undefined =>
    knownViolations.find(known => known.rule === rule && known.target.test(target) && (!known.where || known.where.test(where)))?.issue;

/**
 * Runs axe on the page as it is now and fails on any serious or critical violation not already tracked.
 * @param page The page.
 * @param testInfo The running test, which gets every violation attached for the report.
 * @param where What the page is showing, for the failure message and the attachment.
 */
export const expectNoSeriousViolations = async (page: Page, testInfo: TestInfo, where: string): Promise<void> => {
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
