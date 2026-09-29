// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Outbox;

/// <summary>
/// How far an onboarding flow's facts have got, judged from the authoritative local event log and outbox
/// rather than from the projections that lag them.
/// </summary>
public enum PublicationProgress
{
    /// <summary>
    /// Nothing was recorded for the flow: there is nothing to resume, so a client may safely (re)submit.
    /// </summary>
    None,

    /// <summary>
    /// The acceptance is recorded locally but it, or a legal fact recorded with it, has not reached the outbox.
    /// </summary>
    Recorded,

    /// <summary>
    /// The acceptance, and every legal fact recorded with it, has reached the outbox.
    /// </summary>
    Published,
}
