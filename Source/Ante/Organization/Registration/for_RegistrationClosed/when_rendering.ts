// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import strings from 'Strings';
import { RegistrationClosed } from '../RegistrationClosed';

describe('when rendering closed registration', () => {
    it('should say sign-up is not available', () =>
        renderToStaticMarkup(React.createElement(RegistrationClosed, { closedUrl: '' })).should.include(strings.registration.closed));

    it('should not render a link when none is configured', () =>
        renderToStaticMarkup(React.createElement(RegistrationClosed, { closedUrl: '' })).should.not.include('<a'));

    it('should link to the configured page', () =>
        renderToStaticMarkup(React.createElement(RegistrationClosed, { closedUrl: 'https://cratis.studio/waitlist' }))
            .should.include('href="https://cratis.studio/waitlist"'));
});
