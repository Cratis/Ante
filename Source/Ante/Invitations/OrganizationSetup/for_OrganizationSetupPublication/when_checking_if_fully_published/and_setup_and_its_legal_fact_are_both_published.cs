// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.OrganizationSetup.for_OrganizationSetupPublication.when_checking_if_fully_published;

public class and_setup_and_its_legal_fact_are_both_published : Specification
{
    static readonly InvitationId _invitationId = InvitationId.New();

    bool _result;

    void Because() =>
        _result = OrganizationSetupPublication.IsFullyPublished(
            recorded: new OrganizationSetupProgress(_invitationId, "Acme", LegalRecorded: true),
            published: new OrganizationSetupPublished(_invitationId, AcceptancePublished: true, LegalPublished: true));

    [Fact] void should_be_fully_published() => Assert.True(_result);
}
#endif
