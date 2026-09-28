// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using MongoDB.Driver;

namespace Ante.Invitations.for_query_access;

internal static class QueryCollections
{
    public static IEventStore ReadModelStoreWith<T>(T value)
    {
        var store = Substitute.For<IEventStore>();
        var readModels = Substitute.For<IReadModels>();
        store.ReadModels.Returns(readModels);
        readModels.GetInstanceById<T>(Arg.Any<ReadModelKey>(), Arg.Any<ReadModelSessionId?>()).Returns(Task.FromResult(value));
        return store;
    }

    public static IMongoCollection<T> With<T>(T value)
    {
        var collection = Substitute.For<IMongoCollection<T>>();
        collection.FindSync(Arg.Any<FilterDefinition<T>>(), Arg.Any<FindOptions<T, T>>(), Arg.Any<CancellationToken>())
            .Returns(_ => CursorWith(value));
        collection.FindAsync(Arg.Any<FilterDefinition<T>>(), Arg.Any<FindOptions<T, T>>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(CursorWith(value)));
        return collection;
    }

    static IAsyncCursor<T> CursorWith<T>(T value)
    {
        var cursor = Substitute.For<IAsyncCursor<T>>();
        cursor.MoveNext(Arg.Any<CancellationToken>()).Returns(true, false);
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true), Task.FromResult(false));
        cursor.Current.Returns([value]);
        return cursor;
    }
}
#endif
