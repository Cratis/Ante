// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Organization;
using Ante.Integration.given;
using Ante.Organization.Registration;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Integration.Organization.when_ante_restarts;

[Collection(ChronicleCollection.Name)]
public class and_a_registration_was_recorded_but_not_published : a_running_ante
{
    readonly Guid _registrationId = Guid.NewGuid();
    readonly string _owner = $"visitor-{Guid.NewGuid():N}";
    readonly string _organization = $"Restart-{Guid.NewGuid():N}"[..20];
    bool _recordedBeforePublication;
    int _publications;
    int _receipts;

    async Task Establish()
    {
        var started = await Ante.Execute("/api/organization/registration/start", new { registrationId = _registrationId }, _owner, $"{_owner}@example.com");
        IsSuccess(started).ShouldBeTrue();

        await using var scope = Ante.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<IEventStore>().Reactors.GetHandlerFor<OrganizationRegistrationOutbox>();
        handler.Disconnect();
        await Eventually.Until(async () => !(await handler.GetState()).IsSubscribed, what: "the registration outbox reactor disconnecting");

        var result = await ExecuteOnceProjected(
            "/api/organization/registration",
            new { registrationId = _registrationId, organizationName = _organization, firstName = "Grace", lastName = "Hopper", acceptedLegalTerms = false, acceptedLegalVersion = string.Empty },
            _owner,
            $"{_owner}@example.com");
        IsSuccess(result).ShouldBeTrue();
        _recordedBeforePublication = await AcceptanceRecordedButNotPublished<OrganizationRegistrationRecorded, OrganizationRegistrationCompleted>(_registrationId);
    }

    async Task Because()
    {
        await Restart();
        await Host.WaitForFromAnte<OrganizationRegistrationCompleted>(_registrationId.ToString("D"));
        await Restart();
        await using var scope = Ante.Services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IEventStore>();
        _publications = (await store.GetEventSequence(EventSequenceId.Outbox).GetForEventSourceIdAndEventTypes(
            _registrationId.ToString("D"), [typeof(OrganizationRegistrationCompleted).GetEventType()])).Count;
        _receipts = (await Host.ReceivedFromAnte(_registrationId.ToString("D"))).Count(entry => entry.Content is OrganizationRegistrationCompleted);
    }

    [Fact] void should_record_the_private_fact_before_publication() => _recordedBeforePublication.ShouldBeTrue();
    [Fact] void should_publish_the_registration_once_across_restarts() => _publications.ShouldEqual(1);
    [Fact] void should_deliver_the_registration_once() => _receipts.ShouldEqual(1);
}
