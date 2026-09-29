// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Ante.Invitations.Issuing;

/// <summary>
/// Decides whether a token without issuer and audience may still be accepted.
/// </summary>
public interface IInvitationTokenUpgradeWindow
{
    /// <summary>
    /// Determines whether a token without issuer and audience, issued at the given time, may be accepted now.
    /// </summary>
    /// <param name="issuedAt">When the token was issued.</param>
    /// <param name="now">The current time.</param>
    /// <returns>True while the upgrade window is open and the token was issued no later than activation plus the rollout grace.</returns>
    bool AcceptsLegacyToken(DateTimeOffset issuedAt, DateTimeOffset now);

    /// <summary>
    /// Determines whether a token without issuer and audience, issued and expiring at the given times, may be accepted now.
    /// </summary>
    /// <param name="issuedAt">When the token was issued.</param>
    /// <param name="expiresAt">When the token expires.</param>
    /// <param name="now">The current time.</param>
    /// <returns>True while the upgrade window is open and the token's issue time and lifetime are within its bounds.</returns>
    /// <remarks>
    /// The default ignores the lifetime, so an implementation written against the two-argument overload keeps working.
    /// </remarks>
    bool AcceptsLegacyToken(DateTimeOffset issuedAt, DateTimeOffset expiresAt, DateTimeOffset now) => AcceptsLegacyToken(issuedAt, now);
}

/// <summary>
/// When this deployment first ran with per-deployment token isolation, and when it stops accepting older tokens.
/// </summary>
/// <remarks>
/// Tokens issued before isolation carry no issuer or audience. They keep working - if signed by a trusted key,
/// issued no later than activation plus <see cref="InvitationTokenUpgradeWindow.RolloutGrace"/> and no longer-lived
/// than the recorded window - until <see cref="LegacyUntil"/>, so an upgrade needs no draining. The window then
/// closes by itself.
/// </remarks>
/// <param name="Id">The fixed document id.</param>
/// <param name="ActivatedAt">When isolation was first activated.</param>
public record InvitationTokenIsolationActivation([property: BsonId] string Id, DateTimeOffset ActivatedAt)
{
    /// <summary>
    /// The fixed document id.
    /// </summary>
    public const string Singleton = "invitation-token-isolation";

    /// <summary>
    /// Gets when the window closes, recorded once at activation. Absent on documents written by 1.0.0, where
    /// it is the activation plus the configured token expiry.
    /// </summary>
    public DateTimeOffset? LegacyUntil { get; init; }
}

/// <summary>
/// The upgrade window, recorded durably in MongoDB the first time this deployment runs with isolation.
/// </summary>
/// <param name="activatedAt">When isolation was first activated.</param>
/// <param name="legacyUntil">When the window closes.</param>
/// <param name="maximumLifetime">The longest lifetime (expiry minus issue time) a token accepted through the window can have.</param>
public class InvitationTokenUpgradeWindow(DateTimeOffset activatedAt, DateTimeOffset legacyUntil, TimeSpan maximumLifetime) : IInvitationTokenUpgradeWindow
{
    /// <summary>
    /// How long after activation a token without issuer and audience may still have been issued. Replicas that
    /// have not yet been upgraded keep issuing such tokens while a rolling upgrade proceeds.
    /// </summary>
    public static readonly TimeSpan RolloutGrace = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Gets a window that is already closed: no token without issuer and audience is accepted.
    /// </summary>
    public static readonly InvitationTokenUpgradeWindow Closed = new(DateTimeOffset.MinValue, TimeSpan.Zero);

    /// <summary>
    /// Initializes a new instance of the <see cref="InvitationTokenUpgradeWindow"/> class, open until activation plus the maximum lifetime.
    /// </summary>
    /// <param name="activatedAt">When isolation was first activated.</param>
    /// <param name="maximumLifetime">The longest lifetime a token issued before activation can have.</param>
    public InvitationTokenUpgradeWindow(DateTimeOffset activatedAt, TimeSpan maximumLifetime)
        : this(activatedAt, activatedAt + maximumLifetime, maximumLifetime)
    {
    }

    /// <summary>
    /// Reads the recorded activation, recording now (and the end of the window) when this is the first run with isolation.
    /// </summary>
    /// <param name="database">Ante's MongoDB database.</param>
    /// <param name="now">The current time.</param>
    /// <param name="maximumLifetime">The longest lifetime a token issued before activation can have.</param>
    /// <returns>The upgrade window.</returns>
    /// <remarks>
    /// A document without a recorded end (written by 1.0.0) closes the window at activation plus <paramref name="maximumLifetime"/>.
    /// A recorded end bounds the token lifetime by the recorded window length, so a later change of the configured
    /// expiry neither extends nor shortens what an earlier activation allowed.
    /// </remarks>
    public static async Task<InvitationTokenUpgradeWindow> Load(IMongoDatabase database, DateTimeOffset now, TimeSpan maximumLifetime)
    {
        var collection = database.GetCollection<InvitationTokenIsolationActivation>("invitation-token-isolation");
        var activation = await collection.FindOneAndUpdateAsync(
            Builders<InvitationTokenIsolationActivation>.Filter.Eq(document => document.Id, InvitationTokenIsolationActivation.Singleton),
            Builders<InvitationTokenIsolationActivation>.Update.Combine(
                Builders<InvitationTokenIsolationActivation>.Update.SetOnInsert(document => document.ActivatedAt, now),
                Builders<InvitationTokenIsolationActivation>.Update.SetOnInsert(document => document.LegacyUntil, now + maximumLifetime)),
            new FindOneAndUpdateOptions<InvitationTokenIsolationActivation> { IsUpsert = true, ReturnDocument = ReturnDocument.After });
        var legacyUntil = activation.LegacyUntil ?? (activation.ActivatedAt + maximumLifetime);
        return new(activation.ActivatedAt, legacyUntil, legacyUntil - activation.ActivatedAt);
    }

    /// <inheritdoc/>
    public bool AcceptsLegacyToken(DateTimeOffset issuedAt, DateTimeOffset now) =>
        issuedAt <= (activatedAt + RolloutGrace) && now < legacyUntil;

    /// <inheritdoc/>
    public bool AcceptsLegacyToken(DateTimeOffset issuedAt, DateTimeOffset expiresAt, DateTimeOffset now) =>
        AcceptsLegacyToken(issuedAt, now) && expiresAt > issuedAt && (expiresAt - issuedAt) <= maximumLifetime;
}
