// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Legal;
using Ante.Contracts.Organization;
using Ante.IdentityProviders;
using Ante.Invitations;
using Ante.Invitations.OrganizationSetup;
using Ante.Invitations.Receiving;
using Ante.Invitations.UserSetup;
using Ante.Legal;
using Ante.Outbox;
using Ante.Resources;
using Cratis.Arc.Validation;
using Cratis.Types;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace Ante.Organization.Registration;

/// <summary>
/// Validator for the <see cref="RegisterOrganization"/> command.
/// </summary>
public class RegisterOrganizationValidator : CommandValidator<RegisterOrganization>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterOrganizationValidator"/> class.
    /// </summary>
    /// <param name="legalDocumentSource">The legal document source the registering user has to accept, when the host has configured one.</param>
    /// <param name="acceptedOrganizationNames">The organization names already claimed by accepted invitations.</param>
    /// <param name="httpContextAccessor">Accessor for the current sign-in.</param>
    /// <param name="identityProviderResolver">Resolver of the current sign-in provider.</param>
    /// <param name="eventStore">The current namespace's read models for checking prior use of the registration id.</param>
    public RegisterOrganizationValidator(
        ILegalDocumentSource legalDocumentSource,
        IMongoCollection<AcceptedOrganizationName> acceptedOrganizationNames,
        IHttpContextAccessor httpContextAccessor,
        IIdentityProviderResolver identityProviderResolver,
        IEventStore eventStore)
    {
        RuleFor(c => (string)c.OrganizationName)
            .MustBeAValidOrganizationName();

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
            .OverridePropertyName(nameof(RegisterOrganization.MiddleName));

        LegalTermsRules.Apply(this, legalDocumentSource, c => c.AcceptedLegalTerms, c => c.AcceptedLegalVersion);

        RuleFor(c => c)
            .Must(_ => RegistrationOwner.Resolve(httpContextAccessor, identityProviderResolver) is { } owner &&
                !string.IsNullOrWhiteSpace(owner.Provider.Value))
            .WithMessage(_ => Messages.Get("RegisterIdentityRequired"));

        RuleFor(c => c.RegistrationId)
            .MustAsync(async (id, _) => await RegistrationSourceAvailability.IsAvailable(id, eventStore))
            .WithMessage(_ => Messages.Get("AttemptAlreadySubmitted"))
            .WithState(_ => OnboardingAttemptConstraintNames.OneUseAttempt);
    }
}

/// <summary>
/// Command for self-service organization registration, used when a user signs in via the host's
/// <c language="csharp">/register</c> entry point without an invitation.
/// </summary>
/// <param name="RegistrationId">
/// A client-generated identifier used as the event source id and as a correlation key for polling
/// setup status - reuses <see cref="Invitations.InvitationId"/>'s shape since self-service registration
/// shares the same "submit, then watch status" flow as an invitation acceptance.
/// </param>
/// <param name="OrganizationName">The name of the organization (tenant) to create.</param>
/// <param name="FirstName">The first name of the registering user.</param>
/// <param name="MiddleName">The middle name of the registering user.</param>
/// <param name="LastName">The last name of the registering user.</param>
/// <param name="AcceptedLegalTerms">Whether the user has accepted the terms and conditions and the privacy policy.</param>
/// <param name="AcceptedLegalVersion">The version of the legal document set that was presented and accepted.</param>
[Command]
public record RegisterOrganization(InvitationId RegistrationId, TenantName OrganizationName, FirstName FirstName, MiddleName? MiddleName, LastName LastName, bool AcceptedLegalTerms, LegalVersion AcceptedLegalVersion)
{
    /// <summary>
    /// Handles the command by producing an <see cref="OrganizationRegistrationCompleted"/> event and,
    /// when the host has a legal document source configured, the <see cref="LegalTermsAccepted"/>
    /// record of what the registering user agreed to.
    /// </summary>
    /// <param name="httpContextAccessor">Accessor for resolving the current user's identity claims.</param>
    /// <param name="acceptedOrganizationNames">The organization names already claimed by accepted invitations.</param>
    /// <param name="identityProviderResolver">Resolver used to attribute the sign-in to a configured provider.</param>
    /// <param name="legalDocumentSource">The legal document source to resolve authoritative acceptance evidence against.</param>
    /// <param name="eventStore">The event store used to check whether the registration id belongs to an invitation.</param>
    /// <returns>
    /// A <see cref="Result{T0, T1}"/> containing either a failed <see cref="ValidationResult"/> or the
    /// events to append.
    /// </returns>
    /// <remarks>
    /// Does not mark the registration as accepted here - that would be a pre-append success signal,
    /// visible to a polling client before the event this method returns has even been appended, let alone
    /// forwarded to the outbox. <see cref="OrganizationRegistrationOutbox"/> marks it once registration is
    /// verifiably durable in Ante's own outbox instead.
    /// </remarks>
    public async Task<Result<ValidationResult, EventsWithConcurrencyScopes>> Handle(
        IHttpContextAccessor httpContextAccessor,
        IMongoCollection<AcceptedOrganizationName> acceptedOrganizationNames,
        IIdentityProviderResolver identityProviderResolver,
        ILegalDocumentSource legalDocumentSource,
        IEventStore eventStore)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var user = httpContext?.User;
        var owner = RegistrationOwner.Resolve(httpContextAccessor, identityProviderResolver);
        var email = SignedInEmail.Resolve(user, httpContext?.Request.Headers);

