// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Legal;
using Ante.Contracts.Organization;
using Ante.Invitations.Accepting;
using Ante.Invitations.Receiving;
using Ante.Invitations.UserSetup;
using Ante.Legal;
using Ante.Organization;
using Ante.Organization.Names;
using Ante.Organization.Registration;
using Ante.Outbox;
using Ante.Resources;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Keys;
using Cratis.Types;
using MongoDB.Driver;

namespace Ante.Invitations.OrganizationSetup;

/// <summary>
/// Represents the current acceptance status for organization setup onboarding.
/// </summary>
public enum OrganizationSetupAcceptanceStatus
{
    /// <summary>
    /// Setup has not been recorded yet - there is nothing to resume, so a client may safely (re)submit.
    /// </summary>
    Pending,

    /// <summary>
    /// Setup has been recorded to Ante's own event log but has not yet fully reached the outbox. A
    /// client observing this must keep waiting rather than resubmitting: resubmitting would collide with
    /// the one-use invitation constraint, or, for self-service registration, would attempt to claim the
    /// same organization name a second time.
    /// </summary>
    Recorded,

    /// <summary>
    /// Setup - and any required legal fact - has fully reached the outbox. This is the only state safe
    /// to hand off to the host.
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
    /// <param name="options">The deployment's options, holding the reserved organization names.</param>
    public SetupOrganizationValidator(
        ILegalDocumentSource legalDocumentSource,
        IMongoCollection<OrganizationNameClaim> acceptedOrganizationNames,
        ISignedInIdentity signedInIdentity,
        IOptions<AnteOptions> options)
    {
        // The invitation id travels in the open - a URL, a token, an invite link - so knowing it must
        // never be enough to act on it. Only a caller who has verifiably exchanged this exact invitation
        // may use it to set up the organization; the same message the pending-invitation check below
        // uses, so a mismatched owner is indistinguishable from an invitation that is no longer pending.
        RuleFor(c => c.InvitationId)
            .Must(invitationId => signedInIdentity.IsVerifiedOwnerOf(invitationId))
            .WithMessage(_ => Messages.Get("SetupNotPending"));

        RuleFor(c => (string)c.OrganizationName)
            .MustBeAValidOrganizationName();

        RuleFor(c => (string)c.OrganizationName)
            .Must(organizationName => !ReservedOrganizationNames.IsReserved(options.Value, organizationName))
            .WithMessage(_ => Messages.Get("OrganizationNameReserved"));

        // Expressed as a rule rather than only as a handler check so the wizard's eager server
        // validation reaches it: the validator is what the /validate endpoint runs, and a failure it
        // reports is attributed to the organization name field, so the rejection lands on the step that
        // owns the field instead of only surfacing once the final submit has already failed.
        RuleFor(c => (string)c.OrganizationName)
            .MustAsync(async (organizationName, _) => !await ClaimedOrganizationNames.Contains(acceptedOrganizationNames, organizationName))
            .WithMessage(_ => Messages.Get("OrganizationNameExists"));

        RuleFor(c => (string)c.FirstName).MustBeARequiredName("First name");
        RuleFor(c => (string)c.LastName).MustBeARequiredName("Last name");

        // Compares against null and casts rather than coalescing against MiddleName.NotSet, so the
        // selector never reads a static member of the concept type itself - a shape ARC0013 cannot tell
        // apart from dereferencing a possibly-null concept (false positive: Cratis/Arc#2658). The lambda
        // also can't yield a member name FluentValidation can infer, so OverridePropertyName pins the
        // failure to the field the wizard actually renders instead of leaving it unattributed.
        RuleFor(c => c.MiddleName == null ? string.Empty : (string)c.MiddleName)
            .MustBeAValidName("Middle name")
            .OverridePropertyName(nameof(SetupOrganization.MiddleName));

        LegalTermsRules.Apply(this, legalDocumentSource, c => c.AcceptedLegalTerms, c => c.AcceptedLegalVersion);
    }
}

