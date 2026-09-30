// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// @vitest-environment jsdom

import React, { act } from 'react';
import { createRoot, Root } from 'react-dom/client';
import { afterEach, beforeEach, describe, it } from 'vitest';
import { LocaleProvider } from '../../Locale/LocaleContext';
import { useAccessibleStepper } from '../useAccessibleStepper';

Object.assign(globalThis, { IS_REACT_ACT_ENVIRONMENT: true });

describe('when the stepper mounts after the page', () => {
    let root: Root;
    let container: HTMLDivElement;
    let announcement: string;

    // A page first renders a loading state; the stepper's container only appears once the form does.
    const Page = ({ showForm }: { showForm: boolean }) => {
        const stepper = useAccessibleStepper({ idPrefix: 'spec', announcementTemplate: 'Step {current} of {total}: {label}' });
        announcement = stepper.announcement;
        if (!showForm) return React.createElement('p', undefined, 'Loading');
        return React.createElement('div', { ref: stepper.containerRef },
            React.createElement('ol', { 'data-cratis-part': 'list' },
                React.createElement('li', undefined,
                    React.createElement('button', { 'data-cratis-part': 'header' },
                        React.createElement('span', undefined, '1'),
                        React.createElement('span', { 'data-cratis-part': 'title' }, 'Your Information'))),
                React.createElement('li', undefined,
                    React.createElement('button', { 'data-cratis-part': 'header' },
                        React.createElement('span', undefined, '2'),
                        React.createElement('span', { 'data-cratis-part': 'title' }, 'Terms')))),
            React.createElement('section', { 'data-cratis-part': 'panel' }, 'first'),
            React.createElement('section', { 'data-cratis-part': 'panel', hidden: true }, 'second'));
    };

    const render = async (showForm: boolean) => {
        await act(async () => {
            root.render(React.createElement(LocaleProvider, {
                locale: 'en', settings: { defaultLocale: 'en', supportedLocales: ['en', 'nb-NO'] }, onChange: () => { }
            }, React.createElement(Page, { showForm })));
        });
    };

    beforeEach(async () => {
        container = document.createElement('div');
        document.body.append(container);
        root = createRoot(container);
        await render(false);
        await render(true);
    });

    afterEach(async () => {
        await act(async () => root.unmount());
        container.remove();
    });

    it('should make the step list a tab list', () => container.querySelector('ol')!.getAttribute('role')!.should.equal('tablist'));
    it('should take the list items out of the tab list', () => container.querySelector('li')!.getAttribute('role')!.should.equal('none'));
    it('should make the panels tab panels', () => container.querySelector('section')!.getAttribute('role')!.should.equal('tabpanel'));
    it('should announce the current step by its title', () => announcement.should.equal('Step 1 of 2: Your Information'));
});
