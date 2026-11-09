// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Onboarding;
using Ante.Invitations.Receiving;

namespace Ante.Onboarding.Provisioning.for_HostProvisioningOutcomeReceiver.when_receiving;

public class and_the_outcome_must_be_refused : given.an_accepted_operation
{
    readonly List<string> _accepted = [];

    async Task Because()
    {
        var accepted = new[] { _joined, (object)new InvitationSourceInboxEventRecorded(1, Host) };
        var context = _context;
        var cases = new (string Name, Func<object> Arrange)[]
        {
            ("a proof whose signature belongs to another proof", () =>
            {
                var proof = Succeeded().Proof.Value;
                var other = Succeeded().Proof.Value;
                return new HostProvisioningSucceeded(_attemptId, $"{proof[..proof.LastIndexOf('.')]}{other[other.LastIndexOf('.')..]}");
            }),
            ("a proof signed for another attempt", () => Succeeded() with { AttemptId = Guid.NewGuid() }),
            ("a proof signed for another outcome", () => new HostProvisioningSucceeded(_attemptId, Failed().Proof)),
            ("an expired proof", () => new HostProvisioningSucceeded(_attemptId, Sign(SucceededClaims(), _now.AddMinutes(-10)))),
            ("a proof signed with another host's key", () => new HostProvisioningSucceeded(_attemptId, Sign(SucceededClaims(), _now, key: _otherHostKey, kid: "other-key"))),
            ("a proof signed by an unpinned key", () => new HostProvisioningSucceeded(_attemptId, Sign(SucceededClaims(), _now, key: _unpinnedKey))),
            ("a reason code that is not generic", () => Failed("Contact jane@example.com")),
            ("no proof", () => new HostProvisioningSucceeded(_attemptId, null!)),
            ("no attempt", () => Succeeded() with { AttemptId = null! }),
            ("an operation Ante never accepted", () =>
            {
                _history = [];
                return Succeeded();
            }),
            ("an operation received from another host", () =>
            {
                _history = [_joined, new InvitationSourceInboxEventRecorded(1, OtherHost)];
                return Succeeded();
            }),
            ("an operation id that is not canonical", () =>
            {
                _context = context with { EventSourceId = _operationId.ToUpperInvariant() };
                return new HostProvisioningSucceeded(_attemptId, Sign(Claims(nameof(HostProvisioningSucceeded), _operationId.ToUpperInvariant(), _attemptId, "succeeded"), _now));
            }),
        };

        foreach (var (name, arrange) in cases)
        {
            _history = accepted;
            _context = context;
            var outcome = arrange();
            var before = _appended.Count;
            await Deliver(outcome);
            if (_appended.Count != before)
            {
                _accepted.Add(name);
            }
        }
    }

    [Fact] void should_refuse_every_case() => _accepted.ShouldBeEmpty();
    [Fact] void should_append_nothing() => ShouldNotHaveAppended();
}
#endif
