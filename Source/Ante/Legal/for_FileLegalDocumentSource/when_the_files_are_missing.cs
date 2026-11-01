// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Legal.for_FileLegalDocumentSource;

public class when_the_files_are_missing : Specification
{
    FileLegalDocumentSource _source = null!;
    LegalDocumentSet? _current;

    void Establish() => _source = new(Options.Create(new AnteOptions
    {
        Legal = new() { Source = "Files", Files = new() { TermsAndConditions = "/nonexistent/terms.md", PrivacyPolicy = "/nonexistent/privacy.md" } },
    }));

    async Task Because() => _current = await _source.GetCurrent();

    [Fact] void should_report_nothing_to_present() => Assert.Null(_current);
    [Fact] void should_block_onboarding_instead_of_skipping_the_step() => Assert.True(_source.RequiresDocuments);
    [Fact] void should_fail_startup_validation() => Assert.Throws<LegalFilesConfigurationInvalid>(() => FileLegalDocumentSource.Validate(new() { Folder = "/nonexistent" }));
}
#endif
