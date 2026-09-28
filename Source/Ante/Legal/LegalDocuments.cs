// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Ante.Contracts.Legal;
using Ante.Legal.Receiving;
using Ante.Resources;
using Cratis.Chronicle.EventSequences.Concurrency;
using FluentValidation;

namespace Ante.Legal;

/// <summary>
/// Defines where Ante gets a host's legal documents from. A host that wants Ante's wizards to collect
/// terms-and-conditions acceptance registers an implementation of this in its own dependency injection
/// container; a host that does not is one that has nothing for onboarding to present, and the wizards
/// skip the legal step entirely.
/// </summary>
/// <remarks>
/// This is Ante's one legal-document extension point on purpose - there is no bundled fallback text and
/// no document versioning/authoring pipeline. Ante is not a legal-document management product; it only
/// needs to know, at the moment someone is onboarding, whether there is something to show them.
/// </remarks>
public interface ILegalDocumentSource
{
    /// <summary>
    /// Reads the documents as they currently stand.
    /// </summary>
    /// <returns>The current documents, or <see langword="null"/> when the host has nothing to present.</returns>
    Task<LegalDocumentSet?> GetCurrent();
}

/// <summary>
/// Marks a source for which no activated documents means temporarily unavailable, not no legal step.
/// Existing in-process source implementations need not implement this interface.
/// </summary>
public interface ILegalDocumentAvailability
{
    /// <summary>
    /// Gets whether this source requires an activated document set before onboarding may proceed.
    /// </summary>
    bool RequiresDocuments { get; }
}

/// <summary>
/// The legal document set as a host currently presents it.
/// </summary>
/// <param name="TermsAndConditions">The terms and conditions as they now read.</param>
/// <param name="PrivacyPolicy">The privacy policy as it now reads.</param>
/// <param name="Version">The version the pair is published under.</param>
/// <remarks>
/// The two documents share one version because they are presented and accepted together, so a single
/// marker on an acceptance has to identify both.
/// </remarks>
public record LegalDocumentSet(LegalDocumentBody TermsAndConditions, LegalDocumentBody PrivacyPolicy, LegalVersion Version);

/// <summary>
/// The validation rules every command that captures acceptance of the legal document set applies.
/// </summary>
/// <remarks>
/// This is the <em>preflight</em> check - what the wizard's eager <c language="csharp">/validate</c> round trip runs
/// while the user is still filling in the form, advisory by nature because the host's document can
/// change between that call and the moment the command actually executes. <see cref="LegalAcceptanceEvidence"/>
/// is the authoritative counterpart that runs during execution itself, resolving the document source
/// exactly once and using that single read for both its own rejection and the evidence it emits.
/// </remarks>
public static class LegalTermsRules
{
    /// <summary>
    /// The message shown when the host has a legal document set configured and the command did not
    /// claim it was accepted.
    /// </summary>
    public const string MustAcceptMessage = "You must accept the terms and conditions and the privacy policy to proceed.";

    /// <summary>
    /// The message shown when the version a command claims was accepted no longer matches the version
    /// the host currently presents.
    /// </summary>
    public const string StaleVersionMessage = "The terms and conditions and privacy policy have changed. Reload the page and accept the current version.";

    /// <summary>
    /// The retryable message when an inbox has not activated its first usable document set.
    /// </summary>
    public const string UnavailableMessage = "Legal documents are not available yet. Check again before continuing.";

    /// <summary>
    /// The message shown when a command claims acceptance of a legal document set the host has not
    /// configured - there is nothing for the claim to refer to, so it is rejected rather than recorded.
    /// </summary>
    public const string UnsolicitedAcceptanceMessage = "There are no terms and conditions to accept right now. Reload the page and continue.";

