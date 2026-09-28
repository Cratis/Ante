// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Organization.Registration.for_RegistrationQuota.when_checking;

public class and_older_registrations_fall_outside_the_window : a_sign_in_with_registrations
{
    RegistrationQuotaCheck _result = null!;

    void Establish() => History = [ConsumedAt(Now.AddDays(-40), 1), ConsumedAt(Now.AddDays(-35), 2), ConsumedAt(Now.AddDays(-1), 3)];

    async Task Because() => _result = await RegistrationQuota.Check(Store, new() { MaxPerIdentity = 2, Window = TimeSpan.FromDays(30) }, Owner, Now);

    [Fact] void should_allow_another() => Assert.True(_result.IsAllowed);
}
#endif
