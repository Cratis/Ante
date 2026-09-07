// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { RenderRecoveryBoundary } from '../RenderRecoveryBoundary';

describe('when nothing beneath the boundary has failed', () => {
    const html = renderToStaticMarkup(
        React.createElement(RenderRecoveryBoundary, null, React.createElement('p', null, 'protected content')));

    it('should render the children unchanged', () => html.should.include('protected content'));

    it('should not render the recovery view', () => html.should.not.include('render-recovery'));
});
