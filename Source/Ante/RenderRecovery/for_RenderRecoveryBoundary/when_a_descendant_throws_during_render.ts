// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { RenderRecoveryBoundary } from '../RenderRecoveryBoundary';

// React's error-boundary lifecycle (getDerivedStateFromError/componentDidCatch) only runs during a
// real client commit, not under renderToStaticMarkup (see frontend-testing.md's SSR component-spec
// pattern) - so the boundary's own state transition is specified directly against the static method
// React itself invokes when a descendant throws, rather than by actually throwing through a render.
describe('when a descendant throws during render', () => {
    it('should switch the boundary into its failed state', () =>
        RenderRecoveryBoundary.getDerivedStateFromError().should.deep.equal({ hasFailed: true }));
});