    /// <summary>
    /// Requires that the legal document set was accepted, and that the version accepted is the one
    /// currently presented - but only when the host has registered a document source that currently
    /// has something to present. When it has not, acceptance is neither required nor permitted: there
    /// is no document for the user to have agreed to, so a claim of acceptance is rejected rather than
    /// silently accepted.
    /// </summary>
    /// <typeparam name="TCommand">The type of command being validated.</typeparam>
    /// <param name="validator">The validator to add the rules to.</param>
    /// <param name="legalDocumentSource">The legal document source to validate against.</param>
    /// <param name="accepted">Selects the property carrying whether the documents were accepted.</param>
    /// <param name="version">Selects the property carrying the version that was accepted.</param>
    public static void Apply<TCommand>(
        AbstractValidator<TCommand> validator,
        ILegalDocumentSource legalDocumentSource,
        Expression<Func<TCommand, bool>> accepted,
        Expression<Func<TCommand, LegalVersion>> version)
    {
        var isAccepted = accepted.Compile();
        var acceptedVersion = version.Compile();
        var acceptedProperty = ((MemberExpression)accepted.Body).Member.Name;
        var versionProperty = ((MemberExpression)version.Body).Member.Name;
        validator.RuleFor(command => command).CustomAsync(async (command, context, _) =>
        {
            var current = await legalDocumentSource.GetCurrent();
            if (current is null)
            {
                if (legalDocumentSource is ILegalDocumentAvailability { RequiresDocuments: true })
                {
                    context.AddFailure(acceptedProperty, Messages.Get(nameof(UnavailableMessage)));
                }
                else if (isAccepted(command))
                {
                    context.AddFailure(acceptedProperty, Messages.Get(nameof(UnsolicitedAcceptanceMessage)));
                }
                return;
            }

            if (!isAccepted(command))
            {
                context.AddFailure(acceptedProperty, Messages.Get(nameof(MustAcceptMessage)));
            }
            if (acceptedVersion(command) != current.Version)
            {
                context.AddFailure(versionProperty, Messages.Get(nameof(StaleVersionMessage)));
            }
        });
    }
}

/// <summary>
/// Resolves the authoritative legal-acceptance evidence for one command execution - the legal
/// document status actually read for this command, checked against what the command claims, so the
/// <see cref="LegalTermsAccepted"/> event a command emits is always built from the same read that
/// decided whether to emit it at all.
/// </summary>
/// <remarks>
/// <see cref="LegalTermsRules"/> validates the same claim earlier, as advisory preflight feedback -
/// but it reads the document source independently, and a host's document can change between that call
/// and command execution. A command's <c language="csharp">Handle()</c> calls this, once, as the last word: it re-reads
/// the source itself rather than trusting either the client's claim or the preflight validator's
/// earlier read, so a document that changed, disappeared, or was never configured between display and
/// execution cannot produce a mismatch between what was validated and what gets appended.
/// </remarks>
public static class LegalAcceptanceEvidence
{
    /// <summary>
    /// Resolves whether a command's claimed legal-terms acceptance is trustworthy against the legal
    /// document source as it reads right now, and if so, the event to append for it.
    /// </summary>
    /// <param name="legalDocumentSource">The legal document source to resolve against.</param>
    /// <param name="acceptedLegalTerms">Whether the command claims the presented document set was accepted.</param>
    /// <param name="acceptedLegalVersion">The version the command claims was presented and accepted.</param>
    /// <param name="tenantName">The organization the acceptance is for.</param>
    /// <param name="identityProvider">The identity provider the accepting user authenticated with.</param>
    /// <param name="subject">The compliance subject the acceptance is appended under.</param>
    /// <returns>
    /// A <see cref="Result{TResult, TError}"/> containing either a failed <see cref="ValidationResult"/>
    /// or the events to append - empty when the host currently has no legal document source configured
    /// and the command claims no acceptance, or exactly one <see cref="LegalTermsAccepted"/> otherwise.
    /// </returns>
    public static async Task<Result<IEnumerable<object>, ValidationResult>> Resolve(
        ILegalDocumentSource legalDocumentSource,
        bool acceptedLegalTerms,
        LegalVersion acceptedLegalVersion,
        TenantName tenantName,
        IdentityProviderName identityProvider,
        string subject)
    {
        var resolution = await ResolveWithScope(
            legalDocumentSource, acceptedLegalTerms, acceptedLegalVersion, tenantName, identityProvider, subject);
        if (!resolution.TryGetResult(out var evidence))
        {
            resolution.TryGetError(out var error);
            return error;
        }

        return Result<IEnumerable<object>, ValidationResult>.Success(evidence.Events);
    }

