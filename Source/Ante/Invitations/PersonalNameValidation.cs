// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using FluentValidation;

namespace Ante.Invitations;

/// <summary>
/// Shared validation rules for the structured personal-name fields - first, middle, last - captured
/// while onboarding a person through an invitation, organization setup, or self-service registration.
/// </summary>
/// <remarks>
/// Real names worldwide use far more than the Latin alphabet, so the rules are deliberately inclusive:
/// diacritics, combining marks, non-Latin scripts, apostrophes, hyphens, and the zero-width joiner and
/// non-joiner some scripts need to render the correct letterforms all pass. What is rejected is
/// narrower and chosen for a concrete reason rather than by exclusion: control characters, and the
/// Unicode "format" characters that override how text direction is displayed - which have no
/// legitimate place in a personal name and could otherwise make a name render differently than it is
/// stored.
/// <para>
/// The length limit is expressed in UTF-16 code units - the same unit JavaScript's
/// <c language="csharp">string.length</c> uses - so client and server agree on the unit without a separate client-side
/// copy of the number: the wizard discovers a violation through the same eager <c language="csharp">/validate</c> round
/// trip it uses for every other field, and this validator is the only place the limit is defined.
/// </para>
/// <para>
/// Whether a first/last name pair may ever be optional (a mononym) is a product decision tracked in
/// <see href="https://github.com/Cratis/Ante/issues/11">Cratis/Ante#11</see>; these rules preserve
/// today's required-name compatibility rather than deciding it here.
/// </para>
/// </remarks>
public static class PersonalNameValidation
{
    /// <summary>
    /// The maximum length of a structured name field, in UTF-16 code units.
    /// </summary>
    public const int MaximumLength = 100;

    /// <summary>
    /// Applies the shared name rules - within length, free of disallowed characters - without
    /// requiring the value to be present. Use for an optional field such as a middle name.
    /// </summary>
    /// <typeparam name="T">The type owning the value being validated.</typeparam>
    /// <param name="rule">The rule builder for the name value.</param>
    /// <param name="fieldLabel">The human-readable field name to use in messages.</param>
    /// <returns>The configured rule builder.</returns>
    public static IRuleBuilderOptions<T, string> MustBeAValidName<T>(this IRuleBuilder<T, string> rule, string fieldLabel) =>
        rule
            .MaximumLength(MaximumLength)
                .WithMessage($"{fieldLabel} cannot be longer than {MaximumLength} characters.")
            .Must(BeFreeOfDisallowedCharacters)
                .WithMessage($"{fieldLabel} cannot contain control or text-direction-override characters.");

    /// <summary>
    /// Applies the shared name rules and requires the value to be present. Use for a required field
    /// such as a first or last name.
    /// </summary>
    /// <typeparam name="T">The type owning the value being validated.</typeparam>
    /// <param name="rule">The rule builder for the name value.</param>
    /// <param name="fieldLabel">The human-readable field name to use in messages.</param>
    /// <returns>The configured rule builder.</returns>
    public static IRuleBuilderOptions<T, string> MustBeARequiredName<T>(this IRuleBuilder<T, string> rule, string fieldLabel) =>
        rule
            .NotEmpty()
                .WithMessage($"{fieldLabel} is required.")
            .MustBeAValidName(fieldLabel);

    /// <summary>
    /// Determines whether a name is free of Unicode control characters and of "format" characters
    /// other than the zero-width joiner/non-joiner - so a legitimate combining sequence or ligature
    /// joiner passes, while a bidirectional-override character or a raw control character does not.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>True if the value contains no disallowed character; otherwise false.</returns>
    static bool BeFreeOfDisallowedCharacters(string value)
    {
        foreach (var rune in value.EnumerateRunes())
        {
            var category = Rune.GetUnicodeCategory(rune);
            if (category == UnicodeCategory.Control)
            {
                return false;
            }

            if (category == UnicodeCategory.Format && rune.Value is not (0x200C or 0x200D))
            {
                return false;
            }
        }

        return true;
    }
}
