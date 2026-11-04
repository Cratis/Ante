// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.for_AnteRoutingValidator.when_validating;

public class and_the_audience_is_invalid : Specification
{
    [Theory]
    [InlineData("  ", null)]
    [InlineData(" lobby", null)]
    [InlineData(null, "  ")]
    [InlineData(null, "operator ")]
    void should_reject_blank_or_padded_audiences(string? audience, string? operatorAudience)
    {
        var options = new AnteOptions { Audience = audience, OperatorAudience = operatorAudience };
        Assert.Throws<AnteAudienceInvalid>(() => AnteRoutingValidator.Validate(options));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("lobby", "operator")]
    void should_accept_unset_empty_and_named_audiences(string? audience, string? operatorAudience)
    {
        var options = new AnteOptions { Audience = audience, OperatorAudience = operatorAudience };
        AnteRoutingValidator.Validate(options);
    }
}
#endif
