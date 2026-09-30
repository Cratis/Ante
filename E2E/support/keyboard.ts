// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect, Locator, Page } from '@playwright/test';

/** What has keyboard focus: a readable description and whether a focus indicator is painted. */
interface Focused {
    description: string;
    indicated: boolean;
}

/**
 * Describes the focused element and whether it shows a focus indicator - an outline or a box shadow on the element
 * itself, or on the parent or following sibling a custom control (a styled checkbox) paints its indicator on.
 * @param page The page.
 * @returns The focused element, or undefined when nothing but the document has focus.
 */
const focused = (page: Page): Promise<Focused | undefined> => page.evaluate(() => {
    const element = document.activeElement as HTMLElement | null;
    if (!element || element === document.body) return undefined;

    const paintsIndicator = (candidate: Element | null) => {
        if (!candidate) return false;
        const style = getComputedStyle(candidate);
        const outline = style.outlineStyle !== 'none' && parseFloat(style.outlineWidth) > 0;
        const shadow = Boolean(style.boxShadow) && style.boxShadow !== 'none';
        return outline || shadow;
    };

    const labelledBy = element.getAttribute('aria-labelledby')?.split(' ')
        .map(id => document.getElementById(id)?.textContent ?? '').join(' ');
    const label = element.getAttribute('aria-label') ?? labelledBy ?? (element as HTMLInputElement).labels?.[0]?.textContent ?? element.textContent ?? '';
    return {
        description: `${element.getAttribute('role') ?? element.tagName.toLowerCase()} "${label.trim().replace(/\s+/g, ' ').slice(0, 80)}"`,
        indicated: paintsIndicator(element) || paintsIndicator(element.parentElement) || paintsIndicator(element.nextElementSibling)
    };
});

/**
 * Presses Tab until the target has focus, expecting every stop on the way to show where focus is.
 * @param page The page.
 * @param target What to reach.
 * @param visited Collects the description of every stop, in order.
 * @param maximum The most Tab presses to allow.
 */
export const tabTo = async (page: Page, target: Locator, visited: string[] = [], maximum = 30): Promise<void> => {
    for (let presses = 0; presses < maximum; presses++) {
        if (await target.evaluate(element => element === document.activeElement)) return;
        await page.keyboard.press('Tab');
        const now = await focused(page);
        if (now) {
            visited.push(now.description);
            expect(now.indicated, `a visible focus indicator on ${now.description}`).toBe(true);
        }
    }

    await expect(target, `reaching it with Tab (visited: ${visited.join(', ')})`).toBeFocused();
};

/**
 * Expects the element with focus right now to show a focus indicator.
 * @param page The page.
 */
export const expectFocusIndicated = async (page: Page): Promise<void> => {
    const now = await focused(page);
    expect(now, 'an element with focus').toBeDefined();
    expect(now!.indicated, `a visible focus indicator on ${now!.description}`).toBe(true);
};
