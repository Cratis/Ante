// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.given;

namespace Ante.Integration.Routes.when_reaching_the_invitation_exchange;

[Collection(ChronicleCollection.Name)]
public class and_development_is_served_directly_with_the_legacy_exchange : reaching_the_invitation_exchange
{
    protected override string EnvironmentName => "Development";
    protected override bool Proxied => false;
    protected override bool Attested => false;
}
