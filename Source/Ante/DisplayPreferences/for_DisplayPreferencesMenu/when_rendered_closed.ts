// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { DisplayPreferencesMenu } from '../DisplayPreferencesMenu';

describe('when the display preferences menu is rendered closed', () => {
    const html = renderToStaticMarkup(React.createElement(DisplayPreferencesMenu));

    it('should render a trigger with a required accessible name for the icon-only action', () =>
        html.should.include('aria-label="Display preferences"'));

    it('should not render the preferences panel until opened', () => html.should.not.include('display-preferences-menu__content'));
});
