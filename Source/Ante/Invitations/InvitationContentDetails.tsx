// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { InvitationPageContent } from '../Configuration/Configuration';

/** Optional host-authored invitation details, rendered only as text and an HTTPS link validated at startup. */
export const InvitationContentDetails = ({ content }: { content?: InvitationPageContent }) => (
    <>
        {content?.intro && <p>{content.intro}</p>}
        {content?.supportUrl && <p><a href={content.supportUrl} target='_blank' rel='noopener noreferrer'>{content.supportUrl}</a></p>}
    </>
);
