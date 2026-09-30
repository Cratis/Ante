// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.given;

namespace Ante.Integration.Routes.when_a_foreign_site_forges_a_request;

[Collection(ChronicleCollection.Name)]
public class and_production_is_served_directly : forging_a_request
{
    protected override string EnvironmentName => "Production";
    protected override bool Proxied => false;
}
