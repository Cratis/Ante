// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useEffect, useRef, useState } from 'react';
import { Current } from './LegalDocuments';
import { legalDocumentsForDisplay } from './legalDocumentAvailability';

/**
 * The Arc query hook may start with a cached response while it revalidates in the background.
 * Never offer that old text for acceptance as if it were the current activated set.
 */
export const useFreshLegalDocuments = () => {
    const [status, perform] = Current.use();
    const performRef = useRef(perform);
    performRef.current = perform;
    const [confirmed, setConfirmed] = useState(false);

    const refresh = useCallback(async () => {
        setConfirmed(false);
        await performRef.current();
        setConfirmed(true);
    }, []);

    useEffect(() => {
        void refresh();
    }, [refresh]);

    const isChecking = !confirmed || status.isPerforming;
    const documents = legalDocumentsForDisplay(confirmed, status);

    return { documents, isChecking, refresh };
};
