// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.given;

namespace Ante.Integration.Routes.when_limiting_registrations_by_client;

[Collection(ChronicleCollection.Name)]
public class and_production_is_behind_a_proxy_trusting_forwarded_for : limiting_registrations
{
    protected override string EnvironmentName => "Production";
    protected override bool Proxied => true;
    protected override bool TrustForwardedFor => true;
}
