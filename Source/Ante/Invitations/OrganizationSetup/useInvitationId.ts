// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo } from 'react';
import { useIdentity } from '@cratis/arc.react/identity';
import { Guid } from '@cratis/fundamentals';
import { InvitationIdentityDetails } from '../Accepting/Accepting';
import { getInvitationIdFromToken } from '../Accepting/invitationToken';

/**
 * Resolves the invitation being set up from the invitation token, falling back to the signed-in identity.
 * @param invitationToken The invitation token from the route, when present.
 * @returns The invitation id (null while unresolved) and whether the identity has been set.
 */
export const useInvitationId = (invitationToken?: string | null) => {
    const identity = useIdentity(InvitationIdentityDetails);
    const invitationIdentityDetails = identity.details as unknown as InvitationIdentityDetails | undefined;
    const invitationId = useMemo(() => {
        const invitationIdFromToken = getInvitationIdFromToken(invitationToken);
        if (invitationIdFromToken) return invitationIdFromToken;

        if (!identity.isSet || !identity.details) return null;
        const id = invitationIdentityDetails?.invitationId;
        if (!id) return null;
        const str = id.toString();
        return Guid.isGuid(str) ? Guid.parse(str) : null;
    }, [identity.isSet, invitationIdentityDetails, invitationToken]);

    return { invitationId, isIdentitySet: identity.isSet };
};
