// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Legal.for_FileLegalDocumentSource;

public class when_getting_current : Specification
{
    string _directory = null!;
    FileLegalDocumentSource _source = null!;
    LegalDocumentSet? _first;
    LegalDocumentSet? _again;
    LegalDocumentSet? _edited;

    void Establish()
    {
        _directory = Directory.CreateTempSubdirectory("ante-legal").FullName;
        File.WriteAllText(Path.Combine(_directory, "terms.md"), "# Terms");
        File.WriteAllText(Path.Combine(_directory, "privacy.html"), "<h1>Privacy</h1>");
        _source = new(Options.Create(new AnteOptions { Legal = new() { Source = "Files", Files = new() { Folder = _directory } } }));
    }

    async Task Because()
    {
        _first = await _source.GetCurrent();
        _again = await _source.GetCurrent();
        await File.WriteAllTextAsync(Path.Combine(_directory, "terms.md"), "# Terms, revised");
        _edited = await _source.GetCurrent();
    }

    [Fact] void should_present_both_documents() => Assert.Equal(("# Terms", "<h1>Privacy</h1>"), (_first!.TermsAndConditions.Value, _first.PrivacyPolicy.Value));
    [Fact] void should_version_by_content() => Assert.Equal(_first!.Version, _again!.Version);
    [Fact] void should_publish_a_new_version_when_a_file_changes() => Assert.NotEqual(_first!.Version, _edited!.Version);
    [Fact] void should_use_a_content_hash_as_version() => Assert.StartsWith("sha256-", _first!.Version.Value);
    [Fact] void should_require_acceptance_even_when_unreadable() => Assert.True(_source.RequiresDocuments);
}
#endif
