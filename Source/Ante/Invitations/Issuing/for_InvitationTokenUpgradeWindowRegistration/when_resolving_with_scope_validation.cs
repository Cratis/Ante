// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting;
using MongoDB.Driver;

namespace Ante.Invitations.Issuing.for_InvitationTokenUpgradeWindowRegistration;

public class when_resolving_with_scope_validation : Specification
{
    static readonly DateTimeOffset _now = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
    IInvitationTokenUpgradeWindow? _window;
    Exception? _error;
    IMongoCollection<AcceptedInvitation> _accepted = null!;
    bool _acceptsBeforeLoading;

    void Because()
    {
        _accepted = Substitute.For<IMongoCollection<AcceptedInvitation>>();

        // Like Development: the collection is scoped, and resolving it from the root provider throws.
        var provider = new ServiceCollection()
            .AddScoped(_ => _accepted)
            .AddInvitationTokenUpgradeWindow(TimeSpan.FromDays(7))
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        try
        {
            _window = provider.GetRequiredService<IInvitationTokenUpgradeWindow>();
            _acceptsBeforeLoading = _window.AcceptsLegacyToken(_now, _now.AddDays(1), _now);
        }
        catch (Exception error)
        {
            _error = error;
        }
    }

    [Fact] void should_not_fail_scope_validation() => _error.ShouldBeNull();
    [Fact] void should_provide_a_window() => _window.ShouldNotBeNull();
    [Fact] void should_not_touch_mongodb_while_resolving() => _accepted.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_accept_nothing_until_it_is_loaded() => _acceptsBeforeLoading.ShouldBeFalse();
}
#endif
