// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * The minimal surface {@link applyStepperHeaderAria}/{@link applyStepperPanelAria} need from a DOM
 * element. Narrowed down from `Element` so specs can pass a plain fake instead of requiring a real DOM.
 */
export interface AriaElement {
    setAttribute(name: string, value: string): void;
}

/** The paired ids linking one stepper header to the panel it discloses. */
export interface StepperAriaIds {
    readonly headerId: string;
    readonly panelId: string;
}

/**
 * Builds the paired header/panel ids for one step index, stable across re-renders as long as the
 * step order does not change.
 * @param idPrefix A prefix unique to the stepper instance (e.g. the onboarding page name), so ids never collide between the three onboarding journeys.
 * @param index The zero-based step index.
 */
export const buildStepperAriaIds = (idPrefix: string, index: number): StepperAriaIds => ({
    headerId: `${idPrefix}-stepper-header-${index}`,
    panelId: `${idPrefix}-stepper-panel-${index}`
});

/**
 * Applies the ARIA tab role and relationships `Cratis/Components`' `CommandStepper` does not itself
 * render onto a step's header button (see `Cratis/Components#249`, filed alongside this change).
 * @param header The header button element.
 * @param ids The ids pairing this header with its panel.
 * @param selected Whether this step is the one currently showing.
 */
export const applyStepperHeaderAria = (header: AriaElement, ids: StepperAriaIds, selected: boolean): void => {
    header.setAttribute('role', 'tab');
    header.setAttribute('id', ids.headerId);
    header.setAttribute('aria-controls', ids.panelId);
    header.setAttribute('aria-selected', selected ? 'true' : 'false');
};

/**
 * Applies the ARIA tabpanel role, labeling, and a `tabindex` making the panel itself a valid focus
 * target for step-transition focus management (see `useAccessibleStepper.ts`).
 * @param panel The panel element.
 * @param ids The ids pairing this panel with its header.
 */
export const applyStepperPanelAria = (panel: AriaElement, ids: StepperAriaIds): void => {
    panel.setAttribute('role', 'tabpanel');
    panel.setAttribute('id', ids.panelId);
    panel.setAttribute('aria-labelledby', ids.headerId);
    panel.setAttribute('tabindex', '-1');
};
