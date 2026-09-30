// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Integration.Routes.given;

/// <summary>
/// The replies to the same requests made anonymously and signed in, so one table row states what both visitors get.
/// </summary>
public sealed class Visits
{
    readonly Dictionary<(bool SignedIn, string Path), Reply> _replies = [];

    /// <summary>Gets the reply an anonymous visitor got.</summary>
    /// <param name="path">The requested path.</param>
    /// <returns>The reply.</returns>
    public Reply Anonymous(string path) => _replies[(false, path)];

    /// <summary>Gets the reply a signed-in user got.</summary>
    /// <param name="path">The requested path.</param>
    /// <returns>The reply.</returns>
    public Reply SignedIn(string path) => _replies[(true, path)];

    /// <summary>Records a reply.</summary>
    /// <param name="signedIn">Whether the request carried the forwarded identity.</param>
    /// <param name="path">The requested path.</param>
    /// <param name="reply">The reply.</param>
    public void Add(bool signedIn, string path, Reply reply) => _replies[(signedIn, path)] = reply;

    /// <summary>
    /// Lists the visits to the given paths whose reply is not what is expected; empty when every visitor got it.
    /// </summary>
    /// <param name="paths">The paths to check.</param>
    /// <param name="expected">What a correct reply looks like.</param>
    /// <returns>Each departure, naming the visitor, the path and the status it got.</returns>
    public IReadOnlyList<string> Failing(IEnumerable<string> paths, Func<Reply, bool> expected) =>
        [.. paths.SelectMany(path => new[] { false, true }
            .Where(signedIn => !expected(_replies[(signedIn, path)]))
            .Select(signedIn => $"{(signedIn ? "signed-in" : "anonymous")} {path} -> {(int)_replies[(signedIn, path)].Status}"))];

    /// <summary>
    /// Lists the visits whose reply is not what is expected, whichever path was requested.
    /// </summary>
    /// <param name="expected">What a correct reply looks like.</param>
    /// <returns>Each departure.</returns>
    public IReadOnlyList<string> Failing(Func<Reply, bool> expected) => Failing(_replies.Keys.Select(key => key.Path).Distinct(), expected);
}
