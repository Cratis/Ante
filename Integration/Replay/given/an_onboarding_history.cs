// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Legal;
using Ante.Contracts.Organization;
using Ante.Integration.given;
using Ante.Invitations.OrganizationSetup;
using Ante.Invitations.Receiving;
using Ante.Invitations.UserSetup;
using Ante.Legal;
using Ante.Legal.Receiving;
using Ante.Organization.Names;
using Ante.Organization.Registration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Ante.Integration.Replay.given;

/// <summary>
/// A store that has seen every kind of onboarding to completion, so every forwarding reactor, the token-issuing
/// reactor and every onboarding projection has history to replay.
/// </summary>
/// <remarks>
/// <para>
/// Ante starts without a signing key, defers one invitation's token and resumes it after a restart with a key - so the
/// token-issuing reactor has handled a receipt, a deferral and a resumption. The host then activates a legal document
/// set, and Ante records and publishes: a join-tenant acceptance and a create-tenant acceptance, each with legal terms;
/// two self-service registrations with legal terms; tokens for invitations still pending, one of them reissued; and a
/// rejection for a reissue of a revoked invitation.
/// </para>
/// <para>
/// Organization names get a history: one name the host still reserves; one it reserved and released; one it reserved
/// and released before a visitor registered it; one a visitor registered and the host released afterwards; and one an
/// invited organization claimed. The claim projection holds three of them; the kernel's constraint index holds the same
/// three.
/// </para>
/// </remarks>
public class an_onboarding_history : a_running_ante
{
    protected const string LegalVersion = "2026-01";

    protected readonly Guid Deferred = NewInvitationId();
    protected readonly Guid Joined = NewInvitationId();
    protected readonly Guid Created = NewInvitationId();
    protected readonly Guid Registered = Guid.NewGuid();
    protected readonly Guid RegisteredThenReleased = Guid.NewGuid();
    protected readonly Guid PendingJoin = NewInvitationId();
    protected readonly Guid PendingCreate = NewInvitationId();
    protected readonly Guid Reissued = NewInvitationId();
    protected readonly Guid Revoked = NewInvitationId();

    protected readonly string CreatedName = Name("Created");
    protected readonly string ReservedName = Name("Reserved");
    protected readonly string ReleasedName = Name("Released");
    protected readonly string ReclaimedName = Name("Reclaimed");
    protected readonly string RegisteredThenReleasedName = Name("Unclaimed");

    protected override bool SigningKeyConfigured => false;

    protected override bool UseLegalInbox => true;

    /// <summary>
    /// Gets the reactors that publish to Ante's outbox: every forwarding reactor and the token-issuing reactor.
    /// </summary>
    protected static IReadOnlyList<Type> PublishingReactors =>
    [
        typeof(JoinTenantAcceptanceOutbox),
        typeof(OrganizationSetupOutbox),
        typeof(OrganizationRegistrationOutbox),
        typeof(LegalTermsAcceptanceOutbox),
        typeof(LegalDocumentSetActivationOutbox),
        typeof(InvitationTokenIssuingReactor),
    ];

    /// <summary>
    /// Gets the outbox this history publishes, by type: one token per invitation, a second for the reissued one, a
    /// rejection, both acceptances, four legal acceptances, the legal activation and both registrations.
    /// </summary>
    protected static string ExpectedPublications => string.Join(", ", new[]
    {
        nameof(InvitationRejected),
        nameof(InvitationToCreateTenantAccepted),
        nameof(InvitationToJoinTenantAccepted),
        nameof(InvitationTokenIssued), nameof(InvitationTokenIssued), nameof(InvitationTokenIssued), nameof(InvitationTokenIssued),
        nameof(InvitationTokenIssued), nameof(InvitationTokenIssued), nameof(InvitationTokenIssued), nameof(InvitationTokenIssued),
        nameof(LegalDocumentSetActivated),
        nameof(LegalTermsAccepted), nameof(LegalTermsAccepted), nameof(LegalTermsAccepted), nameof(LegalTermsAccepted),
        nameof(OrganizationRegistrationCompleted), nameof(OrganizationRegistrationCompleted),
    }.Order(StringComparer.Ordinal));

