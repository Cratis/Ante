// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Invitations.Accepting;

namespace Ante.Invitations.HostOutcome;

/// <summary>
/// The outcome a host reports for a specific onboarding attempt, as visible to the attempt's verified
/// owner.
/// </summary>
public enum HostOutcomeStatus
{
    /// <summary>
    /// No host outcome is visible for this attempt. This is the safe default whenever the adapter is not
    /// configured, the caller is not a verified owner of the attempt, or the host could not be reached or
    /// returned something Ante does not recognize. These are deliberately indistinguishable to the
    /// caller: an unauthorized or failed lookup must never reveal whether an attempt exists, and an
    /// unconfigured deployment must behave exactly as one that has nothing to report.
    /// </summary>
    Unknown,

    /// <summary>
    /// The host has not yet reported a terminal result for this attempt.
    /// </summary>
    Pending,

    /// <summary>
    /// The host reported that downstream provisioning for this attempt succeeded.
    /// </summary>
    Succeeded,

    /// <summary>
    /// The host reported that downstream provisioning for this attempt failed. This never rewrites
    /// Ante's own durable publication - by the time a host outcome can even be requested, Ante's own
    /// onboarding facts already published successfully.
    /// </summary>
    Failed,
}

/// <summary>
/// Defines the backchannel Ante uses to ask a host what happened to a specific onboarding attempt after
/// Ante's own publication.
/// </summary>
/// <remarks>
/// This is a purely informational, opt-in extension - a host is never required to implement it, and
/// nothing about Ante's own onboarding completion depends on it. It intentionally does not attempt the
/// broader push-based host-outcome contract (host-appended success/failure events, terminal-result
/// precedence, quarantine, replay reconciliation) sketched in
/// <see href="https://github.com/Cratis/Ante/issues/22">Cratis/Ante#22</see> - that shape is proposed and
/// API-verification-gated on trust/protocol decisions
/// (<see href="https://github.com/Cratis/Ante/issues/11">Cratis/Ante#11</see>) that remain open. This
/// pull-based lookup mirrors the already-shipped
/// <see cref="Ante.Invitations.UserSetup.IIdentityBackchannel"/> pattern instead, so it needs no new wire
/// contract to agree on.
/// </remarks>
public interface IHostOutcomeBackchannel
{
    /// <summary>
    /// Asks the configured host backchannel for the outcome of a specific attempt.
    /// </summary>
    /// <param name="attemptId">The invitation or registration identifier the attempt is bound to.</param>
    /// <returns>
    /// The reported status and its reason code, or <see cref="HostOutcomeStatus.Unknown"/> with an empty
    /// reason code when the backchannel is unreachable, returns a malformed response, or reports a status
    /// Ante does not recognize.
    /// </returns>
    Task<(HostOutcomeStatus Status, string ReasonCode)> GetOutcome(InvitationId attemptId);
}

/// <summary>
/// The answer a host's outcome backchannel gives for one attempt.
/// </summary>
/// <param name="Status">One of <c>"pending"</c>, <c>"succeeded"</c>, or <c>"failed"</c>.</param>
/// <param name="ReasonCode">
/// A stable, low-cardinality reason code accompanying a terminal result - never free text or an internal
/// exception message, the same private-diagnostics discipline every other Ante-facing status uses.
/// </param>
public record HostOutcomeAnswer(string Status, string? ReasonCode);

