// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import strings from 'Strings';

/**
 * Shown at `/register` when the deployment does not offer self-service registration.
 * @param props The optional link the host configured for people who cannot sign up here.
 * @returns The closed notice.
 */
export const RegistrationClosed = ({ closedUrl }: { closedUrl: string }) => (
    <div className='registration-closed' role='status'>
        <p>{strings.registration.closed}</p>
        {closedUrl && <p><a href={closedUrl}>{strings.registration.closedLink}</a></p>}
    </div>
);
