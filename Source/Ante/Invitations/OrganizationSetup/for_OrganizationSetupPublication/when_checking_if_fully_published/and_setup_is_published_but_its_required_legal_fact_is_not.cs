// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.OrganizationSetup.for_OrganizationSetupPublication.when_checking_if_fully_published;

/// <summary>
/// This is the exact scenario the "partial legal publication never displays full completion" acceptance
/// criterion targets: setup itself reached the outbox, but the legal fact this invitation or registration
/// actually recorded has not, so completion must never show yet.
/// </summary>
public class and_setup_is_published_but_its_required_legal_fact_is_not : Specification
{
    static readonly InvitationId _invitationId = InvitationId.New();

    bool _result;

    void Because() =>
        _result = OrganizationSetupPublication.IsFullyPublished(
            recorded: new OrganizationSetupProgress(_invitationId, "Acme", LegalRecorded: true),
            published: new OrganizationSetupPublished(_invitationId, AcceptancePublished: true, LegalPublished: false));

    [Fact] void should_not_be_fully_published() => Assert.False(_result);
}
#endif
