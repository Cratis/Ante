// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.for_InvitationContentOptionsValidator.when_validating;

public class and_options_are_left_at_their_defaults : Specification
{
    Exception? _error;

    void Because() => _error = Cratis.Specifications.Catch.Exception(() => InvitationContentOptionsValidator.Validate(
        new AnteOptions(), new ConfigurationBuilder().Build().GetSection("Ante")));

    [Fact] void should_accept_an_unconfigured_deployment() => Assert.Null(_error);
}
#endif