    /// <summary>
    /// Resolves authoritative content and its exact legal-stream append fence in one read.
    /// </summary>
    /// <param name="legalDocumentSource">The source to read once.</param>
    /// <param name="acceptedLegalTerms">Whether the user accepted both documents.</param>
    /// <param name="acceptedLegalVersion">The version the user saw.</param>
    /// <param name="tenantName">The organization receiving acceptance.</param>
    /// <param name="identityProvider">The user's identity provider.</param>
    /// <param name="subject">The accepting subject.</param>
    /// <returns>Either a rejection or the acceptance event and its optional concurrency fence.</returns>
    public static async Task<Result<LegalAcceptanceSnapshot, ValidationResult>> ResolveWithScope(
        ILegalDocumentSource legalDocumentSource,
        bool acceptedLegalTerms,
        LegalVersion acceptedLegalVersion,
        TenantName tenantName,
        IdentityProviderName identityProvider,
        string subject)
    {
        var activated = legalDocumentSource is IActivatedLegalDocumentSource inbox ? await inbox.GetActivated() : null;
        var current = activated?.Documents ?? (activated is null ? await legalDocumentSource.GetCurrent() : null);
        var scope = activated?.Scope;

        if (current is null)
        {
            if (legalDocumentSource is ILegalDocumentAvailability { RequiresDocuments: true })
            {
                return ValidationResult.Error(Messages.Get(nameof(LegalTermsRules.UnavailableMessage)));
            }

            return acceptedLegalTerms
                ? ValidationResult.Error(Messages.Get(nameof(LegalTermsRules.UnsolicitedAcceptanceMessage)))
                : Result<LegalAcceptanceSnapshot, ValidationResult>.Success(new([], null));
        }

        if (!acceptedLegalTerms)
        {
            return ValidationResult.Error(Messages.Get(nameof(LegalTermsRules.MustAcceptMessage)));
        }

        if (acceptedLegalVersion != current.Version)
        {
            return ValidationResult.Error(Messages.Get(nameof(LegalTermsRules.StaleVersionMessage)));
        }

        return Result<LegalAcceptanceSnapshot, ValidationResult>.Success(
            new([new LegalTermsAccepted(tenantName, identityProvider, subject, current.Version)], scope));
    }

    /// <summary>
    /// Composes the onboarding facts with exact scopes for both the onboarding and legal streams.
    /// </summary>
    /// <param name="store">The local event store.</param>
    /// <param name="id">The onboarding event source.</param>
    /// <param name="events">The onboarding and acceptance facts.</param>
    /// <param name="legal">The legal snapshot read for this command.</param>
    /// <returns>The atomic append, rejected if the activated legal set changes before it commits.</returns>
    public static async Task<EventsWithConcurrencyScopes> ForAppend(
        IEventStore store,
        EventSourceId id,
        IEnumerable<object> events,
        LegalAcceptanceSnapshot legal)
    {
        var tail = await store.EventLog.GetTailSequenceNumber(id);
        var scopes = new List<KeyValuePair<EventSourceId, ConcurrencyScope>>
        {
            new(id, new(tail.IsActualValue ? tail : EventSequenceNumber.BeforeFirst, id)),
        };
        if (legal.Scope is not null)
        {
            scopes.Add(new(legal.Scope.EventSourceId!, legal.Scope));
        }

        return new([.. events.Select(@event => new EventForEventSourceId(id, @event))], scopes);
    }
}

/// <summary>
/// The acceptance evidence and the exact legal-stream scope observed alongside it.
/// </summary>
/// <param name="Events">The acceptance facts.</param>
/// <param name="Scope">The legal stream fence, only in inbox mode.</param>
public record LegalAcceptanceSnapshot(IEnumerable<object> Events, ConcurrencyScope? Scope);

/// <summary>
/// The default <see cref="ILegalDocumentSource"/> registered when a host supplies none of its own -
/// always reports that there is nothing to present, so validation and the wizards uniformly treat legal
/// acceptance as not required.
/// </summary>
public class NoLegalDocumentSource : ILegalDocumentSource
{
    /// <inheritdoc/>
    public Task<LegalDocumentSet?> GetCurrent() => Task.FromResult<LegalDocumentSet?>(null);
}

/// <summary>
/// Response model exposing whether a host has legal documents configured, and if so, their current
/// content and version - so the wizards know whether to show a terms step at all.
/// </summary>
/// <param name="IsConfigured">Whether the host has a legal document source with something to present.</param>
/// <param name="TermsAndConditions">The terms and conditions, empty when not configured.</param>
/// <param name="PrivacyPolicy">The privacy policy, empty when not configured.</param>
/// <param name="Version">The version currently presented, empty when not configured.</param>
/// <param name="IsUnavailable">Whether the required inbox source has not activated a usable set.</param>
[ReadModel]
public record LegalDocumentStatus(bool IsConfigured, LegalDocumentBody TermsAndConditions, LegalDocumentBody PrivacyPolicy, LegalVersion Version, bool IsUnavailable = false)
{
    /// <summary>
    /// Gets the current legal document status.
    /// </summary>
    /// <param name="legalDocumentSource">The legal document source to read from.</param>
    /// <returns>The current <see cref="LegalDocumentStatus"/>.</returns>
    public static async Task<LegalDocumentStatus> Current(ILegalDocumentSource legalDocumentSource)
    {
        var current = await legalDocumentSource.GetCurrent();
        return current is null
            ? new(
                false,
                LegalDocumentBody.NotSet,
                LegalDocumentBody.NotSet,
                LegalVersion.NotSet,
                legalDocumentSource is ILegalDocumentAvailability { RequiresDocuments: true })
            : new(true, current.TermsAndConditions, current.PrivacyPolicy, current.Version);
    }
}
