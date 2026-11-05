// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Onboarding;
using Ante.Integration.given;
using Ante.Onboarding.Provisioning;

namespace Ante.Integration.Invitations.when_a_host_confirms_provisioning;

[Collection(ChronicleCollection.Name)]
public class and_the_member_has_joined : a_running_ante
{
    readonly Guid _invitationId = NewInvitationId();
    readonly string _subject = $"user-{Guid.NewGuid():N}";
    OnboardingTrace _trace = null!;
    OnboardingTrace _beforeConfirmation = null!;

    async Task Because()
    {
        await Host.Publish(_invitationId, new UserInvitedToJoinTenant($"{Guid.NewGuid():N}@example.com", "Acme", ["Member"]));
        var issued = await Host.WaitForFromAnte<InvitationTokenIssued>(_invitationId.ToString("D"));
        using var exchange = await Ante.ExchangeInvitation(issued.Token, _subject);
        await ExecuteOnceProjected(
            "/api/invitations/user-setup",
            new { invitationId = _invitationId, firstName = "Jane", middleName = "", lastName = "Doe", acceptedLegalTerms = false, acceptedLegalVersion = "" },
            _subject);
        await Host.WaitForFromAnte<InvitationToJoinTenantAccepted>(_invitationId.ToString("D"));
        _beforeConfirmation = await OnboardingTrace.Capture(Ante, Host, _invitationId.ToString("D"));

        await Host.Publish(_invitationId, new OnboardingProvisioningCompleted());
        await Host.WaitForFromAnte<OnboardingProvisioningConfirmed>(_invitationId.ToString("D"));
        await Host.Publish(_invitationId, new OnboardingProvisioningCompleted());
        _trace = await OnboardingTrace.CaptureSettled(
            Ante,
            Host,
            _invitationId.ToString("D"),
            trace => trace.AnteInbox.Count(entry => entry.Content is OnboardingProvisioningCompleted) >= 2);
    }

    [Fact] void should_not_publish_readiness_on_acceptance_alone() =>
        _beforeConfirmation.AnteOutbox.Any(entry => entry.Content is OnboardingProvisioningConfirmed).ShouldBeFalse();
    [Fact] void should_record_one_host_confirmation() =>
        _trace.AnteLog.Count(entry => entry.Content is OnboardingProvisioningCompletionReceived).ShouldEqual(1);
    [Fact] void should_publish_readiness_only_once() =>
        _trace.AnteOutbox.Count(entry => entry.Content is OnboardingProvisioningConfirmed).ShouldEqual(1);
    [Fact] void should_deliver_the_confirmation_under_the_attempt_id() =>
        OnboardingTrace.TheOne<OnboardingProvisioningConfirmed>(_trace.HostReceipt).Context.EventSourceId.Value.ShouldEqual(_invitationId.ToString("D"));
}
