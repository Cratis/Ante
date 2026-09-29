// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Organization;
using Ante.Integration.given;
using Ante.Organization.Registration;

namespace Ante.Integration.Organization.when_two_ante_instances_share_a_store;

/// <summary>
/// A visitor registers an organization, and the instance forwarding <see cref="OrganizationRegistrationCompleted"/> stops
/// after its outbox append, before it has acknowledged the event. No constraint refuses a second copy of the fact, so
/// only the forward's own check keeps the remaining instance from publishing the registration to the host twice
/// (Cratis/Ante#135).
/// </summary>
[Collection(ChronicleCollection.Name)]
public class and_the_registration_forward_stops_mid_forward : an_outbox_forward_held_after_its_append<OrganizationRegistrationOutbox, OrganizationRegistrationCompleted>
{
    readonly Guid _registrationId = Guid.NewGuid();
    readonly string _subject = $"visitor-{Guid.NewGuid():N}";
    readonly string _email = $"{Guid.NewGuid():N}@example.com";
    readonly string _organization = $"Self-{Guid.NewGuid():N}"[..20];

    Task Because() => StopTheForwardingInstanceMidForward(_registrationId.ToString("D"), async () =>
    {
        var started = await Ante.Execute("/api/organization/registration/start", new { registrationId = _registrationId }, _subject, _email);
        IsSuccess(started).ShouldBeTrue();
        var result = await ExecuteOnceProjected(
            "/api/organization/registration",
            new { registrationId = _registrationId, organizationName = _organization, firstName = "Grace", lastName = "Hopper", acceptedLegalTerms = false, acceptedLegalVersion = string.Empty },
            _subject,
            _email);
        IsSuccess(result).ShouldBeTrue();
    });

    [Fact] void should_have_appended_to_the_outbox_before_the_instance_stopped() => PublishedWhileHeld.ShouldEqual(1);
    [Fact] void should_stop_the_forwarding_instance() => StoppedWithinTimeout.ShouldBeTrue();
    [Fact] void should_publish_the_registration_once() => Published.ShouldEqual(1);
    [Fact] void should_deliver_the_registration_to_the_host_once() => ReceivedByHost.ShouldEqual(1);
    [Fact] void should_not_fail_the_outbox_reactor_on_the_remaining_instance() => FailedPartitions.ShouldEqual(0);
}
