// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using FluentValidation;

namespace Ante.Organization;

/// <summary>
/// Shared validation rule ensuring an organization / tenant name can be used as a Chronicle namespace
/// by whichever host provisions it.
/// </summary>
/// <remarks>
/// Chronicle namespaces commonly become part of a MongoDB database name, and MongoDB database names
/// cannot contain a space or any of the characters <c>/ \ . " $ * &lt; &gt; : | ?</c> (nor the null
/// character). A name with any of those would permanently break whichever host tries to provision an
/// event store namespace from it. These rules reject those characters - plus <c>+</c>, which Chronicle
/// uses as its own namespace separator - at the command boundary, before the name is ever turned into a
/// namespace. See
/// https://www.mongodb.com/docs/manual/reference/limits/#mongodb-limit-Restrictions-on-Database-Names.
/// </remarks>
public static class OrganizationNameValidation
{
    /// <summary>
    /// The maximum length of an organization name. Kept well below MongoDB's 63-byte database name
    /// limit so a host's own namespace-to-database-name composition still fits.
    /// </summary>
    public const int MaximumLength = 50;

    /// <summary>
    /// The characters not permitted in an organization / tenant name because they are invalid in a
    /// MongoDB database name, or are reserved by Chronicle's namespace composition (the <c>+</c>
    /// separator).
    /// </summary>
    static readonly SearchValues<char> _invalidCharacters = SearchValues.Create(" /\\.\"$*<>:|?+\0");

    /// <summary>
    /// Applies the organization / tenant name rules: non-empty, within the length limit, and free of
    /// any character that is invalid in a MongoDB database name.
    /// </summary>
    /// <typeparam name="T">The type owning the value being validated.</typeparam>
    /// <param name="rule">The rule builder for the name value.</param>
    /// <returns>The configured rule builder.</returns>
    public static IRuleBuilderOptions<T, string> MustBeAValidOrganizationName<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
                .WithMessage("Organization name is required.")
            .MaximumLength(MaximumLength)
                .WithMessage($"Organization name cannot be longer than {MaximumLength} characters.")
            .Must(name => string.IsNullOrEmpty(name) || name.AsSpan().IndexOfAny(_invalidCharacters) < 0)
                .WithMessage("Organization name cannot contain spaces or any of these characters: / \\ . \" $ * < > : | ? +");
}
