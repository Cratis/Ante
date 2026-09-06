// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { LegalDocumentKind } from './LegalDocumentKind';

/**
 * One piece of the acceptance label - either plain text, or the text of a link that opens a document.
 */
export interface LegalLabelSegment {

    /** The text to render. */
    readonly text: string;

    /** The document to open when the segment is activated, or undefined for plain text. */
    readonly kind?: LegalDocumentKind;
}

const placeholders: Record<string, LegalDocumentKind> = {
    [`{${LegalDocumentKind.TermsAndConditions}}`]: LegalDocumentKind.TermsAndConditions,
    [`{${LegalDocumentKind.PrivacyPolicy}}`]: LegalDocumentKind.PrivacyPolicy
};

/**
 * Splits an acceptance label template into renderable segments, turning each document placeholder into a link.
 *
 * The whole sentence stays in one translatable string rather than being concatenated from fragments, so a
 * translation is free to reorder the links, or to word the sentence around them however it needs to.
 * @param template The label template, e.g. `I accept the {termsAndConditions} and the {privacyPolicy}.`
 * @param documentNames The display name to use for each document link.
 * @returns The segments to render, in order.
 */
export const buildLegalAcceptanceLabel = (
    template: string,
    documentNames: Record<LegalDocumentKind, string>): LegalLabelSegment[] =>
    template
        .split(/(\{termsAndConditions\}|\{privacyPolicy\})/)
        .filter(part => part.length > 0)
        .map(part => {
            const kind = placeholders[part];

            return kind === undefined
                ? { text: part }
                : { text: documentNames[kind], kind };
        });
