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

// PrimeReact 11 verifies a license key at runtime. The application owns it and hands it straight to
// PrimeReactProvider; Components never receives it, and takes only the boolean attestation below -
// which is why the key is read here rather than passed through the Components provider.
const primeUiLicense = import.meta.env.ANTE_PRIMEUI_LICENSE;

ReactDOM.createRoot(document.getElementById('root')!).render(
    <React.StrictMode>
        <PrimeReactProvider license={primeUiLicense} theme={primeReactTheme}>
            <CratisComponentsProvider
                value={{ locale: 'en-US' }}
                library={primeReactUiLibrary}
                rendererSetup={{ 'cratis-primereact.license-configured': Boolean(primeUiLicense) }}>
                <App />
            </CratisComponentsProvider>
        </PrimeReactProvider>
    </React.StrictMode>
);
