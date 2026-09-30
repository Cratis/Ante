// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.given;

namespace Ante.Integration.Routes.when_probing_health_without_a_signing_key;

[Collection(ChronicleCollection.Name)]
public class and_production_is_served_directly : probing_health_without_a_signing_key
{
    protected override string EnvironmentName => "Production";
    protected override bool Proxied => false;
}