    async Task Establish()
    {
        // A receipt without a signing key is deferred, and its token is issued when an instance with a key resumes it.
        await Host.Publish(Deferred, JoinInvitation(), subject: Guid.NewGuid());
        await Eventually.Until(
            async () => (await LocalEvents(Deferred.ToString("D"), typeof(InvitationTokenIssuanceDeferred))).Count > 0,
            what: "the first invitation's token to be deferred");
        await Restart(signingKeyConfigured: true);
        await Host.WaitForFromAnte<InvitationTokenIssued>(Deferred.ToString("D"), Eventually.DefaultTimeout + InvitationTokenIssuanceResumption.Interval);

        await Host.Publish(LegalDocumentSetId, new LegalDocumentSetPublished(1, LegalVersion, "Terms", "Privacy"));
        await Host.WaitForFromAnte<LegalDocumentSetActivated>(LegalDocumentSetId);

        var joiner = $"joiner-{Suffix}";
        await Exchange(await Invite(Host, Joined, JoinInvitation()), joiner);
        await ExecuteOnceProjected(
            "/api/invitations/user-setup",
            new { invitationId = Joined, firstName = "Jane", lastName = "Doe", acceptedLegalTerms = true, acceptedLegalVersion = LegalVersion },
            joiner);

        var creator = $"creator-{Suffix}";
        await Exchange(await Invite(Host, Created, CreateInvitation()), creator);
        await ExecuteOnceProjected(
            "/api/invitations/organization-setup",
            new { invitationId = Created, organizationName = CreatedName, firstName = "Ada", middleName = "Byron", lastName = "Lovelace", acceptedLegalTerms = true, acceptedLegalVersion = LegalVersion },
            creator);

        await Host.Publish(ReservedName, new OrganizationNameReserved(ReservedName));
        foreach (var name in new[] { ReleasedName, ReclaimedName })
        {
            await Host.Publish(name, new OrganizationNameReserved(name));
            await UntilClaimed(name, claimed: true);
            await Host.Publish(name, new OrganizationNameReleased(name));
            await UntilClaimed(name, claimed: false);
        }

        await Register(Registered, ReclaimedName);
        await Register(RegisteredThenReleased, RegisteredThenReleasedName);
        await UntilClaimed(RegisteredThenReleasedName, claimed: true);
        await Host.Publish(RegisteredThenReleasedName, new OrganizationNameReleased(RegisteredThenReleasedName));
        await UntilClaimed(RegisteredThenReleasedName, claimed: false);
        await UntilClaimed(ReservedName, claimed: true);

        await Invite(Host, PendingJoin, JoinInvitation());
        await Invite(Host, PendingCreate, CreateInvitation());
        await Invite(Host, Reissued, JoinInvitation());
        await Host.Publish(Reissued, new InvitationReissueRequested());
        await Invite(Host, Revoked, JoinInvitation());
        await Host.Publish(Revoked, new InvitationRevoked());
        await Host.Publish(Revoked, new InvitationReissueRequested());

        await UntilDelivered();
    }

    /// <summary>
    /// Gets the id Chronicle identifies a reactor's observer by.
    /// </summary>
    /// <param name="reactor">The reactor type.</param>
    /// <returns>The observer id.</returns>
    protected async Task<string> ReactorId(Type reactor)
    {
        await using var scope = Ante.Services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IEventStore>();
        return store.Reactors.GetHandlerById(reactor.FullName!).Id.Value;
    }

