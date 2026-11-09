// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Ante.Contracts.Legal;

namespace Ante.Legal;

/// <summary>
/// The files the <c language="csharp">Files</c> legal source reads the terms and conditions and the privacy policy from.
/// </summary>
public class LegalFilesOptions
{
    /// <summary>
    /// Gets or sets a folder holding <c language="csharp">terms</c> (or <c language="csharp">terms-and-conditions</c>) and
    /// <c language="csharp">privacy</c> (or <c language="csharp">privacy-policy</c>) as <c language="csharp">.md</c> or
    /// <c language="csharp">.html</c> files. Explicit paths below win over the folder.
    /// </summary>
    public string? Folder { get; set; }

    /// <summary>
    /// Gets or sets the path of the terms and conditions file.
    /// </summary>
    public string? TermsAndConditions { get; set; }

    /// <summary>
    /// Gets or sets the path of the privacy policy file.
    /// </summary>
    public string? PrivacyPolicy { get; set; }
}

/// <summary>
/// Presents the legal documents from files in a mounted folder, versioned by their content. Editing a file
/// publishes a new version, so everyone who is onboarding afterwards must accept it. Files that cannot be
/// read make the set unavailable, which blocks onboarding rather than skipping the legal step.
/// </summary>
/// <param name="options">The configured files.</param>
public class FileLegalDocumentSource(IOptions<AnteOptions> options) : ILegalDocumentSource, ILegalDocumentAvailability
{
    static readonly string[] _termsNames = ["terms", "terms-and-conditions", "terms_and_conditions"];
    static readonly string[] _privacyNames = ["privacy", "privacy-policy", "privacy_policy"];
    static readonly string[] _extensions = [".md", ".markdown", ".html", ".htm", ".txt"];

    /// <inheritdoc/>
    public bool RequiresDocuments => true;

    /// <summary>
    /// Computes the version of a document pair from its content: identical content always yields the same version.
    /// </summary>
    /// <param name="termsAndConditions">The terms and conditions as read.</param>
    /// <param name="privacyPolicy">The privacy policy as read.</param>
    /// <returns>The content version, <c language="csharp">sha256-</c> followed by sixteen hexadecimal characters.</returns>
    public static LegalVersion VersionOf(string termsAndConditions, string privacyPolicy)
    {
        var terms = Encoding.UTF8.GetBytes(termsAndConditions);
        var privacy = Encoding.UTF8.GetBytes(privacyPolicy);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(BitConverter.GetBytes(terms.Length));
        hash.AppendData(terms);
        hash.AppendData(BitConverter.GetBytes(privacy.Length));
        hash.AppendData(privacy);
        return $"sha256-{Convert.ToHexStringLower(hash.GetHashAndReset())[..16]}";
    }

    /// <summary>
    /// Resolves the two document paths from the configuration, or fails startup when they cannot be found.
    /// </summary>
    /// <param name="files">The configured files.</param>
    /// <returns>The terms and conditions path and the privacy policy path.</returns>
    /// <exception cref="LegalFilesConfigurationInvalid">A document is not configured or does not exist.</exception>
    public static (string Terms, string Privacy) Validate(LegalFilesOptions files)
    {
        var terms = files.TermsAndConditions ?? Find(files.Folder, _termsNames);
        var privacy = files.PrivacyPolicy ?? Find(files.Folder, _privacyNames);
        if (terms is null || privacy is null)
        {
            throw new LegalFilesConfigurationInvalid(
                "Ante:Legal:Files needs the terms and conditions and the privacy policy: set TermsAndConditions and PrivacyPolicy, " +
                "or a Folder holding terms.md and privacy.md (or .html).");
        }

        var missing = new[] { terms, privacy }.FirstOrDefault(path => !File.Exists(path));
        return missing is null
            ? (terms, privacy)
            : throw new LegalFilesConfigurationInvalid($"The legal document file '{missing}' does not exist.");
    }

    /// <inheritdoc/>
    public async Task<LegalDocumentSet?> GetCurrent()
    {
        try
        {
            var (terms, privacy) = Validate(options.Value.Legal.Files);
            var termsBody = await File.ReadAllTextAsync(terms);
            var privacyBody = await File.ReadAllTextAsync(privacy);
            return string.IsNullOrWhiteSpace(termsBody) || string.IsNullOrWhiteSpace(privacyBody)
                ? null
                : new(termsBody, privacyBody, VersionOf(termsBody, privacyBody));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or LegalFilesConfigurationInvalid)
        {
            return null;
        }
    }

    static string? Find(string? folder, string[] names)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            return null;
        }

        return Directory.EnumerateFiles(folder)
            .Where(path => _extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase) &&
                names.Contains(Path.GetFileNameWithoutExtension(path), StringComparer.OrdinalIgnoreCase))
            .Order(StringComparer.Ordinal)
            .FirstOrDefault();
    }
}

/// <summary>
/// The exception that is thrown when the Files legal source does not name two existing documents.
/// </summary>
/// <param name="message">A diagnostic naming the missing document.</param>
public class LegalFilesConfigurationInvalid(string message) : Exception(message);
