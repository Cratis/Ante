// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.for_AnteRoutingValidator.when_validating;

/// <summary>
/// An instance that never touches <c language="csharp">Ante:EventStore</c>, <c language="csharp">Ante:Namespace</c> or
/// <c language="csharp">Ante:InboxSourceStore</c> at all must keep booting exactly as it did before this validation existed.
/// </summary>
public class and_options_are_left_at_their_defaults : Specification
{
    AnteOptions _options = null!;
    Exception? _result;

    void Establish() => _options = new();

    void Because() => _result = Cratis.Specifications.Catch.Exception(() => AnteRoutingValidator.Validate(_options));

    [Fact] void should_not_throw() => Assert.Null(_result);
}
#endif
