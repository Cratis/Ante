// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.given;
using Ante.Invitations.Receiving;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Integration.Invitations.when_two_ante_instances_share_a_store;

/// <summary>
/// An invitation arrives while neither instance has a signing key, so its token is deferred (Cratis/Ante#107). Both
/// instances then restart with a key at the same time, and each one's resumption runs its startup pass and is also
/// driven by hand, concurrently, so both try to resume the one waiting invitation. The resumer's concurrency scope lets
/// only one resumption through, and exactly one token reaches the host (Cratis/Ante#110, Cratis/Ante#119).
/// </summary>
[Collection(ChronicleCollection.Name)]
public class and_both_restart_with_a_signing_key : two_running_antes
{
    readonly Guid _invitationId = NewInvitationId();
    int _deferredBeforeRestart;
    int[] _resumedByHand;
    InvitationTokenIssued _issued;
    int _resumptions;
    int _tokensPublished;
    int _tokensReceivedByHost;

    protected override bool SigningKeyConfigured => false;

    string Id => _invitationId.ToString("D");

    async Task Because()
    {
        await Host.Publish(_invitationId, JoinInvitation("Acme"), subject: Guid.NewGuid());
        await Eventually.Until(
            async () => (_deferredBeforeRestart = (await EventsFor(Ante, Id, EventSequenceId.Log, typeof(InvitationTokenIssuanceDeferred))).Count) > 0,
            what: "an instance to defer the invitation's token while neither has a signing key");

        await Other.DisposeAsync();
        var restarts = Task.WhenAll(
            Restart(signingKeyConfigured: true),
            Task.Run(async () => Other = await StartAnotherInstance(signingKeyConfigured: true)));
        await restarts;

        _resumedByHand = await Task.WhenAll(ResumeAll(Ante), ResumeAll(Other));

        _issued = await Host.WaitForFromAnte<InvitationTokenIssued>(Id, Eventually.DefaultTimeout + InvitationTokenIssuanceResumption.Interval);
        var resumptions = await EventsFor(Ante, Id, EventSequenceId.Log, typeof(InvitationTokenIssuanceResumed));
        _resumptions = resumptions.Count;

        // A second token could only come from handling a second resumption; count once every resumption is handled.
        var lastResumption = resumptions[^1].Context.SequenceNumber;
        await Eventually.Until(
            async () =>
            {
                await using var scope = Ante.Services.CreateAsyncScope();
                var state = await scope.ServiceProvider.GetRequiredService<IEventStore>().Reactors.GetStateFor<InvitationTokenIssuingReactor>();
                return state.LastHandledEventSequenceNumber.IsActualValue && state.LastHandledEventSequenceNumber >= lastResumption;
            },
            what: "the issuing reactor to handle every resumption");
        _tokensPublished = (await EventsFor(Ante, Id, EventSequenceId.Outbox, typeof(InvitationTokenIssued))).Count;
        _tokensReceivedByHost = (await Host.ReceivedFromAnte(Id)).Count(entry => entry.Content is InvitationTokenIssued);
    }

    [Fact] void should_have_deferred_the_token_once_before_the_restart() => _deferredBeforeRestart.ShouldEqual(1);
    [Fact] void should_resume_the_invitation_at_most_once_by_hand() => _resumedByHand.Sum().ShouldBeLessThanOrEqual(1);
    [Fact] void should_record_one_resumption() => _resumptions.ShouldEqual(1);
    [Fact] void should_issue_the_token() => _issued.Token.ShouldNotBeEmpty();
    [Fact] void should_publish_one_token() => _tokensPublished.ShouldEqual(1);
    [Fact] void should_deliver_one_token_to_the_host() => _tokensReceivedByHost.ShouldEqual(1);

    static async Task<int> ResumeAll(AnteApplication instance)
    {
        await using var scope = instance.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<InvitationTokenIssuanceResumer>().ResumeAll();
    }
}
