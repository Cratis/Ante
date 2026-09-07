// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { RenderRecoveryBoundary } from '../RenderRecoveryBoundary';

const SECRET_DIAGNOSTIC_MESSAGE = 'organizationId=12345 leaked from the failing component';

describe('when rendering after a descendant has already thrown', () => {
    let html: string;

    beforeEach(() => {
        // Constructed directly with the failed state getDerivedStateFromError would have produced -
        // render() itself never sees, and never stores, the thrown error (see RenderRecoveryBoundaryState),
        // so there is nothing for it to leak by construction.
        const boundary = new RenderRecoveryBoundary({ children: React.createElement('p', null, SECRET_DIAGNOSTIC_MESSAGE) });
        boundary.state = { hasFailed: true };
        html = renderToStaticMarkup(boundary.render());
    });

    it('should render the neutral recovery view instead of the original children', () => html.should.include('render-recovery'));

    it('should not render the original children', () => html.should.not.include(SECRET_DIAGNOSTIC_MESSAGE));

    it('should offer a reload action', () => html.should.include('render-recovery__action'));
});
