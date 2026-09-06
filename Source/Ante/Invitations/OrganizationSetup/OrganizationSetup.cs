// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Legal;
using Ante.Contracts.Organization;
using Ante.Invitations.Accepting;
using Ante.Invitations.Receiving;
using Ante.Legal;
using Ante.Organization;
using Cratis.Chronicle.Keys;
using MongoDB.Driver;

namespace Ante.Invitations.OrganizationSetup;

/// <summary>
/// Represents the current acceptance status for organization setup onboarding.
/// </summary>
public enum OrganizationSetupAcceptanceStatus
{
    /// <summary>
    /// The organization creation confirmation has not been received yet.
    /// </summary>
    Pending,

    /// <summary>
    /// The organization creation confirmation has been received.
    /// </summary>
    Accepted,
}

/// <summary>
/// The names of the constraints guarding organization setup.
/// </summary>
public static class OrganizationSetupConstraintNames
{
    /// <summary>
    /// The constraint keeping an organization name bound to a single organization, across both
    /// invited tenant creation and self-service registration.
    /// </summary>
    public const string UniqueOrganizationName = "UniqueOrganizationName";

    /// <summary>
    /// The constraint keeping a create-tenant invitation acceptable only once.
    /// </summary>
    public const string OneUseInvitation = "OneUseCreateTenantInvitation";
}

/// <summary>
/// Validator for the <see cref="SetupOrganization"/> command.
/// </summary>
public class SetupOrganizationValidator : CommandValidator<SetupOrganization>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetupOrganizationValidator"/> class.
    /// </summary>
    /// <param name="legalDocumentSource">The legal document source the onboarding user has to accept, when the host has configured one.</param>
    /// <param name="acceptedOrganizationNames">The organization names already claimed by accepted invitations.</param>
    /// <param name="signedInIdentity">The identity the user is signed in with for this request.</param>
    public SetupOrganizationValidator(
        ILegalDocumentSource legalDocumentSource,
        IMongoCollection<AcceptedOrganizationName> acceptedOrganizationNames,
        ISignedInIdentity signedInIdentity)
    {
        // The invitation id travels in the open - a URL, a token, an invite link - so knowing it must
        // never be enough to act on it. Only a caller who has verifiably exchanged this exact invitation
        // may use it to set up the organization; the same message the pending-invitation check below
        // uses, so a mismatched owner is indistinguishable from an invitation that is no longer pending.
        RuleFor(c => c.InvitationId)
            .Must(invitationId => signedInIdentity.IsVerifiedOwnerOf(invitationId))
            .WithMessage("Invitation is no longer pending and cannot be used for organization setup.");

        RuleFor(c => (string)c.OrganizationName)
            .MustBeAValidOrganizationName();

        // Expressed as a rule rather than only as a handler check so the wizard's eager server
        // validation reaches it: the validator is what the /validate endpoint runs, and a failure it
        // reports is attributed to the organization name field, so the rejection lands on the step that
        // owns the field instead of only surfacing once the final submit has already failed.
        RuleFor(c => (string)c.OrganizationName)
            .MustAsync(async (organizationName, _) => !await ClaimedOrganizationNames.Contains(acceptedOrganizationNames, organizationName))
            .WithMessage("An organization with this name already exists.");

        RuleFor(c => (string)c.FirstName)
            .NotEmpty()
            .WithMessage("First name is required.");

        RuleFor(c => (string)c.LastName)
            .NotEmpty()
            .WithMessage("Last name is required.");

        LegalTermsRules.Apply(this, legalDocumentSource, c => c.AcceptedLegalTerms, c => c.AcceptedLegalVersion);
    }
}

/// <summary>
/// Enforces that each organization name is used only once across all organizations, whether the
/// organization was created from an accepted invitation or through self-service registration.
/// </summary>
/// <remarks>
/// The validator and the command handler both reject a duplicate by reading
/// <see cref="AcceptedOrganizationName"/>, which produces the friendly message users see. This
/// constraint is the race-safe backstop for those checks: two invitations - or an invitation and a
/// self-service registration - can be accepted concurrently with the same organization name, and both
/// would read no match. Both events are declared under one constraint name, so an invited creation and
/// a self-registration compete for the name under a single coordinated decision instead of two
/// independent ones that could each let the name through.
/// </remarks>
public class UniqueOrganizationNameConstraint : IConstraint
{
    /// <inheritdoc/>
    public void Define(IConstraintBuilder builder) => builder
        .Unique(unique => unique
            .WithName(OrganizationSetupConstraintNames.UniqueOrganizationName)
            .On<InvitationToCreateTenantAccepted>(@event => @event.TenantName)
            .On<OrganizationRegistrationCompleted>(@event => @event.TenantName)
            .WithMessage("An organization with this name already exists."));
}

