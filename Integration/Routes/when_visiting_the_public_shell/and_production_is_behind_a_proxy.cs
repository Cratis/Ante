// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.given;

namespace Ante.Integration.Routes.when_visiting_the_public_shell;

[Collection(ChronicleCollection.Name)]
public class and_production_is_behind_a_proxy : visiting_the_public_shell
{
    protected override string EnvironmentName => "Production";
    protected override bool Proxied => true;
}
