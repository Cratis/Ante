// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.for_AnteRoutingValidator.when_validating;

/// <summary>
/// A deployment renaming its store and namespace away from the documented defaults - the entire point of
/// this work - must still pass validation and boot normally.
/// </summary>
public class and_everything_is_valid : Specification
{
    AnteOptions _options = null!;
    Exception? _result;

    void Establish() => _options = new()
    {
        EventStore = "DirectLobby",
        Namespace = "Acme",
    };

    void Because() => _result = Cratis.Specifications.Catch.Exception(() => AnteRoutingValidator.Validate(_options));

    [Fact] void should_not_throw() => Assert.Null(_result);
}
#endif