/// <summary>
/// Asks the configured host outcome backchannel over HTTP what happened to a specific onboarding attempt.
/// </summary>
/// <remarks>
/// Mirrors <see cref="Ante.Invitations.UserSetup.IdentityBackchannel"/>'s fail-safe shape deliberately: an
/// unconfigured or unreachable host must never block or fail onboarding, because the host outcome is
/// informational only - Ante's own publication is already the definition of success by the time this is
/// ever queried.
/// </remarks>
/// <param name="httpClient">The HTTP client used to reach the host.</param>
/// <param name="options">The Ante options carrying the configured host outcome URL.</param>
/// <param name="logger">The logger.</param>
public class HostOutcomeBackchannel(
    HttpClient httpClient,
    IOptions<AnteOptions> options,
    ILogger<HostOutcomeBackchannel> logger) : IHostOutcomeBackchannel
{
    /// <inheritdoc/>
    public async Task<(HostOutcomeStatus Status, string ReasonCode)> GetOutcome(InvitationId attemptId)
    {
        var backchannelUrl = options.Value.HostOutcomeUrl;

        if (string.IsNullOrWhiteSpace(backchannelUrl) || attemptId == InvitationId.NotSet)
        {
            return (HostOutcomeStatus.Unknown, string.Empty);
        }

        var url = $"{backchannelUrl.TrimEnd('/')}/outcome?attempt={Uri.EscapeDataString(attemptId.Value.ToString())}";

        try
        {
            var answer = await httpClient.GetFromJsonAsync<HostOutcomeAnswer>(url);
            return ParseAnswer(answer);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or NotSupportedException or System.Text.Json.JsonException)
        {
            logger.LogHostOutcomeBackchannelUnavailable(ex);
            return (HostOutcomeStatus.Unknown, string.Empty);
        }
    }

    /// <summary>
    /// Interprets a raw backchannel answer as a <see cref="HostOutcomeStatus"/> - kept as a pure,
    /// side-effect-free mapping so the "degrade gracefully on anything unexpected" behavior can be
    /// exercised directly, without an HTTP round trip.
    /// </summary>
    /// <param name="answer">The raw answer, or <see langword="null"/> when the response body was empty.</param>
    /// <returns>The interpreted status and reason code.</returns>
    internal static (HostOutcomeStatus Status, string ReasonCode) ParseAnswer(HostOutcomeAnswer? answer) => answer?.Status switch
    {
        "succeeded" => (HostOutcomeStatus.Succeeded, answer.ReasonCode ?? string.Empty),
        "failed" => (HostOutcomeStatus.Failed, answer.ReasonCode ?? string.Empty),
        "pending" => (HostOutcomeStatus.Pending, string.Empty),
        _ => (HostOutcomeStatus.Unknown, string.Empty),
    };
}

/// <summary>
/// Represents the host-reported outcome visible to the verified owner of one onboarding attempt.
/// </summary>
/// <param name="AttemptId">The invitation or registration identifier the outcome is bound to.</param>
/// <param name="IsConfigured">
/// Whether this Ante deployment has a host outcome backchannel configured at all - deployment-level
/// configuration, not attempt-specific, so exposing it carries no existence-leak risk the way
/// <see cref="Status"/> would.
/// </param>
/// <param name="Status">The outcome as currently known.</param>
/// <param name="ReasonCode">A stable, low-cardinality reason code accompanying a terminal result; empty otherwise.</param>
[ReadModel]
public record HostOutcomeView(InvitationId AttemptId, bool IsConfigured, HostOutcomeStatus Status, string ReasonCode)
{
    /// <summary>
    /// Gets the host-reported outcome for a specific onboarding attempt.
    /// </summary>
    /// <remarks>
    /// Authenticated and attempt-bound by construction: a caller who is not a verified owner of this exact
    /// attempt id - see <see cref="ISignedInIdentity.IsVerifiedOwnerOf"/> - gets
    /// <see cref="HostOutcomeStatus.Unknown"/>, the same value an unconfigured deployment or an
    /// unreachable host produces. This query therefore never reveals whether an attempt exists to anyone
    /// but its own verified owner, and never leaks another actor's outcome. It never appends or mutates
    /// anything - a host outcome is display-only and can never affect Ante's own durable progress.
    /// </remarks>
    /// <param name="attemptId">The invitation or registration identifier to look up.</param>
    /// <param name="signedInIdentity">The identity the current request is signed in with.</param>
    /// <param name="options">The Ante options carrying whether the host outcome adapter is configured.</param>
    /// <param name="backchannel">The backchannel used to ask the host for the outcome.</param>
    /// <returns>The host-reported outcome, safely defaulted when it is not visible to the caller.</returns>
    public static async Task<HostOutcomeView> ForAttempt(
        InvitationId attemptId,
        ISignedInIdentity signedInIdentity,
        IOptions<AnteOptions> options,
        IHostOutcomeBackchannel backchannel)
    {
        var isConfigured = !string.IsNullOrWhiteSpace(options.Value.HostOutcomeUrl);

        if (!isConfigured || !signedInIdentity.IsVerifiedOwnerOf(attemptId))
        {
            return new(attemptId, isConfigured, HostOutcomeStatus.Unknown, string.Empty);
        }

        var (status, reasonCode) = await backchannel.GetOutcome(attemptId);
        return new(attemptId, isConfigured, status, reasonCode);
    }
}
