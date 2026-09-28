// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Cryptography;
using Ante.Invitations.Accepting.for_AttestedInvitationCompletion.given;
using Microsoft.AspNetCore.WebUtilities;

namespace Ante.Invitations.Accepting.for_AttestedInvitationCompletion.when_completing;

public class and_a_fresh_signed_assertion_retries_the_committed_transaction : a_staged_completion
{
    bool _firstSucceeded;
    bool _committed;
    string _firstAuthorization = string.Empty;

    async Task Because()
    {
        Sessions.Retry(Arg.Any<StagedInvitationTransaction>(), Arg.Any<VerifiedInvitationAttestation>())
            .Returns(_ => _committed ? AttestedSessionOutcome.Accepted : AttestedSessionOutcome.Missing);
        await Exchange();
        _firstSucceeded = Result;
        _firstAuthorization = Authorization;
        _committed = true;

        // A second request opens a fresh MongoDB cursor for the same staged transaction.
        UseStage(Stage);
        _claims["jti"] = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        await Exchange();
    }

    [Fact] void should_sign_a_new_assertion() => Assert.NotEqual(_firstAuthorization, Authorization);
    [Fact] void should_have_committed_the_first_completion() => Assert.True(_firstSucceeded);
    [Fact] void should_accept_the_fresh_signed_retry() => Assert.True(Result);
    [Fact] async Task should_not_insert_another_session() => await Sessions.Received(1).Complete(Arg.Any<StagedInvitationTransaction>(), Arg.Any<VerifiedInvitationAttestation>());
}
#endif
