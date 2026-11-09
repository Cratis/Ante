// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Headers;

namespace Ante;

/// <summary>
/// Authenticates Ante's calls to the host backchannels with an optional per-backchannel bearer secret,
/// and checks that secret at startup.
/// </summary>
/// <remarks>
/// Opt-in: an empty credential sends no <c language="csharp">Authorization</c> header, exactly as before. The credential is
/// never logged, never part of an exception message and never placed in a URL.
/// </remarks>
public static class BackchannelCredentials
{
    /// <summary>
    /// The shortest credential accepted, in characters.
    /// </summary>
    public const int MinimumLength = 32;

    /// <summary>
    /// Creates a GET request for a backchannel URL, carrying the credential as a bearer token when one is configured.
    /// </summary>
    /// <param name="url">The request URL.</param>
    /// <param name="credential">The configured credential, or empty for none.</param>
    /// <returns>The request.</returns>
    public static HttpRequestMessage CreateRequest(string url, string credential)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (!string.IsNullOrEmpty(credential))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credential);
        }

        return request;
    }

    /// <summary>
    /// Stops a backchannel client from following redirects, so the credential is only ever sent to the configured URL.
    /// </summary>
    /// <param name="builder">The HTTP client builder of a backchannel that carries a credential.</param>
    /// <returns>The same builder.</returns>
    public static IHttpClientBuilder WithoutRedirects(this IHttpClientBuilder builder) =>
        builder.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false });

    /// <summary>
    /// Rejects a credential that is too short or not visible ASCII, or one that would be sent over plain HTTP outside Development.
    /// </summary>
    /// <param name="options">The deployment's options.</param>
    /// <param name="isDevelopment">Whether the application runs in the Development environment.</param>
    /// <exception cref="BackchannelCredentialInvalid">A credential is unusable.</exception>
    public static void Validate(AnteOptions options, bool isDevelopment)
    {
        ValidateOne(nameof(AnteOptions.IdentityBackchannelCredential), options.IdentityBackchannelCredential, options.IdentityBackchannelUrl, isDevelopment);
        ValidateOne(nameof(AnteOptions.HostOutcomeCredential), options.HostOutcomeCredential, options.HostOutcomeUrl, isDevelopment);
    }

    /// <summary>
    /// Warns about a configured backchannel that is called without a credential outside Development.
    /// </summary>
    /// <param name="options">The deployment's options.</param>
    /// <param name="isDevelopment">Whether the application runs in the Development environment.</param>
    /// <param name="logger">The startup logger.</param>
    public static void Report(AnteOptions options, bool isDevelopment, ILogger<AnteOptions> logger)
    {
        if (isDevelopment)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(options.IdentityBackchannelUrl) && string.IsNullOrEmpty(options.IdentityBackchannelCredential))
        {
            logger.LogBackchannelWithoutCredential(nameof(AnteOptions.IdentityBackchannelUrl), nameof(AnteOptions.IdentityBackchannelCredential));
        }

        if (!string.IsNullOrWhiteSpace(options.HostOutcomeUrl) && string.IsNullOrEmpty(options.HostOutcomeCredential))
        {
            logger.LogBackchannelWithoutCredential(nameof(AnteOptions.HostOutcomeUrl), nameof(AnteOptions.HostOutcomeCredential));
        }
    }

    static void ValidateOne(string name, string credential, string url, bool isDevelopment)
    {
        if (string.IsNullOrEmpty(credential))
        {
            return;
        }

        if (credential.Length < MinimumLength || string.IsNullOrWhiteSpace(credential))
        {
            throw new BackchannelCredentialInvalid($"Ante:{name} must be at least {MinimumLength} characters.");
        }

        if (!credential.All(character => character is >= '!' and <= '~'))
        {
            throw new BackchannelCredentialInvalid($"Ante:{name} must contain only visible ASCII characters, with no spaces, control characters or line breaks.");
        }

        if (string.IsNullOrWhiteSpace(url) || isDevelopment)
        {
            return;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new BackchannelCredentialInvalid($"Ante:{name} is configured, so its backchannel URL must be an absolute https URL outside Development.");
        }
    }
}

/// <summary>
/// The exception that is thrown when a host backchannel credential cannot be used safely.
/// </summary>
/// <param name="message">What is wrong. Never contains the credential.</param>
public class BackchannelCredentialInvalid(string message) : Exception(message);
