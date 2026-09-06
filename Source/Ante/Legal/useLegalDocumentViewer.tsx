// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ReactNode, useState } from 'react';
import { LegalDocumentKind } from './LegalDocumentKind';
import { LegalDocumentDialog } from './LegalDocumentDialog';

/** The markdown bodies of the legal document set, as returned by the `LegalDocuments.Current` query. */
export interface LegalDocumentBodies {
    termsAndConditions: string;
    privacyPolicy: string;
}

export interface LegalDocumentViewer {

    /** Opens the given legal document in a dialog. Pass directly as `LegalAcceptanceField`'s `onShowDocument`. */
    showDocument: (kind: LegalDocumentKind) => void;

    /** The currently open document's dialog, or `null` when none is open. Render once, anywhere on the page. */
    dialog: ReactNode;
}

/**
 * Owns which legal document (terms, privacy policy) is currently open in a dialog for a `LegalAcceptanceField`
 * checkbox's inline links.
 *
 * `LegalAcceptanceField` must be rendered as a direct JSX child of its `StepperPanel` (or `CommandForm`) -
 * `CommandStepper`/`CommandForm` recognize a command-bound field by walking the JSX children they were given
 * as authored, and can only see fields written directly in that tree, not ones rendered from inside another
 * component's own render body. Wrapping both the field and this dialog state in a single step wrapper
 * component would hide the field from that walk: the checkbox would render but never bind to the command,
 * and submission would always fail server-side validation since `acceptedLegalTerms` never left its initial
 * `false`. This hook keeps the shared dialog-opening logic in one place without introducing that indirection -
 * render `dialog` anywhere on the page; it needs no special position relative to the field.
 * @param documents The markdown bodies to show, already loaded by the caller from `LegalDocuments.Current`.
 */
export const useLegalDocumentViewer = (documents: LegalDocumentBodies): LegalDocumentViewer => {
    const [openDocument, setOpenDocument] = useState<LegalDocumentKind | undefined>(undefined);

    return {
        showDocument: setOpenDocument,
        dialog: openDocument !== undefined
            ? <LegalDocumentDialog kind={openDocument} body={documents[openDocument]} onClose={() => setOpenDocument(undefined)} />
            : null
    };
};