/// <summary>
/// Enforces that a create-tenant invitation can be accepted at most once.
/// </summary>
/// <remarks>
/// <see cref="SetupOrganization"/> treats an already-accepted invitation as no longer pending by
/// reading <see cref="PendingInvitationToCreateOrganization"/>, which is removed once accepted - but
/// that is a read-model check and races: two concurrent submits of the same invitation can both observe
/// it as still pending before either append lands. This constraint is the atomic backstop, enforced by
/// the kernel at append time - only one <see cref="InvitationToCreateTenantAccepted"/> can ever be
/// appended per invitation (its event source).
/// </remarks>
public class OneUseCreateTenantInvitationConstraint : IConstraint
{
    /// <inheritdoc/>
    public void Define(IConstraintBuilder builder) => builder
        .Unique<InvitationToCreateTenantAccepted>(
            "This invitation has already been accepted.",
            OrganizationSetupConstraintNames.OneUseInvitation);
}

/// <summary>
/// Read model tracking every organization name claimed by an accepted invitation or self-service
/// registration.
/// </summary>
/// <param name="TenantName">The claimed name.</param>
[ReadModel]
[FromEvent<InvitationToCreateTenantAccepted>]
[FromEvent<OrganizationRegistrationCompleted>]
public record AcceptedOrganizationName([Key] TenantName TenantName);

/// <summary>
/// Reads whether an organization name has already been claimed by an accepted invitation.
/// </summary>
/// <remarks>
/// Asking whether a <em>name</em> is taken is a search across every claim, not a lookup of one: the
/// model is keyed by the invitation the accepting event was appended to, and the name is a field on it.
/// So it is counted with a filter, rather than injected - Arc injects a read model only when it
/// resolves for the command's own event source id, and a command that named an organization would then
/// get the claim belonging to its own invitation.
/// <para>
/// It lives here rather than as a static method on <see cref="AcceptedOrganizationName"/> because every
/// static method on a <c>[ReadModel]</c> is discovered as a query and published as a proxy, and this is
/// command-side only.
/// </para>
/// </remarks>
public static class ClaimedOrganizationNames
{
    /// <summary>
    /// Determines whether an organization name has already been claimed.
    /// </summary>
    /// <param name="acceptedOrganizationNames">The claimed organization names to search.</param>
    /// <param name="organizationName">The organization name to look for.</param>
    /// <returns>True when the name is already claimed; otherwise false.</returns>
    public static async Task<bool> Contains(IMongoCollection<AcceptedOrganizationName> acceptedOrganizationNames, string organizationName) =>
        !string.IsNullOrEmpty(organizationName) &&
        await acceptedOrganizationNames.CountDocumentsAsync(
            Builders<AcceptedOrganizationName>.Filter.Eq(accepted => accepted.TenantName, (TenantName)organizationName),
            cancellationToken: default) > 0;
}

/// <summary>
/// Command for setting up a new tenant organization during the create-tenant invitation flow.
/// </summary>
/// <param name="InvitationId">The invitation this organization setup is for.</param>
/// <param name="OrganizationName">The name of the organization (tenant) to create.</param>
/// <param name="FirstName">The first name of the user being onboarded.</param>
/// <param name="MiddleName">The middle name of the user being onboarded.</param>
/// <param name="LastName">The last name of the user being onboarded.</param>
/// <param name="AcceptedLegalTerms">Whether the user has accepted the terms and conditions and the privacy policy.</param>
/// <param name="AcceptedLegalVersion">The version of the legal document set that was presented and accepted.</param>
[Command]
public record SetupOrganization(InvitationId InvitationId, TenantName OrganizationName, FirstName FirstName, MiddleName? MiddleName, LastName LastName, bool AcceptedLegalTerms, LegalVersion AcceptedLegalVersion)
{
    /// <summary>
    /// Handles the command by producing an <see cref="InvitationToCreateTenantAccepted"/> event and,
    /// when the host has a legal document source configured, the <see cref="LegalTermsAccepted"/>
    /// record of what the onboarding user agreed to.
    /// </summary>
    /// <param name="httpContextAccessor">Accessor for the current request, used to clear the stale identity cookie.</param>
    /// <param name="pendingInvitation">Read model for validating the command.</param>
    /// <param name="acceptedOrganizationNames">The organization names already claimed by accepted invitations.</param>
    /// <param name="subscriptions">The organization setup status subscriptions.</param>
    /// <param name="signedInIdentity">The identity the user is signed in with for this request.</param>
    /// <returns>
    /// A <see cref="Result{T0, T1}"/> containing either a failed <see cref="ValidationResult"/> or the
    /// compliance subject and events to append.
    /// </returns>
    public async Task<Result<ValidationResult, (Cratis.Chronicle.Subject, IEnumerable<object>)>> Handle(
        IHttpContextAccessor httpContextAccessor,
        PendingInvitationToCreateOrganization? pendingInvitation,
        IMongoCollection<AcceptedOrganizationName> acceptedOrganizationNames,
        OrganizationSetupStatusSubscriptions subscriptions,
        ISignedInIdentity signedInIdentity)
    {
        // Re-read rather than trust the validator: the name can be claimed between the two, and this is
        // the last look before the events are composed. The member is named the way the client names
        // it, because a result composed here is passed through as written - only failures a validator
        // reports get camelCased for the client - and without it the wizard cannot put the message on
        // the field.
        if (await ClaimedOrganizationNames.Contains(acceptedOrganizationNames, OrganizationName))
        {
            return ValidationResult.Error("Organization name is already in use.", ["organizationName"]);
        }

        if (pendingInvitation is null)
        {
            return ValidationResult.Error("Invitation is no longer pending and cannot be used for organization setup.");
        }

        var (identityProviderValue, complianceSubject) = signedInIdentity.Resolve(InvitationId, (Cratis.Chronicle.Subject)pendingInvitation.Subject);

        subscriptions.MarkAccepted(InvitationId, OrganizationName);
        httpContextAccessor.HttpContext?.Response.Cookies.Delete(Cratis.Arc.Identity.IdentityProvider.IdentityCookieName);

        var events = new List<object>
        {
            new InvitationToCreateTenantAccepted(
                OrganizationName,
                identityProviderValue,
                complianceSubject.ToString(),
                FirstName,
                MiddleName ?? Contracts.Invitations.MiddleName.NotSet,
                LastName,
                pendingInvitation.Email,
                pendingInvitation.Roles),
        };

        if (AcceptedLegalTerms)
        {
            events.Add(new LegalTermsAccepted(OrganizationName, identityProviderValue, complianceSubject.ToString(), AcceptedLegalVersion));
        }

        return (complianceSubject, events);
    }
}

