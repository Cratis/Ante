// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Organization.Registration.for_RegistrationQuota.when_checking;

public class and_keying_a_sign_in : Specification
{
    EventSourceId _key = null!;

    void Because() => _key = RegistrationQuota.KeyFor(new((RegistrationOwnerSubject)"sub-1", "github"));

    [Fact] void should_not_contain_the_subject() => Assert.DoesNotContain("sub-1", _key.Value, StringComparison.Ordinal);
    [Fact] void should_be_stable() => Assert.Equal(_key, RegistrationQuota.KeyFor(new((RegistrationOwnerSubject)"sub-1", "github")));
    [Fact] void should_differ_per_provider() => Assert.NotEqual(_key, RegistrationQuota.KeyFor(new((RegistrationOwnerSubject)"sub-1", "google")));
}
#endif
