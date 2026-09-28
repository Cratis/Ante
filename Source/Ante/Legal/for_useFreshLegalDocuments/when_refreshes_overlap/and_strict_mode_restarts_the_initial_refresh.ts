// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// @vitest-environment jsdom

import React, { act, StrictMode } from 'react';
import { createRoot, Root } from 'react-dom/client';
import sinon from 'sinon';
import { afterEach, beforeEach, describe, it } from 'vitest';
import { Current } from '../../LegalDocuments';
import { useFreshLegalDocuments } from '../../useFreshLegalDocuments';

Object.assign(globalThis, { IS_REACT_ACT_ENVIRONMENT: true });

describe('when cached legal documents are refreshed under StrictMode', () => {
    let root: Root;
    let container: HTMLDivElement;
    let completions: Array<() => void>;
    let latest: ReturnType<typeof useFreshLegalDocuments>;

    const Capture = () => {
        latest = useFreshLegalDocuments();
        return null;
    };

    beforeEach(async () => {
        completions = [];
        const cached = {
            data: { isConfigured: true, termsAndConditions: 'Cached terms', privacyPolicy: 'Cached privacy', version: '2026-02', isUnavailable: false },
            hasData: true, isSuccess: true, isPerforming: false,
        };
        sinon.stub(Current, 'use').callsFake(() => [cached, () => new Promise<void>(resolve => { completions.push(resolve); }), () => { }] as ReturnType<typeof Current.use>);
        container = document.createElement('div');
        document.body.append(container);
        root = createRoot(container);
        await act(async () => root.render(React.createElement(StrictMode, null, React.createElement(Capture))));
    });

    afterEach(async () => {
        await act(async () => root.unmount());
        container.remove();
        sinon.restore();
    });

    it('should not confirm an effect cleaned up by StrictMode while its replacement is pending', async () => {
        completions.length.should.equal(2);
        await act(async () => completions[0]());
        (latest.documents === undefined).should.be.true;
        latest.isChecking.should.be.true;
    });

    it('should only confirm the newest refresh after older responses arrive', async () => {
        await act(async () => { void latest.refresh(); });
        completions.length.should.equal(3);
        await act(async () => { completions[1](); completions[0](); });
        (latest.documents === undefined).should.be.true;
        latest.isChecking.should.be.true;
        await act(async () => completions[2]());
        latest.documents!.version.should.equal('2026-02');
        latest.isChecking.should.be.false;
    });
});
