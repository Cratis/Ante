// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting;
using Ante.Invitations.OrganizationSetup;
using Ante.Organization.Registration;
using Microsoft.Extensions.Options;

namespace Ante.Invitations.HostOutcome.for_HostOutcomeView.when_getting_the_outcome_for_an_attempt;

public class and_the_registration_owner_asks : Specification
{
    static readonly InvitationId _registrationId = InvitationId.New();

    ISignedInIdentity _signedInIdentity = null!;
    IHostOutcomeBackchannel _backchannel = null!;
    IEventStore _eventStore = null!;
    HostOutcomeView _result = null!;

    void Establish()
    {
        _eventStore = Substitute.For<IEventStore>();
        var readModels = Substitute.For<IReadModels>();
        _eventStore.ReadModels.Returns(readModels);
        readModels.GetInstanceById<OrganizationSetupProgress>(Arg.Any<ReadModelKey>(), Arg.Any<ReadModelSessionId?>())
            .Returns(Task.FromResult(new OrganizationSetupProgress(_registrationId, "Acme") { OwnerSubject = (RegistrationOwnerSubject)"sub-1", OwnerProvider = "github" }));
        _signedInIdentity = Substitute.For<ISignedInIdentity>();
        _signedInIdentity.IsVerifiedRecoveryOwnerOf(_registrationId, _eventStore).Returns(false);
        _signedInIdentity.IsVerifiedRegistrationOwner(Arg.Any<RegistrationOwner>())
            .Returns(call => call.Arg<RegistrationOwner>().Subject.Value == "sub-1");

        _backchannel = Substitute.For<IHostOutcomeBackchannel>();
        _backchannel.GetOutcome(_registrationId).Returns(Task.FromResult((HostOutcomeStatus.Succeeded, "tenant-provisioned")));
    }

    async Task Because() =>
        _result = await HostOutcomeView.ForAttempt(_registrationId, _signedInIdentity, Options.Create(new AnteOptions { HostOutcomeUrl = "https://host.example.com/onboarding" }), _backchannel, _eventStore);

    [Fact] void should_report_the_hosts_outcome() => Assert.Equal(HostOutcomeStatus.Succeeded, _result.Status);
    [Fact] void should_carry_the_reason_code() => Assert.Equal("tenant-provisioned", _result.ReasonCode);
}
#endif
