// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.UserSetup.for_JoinTenantPublication.when_checking_if_fully_published;

/// <summary>
/// The "Recorded" state on its own must never read as fully published - a client watching this exact
/// signal is what pre-append/pre-publish success used to look like.
/// </summary>
public class and_acceptance_is_recorded_but_not_yet_published : Specification
{
    static readonly InvitationId _invitationId = InvitationId.New();

    bool _result;

    void Because() =>
        _result = JoinTenantPublication.IsFullyPublished(
            recorded: new UserSetupProgress(_invitationId, AcceptanceRecorded: true),
            published: null);

    [Fact] void should_not_be_fully_published() => Assert.False(_result);
}
#endif
