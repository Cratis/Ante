// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { OrganizationSetupPage } from '../Invitations/OrganizationSetup/OrganizationSetupPage';
import { UserSetupPage } from '../Invitations/UserSetup/UserSetupPage';
import { RegistrationPage } from '../Organization/Registration/RegistrationPage';
import { Arc } from '@cratis/arc.react';
import { useIdentity } from '@cratis/arc.react/identity';
import { InvitationIdentityDetails } from '../Invitations/Accepting/Accepting';
import { InvitationFlowType } from '../Contracts/Invitations/InvitationFlowType';
import { getFlowTypeFromInvitationToken, getInvitationToken, normalizeFlowType } from '../Invitations/Accepting/invitationToken';
import '@cratis/components/styles';

function InvitationRouter() {
    const identity = useIdentity(InvitationIdentityDetails);

    const details = identity.isSet ? (identity.details as unknown as InvitationIdentityDetails) : null;
    const invitationToken = getInvitationToken();
    const flowType = normalizeFlowType(details?.flowType)
        ?? getFlowTypeFromInvitationToken(invitationToken)
        ?? InvitationFlowType.joinTenant;

    return flowType === InvitationFlowType.createTenant
        ? <OrganizationSetupPage invitationToken={invitationToken} />
        : <UserSetupPage invitationToken={invitationToken} />;
}

function App() {
    const isRegistrationPath = window.location.pathname === '/register' || window.location.pathname.startsWith('/register/');

    return (
        <Arc queryDirectMode={true}>
            {isRegistrationPath ? <RegistrationPage /> : <InvitationRouter />}
        </Arc>
    );
}

export default App;
