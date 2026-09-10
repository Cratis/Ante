// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Invitations.Receiving;

/// <summary>
/// Names the Chronicle event store Ante's inbox cross-subscribes to for invitation events a host
/// product appends to its own outbox.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is a compile-time literal, not a runtime configuration value, and that is a known
/// limitation - not a design choice.</b> Chronicle's <c language="csharp">[EventStore]</c> attribute, the only mechanism
/// for pointing an observer at a store other than its own, takes a C# attribute-constructor argument,
/// which the language requires to be a compile-time constant. There is currently no fluent/runtime
/// equivalent in the Cratis.Chronicle client API that would let this name come from
/// <c language="csharp">IConfiguration</c> the way <c language="csharp">Ante:EventStore</c> does for Ante's own store (see the
/// <c language="csharp">AddCratis</c> call in <c language="csharp">Program.cs</c>). Investigated for this
/// extraction: the Chronicle 16.44.1 / 17.0.0 XML documentation for
/// <c language="csharp">Cratis.Chronicle.Events.EventStoreAttribute</c> confirms the attribute is the sole mechanism - "when an
/// observer (Reactor, Reducer, or Projection) handles event types annotated with this attribute, it will
/// automatically subscribe to the inbox event sequence for the specified event store" - with no
/// programmatic alternative documented or discoverable in the shipped API surface.
/// </para>
/// <para>
/// Until Chronicle supports a runtime-registered cross-store observer, an Ante deployment that needs a
/// host store name other than <see cref="Name"/> has to change this constant and rebuild. The value is
/// isolated to this one file, named after the reference "Direct" host instance the Ante README
/// documents, precisely so that edit is the only thing that has to happen - no other file in this
/// project names a source store. An upstream issue describing the missing capability belongs on
/// Cratis/Chronicle; this comment is the workaround note <c language="csharp">upstream-issues.md</c> asks for, kept next
/// to the one place the workaround lives.
/// </para>
/// <para>
/// <see cref="AnteOptions.InboxSourceStore"/> exists precisely so a deployment cannot silently ask for a
/// different value here and have it ignored: <see cref="AnteRoutingValidator"/> compares the configured
/// value against <see cref="Name"/> at startup and throws <see cref="InboxSourceStoreCannotBeReconfigured"/>
/// on a mismatch, rather than accepting an option this build cannot actually honor.
/// </para>
/// </remarks>
public static class InboxSourceStore
{
    /// <summary>
    /// The name of the host product's event store Ante's inbox reactor cross-subscribes to.
    /// </summary>
    public const string Name = "Direct";
}
