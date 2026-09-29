// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Net;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace Ante.Invitations.Accepting.for_AcceptedInvitationIndexRegistration.given;

internal static class an_index_error
{
    static readonly ConnectionId _connection = new(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017)));

    public static MongoCommandException Command(int code) =>
        new(_connection, "index command failed", [], new BsonDocument("code", code));

    public static MongoNotPrimaryException NotPrimary() =>
        new(_connection, [], new BsonDocument("code", 10107));

    public static MongoNodeIsRecoveringException Recovering() =>
        new(_connection, [], new BsonDocument("code", 91));
}
#endif
