// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using Ante.Invitations;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

namespace Ante.Integration.given;

/// <summary>
/// One real Chronicle kernel with its embedded MongoDB, shared by every spec in <see cref="ChronicleCollection"/>.
/// </summary>
/// <remarks>
/// Mirrors Chronicle's own <c>ChronicleOutOfProcessFixture</c> (same <c>cratis/chronicle:*-development</c> image,
/// same TLS-skipping connection, same health wait) without taking its package: that fixture binds fixed host ports
/// (27018/35001), fixes the event store to <c>testing</c> and embeds a whole in-process kernel, while Ante needs its
/// own store names, several host stores and a pinnable server version. Isolation between specs comes from unique
/// store and database names, not from a fresh container.
/// <para>
/// Kernel restart specs stop and start this same container (<see cref="StopChronicle"/>/<see cref="StartChronicle"/>):
/// its writable layer - the embedded MongoDB included - survives, and the host ports are fixed up front so every
/// running client and Ante instance can reconnect to the same address.
/// </para>
/// <para>
/// The image defaults to the server release matching the pinned client; override it with
/// <c>ANTE_CHRONICLE_IMAGE</c> (for example <c>cratis/chronicle:19.4.7-development</c>) to check another server.
/// </para>
/// </remarks>
public sealed class ChronicleInfrastructure : IAsyncLifetime
{
    public const string DefaultImage = "cratis/chronicle:19.22.1-development";
    const ushort ChroniclePort = 35000;
    const ushort HttpPort = 8080;
    const ushort MongoDBPort = 27017;

    const int MaxStartAttempts = 3;

    IContainer? _container;
    int _chroniclePort;
    int _mongoDBPort;

    /// <summary>
    /// Gets the kernel started for <see cref="ChronicleCollection"/>. Specs reach it statically rather than through
    /// constructor injection: Cratis.Specifications only runs Establish/Because once per class for classes with a
    /// parameterless constructor.
    /// </summary>
    public static ChronicleInfrastructure Current
    {
        get => field ?? throw new InvalidOperationException($"Specs using the kernel must be in [Collection({nameof(ChronicleCollection)}.{nameof(ChronicleCollection.Name)})].");
        private set;
    }

    public static string Image => Environment.GetEnvironmentVariable("ANTE_CHRONICLE_IMAGE") is { Length: > 0 } image ? image : DefaultImage;

    /// <summary>
    /// Gets a value indicating whether to leave the container running after the run for inspection
    /// (<c>ANTE_INTEGRATION_KEEP_CONTAINER=true</c>). Remove it yourself afterwards.
    /// </summary>
    public static bool KeepContainer => string.Equals(Environment.GetEnvironmentVariable("ANTE_INTEGRATION_KEEP_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase);

    public string ChronicleConnectionString => $"chronicle://localhost:{_chroniclePort}/?skipTlsValidation=true";

    // The embedded replica set advertises localhost:27017 inside the container; a direct connection stops the
    // driver from trying to follow that address from the host.
    public string MongoDBServer => $"mongodb://localhost:{_mongoDBPort}/?directConnection=true";

    IContainer Container => _container ?? throw new InvalidOperationException("The Chronicle container has not been started.");

    public async Task InitializeAsync()
    {
        // Client-only Mongo tests run before any Ante host starts; use the same concept map.
        InvitationMongoSerialization.EnsureConfigured();

        // A free port is only known to be free when it is picked: it is released before Docker binds it, so another
        // process can take it in between. Such a container fails to start; a new one with freshly picked ports is
        // tried instead. Any other failure is not retried.
        for (var attempt = 1; ; attempt++)
        {
            var container = CreateContainer();
            try
            {
                await container.StartAsync();
                _container = container;
                Current = this;
                return;
            }
            catch (Exception exception) when (attempt < MaxStartAttempts && IsPortBindFailure(exception))
            {
                await Console.Error.WriteLineAsync(
                    $"Chronicle container start attempt {attempt} of {MaxStartAttempts} could not bind its ports ({_chroniclePort}, {_mongoDBPort}); retrying with new ports.");
                await container.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Creates the container with freshly picked host ports. The ports stay with that container, so stopping and
    /// starting it (<see cref="StopChronicle"/>/<see cref="StartChronicle"/>) keeps its address.
    /// </summary>
    IContainer CreateContainer()
    {
        // Docker picks new random host ports when a stopped container starts again; bind free ones explicitly.
        var reserved = new HashSet<int>();
        _chroniclePort = FreePort(reserved);
        _mongoDBPort = FreePort(reserved);
        return new ContainerBuilder(Image)
            .WithPortBinding(_chroniclePort, ChroniclePort)
            .WithPortBinding(FreePort(reserved), HttpPort)
            .WithPortBinding(_mongoDBPort, MongoDBPort)
            .WithLabel("cratis.ante.integration", "true")
            .WithCleanUp(!KeepContainer)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilInternalTcpPortIsAvailable(MongoDBPort)
                .AddCustomWaitStrategy(new ChronicleHealthWait(ChroniclePort, HttpPort)))
            .Build();
    }

    // Docker reports a host port taken as "port is already allocated" or "address already in use"; a failed start's
    // exception carries the container's log, where the kernel reports the same as "address already in use".
    static bool IsPortBindFailure(Exception exception)
    {
        var description = exception.ToString();
        return description.Contains("address already in use", StringComparison.OrdinalIgnoreCase) ||
            description.Contains("port is already allocated", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Stops the kernel and its MongoDB without removing the container, its data or its port bindings.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    public Task StopChronicle() => Container.StopAsync();

    /// <summary>
    /// Starts the stopped kernel again and waits for its health endpoint.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    public Task StartChronicle() => Container.StartAsync();

    public async Task DisposeAsync()
    {
        if (_container is not null && !KeepContainer)
        {
            await _container.DisposeAsync();
        }
    }

    static int FreePort(HashSet<int> reserved)
    {
        while (true)
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            if (reserved.Add(port))
            {
                return port;
            }
        }
    }

    /// <summary>
    /// Waits for the kernel's health endpoint - served over TLS on the gRPC port by current servers, and over plain
    /// HTTP on 8080 by older ones.
    /// </summary>
    sealed class ChronicleHealthWait(ushort tlsPort, ushort httpPort) : IWaitUntil
    {
        public async Task<bool> UntilAsync(IContainer container)
        {
#pragma warning disable MA0039, CA5400 // Test-only: accept the kernel's self-signed development certificate.
            using var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator };
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(2) };
#pragma warning restore MA0039, CA5400
            foreach (var uri in new[]
            {
                new Uri($"https://{container.Hostname}:{container.GetMappedPublicPort(tlsPort)}/health"),
                new Uri($"http://{container.Hostname}:{container.GetMappedPublicPort(httpPort)}/health"),
            })
            {
                try
                {
                    using var response = await client.GetAsync(uri);
                    if (response.IsSuccessStatusCode)
                    {
                        return true;
                    }
                }
                catch (HttpRequestException)
                {
                }
                catch (TaskCanceledException)
                {
                }
            }

            return false;
        }
    }
}
