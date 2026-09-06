// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ReactNode } from 'react';
import strings from 'Strings';
import { GetConfiguration } from '../../Configuration/Configuration';
import './OrganizationSetupPage.css';

interface OrganizationSetupFrameProps {
    children: ReactNode;
    subtitle?: string;
}

export const OrganizationSetupFrame = ({ children, subtitle }: OrganizationSetupFrameProps) => {
    const [brandingResult] = GetConfiguration.use();
    const logoUrl = brandingResult.data?.logoUrl;

    return (
        <div className='organization-setup-container'>
            <div className='organization-setup-card'>
                <div className='organization-setup-header'>
                    {logoUrl
                        ? <img src={logoUrl} alt={strings.branding.wordmark} className='organization-setup-logo' />
                        : <span className='ante-logo__text'>{strings.branding.wordmark}</span>}
                    <p className='organization-setup-subtitle'>{subtitle ?? strings.organizationSetup.subtitle}</p>
                </div>
                {children}
            </div>
        </div>
    );
};
