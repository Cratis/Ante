// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Legal;

namespace Ante.Legal.Receiving;

public class when_deciding_an_inbox_publication : Specification
{
    static readonly LegalDocumentSetPublished _first = new(1, "terms-v1", "Terms one", "Privacy one");
    static readonly LegalDocumentSetPublished _second = new(2, "terms-v2", "Terms two", "Privacy two");
    static readonly LegalDocumentSetReceived _active = new(2, "terms-v2", "Terms two", "Privacy two");

    [Fact]
    public void should_activate_the_first_complete_set() =>
        Assert.IsType<LegalDocumentSetReceived>(LegalDocumentSetReceiver.Decide([], [], _first, 1)).Version.ShouldEqual(_first.Version);

    [Fact]
    public void should_activate_a_newer_snapshot() =>
        Assert.IsType<LegalDocumentSetReceived>(LegalDocumentSetReceiver.Decide([new(1, "terms-v1", "Terms one", "Privacy one")], [], _second, 2))
            .Revision.ShouldEqual(_second.Revision);

    [Fact]
    public void should_ignore_an_older_arrival() =>
        Assert.Null(LegalDocumentSetReceiver.Decide([_active], [], _first, 3));

    [Fact]
    public void should_ignore_an_identical_duplicate() =>
        Assert.Null(LegalDocumentSetReceiver.Decide([_active], [], _second, 4));

    [Fact]
    public void should_accept_a_new_revision_for_an_unchanged_version_and_text() =>
        Assert.IsType<LegalDocumentSetReceived>(LegalDocumentSetReceiver.Decide(
            [_active],
            [],
            new(3, "terms-v2", "Terms two", "Privacy two"),
            5)).Revision.Value.ShouldEqual(3);

    [Fact]
    public void should_quarantine_a_reused_revision_with_different_text() =>
        Assert.IsType<LegalDocumentSetRejected>(LegalDocumentSetReceiver.Decide(
            [_active],
            [],
            new(2, "terms-v2", "Changed terms", "Privacy two"),
            5)).Reason.ShouldEqual(LegalDocumentRejectionReason.ConflictingRevisionOrVersion);

    [Fact]
    public void should_quarantine_a_reused_version_with_a_new_revision() =>
        Assert.IsType<LegalDocumentSetRejected>(LegalDocumentSetReceiver.Decide(
            [_active],
            [],
            new(3, "terms-v2", "Different terms", "Privacy three"),
            6)).Reason.ShouldEqual(LegalDocumentRejectionReason.ConflictingRevisionOrVersion);

    [Fact]
    public void should_not_repeat_a_rejection_for_the_same_inbox_event() =>
        Assert.Null(LegalDocumentSetReceiver.Decide(
            [_active],
            [new(2, "terms-v2", 7, LegalDocumentRejectionReason.ConflictingRevisionOrVersion)],
            new(2, "terms-v2", "Changed terms", "Privacy two"),
            7));
}
#endif
