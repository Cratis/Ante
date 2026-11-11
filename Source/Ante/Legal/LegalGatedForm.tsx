// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ReactNode } from 'react';
import { ProgressSpinner } from '@cratis/components/Display';
import { LegalDocumentsUnavailable } from './LegalDocumentsUnavailable';
import strings from 'Strings';

type LegalGatedFormProps = {
    /** Whether the current legal documents are confirmed; otherwise the form is hidden and disabled. */
    isAvailable: boolean;
    /** Whether the documents are being (re)checked right now. */
    isChecking: boolean;
    /** Re-runs the legal document check. */
    onRetry: () => void;
    /** The legal document viewer dialog, shown only while the documents are available. */
    dialog: ReactNode;
    /** The wizard form. */
    children: ReactNode;
};

/**
 * Hides and disables an onboarding wizard while only previously seen legal documents are known, showing
 * the check in progress or the retry notice above it.
 * @param props The availability state, the retry action, the viewer dialog and the form.
 * @returns The gated form.
 */
export const LegalGatedForm = ({ isAvailable, isChecking, onRetry, dialog, children }: LegalGatedFormProps) => (
    <>
        {!isAvailable && (isChecking
            ? <ProgressSpinner aria-label={strings.onboarding.legalDocumentsChecking} />
            : <LegalDocumentsUnavailable onRetry={onRetry} />)}
        <div hidden={!isAvailable} inert={!isAvailable}>
            {children}
        </div>
        {isAvailable && dialog}
    </>
);
