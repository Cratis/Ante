// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// @vitest-environment jsdom

import React, { act } from 'react';
import { createRoot, Root } from 'react-dom/client';
import { afterEach, beforeEach, describe, it } from 'vitest';
import { FieldError } from '../FieldError';

Object.assign(globalThis, { IS_REACT_ACT_ENVIRONMENT: true });

describe('when showing a field error', () => {
    let root: Root;
    let container: HTMLDivElement;
    let message: Element;

    beforeEach(async () => {
        container = document.createElement('div');
        document.body.append(container);
        root = createRoot(container);
        await act(async () => {
            root.render(React.createElement(FieldError, { errors: ['First name is required.'], fieldName: 'firstName' }));
        });
        message = container.firstElementChild!;
    });

    afterEach(async () => {
        await act(async () => root.unmount());
        container.remove();
    });

    it('should show the message', () => message.textContent!.should.equal('First name is required.'));
    it('should hide the message from assistive technology, which reads the field\'s own error element', () => message.getAttribute('aria-hidden')!.should.equal('true'));
    it('should not be a live region', () => container.querySelectorAll('[role], [aria-live]').length.should.equal(0));
});
