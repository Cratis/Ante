// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// @vitest-environment jsdom

import React, { act } from 'react';
import { createRoot, Root } from 'react-dom/client';
import { afterEach, beforeEach, describe, it } from 'vitest';
import { ErrorSummary } from '../ErrorSummary';

Object.assign(globalThis, { IS_REACT_ACT_ENVIRONMENT: true });

describe('when showing messages', () => {
    let root: Root;
    let container: HTMLDivElement;
    let alerts: Element[];
    let liveRegions: Element[];

    beforeEach(async () => {
        container = document.createElement('div');
        document.body.append(container);
        root = createRoot(container);
        await act(async () => {
            root.render(React.createElement(ErrorSummary, {
                messages: ['Last name contains a control character.', 'Organization name cannot contain spaces.'],
                className: 'summary',
                itemClassName: 'item'
            }));
        });
        alerts = [...container.querySelectorAll('[role="alert"]')];
        liveRegions = [...container.querySelectorAll('[role="alert"], [role="status"], [aria-live]')];
    });

    afterEach(async () => {
        await act(async () => root.unmount());
        container.remove();
    });

    it('should be a single alert', () => alerts.length.should.equal(1));
    it('should have no live region nested inside another', () => liveRegions.length.should.equal(1));
    it('should be the alert that has the focus', () => (document.activeElement === alerts[0]).should.be.true);
    it('should have the page class on the alert', () => alerts[0].className.should.equal('summary'));
    it('should list every message once', () => [...alerts[0].querySelectorAll('.cratis-message__text')].map(item => item.textContent).should.deep.equal([
        'Last name contains a control character.',
        'Organization name cannot contain spaces.'
    ]));
    it('should use error messages from Components', () => alerts[0].querySelectorAll('.cratis-message[data-severity="error"]').length.should.equal(2));
    it('should have the item class on each message', () => alerts[0].querySelectorAll('.cratis-message.item').length.should.equal(2));
    it('should hide the icon from assistive technology', () => alerts[0].querySelectorAll('.cratis-message__icon[aria-hidden="true"]').length.should.equal(2));
});
