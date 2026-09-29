// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting.for_AcceptedInvitationIndexRegistration.given;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_AcceptedInvitationIndexRegistration.when_installing_indexes;

public class and_authentication_fails : Specification
{
    readonly List<(LogLevel Level, string Message)> _logged = [];
    ServiceProvider _provider = null!;
    AcceptedInvitationIndexRegistration _registration = null!;
    Exception? _error;
    int _attempts;

    void Establish()
    {
        var indexes = Substitute.For<IMongoIndexManager<AcceptedInvitation>>();
        indexes.CreateManyAsync(Arg.Any<IEnumerable<CreateIndexModel<AcceptedInvitation>>>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                Interlocked.Increment(ref _attempts);
                return Task.FromException<IEnumerable<string>>(an_index_error.Authentication());
            });
        var collection = Substitute.For<IMongoCollection<AcceptedInvitation>>();
        collection.Indexes.Returns(indexes);
        _provider = new ServiceCollection().AddSingleton(collection).BuildServiceProvider();
        _registration = new(_provider.GetRequiredService<IServiceScopeFactory>(), new RecordingLogger(_logged));
    }

    async Task Because() => _error = await Cratis.Specifications.Catch.Exception(async () =>
    {
        await _registration.StartAsync(CancellationToken.None);
        await _registration.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5));
    });

    async Task Destroy() => await _provider.DisposeAsync();

    [Fact] void should_fail_rather_than_retry() => _error.ShouldBeOfExactType<MongoAuthenticationException>();
    [Fact] void should_attempt_once() => _attempts.ShouldEqual(1);
    [Fact] void should_not_mark_the_indexes_ready() => _registration.IsReady.ShouldBeFalse();
    [Fact] void should_log_it_as_a_critical_misconfiguration() =>
        _logged.ShouldContain(entry => entry.Level == LogLevel.Critical && entry.Message.Contains("credentials, connection settings or driver"));
    [Fact] void should_not_log_it_as_unavailable() => _logged.ShouldNotContain(entry => entry.Level == LogLevel.Warning);

    sealed class RecordingLogger(List<(LogLevel Level, string Message)> logged) : ILogger<AcceptedInvitationIndexRegistration>
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            logged.Add((logLevel, formatter(state, exception)));
    }
}
#endif
