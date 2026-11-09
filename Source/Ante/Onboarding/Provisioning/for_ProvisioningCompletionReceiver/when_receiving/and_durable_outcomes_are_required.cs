// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving;

namespace Ante.Onboarding.Provisioning.for_ProvisioningCompletionReceiver.when_receiving;

/// <summary>With durable outcomes required only a verified, host-signed success may confirm provisioning.</summary>
public class and_durable_outcomes_are_required : given.a_provisioning_completion
{
    void Establish()
    {
        _history = [_joined, new InvitationSourceInboxEventRecorded(1, "Host")];
        var store = Substitute.For<IEventStore>();
        store.EventLog.Returns(_log);
        _receiver = new(store, Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance, "Host", durableOutcomesRequired: true);
    }

    Task Because() => _receiver.Receive(_context);

    [Fact] void should_ignore_the_unsigned_confirmation() => ShouldNotHaveAppended();
}
#endif