/// <summary>
/// Enforces that each organization name is used only once across all organizations, whether the
/// organization was created from an accepted invitation or through self-service registration.
/// </summary>
/// <remarks>
/// The validator and the command handler both reject a duplicate by reading
/// <see cref="OrganizationNameClaim"/>, which produces the friendly message users see. This
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
            .On<OrganizationNameReservationReceived>(@event => @event.TenantName)
            .RemovedWith<OrganizationNameReleaseReceived>()
            .IgnoreCasing()
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
    /// <param name="existingSetup">Durable evidence that this stream was already used before the shared claim event existed.</param>
    /// <param name="existingJoin">Durable join acceptance on a reused id predating the shared claim event.</param>
    /// <param name="acceptedOrganizationNames">The organization names already claimed by accepted invitations.</param>
    /// <param name="signedInIdentity">The identity the user is signed in with for this request.</param>
    /// <param name="legalDocumentSource">The legal document source to resolve authoritative acceptance evidence against.</param>
    /// <param name="acceptanceFence">The authoritative invitation revision used to fence revocation.</param>
    /// <param name="eventStore">The local event store for composing the atomic append.</param>
    /// <returns>
    /// A <see cref="Result{T0, T1}"/> containing either a failed <see cref="ValidationResult"/> or the
    /// compliance subject and events to append.
    /// </returns>
    /// <remarks>
    /// Does not mark the invitation as accepted here - that would be a pre-append success signal, visible
    /// to a polling client before the event this method returns has even been appended, let alone
    /// forwarded to the outbox. <see cref="OrganizationSetupOutbox"/> marks it once the acceptance is
    /// verifiably durable in Ante's own outbox instead.
    /// </remarks>
    public async Task<Result<ValidationResult, (Cratis.Chronicle.Subject, EventsWithConcurrencyScopes)>> Handle(
        IHttpContextAccessor httpContextAccessor,
        PendingInvitationToCreateOrganization? pendingInvitation,
        OrganizationSetupProgress? existingSetup,
        UserSetupProgress? existingJoin,
        IMongoCollection<OrganizationNameClaim> acceptedOrganizationNames,
        ISignedInIdentity signedInIdentity,
        ILegalDocumentSource legalDocumentSource,
        IInvitationAcceptanceFence acceptanceFence,
        IEventStore eventStore)
    {
        // Re-read rather than trust the validator: the name can be claimed between the two, and this is
        // the last look before the events are composed. The member is named the way the client names
        // it, because a result composed here is passed through as written - only failures a validator
        // reports get camelCased for the client - and without it the wizard cannot put the message on
        // the field.
        if (await ClaimedOrganizationNames.Contains(acceptedOrganizationNames, OrganizationName))
        {
            return ValidationResult.Error(Messages.Get("OrganizationNameInUse"), ["organizationName"]);
        }

        if (pendingInvitation is null || existingSetup is not null || existingJoin is not null)
        {
            return ValidationResult.Error(Messages.Get("SetupNotPending"));
        }

        var (identityProviderValue, complianceSubject) = signedInIdentity.Resolve(InvitationId, (Cratis.Chronicle.Subject)pendingInvitation.Subject);
        if (string.IsNullOrWhiteSpace(identityProviderValue.Value))
        {
            return ValidationResult.Error(Messages.Get("SetupIdentityRequired"));
        }

        var legalResolution = await LegalAcceptanceEvidence.ResolveWithScope(
            legalDocumentSource,
            AcceptedLegalTerms,
            AcceptedLegalVersion,
            OrganizationName,
            identityProviderValue,
            complianceSubject.ToString());
        if (!legalResolution.TryGetResult(out var legalEvidence))
        {
            legalResolution.TryGetError(out var legalError);
            return legalError;
        }

        if (!signedInIdentity.IsVerifiedOwnerOf(InvitationId))
        {
            return ValidationResult.Error(Messages.Get("SetupNotPending"));
        }

        var owner = signedInIdentity.AttestedOwnerOf(InvitationId);
        if (signedInIdentity.IsAttestedExchange && owner is null)
        {
            return ValidationResult.Error(Messages.Get("SetupNotPending"));
        }

        var events = new List<object>
        {
            new OnboardingAttemptClaimed(),
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
        if (owner is not null)
        {
            events.Add(owner);
        }

        events.AddRange(legalEvidence.Events);

        var scope = await acceptanceFence.For(InvitationId, InvitationFlowType.CreateTenant);
        if (scope is null)
        {
            return ValidationResult.Error(Messages.Get("SetupNotPending"));
        }

        httpContextAccessor.HttpContext?.Response.Cookies.Delete(Cratis.Arc.Identity.IdentityProvider.IdentityCookieName);

        return (complianceSubject, await LegalAcceptanceEvidence.ForAppend(eventStore, InvitationId, events, legalEvidence, scope));
    }
}

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
    /// Gets the organization setup acceptance status for a specific invitation or registration.
    /// </summary>
    /// <remarks>
    /// Durable evidence from both the local record and the outbox is read first, so a re-entering user -
    /// new tab, restarted Ante, or a dropped connection reconnecting to a different replica - resumes
    /// into <see cref="OrganizationSetupAcceptanceStatus.Recorded"/> or
    /// <see cref="OrganizationSetupAcceptanceStatus.Accepted"/> precisely once durable evidence supports
    /// it, never from an in-memory flag alone. A client that only ever sees Pending has never actually
    /// recorded anything and may safely (re)submit; one that sees Recorded has already submitted and must
    /// keep waiting rather than resubmitting, even if the local browser tab restarted in between.
    /// </remarks>
    /// <param name="invitationId">The invitation identifier.</param>
    /// <param name="signedInIdentity">Verifier of invitation ownership.</param>
    /// <param name="subscriptions">The subscription tracker.</param>
    /// <param name="recordedCollection">The durable setup-record collection.</param>
    /// <param name="publishedCollection">The durable outbox-publication collection.</param>
    /// <param name="eventStore">The scoped store used to release the committed owner's identity.</param>
    /// <returns>An observable status stream for the invitation.</returns>
    public static ISubject<OrganizationSetupAcceptanceStatusView> StatusForInvitation(
        InvitationId invitationId,
        ISignedInIdentity signedInIdentity,
        OrganizationSetupStatusSubscriptions subscriptions,
        IMongoCollection<OrganizationSetupProgress> recordedCollection,
        IMongoCollection<OrganizationSetupPublished> publishedCollection,
        IEventStore eventStore)
    {
        if (!signedInIdentity.IsVerifiedRecoveryOwnerOf(invitationId, eventStore))
        {
            return new BehaviorSubject<OrganizationSetupAcceptanceStatusView>(new(invitationId, OrganizationSetupAcceptanceStatus.Pending, TenantName.NotSet));
        }

        var recorded = recordedCollection.Find(Builders<OrganizationSetupProgress>.Filter.Eq(progress => progress.Id, invitationId)).FirstOrDefault();
        var published = publishedCollection.Find(Builders<OrganizationSetupPublished>.Filter.Eq(progress => progress.Id, invitationId)).FirstOrDefault();
        var status = subscriptions.GetStatus(
            invitationId,
            recorded?.OrganizationName,
            isRecorded: recorded is not null,
            isFullyPublished: OrganizationSetupPublication.IsFullyPublished(recorded, published));
        if (!signedInIdentity.IsAttestedExchange)
        {
            return status;
        }

        var actor = signedInIdentity.CaptureRecoveryActor();
        return actor is null
            ? new BehaviorSubject<OrganizationSetupAcceptanceStatusView>(new(invitationId, OrganizationSetupAcceptanceStatus.Pending, TenantName.NotSet))
            : new InvitationStatusOwnerFilter<OrganizationSetupAcceptanceStatusView>(
                status, invitationId, actor, eventStore, view => view.Status == OrganizationSetupAcceptanceStatus.Pending);
    }

    /// <summary>
    /// Returns a snapshot of a self-service registration only to the owner recorded with it.
    /// A missing owner (including registrations created before ownership was recorded) is unknown.
    /// </summary>
    /// <param name="registrationId">The registration identifier.</param>
    /// <param name="signedInIdentity">Verifier of the current login.</param>
    /// <param name="subscriptions">The subscription tracker.</param>
    /// <param name="eventStore">The scoped event store that releases protected registration owner data.</param>
    /// <param name="publishedCollection">The durable outbox-publication collection.</param>
    /// <returns>Current status or the same pending response as an unknown registration.</returns>
    public static async Task<OrganizationSetupAcceptanceStatusView> StatusForRegistration(
        InvitationId registrationId,
        ISignedInIdentity signedInIdentity,
        OrganizationSetupStatusSubscriptions subscriptions,
        IEventStore eventStore,
        IMongoCollection<OrganizationSetupPublished> publishedCollection)
    {
        var unknown = new OrganizationSetupAcceptanceStatusView(registrationId, OrganizationSetupAcceptanceStatus.Pending, TenantName.NotSet);
        var recorded = await eventStore.ReadModels.GetInstanceById<OrganizationSetupProgress>(registrationId.Value);
        if (recorded?.OwnerSubject is not { } ownerSubject || recorded.OwnerProvider is not { } ownerProvider ||
            !signedInIdentity.IsVerifiedRegistrationOwner(new RegistrationOwner(ownerSubject, ownerProvider)))
        {
            return unknown;
        }

        var published = await publishedCollection.Find(Builders<OrganizationSetupPublished>.Filter.Eq(progress => progress.Id, registrationId)).FirstOrDefaultAsync();
        return ((BehaviorSubject<OrganizationSetupAcceptanceStatusView>)subscriptions.GetStatus(
            registrationId,
            recorded.OrganizationName,
            isRecorded: true,
            isFullyPublished: OrganizationSetupPublication.IsFullyPublished(recorded, published))).Value;
    }
}

/// <summary>
/// Forwards <see cref="InvitationToCreateTenantAccepted"/> to the outbox so the host can subscribe.
/// </summary>
/// <remarks>
/// Pinned to the event log deliberately - see the remarks on <c language="csharp">LegalTermsAcceptanceOutbox</c>
/// for why an unattributed reactor handling a <c language="csharp">Cratis.Ante.Contracts</c> event is unsafe to route once a
/// deployment renames its store away from the compiled "Ante" literal.
/// </remarks>
/// <param name="eventStore">The event store.</param>
/// <param name="notifiers">Every registered <see cref="IPublicationStatusNotifier"/>, given a chance to accelerate a live status subscription once this fact is durably published.</param>
/// <param name="logger">The logger.</param>
[Reactor(eventSequence: EventSequenceId.LogId)]
public class OrganizationSetupOutbox(IEventStore eventStore, IInstancesOf<IPublicationStatusNotifier> notifiers, ILogger<OrganizationSetupOutbox> logger) : IReactor
{
    /// <summary>
    /// Forwards the create-tenant accepted event to the outbox.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The event context.</param>
    public async Task On(InvitationToCreateTenantAccepted @event, EventContext context) =>
        await eventStore.PublishToOutbox(context, @event, notifiers, logger);
}
