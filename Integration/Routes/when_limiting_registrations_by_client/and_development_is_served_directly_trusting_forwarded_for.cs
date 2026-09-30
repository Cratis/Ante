// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.given;

namespace Ante.Integration.Routes.when_limiting_registrations_by_client;

[Collection(ChronicleCollection.Name)]
public class and_development_is_served_directly_trusting_forwarded_for : limiting_registrations
{
    protected override string EnvironmentName => "Development";
    protected override bool Proxied => false;
    protected override bool TrustForwardedFor => true;
}
