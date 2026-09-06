// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.UserSetup.for_JoinTenantPublication.when_checking_if_fully_published;

public class and_acceptance_and_its_legal_fact_are_both_published : Specification
{
    static readonly InvitationId _invitationId = InvitationId.New();

    bool _result;

    void Because() =>
        _result = JoinTenantPublication.IsFullyPublished(
            recorded: new UserSetupProgress(_invitationId, AcceptanceRecorded: true, LegalRecorded: true),
            published: new JoinTenantAcceptancePublished(_invitationId, AcceptancePublished: true, LegalPublished: true));

    [Fact] void should_be_fully_published() => Assert.True(_result);
}
#endif
