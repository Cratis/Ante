// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as chai from 'chai';
chai.should();
import * as chaiAsPromised from 'chai-as-promised';
import * as sinonChai from 'sinon-chai';
chai.use(sinonChai.default);
chai.use(chaiAsPromised.default);

import React from 'react';
import { vi } from 'vitest';

// PrimeReact v11 throws "PrimeReactProvider not found" the moment any of its components render
// outside one, and SSR specs (renderToStaticMarkup) deliberately render provider-free per
// frontend-testing.md. Individual spec files would otherwise each redeclare this same stub for
// 'primereact/button' - harmless on its own, but under this suite's `isolate: false` /
// `fileParallelism: false` config every spec shares one module registry, and racing,
// independently-registered mocks for the same specifier produce order-dependent flakiness (a
// file could "win" the real, unmocked module for every later importer in the same run). One
// registration here, guaranteed to apply before any test file's own vi.mock, removes the race.
vi.mock('primereact/button', () => ({
    Button: (props: { label?: string; disabled?: boolean }) =>
        React.createElement('button', { disabled: props.disabled }, props.label),
}));
