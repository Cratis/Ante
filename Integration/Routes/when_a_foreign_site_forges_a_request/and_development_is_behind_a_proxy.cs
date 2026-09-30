// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.given;

namespace Ante.Integration.Routes.when_a_foreign_site_forges_a_request;

[Collection(ChronicleCollection.Name)]
public class and_development_is_behind_a_proxy : forging_a_request
{
    protected override string EnvironmentName => "Development";
    protected override bool Proxied => true;
}
