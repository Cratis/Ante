// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations;

namespace Ante.Organization.Registration.Start.for_BeginRegistration.when_starting;

public class and_a_second_start_races_the_first : Specification
{
    readonly InvitationId _id = InvitationId.New();
    readonly EventScenario _scenario = new();
    IAppendResult _result = null!;

    async Task Establish() => await _scenario.Given.ForEventSource(_id).Events(new RegistrationStarted((RegistrationOwnerSubject)"subject-1", "github"));

    async Task Because() => _result = await _scenario.EventLog.Append(_id, new RegistrationStarted((RegistrationOwnerSubject)"subject-2", "github"));

    [Fact] void should_reject_the_second_start() => _result.ShouldBeFailed();
    [Fact] void should_enforce_one_owner_at_append_time() => _result.ShouldHaveConstraintViolationFor("OneRegistrationStart");
}
#endif
