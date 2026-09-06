// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Ante.Contracts.Legal;
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
public static class LegalTermsRules
{
    /// <summary>
    /// Requires that the legal document set was accepted, and that the version accepted is the one
    /// currently presented - but only when the host has registered a document source that currently
    /// has something to present. When it has not, the rule imposes nothing: there is no document for
    /// the user to have agreed to.
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
        validator.RuleFor(accepted)
            .Equal(true)
            .WithMessage("You must accept the terms and conditions and the privacy policy to proceed.")
            .WhenAsync(async (_, _) => await legalDocumentSource.GetCurrent() is not null);

        validator.RuleFor(version)
            .MustAsync(async (accepted, _) => accepted is not null && accepted == (await legalDocumentSource.GetCurrent())?.Version)
            .WithMessage("The terms and conditions and privacy policy have changed. Reload the page and accept the current version.")
            .WhenAsync(async (_, _) => await legalDocumentSource.GetCurrent() is not null);
    }
}

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
[ReadModel]
public record LegalDocumentStatus(bool IsConfigured, LegalDocumentBody TermsAndConditions, LegalDocumentBody PrivacyPolicy, LegalVersion Version)
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
            ? new(false, LegalDocumentBody.NotSet, LegalDocumentBody.NotSet, LegalVersion.NotSet)
            : new(true, current.TermsAndConditions, current.PrivacyPolicy, current.Version);
    }
}
