// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import strings from 'Strings';
import { RegistrationIntro } from '../RegistrationIntro';
import { RegistrationPageContent } from '../../../Configuration/Configuration';

const contentWith = (values: Partial<RegistrationPageContent>): RegistrationPageContent =>
    Object.assign(new RegistrationPageContent(), { title: '', intro: '', highlights: [], pricingUrl: '', loginUrl: '', completionMessage: '' }, values);

describe('when rendering the registration introduction', () => {
    it('should render nothing when the host configured no copy', () =>
        renderToStaticMarkup(React.createElement(RegistrationIntro, { content: contentWith({}) })).should.equal(''));

    it('should render the offer, highlights and links', () => {
        const html = renderToStaticMarkup(React.createElement(RegistrationIntro, {
            content: contentWith({
                intro: 'Start your 14-day free trial',
                highlights: ['No credit card required'],
                pricingUrl: 'https://cratis.studio/pricing',
                loginUrl: 'https://app.cratis.studio',
            })
        }));
        html.should.include('Start your 14-day free trial');
        html.should.include('<li>No credit card required</li>');
        html.should.include('href="https://cratis.studio/pricing"');
        html.should.include(strings.registration.loginLink);
    });

    it('should render plain text, never markup', () =>
        renderToStaticMarkup(React.createElement(RegistrationIntro, { content: contentWith({ intro: '<b>bold</b>' }) }))
            .should.include('&lt;b&gt;bold&lt;/b&gt;'));
});
