// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Button } from '@cratis/components/Common';
import strings from 'Strings';

type LegalDocumentsUnavailableProps = {
    onRetry: () => void;
};

/** Prevents onboarding from silently skipping the legal step while the source is unavailable. */
export const LegalDocumentsUnavailable = ({ onRetry }: LegalDocumentsUnavailableProps) => (
    <div role='status' aria-live='polite'>
        <p>{strings.onboarding.legalDocumentsUnavailable}</p>
        <Button label={strings.onboarding.checkAgain} onClick={onRetry} />
    </div>
);
