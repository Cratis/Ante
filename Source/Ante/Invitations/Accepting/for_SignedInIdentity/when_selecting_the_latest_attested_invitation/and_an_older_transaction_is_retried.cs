// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Accepting.for_SignedInIdentity.when_selecting_the_latest_attested_invitation;

public class and_an_older_transaction_is_retried : Specification
{
    readonly InvitationId _firstId = InvitationId.New();
    readonly InvitationId _secondId = InvitationId.New();
    InvitationId _afterRetry = InvitationId.NotSet;
    InvitationId _afterNewTransaction = InvitationId.NotSet;

    void Because()
    {
        var now = DateTime.UtcNow;
        var first = Session(_firstId, now.AddMinutes(-3), now.AddMinutes(-2));
        var second = Session(_secondId, now.AddMinutes(-1), now.AddMinutes(-1));

        // Retrying the old transaction with a fresh signed assertion claims a jti, not a
        // new completion; its ordering time and original expiry remain unchanged.
        var retry = first with { AssertionIds = ["first-jti", "retry-jti"] };
        _afterRetry = SignedInIdentity.SelectAttestedSession(
            [retry, second], "lobby", InvitationId.NotSet, "provider", "https://issuer.example", "actor", now)!.InvitationId;

        var newTransaction = retry with { LatestTransactionId = "new-transaction", LatestNewCompletionAtUtc = now };
        _afterNewTransaction = SignedInIdentity.SelectAttestedSession(
            [newTransaction, second], "lobby", InvitationId.NotSet, "provider", "https://issuer.example", "actor", now)!.InvitationId;
    }

    static AttestedInvitationSession Session(InvitationId id, DateTime firstCompletion, DateTime latestCompletion) =>
        new("original-transaction", "lobby", id, InvitationFlowType.JoinTenant, "provider", "https://issuer.example", "actor", ["first-jti"], DateTime.UtcNow.AddDays(1))
        {
            CompletedAtUtc = firstCompletion,
            LatestNewCompletionAtUtc = latestCompletion,
            LatestTransactionId = "original-transaction",
        };

    [Fact] void should_keep_the_second_invitation_as_default_after_the_retry() => _afterRetry.ShouldEqual(_secondId);
    [Fact] void should_select_the_first_invitation_after_a_new_transaction_completes() => _afterNewTransaction.ShouldEqual(_firstId);
}
#endif
