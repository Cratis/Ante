// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.UserSetup.for_JoinTenantPublication.when_checking_if_fully_published;

/// <summary>
/// This is the exact scenario the "partial legal publication never displays full completion" acceptance
/// criterion targets: the acceptance itself reached the outbox, but the legal fact this invitation
/// actually recorded has not, so completion must never show yet.
/// </summary>
public class and_acceptance_is_published_but_its_required_legal_fact_is_not : Specification
{
    static readonly InvitationId _invitationId = InvitationId.New();

    bool _result;

    void Because() =>
        _result = JoinTenantPublication.IsFullyPublished(
            recorded: new UserSetupProgress(_invitationId, AcceptanceRecorded: true, LegalRecorded: true),
            published: new JoinTenantAcceptancePublished(_invitationId, AcceptancePublished: true, LegalPublished: false));

    [Fact] void should_not_be_fully_published() => Assert.False(_result);
}
#endif
