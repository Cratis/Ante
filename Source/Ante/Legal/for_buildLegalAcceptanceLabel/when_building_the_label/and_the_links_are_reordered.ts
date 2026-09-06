// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { LegalDocumentKind } from '../../LegalDocumentKind';
import { buildLegalAcceptanceLabel, LegalLabelSegment } from '../../legalAcceptanceLabel';

// The whole sentence is one translatable string precisely so a translation can put the links wherever its
// grammar needs them - including in the opposite order.
describe('when building the label and the links are reordered', () => {
    const documentNames = {
        [LegalDocumentKind.TermsAndConditions]: 'Terms and Conditions',
        [LegalDocumentKind.PrivacyPolicy]: 'Privacy Policy'
    };

    let segments: LegalLabelSegment[];

    beforeEach(() => {
        segments = buildLegalAcceptanceLabel(
            '{privacyPolicy} and {termsAndConditions} accepted',
            documentNames);
    });

    it('should follow the order the template uses', () => {
        segments.filter(segment => segment.kind !== undefined).map(segment => segment.kind)
            .should.deep.equal([LegalDocumentKind.PrivacyPolicy, LegalDocumentKind.TermsAndConditions]);
    });

    it('should not produce an empty leading segment', () => {
        segments[0].kind!.should.equal(LegalDocumentKind.PrivacyPolicy);
    });
});
