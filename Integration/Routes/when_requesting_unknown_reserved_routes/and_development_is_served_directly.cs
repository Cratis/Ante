// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.given;

namespace Ante.Integration.Routes.when_requesting_unknown_reserved_routes;

[Collection(ChronicleCollection.Name)]
public class and_development_is_served_directly : requesting_unknown_reserved_routes
{
    protected override string EnvironmentName => "Development";
    protected override bool Proxied => false;
}
