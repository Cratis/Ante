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
    const lastAvailableDocuments = useRef<typeof status.data | undefined>(undefined);
    const refreshGeneration = useRef(0);

    const refresh = useCallback(async () => {
        const generation = ++refreshGeneration.current;
        setConfirmed(false);
        await performRef.current();
        if (generation === refreshGeneration.current) setConfirmed(true);
    }, []);

    useEffect(() => {
        void refresh();
        return () => { refreshGeneration.current++; };
    }, [refresh]);

    const isChecking = !confirmed || status.isPerforming;
    const documents = legalDocumentsForDisplay(confirmed, status);
    if (documents) lastAvailableDocuments.current = documents;

    return { documents, lastAvailableDocuments: lastAvailableDocuments.current, isChecking, refresh };
};
