// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.OrganizationSetup.for_OrganizationSetupPublication.when_checking_if_fully_published;

/// <summary>
/// The "Recorded" state on its own must never read as fully published - this applies identically whether
/// setup came from an invitation or from self-service registration, since both share this read model.
/// </summary>
public class and_setup_is_recorded_but_not_yet_published : Specification
{
    static readonly InvitationId _invitationId = InvitationId.New();

    bool _result;

    void Because() =>
        _result = OrganizationSetupPublication.IsFullyPublished(
            recorded: new OrganizationSetupProgress(_invitationId, "Acme"),
            published: null);

    [Fact] void should_not_be_fully_published() => Assert.False(_result);
}
#endif
