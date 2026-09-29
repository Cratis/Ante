// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.given;
using Ante.Invitations.Receiving;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Integration.Invitations.when_ante_starts_without_a_signing_key;

/// <summary>
/// An invitation received while no signing key is configured waits - without failing the issuing reactor - and gets
/// its link once Ante restarts with a key (Cratis/Ante#107).
/// </summary>
[Collection(ChronicleCollection.Name)]
public class and_a_key_is_configured_on_restart : a_running_ante
{
    readonly Guid _invitationId = NewInvitationId();
    IReadOnlyList<AppendedEvent> _publishedWithoutKey;
    IEnumerable<FailedPartition> _failedPartitionsWithoutKey;
    ReactorState _stateWithoutKey;
    InvitationTokenIssued _issued;
    IReadOnlyList<AppendedEvent> _publishedWithKey;

    protected override bool SigningKeyConfigured => false;

    async Task Because()
    {
        await Host.Publish(_invitationId, JoinInvitation("Acme"), subject: Guid.NewGuid());
        await Eventually.Until(
            async () => (await LocalEvents<InvitationTokenIssuanceDeferred>()).Count > 0,
            what: "Ante to defer the invitation's token while it has no signing key");

        await using (var scope = Ante.Services.CreateAsyncScope())
        {
            var reactors = scope.ServiceProvider.GetRequiredService<IEventStore>().Reactors;
            _failedPartitionsWithoutKey = await reactors.GetFailedPartitionsFor<InvitationTokenIssuingReactor>();
            _stateWithoutKey = await reactors.GetStateFor<InvitationTokenIssuingReactor>();
        }

        _publishedWithoutKey = await Host.ReceivedFromAnte(_invitationId.ToString("D"));

        await Restart(signingKeyConfigured: true);

        // The first resumption pass can run before Chronicle is connected; the next one follows within its interval.
        _issued = await Host.WaitForFromAnte<InvitationTokenIssued>(_invitationId.ToString("D"), Eventually.DefaultTimeout + InvitationTokenIssuanceResumption.Interval);
        _publishedWithKey = await Host.ReceivedFromAnte(_invitationId.ToString("D"));
    }

    [Fact] void should_not_publish_anything_while_no_key_is_configured() => Assert.Empty(_publishedWithoutKey);
    [Fact] void should_not_fail_the_issuing_reactor_while_no_key_is_configured() => Assert.Empty(_failedPartitionsWithoutKey);
    [Fact] void should_keep_the_issuing_reactor_active_while_no_key_is_configured() => Assert.Equal(ObserverRunningState.Active, _stateWithoutKey.RunningState);
    [Fact] void should_issue_the_token_once_a_key_is_configured() => _issued.Token.ShouldNotBeEmpty();
    [Fact] void should_issue_exactly_one_token() => Assert.Single(_publishedWithKey, entry => entry.Content is InvitationTokenIssued);

    async Task<IReadOnlyList<AppendedEvent>> LocalEvents<TEvent>()
    {
        await using var scope = Ante.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IEventStore>().EventLog.GetForEventSourceIdAndEventTypes(
            _invitationId.ToString("D"), [typeof(TEvent).GetEventType()]);
    }
}
