// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.given;
using Ante.Invitations.OrganizationSetup;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Integration.Invitations.when_ante_restarts;

/// <summary>
/// Ante stops after recording an invited organization setup but before its outbox reactor published it; the
/// restarted instance publishes it once and restores the owner's status (Cratis/Ante#112).
/// </summary>
[Collection(ChronicleCollection.Name)]
public class and_an_organization_setup_was_recorded_but_not_published : a_running_ante
{
    readonly Guid _invitationId = NewInvitationId();
    readonly string _owner = $"owner-{Guid.NewGuid():N}";
    readonly string _organizationName = $"Restart{Guid.NewGuid():N}"[..18];
    bool _recordedBeforePublication;
    int _acceptances;
    int _status;
    string _restoredOrganizationName = string.Empty;

    async Task Establish()
    {
        var issued = await Invite(Host, _invitationId, CreateInvitation());
        using var response = await Ante.ExchangeInvitation(issued.Token, _owner);
        response.EnsureSuccessStatusCode();

        await using var scope = Ante.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<IEventStore>().Reactors.GetHandlerFor<OrganizationSetupOutbox>();
        handler.Disconnect();
        await Eventually.Until(async () => !(await handler.GetState()).IsSubscribed, what: "the organization setup outbox reactor disconnecting");

        var result = await ExecuteOnceProjected(
            "/api/invitations/organization-setup",
            new { invitationId = _invitationId, organizationName = _organizationName, firstName = "Jane", lastName = "Doe", acceptedLegalTerms = false, acceptedLegalVersion = "" },
            _owner);
        IsSuccess(result).ShouldBeTrue();
        _recordedBeforePublication = await AcceptanceRecordedButNotPublished<InvitationToCreateTenantAccepted>(_invitationId);
    }

    async Task Because()
    {
        await Restart();
        await Host.WaitForFromAnte<InvitationToCreateTenantAccepted>(_invitationId.ToString());
        _acceptances = (await Host.ReceivedFromAnte(_invitationId.ToString())).Count(entry => entry.Content is InvitationToCreateTenantAccepted);
        await Eventually.Until(
            async () =>
            {
                var status = (await Ante.OrganizationInvitationStatus(_invitationId, _owner)).RootElement.GetProperty("data");
                _status = status.GetProperty("status").GetInt32();
                _restoredOrganizationName = status.GetProperty("organizationName").GetString() ?? string.Empty;
                return _status == 2;
            },
            what: "the owner's terminal organization setup status after the restart");
    }

    [Fact] void should_have_recorded_the_setup_without_publishing_it_before_the_restart() => _recordedBeforePublication.ShouldBeTrue();
    [Fact] void should_publish_the_acceptance_once() => _acceptances.ShouldEqual(1);
    [Fact] void should_show_the_owner_the_terminal_status() => _status.ShouldEqual(2);
    [Fact] void should_restore_the_organization_name() => _restoredOrganizationName.ShouldEqual(_organizationName);
}
