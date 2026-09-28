// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Net;
using Ante.Integration.given;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Ante.Integration.Invitations.when_a_token_reaches_current_and_generation_two_consumers;

[Collection(ChronicleCollection.Name)]
public class and_both_read_the_host_inbox : Specification
{
    readonly ChronicleInfrastructure _infrastructure = ChronicleInfrastructure.Current;
    readonly string _suffix = Guid.NewGuid().ToString("N")[..8];
    HostStore _currentHost = null!;
    AnteApplication _ante = null!;
    InvitationTokenIssued _current = null!;
    string _older = string.Empty;
    Guid _currentId;
    uint _issuedGeneration;

    string AnteStore => $"Ante{_suffix}";
    string OldHost => $"Legacy{_suffix}";

    async Task Establish()
    {
        _currentHost = await HostStore.Connect(_infrastructure, $"Current{_suffix}", AnteStore);
        await RunOlderHost("setup", Guid.Empty);
        _ante = new AnteApplication(_infrastructure, AnteStore, [_currentHost.Name, OldHost]);
        using var client = _ante.CreateClient();
        await Eventually.Until(async () => (await client.GetAsync("/healthz/ready")).StatusCode == HttpStatusCode.OK,
            what: "Ante ready with a generation-2-only host");
    }

    async Task Because()
    {
        _currentId = Guid.NewGuid();
        var oldId = Guid.NewGuid();
        await _currentHost.Publish(_currentId, new UserInvitedToJoinTenant($"{Guid.NewGuid():N}@example.com", "Acme", ["member"]), subject: Guid.NewGuid());
        Assert.Equal("published", await RunOlderHost("publish", oldId));
        _current = await _currentHost.WaitForFromAnte<InvitationTokenIssued>(_currentId.ToString("D"));
        var received = await _currentHost.ReceivedFromAnte(_currentId.ToString("D"));
        _issuedGeneration = received.Single(entry => entry.Content is InvitationTokenIssued).Context.EventType.Generation.Value;
        _older = await Eventually.Get(async () => await RunOlderHost("read", oldId),
            what: "generation-2-only host inbox token");
    }

    async Task Destroy()
    {
        if (_currentHost is not null)
        {
            await _currentHost.DisposeAsync();
        }

        if (_ante is not null)
        {
            await _ante.DisposeAsync();
        }
    }

    [Fact] void should_have_published_generation_three() => Assert.Equal(3u, _issuedGeneration);
    [Fact] void should_read_the_current_contracts_token() => Assert.Equal(_currentId.ToString("D"), new JsonWebToken(_current.Token).Id);
    [Fact] void should_read_a_usable_token_from_the_old_only_store() => Assert.Equal("readable", _older);

    async Task<string?> RunOlderHost(string operation, Guid id)
    {
        var dll = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "LegacyConsumer", "bin", "Debug", "net10.0", "LegacyConsumer.dll");
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in new[] { dll, operation, _infrastructure.ChronicleConnectionString, OldHost, AnteStore, id.ToString("D") })
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start the isolated generation-2 consumer.");
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        await process.WaitForExitAsync(deadline.Token);
        var output = (await process.StandardOutput.ReadToEndAsync()).Trim();
        var error = await process.StandardError.ReadToEndAsync();
        if (process.ExitCode == 4 && output == "missing")
        {
            return null;
        }

        if (process.ExitCode == 5 && output == "unreadable")
        {
            return output;
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Generation-2 consumer failed with exit {process.ExitCode}: {error}");
        }

        return output;
    }
}
