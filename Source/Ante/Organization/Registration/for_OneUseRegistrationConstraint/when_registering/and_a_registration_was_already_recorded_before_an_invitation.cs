// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations;
using Ante.Invitations.OrganizationSetup;

namespace Ante.Organization.Registration.for_OneUseRegistrationConstraint.when_registering;

public class and_a_registration_was_already_recorded_before_an_invitation : Specification
{
    readonly InvitationId _id = InvitationId.New();
    readonly EventScenario _scenario = new();
    IAppendResult _result = null!;

    // Seeds the event log with OrganizationRegistrationRecorded - the private fact RegisterOrganization
    // actually appends - rather than its OrganizationRegistrationCompleted outbox contract: that type
    // carries an assembly-level [EventStore("Ante")], so Chronicle now refuses to append it to the
    // event log at all (PublicEventTypeCannotBeAppendedToEventLog), even for a test fixture.
    async Task Establish() =>
        await _scenario.Given.ForEventSource(_id).Events(
            new OnboardingAttemptClaimed(),
            new OrganizationRegistrationRecorded("Acme", "sub-1", "github", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", []));

    async Task Because() => _result = await _scenario.EventLog.Append(_id, new OnboardingAttemptClaimed());

    [Fact] void should_fail() => _result.ShouldBeFailed();
    [Fact] void should_violate_the_one_use_constraint() => _result.ShouldHaveConstraintViolationFor(OnboardingAttemptConstraintNames.OneUseAttempt);
}
#endif
