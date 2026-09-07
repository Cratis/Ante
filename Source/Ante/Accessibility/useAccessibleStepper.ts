// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { RefObject, useEffect, useState } from 'react';
import { useLocale } from '../Locale/LocaleContext';
import { applyStepperHeaderAria, applyStepperPanelAria, buildStepperAriaIds } from './stepperAria';
import { formatStepAnnouncement } from './stepAnnouncement';
import { pickCurrentStepIndex } from './stepperStepIndex';

const HEADER_SELECTOR = '[data-cratis-part="header"]';
const PANEL_SELECTOR = '[data-cratis-part="panel"]';
const LIST_SELECTOR = '[data-cratis-part="list"]';

export interface UseAccessibleStepperOptions {
    /** A prefix unique to this stepper instance, so generated ids never collide with another stepper on the page. */
    idPrefix: string;

    /** The live-region announcement template, e.g. `Step {current} of {total}: {label}`. */
    announcementTemplate: string;
}

/**
 * Retrofits ARIA tab/tabpanel semantics and step-transition focus onto a `CommandStepper`
 * (`@cratis/components`), which renders a plain `<ol>`/`<button>`/`<section>` structure without
 * `role`, `aria-selected`, or `aria-controls`, and only reports step changes driven by a header
 * click - never Next/Previous, the primary way people actually move through a linear wizard (see
 * `stepperStepIndex.ts`). Filed upstream as `Cratis/Components#249`; this hook is the app-level
 * workaround until it is addressed there.
 *
 * Observes each panel's `hidden` attribute (the one signal `CommandStepperContent` keeps accurate
 * regardless of navigation method), and on every step change: re-marks which header is
 * `aria-selected`, moves focus onto the new panel (labeled via `aria-labelledby`, so a screen reader
 * announces the step name the moment focus lands), and updates a live-region announcement for
 * `Cratis/Ante#20`'s "meaningful step-transition focus" and "polite progress announcements".
 * @param containerRef A ref on the element wrapping the `<CommandStepper>` (and nothing else that would match the selectors above).
 * @param options Id prefix and announcement template.
 * @returns The current announcement text - render it inside a `LiveRegion`.
 */
export const useAccessibleStepper = (
    containerRef: RefObject<HTMLElement | null>,
    options: UseAccessibleStepperOptions
): { announcement: string } => {
    const [announcement, setAnnouncement] = useState('');
    const locale = useLocale();

    useEffect(() => {
        const container = containerRef.current;
        if (!container) return;

        const headers = Array.from(container.querySelectorAll<HTMLElement>(HEADER_SELECTOR));
        const panels = Array.from(container.querySelectorAll<HTMLElement>(PANEL_SELECTOR));
        if (panels.length === 0) return;

        container.querySelector<HTMLElement>(LIST_SELECTOR)?.setAttribute('role', 'tablist');

        const ids = panels.map((_, index) => buildStepperAriaIds(options.idPrefix, index));
        panels.forEach((panel, index) => applyStepperPanelAria(panel, ids[index]));

        const applyCurrentStep = (moveFocus: boolean) => {
            const currentIndex = pickCurrentStepIndex(panels.map(panel => panel.hidden === true));
            headers.forEach((header, index) => applyStepperHeaderAria(header, ids[index], index === currentIndex));

            const currentPanel = panels[currentIndex];
            const label = headers[currentIndex]?.textContent?.trim() ?? '';
            setAnnouncement(formatStepAnnouncement(options.announcementTemplate, currentIndex + 1, panels.length, label, locale));

            // Only move focus on an actual transition - not on first mount, which would steal focus
            // away from wherever the page (or the browser) already put it on load.
            if (moveFocus) currentPanel.focus();
        };

        applyCurrentStep(false);

        const observer = new MutationObserver(() => applyCurrentStep(true));
        panels.forEach(panel => observer.observe(panel, { attributes: true, attributeFilter: ['hidden'] }));

        return () => observer.disconnect();
    }, [containerRef, options.idPrefix, options.announcementTemplate, locale]);

    return { announcement };
};
