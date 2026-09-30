// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.given;

namespace Ante.Integration.Routes.when_reading_the_public_configuration;

[Collection(ChronicleCollection.Name)]
public class and_development_is_behind_a_proxy : reading_the_public_configuration
{
    protected override string EnvironmentName => "Development";
    protected override bool Proxied => true;
}
