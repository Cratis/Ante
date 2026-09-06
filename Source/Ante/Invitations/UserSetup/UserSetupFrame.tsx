// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ReactNode } from 'react';
import strings from 'Strings';
import { GetConfiguration } from '../../Configuration/Configuration';
import './UserSetupPage.css';

interface UserSetupFrameProps {
    children: ReactNode;
}

export const UserSetupFrame = ({ children }: UserSetupFrameProps) => {
    const [brandingResult] = GetConfiguration.use();
    const logoUrl = brandingResult.data?.logoUrl;

    return (
        <div className='user-setup-container'>
            <div className='user-setup-card'>
                <div className='user-setup-header'>
                    {logoUrl
                        ? <img src={logoUrl} alt={strings.branding.wordmark} className='user-setup-logo' />
                        : <span className='ante-logo__text'>{strings.branding.wordmark}</span>}
                    <p className='user-setup-subtitle'>{strings.userSetup.subtitle}</p>
                </div>
                {children}
            </div>
        </div>
    );
};