        // Re-read rather than trust the validator: the name can be claimed between the two, and this is
        // the last look before the events are composed. The member is named the way the client names
        // it, because a result composed here is passed through as written - only failures a validator
        // reports get camelCased for the client - and without it the wizard cannot put the message on
        // the field.
        if (await ClaimedOrganizationNames.Contains(acceptedOrganizationNames, OrganizationName))
        {
            return ValidationResult.Error(Messages.Get("OrganizationNameInUse"), ["organizationName"]);
        }

        if (owner is null || string.IsNullOrWhiteSpace(owner.Provider.Value))
        {
            return ValidationResult.Error(Messages.Get("RegisterIdentityRequired"));
        }

        if (!await RegistrationSourceAvailability.IsAvailable(RegistrationId, eventStore))
        {
            return ValidationResult.Error(Messages.Get("AttemptAlreadySubmitted"), reasonDetail: OnboardingAttemptConstraintNames.OneUseAttempt);
        }

        var subject = owner.Subject.Value;
        var identityProviderValue = owner.Provider;
        var legalResolution = await LegalAcceptanceEvidence.ResolveWithScope(
            legalDocumentSource,
            AcceptedLegalTerms,
            AcceptedLegalVersion,
            OrganizationName,
            identityProviderValue,
            subject);
        if (!legalResolution.TryGetResult(out var legalEvidence))
        {
            legalResolution.TryGetError(out var legalError);
            return legalError;
        }

        httpContext?.Response.Cookies.Delete(Cratis.Arc.Identity.IdentityProvider.IdentityCookieName);

        var events = new List<object>
        {
            new OnboardingAttemptClaimed(),
            new OrganizationRegistrationCompleted(OrganizationName, subject, identityProviderValue, FirstName, MiddleName ?? Contracts.Invitations.MiddleName.NotSet, LastName, email),
            new RegistrationOwnerRecorded(owner.Subject, owner.Provider),
        };
        events.AddRange(legalEvidence.Events);

        return await LegalAcceptanceEvidence.ForAppend(eventStore, RegistrationId, events, legalEvidence);
    }
}

/// <summary>
/// Rejects ids already associated with an invitation or an older registration that predates the
/// shared one-use marker. The append-time marker enforces the same rule for concurrent new writes.
/// </summary>
public static class RegistrationSourceAvailability
{
    /// <summary>
    /// Checks whether the event source can start a self-service registration.
    /// </summary>
    /// <param name="registrationId">The proposed registration id.</param>
    /// <param name="eventStore">The scoped event store providing the current read models.</param>
    /// <returns>True if no prior invitation or registration read model claims this id.</returns>
    public static async Task<bool> IsAvailable(InvitationId registrationId, IEventStore eventStore)
    {
        var key = registrationId.Value;
        return await eventStore.ReadModels.GetInstanceById<PendingInvitationToJoin>(key) is null &&
            await eventStore.ReadModels.GetInstanceById<PendingInvitationToCreateOrganization>(key) is null &&
            await eventStore.ReadModels.GetInstanceById<UserSetupProgress>(key) is null &&
            await eventStore.ReadModels.GetInstanceById<OrganizationSetupProgress>(key) is null;
    }
}

/// <summary>
/// Forwards <see cref="OrganizationRegistrationCompleted"/> to the outbox so the host can subscribe.
/// </summary>
/// <remarks>
/// Pinned to the event log deliberately - see the remarks on <c language="csharp">LegalTermsAcceptanceOutbox</c>
/// for why an unattributed reactor handling a <c language="csharp">Cratis.Ante.Contracts</c> event is unsafe to route once a
/// deployment renames its store away from the compiled "Ante" literal.
/// </remarks>
/// <param name="eventStore">The event store.</param>
/// <param name="notifiers">Every registered <see cref="IPublicationStatusNotifier"/>, given a chance to accelerate a live status subscription once this fact is durably published.</param>
[Reactor(eventSequence: EventSequenceId.LogId)]
public class OrganizationRegistrationOutbox(IEventStore eventStore, IInstancesOf<IPublicationStatusNotifier> notifiers) : IReactor
{
    /// <summary>
    /// Forwards the registration completed event to the outbox.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The event context.</param>
    public async Task On(OrganizationRegistrationCompleted @event, EventContext context) =>
        await eventStore.PublishToOutbox(context, @event, notifiers);
}
