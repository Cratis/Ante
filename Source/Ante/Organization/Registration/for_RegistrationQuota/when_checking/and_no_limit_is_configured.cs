// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Organization.Registration.for_RegistrationQuota.when_checking;

public class and_no_limit_is_configured : a_sign_in_with_registrations
{
    bool _result;

    void Establish() => History = [ConsumedAt(Now.AddHours(-1), 1), ConsumedAt(Now.AddHours(-1), 2)];

    async Task Because() => _result = await RegistrationQuota.IsWithinLimit(Store, new(), Owner, Now);

    [Fact] void should_allow_it() => Assert.True(_result);
}
#endif