    /// <summary>
    /// Waits until every fact Ante published has reached the host - nothing is left in flight to arrive later.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    protected async Task UntilDelivered()
    {
        string? last = null;
        try
        {
            await Eventually.Until(
                async () =>
                {
                    var outbox = await Outbox();
                    var receipt = await All(Host.InboxFromAnte);
                    last = $"outbox [{OnboardingTrace.Types(outbox)}], host [{OnboardingTrace.Types(receipt)}]";
                    return OnboardingTrace.Types(outbox) == ExpectedPublications && OnboardingTrace.Types(receipt) == ExpectedPublications;
                },
                what: "every publication to reach the host");
        }
        catch (TimeoutException timeout)
        {
            throw new TimeoutException($"{timeout.Message} Expected [{ExpectedPublications}]; last seen {last}.", timeout);
        }
    }

    /// <summary>
    /// Reads Ante's outbox.
    /// </summary>
    /// <returns>Every event in it.</returns>
    protected async Task<IReadOnlyList<AppendedEvent>> Outbox()
    {
        await using var scope = Ante.Services.CreateAsyncScope();
        return await All(scope.ServiceProvider.GetRequiredService<IEventStore>().GetEventSequence(EventSequenceId.Outbox));
    }

    /// <summary>
    /// Snapshots Ante's outbox.
    /// </summary>
    /// <returns>One line per event.</returns>
    protected async Task<IReadOnlyList<string>> OutboxSnapshot()
    {
        await using var scope = Ante.Services.CreateAsyncScope();
        return await Snapshots.Of(scope.ServiceProvider.GetRequiredService<IEventStore>().GetEventSequence(EventSequenceId.Outbox));
    }

    /// <summary>
    /// Registers an organization the way the self-service wizard does, and waits for the host to receive it.
    /// </summary>
    /// <param name="registrationId">The registration id.</param>
    /// <param name="organizationName">The name to register.</param>
    /// <param name="subject">The signed-in visitor; a new one when not given.</param>
    /// <returns>The registration command's result.</returns>
    protected async Task<System.Text.Json.JsonDocument> TryRegister(Guid registrationId, string organizationName, string? subject = default)
    {
        subject ??= $"visitor-{Guid.NewGuid():N}";
        await Ante.Execute("/api/organization/registration/start", new { registrationId }, subject);
        return await Ante.Execute(
            "/api/organization/registration",
            new { registrationId, organizationName, firstName = "Grace", lastName = "Hopper", acceptedLegalTerms = true, acceptedLegalVersion = LegalVersion },
            subject);
    }

    static string Name(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..20];

    static async Task<IReadOnlyList<AppendedEvent>> All(IEventSequence sequence) =>
        [.. await sequence.GetFromSequenceNumber(EventSequenceNumber.First)];

    async Task Exchange(InvitationTokenIssued issued, string subject)
    {
        using var response = await Ante.ExchangeInvitation(issued.Token, subject);
        response.EnsureSuccessStatusCode();
    }

    async Task Register(Guid registrationId, string organizationName)
    {
        var result = await TryRegister(registrationId, organizationName);
        if (!IsSuccess(result))
        {
            throw new InvalidOperationException($"Registering {organizationName} did not succeed: {result.RootElement}");
        }

        await Host.WaitForFromAnte<OrganizationRegistrationCompleted>(registrationId.ToString("D"));
    }

    async Task UntilClaimed(string name, bool claimed)
    {
        await using var scope = Ante.Services.CreateAsyncScope();
        var claims = scope.ServiceProvider.GetRequiredService<IMongoCollection<OrganizationNameClaim>>();
        await Eventually.Until(
            async () => await ClaimedOrganizationNames.Contains(claims, name) == claimed,
            what: $"{name} to be {(claimed ? "claimed" : "free")}");
    }

    async Task<IReadOnlyList<AppendedEvent>> LocalEvents(string eventSourceId, Type eventType)
    {
        await using var scope = Ante.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IEventStore>().EventLog.GetForEventSourceIdAndEventTypes(eventSourceId, [eventType.GetEventType()]);
    }
}
