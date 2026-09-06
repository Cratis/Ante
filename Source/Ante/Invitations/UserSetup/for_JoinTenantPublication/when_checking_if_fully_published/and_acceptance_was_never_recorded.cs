// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.UserSetup.for_JoinTenantPublication.when_checking_if_fully_published;

public class and_acceptance_was_never_recorded : Specification
{
    bool _result;

    void Because() => _result = JoinTenantPublication.IsFullyPublished(recorded: null, published: null);

    [Fact] void should_not_be_fully_published() => Assert.False(_result);
}
#endif
