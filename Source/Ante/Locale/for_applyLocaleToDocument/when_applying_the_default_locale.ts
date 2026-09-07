// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { applyLocaleToDocument } from '../applyLocaleToDocument';
import { FakeAttributeTarget } from './FakeAttributeTarget';

describe('when applying the default locale to the document', () => {
    let target: FakeAttributeTarget;

    beforeEach(() => {
        target = new FakeAttributeTarget();
        applyLocaleToDocument(target, 'en');
    });

    it('should set the language tag', () => target.get('lang')!.should.equal('en-US'));

    it('should set left-to-right text direction', () => target.get('dir')!.should.equal('ltr'));
});
