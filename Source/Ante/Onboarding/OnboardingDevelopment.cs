// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Ante.Onboarding;

/// <summary>
/// Development mode for <c language="csharp">cratis run</c>: a signing key generated per data volume, loopback HTTP and a single
/// host store, so a Screenplay application can be onboarded locally with no secrets and no deployment fixture.
/// </summary>
public static class OnboardingDevelopment
{
    /// <summary>The folder, relative to the content root, that holds the generated key when no data path is given.</summary>
    public const string DefaultFolder = ".ante-development";

    const string KeyFile = "invitation-signing-key.pem";

    static readonly string[] _productionNames = ["production", "prod", "live", "staging", "stage", "preprod"];

    /// <summary>
    /// Refuses development mode when the environment looks like production: a production-like environment name,
    /// a Kubernetes pod, or a public (non-loopback) base URL.
    /// </summary>
    /// <param name="options">The contract.</param>
    /// <param name="environment">The hosting environment.</param>
    /// <exception cref="OnboardingConfigurationInvalid">The environment looks like production.</exception>
    public static void Refuse(OnboardingOptions options, IHostEnvironment environment)
    {
        var looksLikeProduction = _productionNames.Contains(environment.EnvironmentName, StringComparer.OrdinalIgnoreCase) ||
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST")) ||
            IsPublic(options.PublicBaseUrl) ||
            IsPublic(options.ApplicationUrl);
        if (looksLikeProduction)
        {
            throw new OnboardingConfigurationInvalid(
                "Ante:Onboarding:Development:Enabled is refused: the environment looks like production " +
                "(production-like environment name, Kubernetes, or a public URL). Provide a signing key through Ante:Onboarding:Secrets instead.");
        }
    }

    /// <summary>
    /// Reads the development signing key from the data volume, generating and persisting it on first use.
    /// </summary>
    /// <param name="dataPath">The folder that holds the key; it should be ignored by Git.</param>
    /// <returns>The PEM private key. Never log it.</returns>
    public static string LoadOrCreateSigningKey(string dataPath)
    {
        Directory.CreateDirectory(dataPath);
        var path = Path.Combine(dataPath, KeyFile);
        if (!File.Exists(path))
        {
            using var rsa = RSA.Create(2048);
            try
            {
                var streamOptions = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write };
                if (!OperatingSystem.IsWindows())
                {
                    streamOptions.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
                }

                using var stream = new FileStream(path, streamOptions);
                using var writer = new StreamWriter(stream);
                writer.Write(rsa.ExportRSAPrivateKeyPem());
            }
            catch (IOException) when (File.Exists(path))
            {
                // A concurrent start created it first; use theirs.
            }
        }

        return File.ReadAllText(path);
    }

    static bool IsPublic(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        // A tenant placeholder like http://{tenant}.localhost:8090/ is not a valid Uri host.
        var candidate = url.Replace("{tenant}", "tenant", StringComparison.Ordinal);
        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
        {
            return true;
        }

        return !(uri.IsLoopback || uri.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase));
    }
}
