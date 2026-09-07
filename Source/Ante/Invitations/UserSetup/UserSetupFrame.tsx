// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ReactNode } from 'react';
import strings from 'Strings';
import { GetConfiguration } from '../../Configuration/Configuration';
import { DisplayPreferencesMenu } from '../../DisplayPreferences/DisplayPreferencesMenu';
import './UserSetupPage.css';

const HEADING_ID = 'user-setup-heading';

interface UserSetupFrameProps {
    children: ReactNode;
}

export const UserSetupFrame = ({ children }: UserSetupFrameProps) => {
    const [brandingResult] = GetConfiguration.use();
    const logoUrl = brandingResult.data?.logoUrl;

    return (
        <div className='user-setup-container'>
            {/* The single landmark and heading for this page - there is no other layout wrapping it. */}
            <main className='user-setup-card' aria-labelledby={HEADING_ID}>
                <DisplayPreferencesMenu />
                <div className='user-setup-header'>
                    <h1 id={HEADING_ID} className={logoUrl ? 'ante-logo__heading' : 'ante-logo__text'}>
                        {logoUrl
                            ? <img src={logoUrl} alt={strings.branding.wordmark} className='user-setup-logo' />
                            : strings.branding.wordmark}
                    </h1>
                    <p className='user-setup-subtitle'>{strings.userSetup.subtitle}</p>
                </div>
                {children}
            </main>
        </div>
    );
};
