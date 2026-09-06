// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Ante.Contracts.Legal;
using Ante.Contracts.Organization;
using Ante.IdentityProviders;
using Ante.Invitations;
using Ante.Invitations.OrganizationSetup;
using Ante.Legal;
using Ante.Outbox;
using Cratis.Arc.Identity;
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
    public RegisterOrganizationValidator(ILegalDocumentSource legalDocumentSource, IMongoCollection<AcceptedOrganizationName> acceptedOrganizationNames)
    {
        RuleFor(c => (string)c.OrganizationName)
            .MustBeAValidOrganizationName();

        // Expressed as a rule rather than only as a handler check so the wizard's eager server
        // validation reaches it: the validator is what the /validate endpoint runs, and a failure it
        // reports is attributed to the organization name field, so the rejection lands on the step that
        // owns the field instead of only surfacing once the final submit has already failed.
        RuleFor(c => (string)c.OrganizationName)
            .MustAsync(async (organizationName, _) => !await ClaimedOrganizationNames.Contains(acceptedOrganizationNames, organizationName))
            .WithMessage("An organization with this name already exists.");

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
    }
}

/// <summary>
/// Command for self-service organization registration, used when a user signs in via the host's
/// <c>/register</c> entry point without an invitation.
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
    public async Task<Result<ValidationResult, IEnumerable<object>>> Handle(
        IHttpContextAccessor httpContextAccessor,
        IMongoCollection<AcceptedOrganizationName> acceptedOrganizationNames,
        IIdentityProviderResolver identityProviderResolver,
        ILegalDocumentSource legalDocumentSource)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var user = httpContext?.User;
        var subject = user?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user?.FindFirstValue("sub")
            ?? httpContext?.Request.Headers[MicrosoftIdentityPlatformHeaders.IdentityIdHeader].FirstOrDefault()
            ?? string.Empty;
        var identityProviderValue = identityProviderResolver.Resolve(user?.FindFirstValue("iss"));
        var email = SignedInEmail.Resolve(user, httpContext?.Request.Headers);

        // Re-read rather than trust the validator: the name can be claimed between the two, and this is
        // the last look before the events are composed. The member is named the way the client names
        // it, because a result composed here is passed through as written - only failures a validator
        // reports get camelCased for the client - and without it the wizard cannot put the message on
        // the field.
        if (await ClaimedOrganizationNames.Contains(acceptedOrganizationNames, OrganizationName))
        {
            return ValidationResult.Error("Organization name is already in use.", ["organizationName"]);
        }

        var legalResolution = await LegalAcceptanceEvidence.Resolve(
            legalDocumentSource,
            AcceptedLegalTerms,
            AcceptedLegalVersion,
            OrganizationName,
            identityProviderValue,
            subject);
        if (!legalResolution.TryGetResult(out var legalEvents))
        {
            legalResolution.TryGetError(out var legalError);
            return legalError;
        }

        httpContext?.Response.Cookies.Delete(Cratis.Arc.Identity.IdentityProvider.IdentityCookieName);

        var events = new List<object>
        {
            new OrganizationRegistrationCompleted(OrganizationName, subject, identityProviderValue, FirstName, MiddleName ?? Contracts.Invitations.MiddleName.NotSet, LastName, email),
        };
        events.AddRange(legalEvents);

        return events;
    }
}

/// <summary>
/// Forwards <see cref="OrganizationRegistrationCompleted"/> to the outbox so the host can subscribe.
/// </summary>
/// <param name="eventStore">The event store.</param>
/// <param name="notifiers">Every registered <see cref="IPublicationStatusNotifier"/>, given a chance to accelerate a live status subscription once this fact is durably published.</param>
[Reactor]
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
