// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ReactNode } from 'react';
import strings from 'Strings';
import { useBrandLogo } from '../../Branding/useBrandLogo';
import { useTrustedBrandingStyles } from '../../Branding/useTrustedBrandingStyles';
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
    const { showImage, onImageError } = useBrandLogo(logoUrl);
    useTrustedBrandingStyles(brandingResult.data?.customCssUrl);

    return (
        <div className='user-setup-container'>
            {/* The single landmark and heading for this page - there is no other layout wrapping it. */}
            <main className='user-setup-card' aria-labelledby={HEADING_ID}>
                <DisplayPreferencesMenu />
                <div className='user-setup-header'>
                    <h1 id={HEADING_ID} className={showImage ? 'ante-logo__heading' : 'ante-logo__text'}>
                        {showImage
                            ? <img src={logoUrl} alt={strings.branding.wordmark} className='user-setup-logo' onError={onImageError} />
                            : strings.branding.wordmark}
                    </h1>
                    <p className='user-setup-subtitle'>{strings.userSetup.subtitle}</p>
                </div>
                {children}
            </main>
        </div>
    );
};
