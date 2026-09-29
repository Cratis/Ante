// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Issuing.for_InvitationTokenUpgradeWindow.when_loading.given;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Ante.Invitations.Issuing.for_InvitationTokenUpgradeWindow.when_loading;

public class and_this_is_the_first_run : an_activation_store
{
    string[] _recordedOnInsert = [];
    BsonValue _recordedEnd = BsonNull.Value;
    BsonValue _expectedEnd = BsonNull.Value;

    async Task Because()
    {
        await InvitationTokenUpgradeWindow.Load(Database, Now, Expiry);
        var recorded = RenderedUpdate()["$setOnInsert"].AsBsonDocument;
        _recordedOnInsert = [.. recorded.Names];
        _recordedEnd = recorded["LegacyUntil"];
        _expectedEnd = Rendered(Builders<InvitationTokenIsolationActivation>.Update.SetOnInsert(document => document.LegacyUntil, Now + InvitationTokenUpgradeWindow.RolloutGrace + Expiry))["$setOnInsert"]["LegacyUntil"];
    }

    [Fact] void should_record_the_activation_only_on_insert() => Assert.Contains("ActivatedAt", _recordedOnInsert);
    [Fact] void should_record_when_the_window_ends_only_on_insert() => Assert.Contains("LegacyUntil", _recordedOnInsert);
    [Fact] void should_end_the_window_after_the_rollout_grace_and_one_expiry() => Assert.Equal(_expectedEnd, _recordedEnd);
}
#endif
