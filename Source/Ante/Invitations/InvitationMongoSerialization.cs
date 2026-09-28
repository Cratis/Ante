// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Ante.Invitations;

/// <summary>
/// Defines the BSON representation of Guid-backed invitation concepts before a Mongo collection
/// first requests their class map. MongoDB.Driver 3's default Guid serializer is unspecified.
/// </summary>
public static class InvitationMongoSerialization
{
    static readonly Lock _sync = new();
    static bool _configured;

    /// <summary>Registers the standard Guid serializer for the inherited concept value.</summary>
    public static void EnsureConfigured()
    {
        lock (_sync)
        {
            if (_configured)
            {
                return;
            }

            BsonClassMap.RegisterClassMap<ConceptAs<Guid>>(map =>
            {
                map.AutoMap();
                map.GetMemberMap(nameof(ConceptAs<>.Value)).SetSerializer(GuidSerializer.StandardInstance);
            });
            BsonClassMap.RegisterClassMap<EventSourceId<Guid>>(map =>
            {
                map.AutoMap();
                map.GetMemberMap(nameof(EventSourceId<>.TypedValue)).SetSerializer(GuidSerializer.StandardInstance);
            });
            _configured = true;
        }
    }
}
