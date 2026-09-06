// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Legal;
using Ante.Invitations.Accepting;
using Ante.Legal;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Invitations.UserSetup.when_accepting;

public class and_the_invitation_is_no_longer_pending : Specification
{
    static readonly InvitationId _invitationId = InvitationId.New();

    readonly CommandScenario<AcceptInvitation> _scenario = new();
    CommandResult _result = null!;

    void Establish()
    {
        // No PendingInvitationToJoin seeded for this event source - it was never invited, or the
        // invitation was already accepted/revoked.
        _scenario.Services.AddSingleton(Substitute.For<ISignedInIdentity>());
        _scenario.Services.AddSingleton(Substitute.For<IIdentityBackchannel>());
        _scenario.Services.AddSingleton<ILegalDocumentSource>(new NoLegalDocumentSource());
        _scenario.Services.AddSingleton(new UserSetupStatusSubscriptions());
    }

    async Task Because() =>
        _result = await _scenario.Execute(new AcceptInvitation(_invitationId, "Jane", null, "Doe", false, LegalVersion.NotSet));

    [Fact] void should_not_succeed() => _result.ShouldNotBeSuccessful();
    [Fact] void should_have_validation_errors() => _result.ShouldHaveValidationErrors();
}
#endif
