// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.for_InvitationContentOptionsValidator.when_validating;

public class and_copy_is_too_long : Specification
{
    Exception? _error;

    void Because()
    {
        var options = new AnteOptions();
        options.Invitations.UserSetup.Content["en"] = new() { Intro = new string('x', 10001) };
        _error = Cratis.Specifications.Catch.Exception(() => InvitationContentOptionsValidator.Validate(options, new ConfigurationBuilder().Build().GetSection("Ante")));
    }

    [Fact] void should_reject_it_before_serving_requests() => Assert.IsType<InvitationContentConfigurationInvalid>(_error);
}
#endif
