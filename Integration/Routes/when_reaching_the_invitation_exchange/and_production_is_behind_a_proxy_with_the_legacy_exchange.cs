// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.given;

namespace Ante.Integration.Routes.when_reaching_the_invitation_exchange;

[Collection(ChronicleCollection.Name)]
public class and_production_is_behind_a_proxy_with_the_legacy_exchange : reaching_the_invitation_exchange
{
    protected override string EnvironmentName => "Production";
    protected override bool Proxied => true;
    protected override bool Attested => false;
}
