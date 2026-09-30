// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { QueryResultWithState } from '@cratis/arc/queries';
import { legalDocumentsForDisplay } from '../legalDocumentAvailability';
import { LegalDocumentStatus } from '../LegalDocuments';

const active = { version: '2026-02', isUnavailable: false };
const result = { data: active, hasData: true, isSuccess: true, isPerforming: false };

describe('when deciding whether to show legal documents as current', () => {
    it('should not display a cached result before a fresh read finishes', () => {
        (legalDocumentsForDisplay(false, result) === undefined).should.be.true;
    });

    it('should not display the default value Arc starts the query with', () => {
        (legalDocumentsForDisplay(true, QueryResultWithState.initial<LegalDocumentStatus>({} as LegalDocumentStatus)) === undefined).should.be.true;
    });

    it('should not display old text while a refresh is in flight', () => {
        (legalDocumentsForDisplay(true, { ...result, isPerforming: true }) === undefined).should.be.true;
    });

    it('should not display an unavailable set as current', () => {
        (legalDocumentsForDisplay(true, { ...result, data: { ...active, isUnavailable: true } }) === undefined).should.be.true;
    });

    it('should show only the freshly confirmed activated set', () => {
        (legalDocumentsForDisplay(true, result)?.version === '2026-02').should.be.true;
    });
});
