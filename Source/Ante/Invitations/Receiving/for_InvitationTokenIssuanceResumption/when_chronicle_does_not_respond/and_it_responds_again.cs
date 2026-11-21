// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Receiving.for_InvitationTokenIssuanceResumption.when_chronicle_does_not_respond;

public class and_it_responds_again : given.a_resumption_against_a_slow_chronicle
{
    Task _started = null!;

    void Establish()
    {
        ReadsBeforeChronicleResponds = 1;
        Build();
    }

    async Task Because()
    {
        try
        {
            _started = Resumption.StartAsync(CancellationToken.None);
            await _started.WaitAsync(Deadline);
            await Appended.Task.WaitAsync(Deadline);
        }
        finally
        {
            await Stop();
        }
    }

    [Fact] void should_start_without_waiting_for_chronicle() => Assert.True(_started.IsCompletedSuccessfully);

    [Fact] void should_retry_after_the_pass_timed_out() => Assert.True(Reads > 1, $"expected a retried read, saw {Reads}");

    [Fact] void should_resume_the_waiting_invitation_once_chronicle_responds() => Assert.True(Appended.Task.IsCompletedSuccessfully);
}
#endif
