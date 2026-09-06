// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogButtons } from '@cratis/arc.react/dialogs';
import { Dialog } from '@cratis/components/Dialogs';
import MarkdownPreview from '@uiw/react-markdown-preview';
import { LegalDocumentKind } from './LegalDocumentKind';
import strings from 'Strings';
// The markdown preview styles restore list bullets/numbering and heading rhythm that a global CSS
// reset would otherwise strip - without them the document renders as a wall of unindented lines.
import '@uiw/react-markdown-preview/markdown.css';
import './LegalDocumentDialog.css';

interface LegalDocumentDialogProps {

    /** The document to show. */
    kind: LegalDocumentKind;

    /** The document's markdown body, as returned by the `LegalDocuments.Current` query. */
    body: string;

    /** Called when the dialog is dismissed. */
    onClose: () => void;
}

/**
 * Shows one of the legal documents, rendered from the markdown the `LegalDocuments.Current` query
 * already returned - there is no separate fetch, since the wizard only offers this dialog once that
 * query has resolved and the terms step is showing.
 */
export const LegalDocumentDialog = ({ kind, body, onClose }: LegalDocumentDialogProps) => {
    const legalStrings = strings.legal;

    return (
        <Dialog
            visible
            title={legalStrings.documents[kind]}
            width='48rem'
            buttons={DialogButtons.Ok}
            okLabel={legalStrings.close}
            onConfirm={onClose}
            onCancel={onClose}
        >
            <div className='legal-document' data-color-mode='dark'>
                <MarkdownPreview source={body} />
            </div>
        </Dialog>
    );
};
