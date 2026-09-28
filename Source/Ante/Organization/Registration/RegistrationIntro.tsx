// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { RegistrationPageContent } from '../../Configuration/Configuration';
import strings from 'Strings';

/**
 * Host-authored introduction above the registration wizard: the offer, its highlights and the links a
 * visitor may need before committing - pricing, or logging in instead. Renders nothing when the host has
 * not configured any copy, so an unconfigured deployment looks exactly as before.
 * @param props The content to render.
 * @returns The introduction, or nothing.
 */
export const RegistrationIntro = ({ content }: { content?: RegistrationPageContent }) => {
    if (!content) return null;
    const highlights = content.highlights ?? [];
    const hasLinks = Boolean(content.pricingUrl || content.loginUrl);
    if (!content.intro && highlights.length === 0 && !hasLinks) return null;

    return (
        <div className='registration-intro'>
            {content.intro && <p className='registration-intro__text'>{content.intro}</p>}
            {highlights.length > 0 && (
                <ul className='registration-intro__highlights'>
                    {highlights.map(highlight => <li key={highlight}>{highlight}</li>)}
                </ul>
            )}
            {hasLinks && (
                <p className='registration-intro__links'>
                    {content.pricingUrl && <a href={content.pricingUrl} target='_blank' rel='noopener noreferrer'>{strings.registration.pricingLink}</a>}
                    {content.pricingUrl && content.loginUrl && ' · '}
                    {content.loginUrl && <a href={content.loginUrl}>{strings.registration.loginLink}</a>}
                </p>
            )}
        </div>
    );
};
