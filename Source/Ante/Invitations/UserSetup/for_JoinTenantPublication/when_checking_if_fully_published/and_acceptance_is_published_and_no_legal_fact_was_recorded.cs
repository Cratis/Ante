// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.UserSetup.for_JoinTenantPublication.when_checking_if_fully_published;

public class and_acceptance_is_published_and_no_legal_fact_was_recorded : Specification
{
    static readonly InvitationId _invitationId = InvitationId.New();

    bool _result;

    void Because() =>
        _result = JoinTenantPublication.IsFullyPublished(
            recorded: new UserSetupProgress(_invitationId, AcceptanceRecorded: true, LegalRecorded: false),
            published: new JoinTenantAcceptancePublished(_invitationId, AcceptancePublished: true, LegalPublished: false));

    [Fact] void should_be_fully_published() => Assert.True(_result);
}
#endif
