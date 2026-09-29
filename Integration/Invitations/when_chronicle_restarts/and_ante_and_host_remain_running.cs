// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Ante.Integration.given;

namespace Ante.Integration.Invitations.when_chronicle_restarts;

/// <summary>
/// Ante and its host keep running while the kernel restarts: Ante reports itself unready during the outage and its
/// inbox reactor resumes afterwards without reissuing earlier tokens (Cratis/Ante#112).
/// </summary>
[Collection(ChronicleCollection.Name)]
public class and_ante_and_host_remain_running : a_running_ante
{
    readonly Guid _before = NewInvitationId();
    readonly Guid _after = NewInvitationId();
    HttpStatusCode _readinessDuringOutage;
    InvitationTokenIssued _issuedAfter;
    int _tokensBefore;

    async Task Establish() => await Invite(Host, _before, JoinInvitation());

    async Task Because()
    {
        _readinessDuringOutage = await RestartChronicle();
        await Host.Publish(_after, JoinInvitation(), subject: Guid.NewGuid());
        _issuedAfter = await Host.WaitForFromAnte<InvitationTokenIssued>(_after.ToString());
        _tokensBefore = (await Host.ReceivedFromAnte(_before.ToString())).Count(entry => entry.Content is InvitationTokenIssued);
    }

    [Fact] void should_lose_readiness_during_the_outage() => _readinessDuringOutage.ShouldEqual(HttpStatusCode.ServiceUnavailable);
    [Fact] void should_resume_the_inbox_reactor() => _issuedAfter.Token.ShouldNotBeEmpty();
    [Fact] void should_not_reissue_the_earlier_token() => _tokensBefore.ShouldEqual(1);
}
