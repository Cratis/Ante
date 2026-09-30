// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.given;

namespace Ante.Integration.Routes.when_a_proxy_forwards_headers;

[Collection(ChronicleCollection.Name)]
public class and_development_is_behind_a_proxy_with_forwarded_headers_off_and_a_loopback_proxy : forwarding_headers
{
    protected override string EnvironmentName => "Development";
    protected override bool Proxied => true;
    protected override bool Enabled => false;
    protected override bool ProxyIsLoopback => true;
}