/// <summary>
/// Durable projection of whether organization setup for an invitation has been submitted. An instance
/// exists only once setup has been submitted; absence means setup never started (or the invitation is
/// unknown), which tells the frontend it is safe to (re)submit.
/// </summary>
/// <param name="Id">The invitation identifier.</param>
/// <param name="OrganizationName">The name of the organization that was set up.</param>
[ReadModel]
[FromEvent<InvitationToCreateTenantAccepted>]
public record OrganizationSetupProgress(
    InvitationId Id,
    [SetFrom<InvitationToCreateTenantAccepted>(nameof(InvitationToCreateTenantAccepted.TenantName))] TenantName OrganizationName);

/// <summary>
/// Represents the current organization setup acceptance status.
/// </summary>
/// <param name="InvitationId">The invitation identifier.</param>
/// <param name="Status">The live acceptance status.</param>
/// <param name="OrganizationName">The name of the organization being set up; empty until known.</param>
[ReadModel]
public record OrganizationSetupAcceptanceStatusView(InvitationId InvitationId, OrganizationSetupAcceptanceStatus Status, TenantName OrganizationName)
{
    /// <summary>
    /// Gets the organization setup acceptance status for a specific invitation.
    /// </summary>
    /// <remarks>
    /// The durable <see cref="OrganizationSetupProgress"/> is consulted first so a re-entering user -
    /// new tab, restarted Ante, expired in-memory entry - resumes into the accepted state instead of
    /// getting a fresh pending entry for a setup that already completed.
    /// </remarks>
    /// <param name="invitationId">The invitation identifier.</param>
    /// <param name="subscriptions">The subscription tracker.</param>
    /// <param name="progressCollection">The durable setup progress collection.</param>
    /// <returns>An observable status stream for the invitation.</returns>
    public static ISubject<OrganizationSetupAcceptanceStatusView> StatusForInvitation(
        InvitationId invitationId,
        OrganizationSetupStatusSubscriptions subscriptions,
        IMongoCollection<OrganizationSetupProgress> progressCollection)
    {
        var durableProgress = progressCollection
            .Find(Builders<OrganizationSetupProgress>.Filter.Eq(progress => progress.Id, invitationId))
            .FirstOrDefault();
        return subscriptions.GetStatus(invitationId, durableProgress);
    }
}

/// <summary>
/// Forwards <see cref="InvitationToCreateTenantAccepted"/> to the outbox so the host can subscribe.
/// </summary>
/// <param name="eventStore">The event store.</param>
[Reactor]
public class OrganizationSetupOutbox(IEventStore eventStore) : IReactor
{
    /// <summary>
    /// Forwards the create-tenant accepted event to the outbox.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The event context.</param>
    public async Task On(InvitationToCreateTenantAccepted @event, EventContext context) =>
        await eventStore.GetEventSequence(EventSequenceId.Outbox).Append(context.EventSourceId, @event);
}
