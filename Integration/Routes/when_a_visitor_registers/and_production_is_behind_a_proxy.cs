// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.given;

namespace Ante.Integration.Routes.when_a_visitor_registers;

[Collection(ChronicleCollection.Name)]
public class and_production_is_behind_a_proxy : registering_an_organization
{
    protected override string EnvironmentName => "Production";
    protected override bool Proxied => true;
}
