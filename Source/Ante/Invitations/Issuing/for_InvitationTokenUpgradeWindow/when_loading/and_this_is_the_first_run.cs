// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Issuing.for_InvitationTokenUpgradeWindow.when_loading.given;

namespace Ante.Invitations.Issuing.for_InvitationTokenUpgradeWindow.when_loading;

public class and_this_is_the_first_run : an_activation_store
{
    string[] _recordedOnInsert = [];

    async Task Because()
    {
        await InvitationTokenUpgradeWindow.Load(Database, Now, Expiry);
        _recordedOnInsert = [.. RenderedUpdate()["$setOnInsert"].AsBsonDocument.Names];
    }

    [Fact] void should_record_the_activation_only_on_insert() => Assert.Contains("ActivatedAt", _recordedOnInsert);
    [Fact] void should_record_when_the_window_ends_only_on_insert() => Assert.Contains("LegacyUntil", _recordedOnInsert);
}
#endif
