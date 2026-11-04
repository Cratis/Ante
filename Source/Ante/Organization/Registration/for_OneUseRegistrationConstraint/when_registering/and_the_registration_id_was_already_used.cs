// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations;

namespace Ante.Organization.Registration.for_OneUseRegistrationConstraint.when_registering;

public class and_the_registration_id_was_already_used : Specification
{
    readonly InvitationId _id = InvitationId.New();
    readonly EventScenario _scenario = new();
    IAppendResult _result = null!;

    async Task Establish() =>
        await _scenario.Given.ForEventSource(_id).Events(
            new OnboardingAttemptClaimed(),

            // OrganizationRegistrationRecorded, not its outbox-only public contract OrganizationRegistrationCompleted:
            // a public event type can never be appended to the event log (Chronicle 19.124.0 now enforces this
            // in Given too, instead of silently dropping the seed - Cratis/Chronicle#5618).
            new OrganizationRegistrationRecorded("Acme", "sub-1", "github", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", []));

    async Task Because() => _result = await _scenario.EventLog.Append(_id, new OnboardingAttemptClaimed());

    [Fact] void should_fail() => _result.ShouldBeFailed();
    [Fact] void should_violate_the_one_use_constraint() => _result.ShouldHaveConstraintViolationFor(OnboardingAttemptConstraintNames.OneUseAttempt);
}
#endif
