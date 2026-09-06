// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { LegalDocumentKind } from '../../LegalDocumentKind';
import { buildLegalAcceptanceLabel, LegalLabelSegment } from '../../legalAcceptanceLabel';

describe('when building the label and the template has no placeholders', () => {
    const documentNames = {
        [LegalDocumentKind.TermsAndConditions]: 'Terms and Conditions',
        [LegalDocumentKind.PrivacyPolicy]: 'Privacy Policy'
    };

    let segments: LegalLabelSegment[];

    beforeEach(() => {
        segments = buildLegalAcceptanceLabel('I accept everything', documentNames);
    });

    it('should produce a single plain segment', () => {
        segments.should.have.lengthOf(1);
    });

    it('should keep the text unchanged', () => {
        segments[0].text.should.equal('I accept everything');
    });

    it('should produce no links', () => {
        segments.filter(segment => segment.kind !== undefined).should.have.lengthOf(0);
    });
});
