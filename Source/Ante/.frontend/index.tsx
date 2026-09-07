// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PrimeReactProvider } from '@primereact/core/config';
import { CratisComponentsProvider } from '@cratis/components/Common';
import { primeReactUiLibrary } from '@cratis/components.primereact';
import ReactDOM from 'react-dom/client';
import 'primeicons/primeicons.css';
import '@cratis/components/tokens';
import '@cratis/components/styles';
import '@cratis/components/theme';
import './index.css';
import React from 'react';
import App from './App';
import { primeReactTheme } from './primeReactTheme';
import { applyInitialDisplayPreferences } from '../DisplayPreferences/applyInitialDisplayPreferences';
import { applyInitialLocale } from '../Locale/applyInitialLocale';
import { LocaleProvider } from '../Locale/LocaleContext';
import { LOCALE_TAGS } from '../Locale/Locale';
import { RenderRecoveryBoundary } from '../RenderRecovery/RenderRecoveryBoundary';

// Applied synchronously, before the first render, so the stored display preferences (Cratis/Ante#20)
// are already on the document by the time the first frame paints - never a flash of the wrong text
// size/contrast/spacing/motion that then snaps to the stored preference a moment later.
applyInitialDisplayPreferences();

// Resolved once, synchronously, and threaded into both the document (lang/dir) and the providers
// below (Cratis/Ante#21) - one resolution applied consistently everywhere the lobby needs it.
const locale = applyInitialLocale();

// PrimeReact 11 verifies a license key at runtime. The application owns it and hands it straight to
// PrimeReactProvider; Components never receives it, and takes only the boolean attestation below -
// which is why the key is read here rather than passed through the Components provider.
const primeUiLicense = import.meta.env.ANTE_PRIMEUI_LICENSE;

ReactDOM.createRoot(document.getElementById('root')!).render(
    <React.StrictMode>
        {/* Outermost on purpose - a render failure inside the providers themselves (a bad PrimeReact
            license/theme, a Cratis Components initialization error) must still degrade to the neutral
            recovery view rather than a blank page (Cratis/Ante#21). */}
        <RenderRecoveryBoundary>
            <PrimeReactProvider license={primeUiLicense} theme={primeReactTheme}>
                <CratisComponentsProvider
                    value={{ locale: LOCALE_TAGS[locale] }}
                    library={primeReactUiLibrary}
                    rendererSetup={{ 'cratis-primereact.license-configured': Boolean(primeUiLicense) }}>
                    <LocaleProvider locale={locale}>
                        <App />
                    </LocaleProvider>
                </CratisComponentsProvider>
            </PrimeReactProvider>
        </RenderRecoveryBoundary>
    </React.StrictMode>
);
