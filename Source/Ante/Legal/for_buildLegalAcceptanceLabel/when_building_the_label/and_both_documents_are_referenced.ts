// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { LegalDocumentKind } from '../../LegalDocumentKind';
import { buildLegalAcceptanceLabel, LegalLabelSegment } from '../../legalAcceptanceLabel';

describe('when building the label and both documents are referenced', () => {
    const documentNames = {
        [LegalDocumentKind.TermsAndConditions]: 'Terms and Conditions',
        [LegalDocumentKind.PrivacyPolicy]: 'Privacy Policy'
    };

    let segments: LegalLabelSegment[];

    beforeEach(() => {
        segments = buildLegalAcceptanceLabel(
            'I accept the {termsAndConditions} and the {privacyPolicy}.',
            documentNames);
    });

    it('should keep the surrounding text as plain segments', () => {
        segments.filter(segment => segment.kind === undefined).map(segment => segment.text)
            .should.deep.equal(['I accept the ', ' and the ', '.']);
    });

    it('should turn the terms placeholder into a link segment', () => {
        segments[1].kind!.should.equal(LegalDocumentKind.TermsAndConditions);
    });

    it('should turn the privacy placeholder into a link segment', () => {
        segments[3].kind!.should.equal(LegalDocumentKind.PrivacyPolicy);
    });

    it('should render each link with its display name', () => {
        segments.filter(segment => segment.kind !== undefined).map(segment => segment.text)
            .should.deep.equal(['Terms and Conditions', 'Privacy Policy']);
    });
});
