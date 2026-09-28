// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Integration.given;

[CollectionDefinition(Name)]
public class ChronicleCollection : ICollectionFixture<ChronicleInfrastructure>
{
    public const string Name = "Chronicle";
}
