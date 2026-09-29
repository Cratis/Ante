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
    /// <returns>True while the upgrade window is open and the token predates activation.</returns>
    bool AcceptsLegacyToken(DateTimeOffset issuedAt, DateTimeOffset now);
}

/// <summary>
/// When this deployment first ran with per-deployment token isolation.
/// </summary>
/// <remarks>
/// Tokens issued before isolation carry no issuer or audience. They keep working - if signed by a trusted key
/// and issued before activation - until activation plus the longest token lifetime, so an upgrade needs no
/// draining. The window then closes by itself.
/// </remarks>
/// <param name="Id">The fixed document id.</param>
/// <param name="ActivatedAt">When isolation was first activated.</param>
public record InvitationTokenIsolationActivation([property: BsonId] string Id, DateTimeOffset ActivatedAt)
{
    /// <summary>
    /// The fixed document id.
    /// </summary>
    public const string Singleton = "invitation-token-isolation";
}

/// <summary>
/// The upgrade window, recorded durably in MongoDB the first time this deployment runs with isolation.
/// </summary>
/// <param name="activatedAt">When isolation was first activated.</param>
/// <param name="maximumLifetime">The longest lifetime a token issued before activation can have.</param>
public class InvitationTokenUpgradeWindow(DateTimeOffset activatedAt, TimeSpan maximumLifetime) : IInvitationTokenUpgradeWindow
{
    /// <summary>
    /// Gets a window that is already closed: no token without issuer and audience is accepted.
    /// </summary>
    public static readonly InvitationTokenUpgradeWindow Closed = new(DateTimeOffset.MinValue, TimeSpan.Zero);

    /// <summary>
    /// Reads the recorded activation, recording now as the activation when this is the first run with isolation.
    /// </summary>
    /// <param name="database">Ante's MongoDB database.</param>
    /// <param name="now">The current time.</param>
    /// <param name="maximumLifetime">The longest lifetime a token issued before activation can have.</param>
    /// <returns>The upgrade window.</returns>
    public static async Task<InvitationTokenUpgradeWindow> Load(IMongoDatabase database, DateTimeOffset now, TimeSpan maximumLifetime)
    {
        var collection = database.GetCollection<InvitationTokenIsolationActivation>("invitation-token-isolation");
        var activation = await collection.FindOneAndUpdateAsync(
            Builders<InvitationTokenIsolationActivation>.Filter.Eq(document => document.Id, InvitationTokenIsolationActivation.Singleton),
            Builders<InvitationTokenIsolationActivation>.Update.SetOnInsert(document => document.ActivatedAt, now),
            new FindOneAndUpdateOptions<InvitationTokenIsolationActivation> { IsUpsert = true, ReturnDocument = ReturnDocument.After });
        return new(activation.ActivatedAt, maximumLifetime);
    }

    /// <inheritdoc/>
    public bool AcceptsLegacyToken(DateTimeOffset issuedAt, DateTimeOffset now) =>
        issuedAt < activatedAt && now < activatedAt + maximumLifetime;
}
