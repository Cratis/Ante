// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Ante.Contracts.Organization;
using Ante.Integration.given;
using Ante.Invitations.Receiving;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Integration.Organization.when_two_ante_instances_share_a_store;

/// <summary>
/// Two self-service registrations and one invited organization setup claim the same organization name, in three
/// casings, at the same moment through two instances. The unique organization name constraint is enforced by the
/// kernel and ignores casing, so exactly one claim is recorded and published whichever instance each went through,
/// and the others are refused with a command result rather than an error (Cratis/Ante#119).
/// </summary>
[Collection(ChronicleCollection.Name)]
public class and_three_onboardings_race_for_one_name : two_running_antes
{
    readonly string _organization = $"Race-{Guid.NewGuid():N}"[..20];
    readonly Guid _firstRegistration = Guid.NewGuid();
    readonly Guid _secondRegistration = Guid.NewGuid();
    readonly Guid _invitationId = NewInvitationId();
    readonly string _firstVisitor = $"visitor-{Guid.NewGuid():N}";
    readonly string _secondVisitor = $"visitor-{Guid.NewGuid():N}";
    readonly string _invitee = $"owner-{Guid.NewGuid():N}";
    JsonDocument[] _results;
    int _recordedClaims;
    int _publishedClaims;
    int _receivedByHost;

    async Task Establish()
    {
        await StartRegistration(Ante, _firstRegistration, _firstVisitor);
        await StartRegistration(Other, _secondRegistration, _secondVisitor);

        var issued = await Invite(Host, _invitationId, CreateInvitation());
        using var exchange = await Other.ExchangeInvitation(issued.Token, _invitee);
        exchange.EnsureSuccessStatusCode();
        await Eventually.Until(
            async () =>
            {
                await using var scope = Other.Services.CreateAsyncScope();
                return await scope.ServiceProvider.GetRequiredService<IEventStore>().ReadModels.GetInstanceById<PendingInvitationToCreateOrganization>(_invitationId) is not null;
            },
            what: "the pending create invitation to be projected");
    }

    async Task Because()
    {
        _results = await Task.WhenAll(
            Ante.Execute("/api/organization/registration", Registration(_firstRegistration, _organization), _firstVisitor, Email(_firstVisitor)),
            Other.Execute("/api/organization/registration", Registration(_secondRegistration, _organization.ToUpperInvariant()), _secondVisitor, Email(_secondVisitor)),
            Other.Execute(
                "/api/invitations/organization-setup",
                new { invitationId = _invitationId, organizationName = _organization.ToLowerInvariant(), firstName = "Ada", lastName = "Lovelace", acceptedLegalTerms = false, acceptedLegalVersion = string.Empty },
                _invitee));

        // Only an onboarding that won can have published; wait for the winner's publication, then count everything.
        if (IsSuccess(_results[0]))
        {
            await Host.WaitForFromAnte<OrganizationRegistrationCompleted>(_firstRegistration.ToString());
        }
        else if (IsSuccess(_results[1]))
        {
            await Host.WaitForFromAnte<OrganizationRegistrationCompleted>(_secondRegistration.ToString());
        }
        else if (IsSuccess(_results[2]))
        {
            await Host.WaitForFromAnte<InvitationToCreateTenantAccepted>(_invitationId.ToString());
        }

        Type[] claims = [typeof(OrganizationRegistrationCompleted), typeof(InvitationToCreateTenantAccepted)];
        foreach (var source in new[] { _firstRegistration, _secondRegistration, _invitationId }.Select(id => id.ToString("D")))
        {
            _recordedClaims += (await EventsFor(Ante, source, EventSequenceId.Log, claims)).Count;
            _publishedClaims += (await EventsFor(Ante, source, EventSequenceId.Outbox, claims)).Count;
            _receivedByHost += (await Host.ReceivedFromAnte(source)).Count(entry => entry.Content is OrganizationRegistrationCompleted or InvitationToCreateTenantAccepted);
        }
    }

    [Fact] void should_let_exactly_one_onboarding_claim_the_name() => _results.Count(IsSuccess).ShouldEqual(1);
    [Fact] void should_refuse_the_others_without_an_exception() => _results.Where(result => !IsSuccess(result)).All(HasNoExceptions).ShouldBeTrue();
    [Fact] void should_record_one_claim() => _recordedClaims.ShouldEqual(1);
    [Fact] void should_publish_one_claim() => _publishedClaims.ShouldEqual(1);
    [Fact] void should_deliver_one_claim_to_the_host() => _receivedByHost.ShouldEqual(1);

    static object Registration(Guid registrationId, string organizationName) =>
        new { registrationId, organizationName, firstName = "Grace", lastName = "Hopper", acceptedLegalTerms = false, acceptedLegalVersion = string.Empty };

    static string Email(string subject) => $"{subject}@example.com";

    static bool HasNoExceptions(JsonDocument result) =>
        !result.RootElement.TryGetProperty("hasExceptions", out var hasExceptions) || !hasExceptions.GetBoolean();

    static async Task StartRegistration(AnteApplication through, Guid registrationId, string subject)
    {
        var started = await through.Execute("/api/organization/registration/start", new { registrationId }, subject, Email(subject));
        if (!IsSuccess(started))
        {
            throw new InvalidOperationException($"Registration start did not succeed: {started.RootElement}");
        }
    }
}
