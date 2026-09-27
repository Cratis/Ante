// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
/// The image defaults to the server release matching the pinned client; override it with
/// <c>ANTE_CHRONICLE_IMAGE</c> (for example <c>cratis/chronicle:19.4.7-development</c>) to check another server.
/// </para>
/// </remarks>
public sealed class ChronicleInfrastructure : IAsyncLifetime
{
    public const string DefaultImage = "cratis/chronicle:19.13.1-development";
    const ushort ChroniclePort = 35000;
    const ushort HttpPort = 8080;
    const ushort MongoDBPort = 27017;

    IContainer? _container;

    public static string Image => Environment.GetEnvironmentVariable("ANTE_CHRONICLE_IMAGE") is { Length: > 0 } image ? image : DefaultImage;

    public string ChronicleConnectionString => $"chronicle://localhost:{Container.GetMappedPublicPort(ChroniclePort)}/?skipTlsValidation=true";

    // The embedded replica set advertises localhost:27017 inside the container; a direct connection stops the
    // driver from trying to follow that address from the host.
    public string MongoDBServer => $"mongodb://localhost:{Container.GetMappedPublicPort(MongoDBPort)}/?directConnection=true";

    IContainer Container => _container ?? throw new InvalidOperationException("The Chronicle container has not been started.");

    public async Task InitializeAsync()
    {
        _container = new ContainerBuilder(Image)
            .WithPortBinding(ChroniclePort, assignRandomHostPort: true)
            .WithPortBinding(HttpPort, assignRandomHostPort: true)
            .WithPortBinding(MongoDBPort, assignRandomHostPort: true)
            .WithLabel("cratis.ante.integration", "true")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilInternalTcpPortIsAvailable(MongoDBPort)
                .AddCustomWaitStrategy(new ChronicleHealthWait(ChroniclePort, HttpPort)))
            .Build();

        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
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
