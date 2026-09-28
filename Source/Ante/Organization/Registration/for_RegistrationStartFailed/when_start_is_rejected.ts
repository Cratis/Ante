// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { describe, beforeEach, it, vi } from 'vitest';
import sinon from 'sinon';
import strings from 'Strings';
import { RegistrationStartFailed } from '../RegistrationStartFailed';

const buttons: { label: string; onClick: () => void }[] = [];
vi.mock('@cratis/components/Common', () => ({
    Button: (props: { label: string; onClick: () => void }) => {
        buttons.push(props);
        return React.createElement('button', null, props.label);
    }
}));

describe('when a registration start is rejected', () => {
    const onRetry = sinon.spy();
    const onStartNew = sinon.spy();
    let html: string;

    beforeEach(() => {
        buttons.length = 0;
        onRetry.resetHistory();
        onStartNew.resetHistory();
        html = renderToStaticMarkup(React.createElement(RegistrationStartFailed, { onRetry, onStartNew }));
    });

    it('should offer a new registration instead of trapping the user on the failed operation', () => {
        html.should.include(strings.registration.startNewRegistration);
        buttons.find(button => button.label === strings.registration.startNewRegistration)!.onClick();
        onStartNew.calledOnce.should.be.true;
    });

    it('should still allow retrying the same operation', () => {
        buttons.find(button => button.label === strings.onboarding.checkAgain)!.onClick();
        onRetry.calledOnce.should.be.true;
    });
});
