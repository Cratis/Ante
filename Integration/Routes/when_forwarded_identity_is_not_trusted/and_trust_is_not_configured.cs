// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.given;

namespace Ante.Integration.Routes.when_forwarded_identity_is_not_trusted;

[Collection(ChronicleCollection.Name)]
public class and_trust_is_not_configured : and_a_client_supplies_a_subject
{
    protected override string? TrustSetting => null;
}
