// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Builds the polite live-region announcement for a stepper step transition, e.g.
 * "Step 2 of 3: Your Information".
 *
 * The whole sentence stays in one translatable template (`{current}`/`{total}`/`{label}`
 * placeholders) rather than being concatenated from fragments, so a translation is free to reorder
 * or reword it - the same reasoning `legalAcceptanceLabel.ts` applies to the acceptance sentence.
 * @param template The announcement template, e.g. `Step {current} of {total}: {label}`.
 * @param current The one-based index of the step now showing.
 * @param total The total number of steps.
 * @param label The step's own header text.
 * @returns The announcement text to place in the live region.
 */
export const formatStepAnnouncement = (template: string, current: number, total: number, label: string): string =>
    template
        .replace('{current}', String(current))
        .replace('{total}', String(total))
        .replace('{label}', label);
