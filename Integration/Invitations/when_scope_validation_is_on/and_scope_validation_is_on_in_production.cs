// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.given;

namespace Ante.Integration.Invitations.when_scope_validation_is_on;

/// <summary>
/// Production leaves scope validation off, so a scoped service captured by a singleton resolves quietly from the root scope
/// there. Switching validation on in Production keeps that from going unnoticed: the invitation must still flow.
/// </summary>
[Collection(ChronicleCollection.Name)]
public class and_scope_validation_is_on_in_production : a_running_ante
{
    Guid _invitationId;
    InvitationTokenIssued _issued;

    protected override string EnvironmentName => "Production";

    protected override bool? ValidateScopes => true;

    async Task Because()
    {
        _invitationId = NewInvitationId();
        await Host.Publish(_invitationId, JoinInvitation(), subject: Guid.NewGuid());
        _issued = await Host.WaitForFromAnte<InvitationTokenIssued>(_invitationId.ToString());
    }

    [Fact] void should_deliver_a_token_back_to_the_host_inbox() => _issued.ShouldNotBeNull();
}
