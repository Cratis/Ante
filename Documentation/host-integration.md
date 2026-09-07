---
title: Host integration
description: What a host product emits to and consumes from Ante, how correlation works, the identity backchannel SPI, and what the fronting authentication proxy must do.
---

Integrating with Ante means appending three event shapes to your own outbox, observing five from Ante's outbox, and standing in front of Ante with an authentication proxy that forwards the right claims. Ante never calls back into a host over HTTP except the one optional backchannel below.

## Emit: events the host appends to its own outbox

| Event | Payload | When |
|---|---|---|
| `UserInvitedToJoinTenant` | `Email`, `TenantName`, `Roles: IReadOnlyList<RoleName>` | An existing tenant invites a user |
| `UserInvitedToCreateTenant` | `Email`, `Roles: IReadOnlyList<RoleName>` | Someone is invited to create a new tenant |
| `InvitationRevoked` | _(no payload)_ | An invitation is revoked before acceptance |

All three are appended with the **invitation id as the Chronicle event source id** — an id the host mints. Ante reuses this same id as the correlation key for everything it appends about the invitation: the issued token's `jti`, and every event in the "consume" table below.

## Consume: events Ante appends to its own outbox

| Event | Payload | When |
|---|---|---|
| `InvitationTokenIssued` | `FlowType: InvitationFlowType`, `Token: string` | Ante has minted a signed JWT for an invitation it received — the host builds the link and sends it |
| `InvitationToJoinTenantAccepted` | `TenantName`, `IdentityProvider`, `IdentityProviderSubject: string`, `FirstName`, `MiddleName`, `LastName`, `Email`, `Roles` | An invitee accepted a join-tenant invitation |
| `InvitationToCreateTenantAccepted` | `TenantName`, `IdentityProvider`, `IdentityProviderSubject: string`, `FirstName`, `MiddleName`, `LastName`, `Email`, `Roles` | An invitee set up a new tenant from an invitation |
| `OrganizationRegistrationCompleted` | `TenantName`, `Subject: string`, `IdentityProvider`, `FirstName`, `MiddleName`, `LastName`, `Email` | Self-service registration completed with no invitation |
| `LegalTermsAccepted` | `TenantName`, `IdentityProvider`, `IdentityProviderSubject: string`, `Version: LegalVersion` | **Only** appended when the host registered an `ILegalDocumentSource` — a host that did not never sees this event |

`IdentityProviderSubject` (and `Subject` on `OrganizationRegistrationCompleted`) is carried in the payload as a plain `string` rather than the compliance subject, because the compliance subject itself does not propagate to downstream observers — the host keys its own user registry off this value directly.

Provisioning — creating the tenant, creating the user, assigning roles — is entirely the host's responsibility. Ante's job ends the moment it appends the accepted/registered event.

## Correlation and delivery

Every event above an invitation flow touches shares the invitation id as its event source id, so a host observer can key its provisioning state off one value from the first `UserInvitedTo...` event through to the final accepted/registered event. Self-service registration has no prior invitation, so `RegisterOrganization` mints a client-generated id of the same shape instead — treat it exactly like an invitation id for correlation purposes.

The event a host observes in Ante's outbox is not a fresh copy assembled at forward time — the correlation id, occurrence time, and compliance subject on it are the exact values Ante recorded locally when the event was first appended. A host correlating Ante's outbox against its own logs, or replaying the same correlation id through its own systems, sees the same values Ante itself used, on every field.

Chronicle reactors are **at-least-once**: a partition that pauses mid-delivery and recovers redelivers the event as an ordinary observation, not a replay. Whatever a host's own reactor does when it observes Ante's outbox — provisioning a tenant, creating a user — must be safe to run more than once for the same invitation id. This holds regardless of *why* a redelivery happens; Ante does not attempt exactly-once delivery, and no `[OnceOnly]`-style reactor attribute changes that. What Ante does guarantee is that a fact is never silently dropped on the way to the outbox: each forward verifies its own append succeeded and throws when it did not, which is what makes Chronicle pause and retry the partition instead of moving on as if the fact had been published. The net effect for a host is unchanged from at-least-once with deduplication — a redelivery is still possible and must still be handled idempotently — but a fact that reached Ante's own event log is no longer at risk of never reaching the outbox at all because of a transient outbox failure.

## The identity backchannel

`Ante:IdentityBackchannelUrl` is an optional pre-flight check Ante calls before accepting a join-tenant invitation, to catch "this login already has a user in this organization" before the invitee fills in the whole form. When set, Ante issues:

```
GET {url}/in-use?organization={tenantName}&subject={identityProviderSubject}
```

expecting `{ "isInUse": bool }` in response. Left empty, the check is skipped. It is also **fail-open**: any exception (unreachable host, timeout, malformed response) is logged and treated as "not in use" — it never blocks onboarding. This is a pre-flight convenience only; the host's own uniqueness constraint at the point it actually associates the identity with a user is the authoritative guard. The warning logged on failure carries only the exception - never the organization name, subject, or query string - private diagnostics never include onboarding-specific facts.

## The host outcome backchannel

`Ante:HostOutcomeUrl` is an optional, purely informational lookup: once an invitation-bound wizard (join, or invited organization creation) has published, Ante can ask a host what happened to that specific attempt afterward — for example, whether downstream provisioning succeeded or failed — and show it on the completion screen instead of redirecting immediately. When set, Ante issues:

```
GET {url}/outcome?attempt={attemptId}
```

expecting `{ "status": "pending" | "succeeded" | "failed", "reasonCode": "..." }` in response, where `reasonCode` is a stable, low-cardinality code (never free text). Left empty, no lookup happens and every wizard behaves exactly as it does without this setting: it redirects to `HostAppUrl` automatically the moment onboarding publishes, unchanged.

The lookup is **authenticated and attempt-bound**: it only ever runs for the verified owner of that exact attempt (the same `IsVerifiedOwnerOf` check every invitation-bound command validator uses), and a caller who is not the verified owner gets back the same "nothing to report" answer an unconfigured deployment or an unreachable host would — so the lookup never reveals whether an attempt exists to anyone but its own owner, and never leaks another actor's outcome. It is also **fail-safe**: any exception (unreachable host, timeout, malformed response) or an unrecognized `status` value degrades to the same "nothing to report" answer, is logged without any onboarding-specific value, and never blocks the wizard — a "Continue" action to the host is always available immediately regardless of what (if anything) the host has reported. A host-reported failure never rewrites Ante's own durable publication as failed, and never triggers a resubmission; Ante's own onboarding has already published by the time this lookup can even run.

**Self-service registration (`RegisterOrganization`/`RegistrationPage`) does not support this.** `IsVerifiedOwnerOf` verifies an *accepted-invitation session* — self-service registration has no invitation and therefore no such session for its client-generated registration id, so the check could never succeed for it. Extending ownership verification to a bare registration id is a trust-model decision, not a display one, and is out of scope here.

This deliberately does **not** implement the broader push-based host-outcome contract sketched in [Cratis/Ante#22](https://github.com/Cratis/Ante/issues/22) — host-appended success/failure events, terminal-result precedence across duplicate/contradictory/late results, quarantine, and replay reconciliation. That shape is proposed and gated on trust/protocol decisions in [Cratis/Ante#11](https://github.com/Cratis/Ante/issues/11), which remains open. This pull-based lookup reuses the already-shipped identity-backchannel pattern above instead, so it needs no new wire contract to agree on.

## What the fronting AuthProxy must do

Ante expects to sit behind an authentication proxy (typically [Cratis AuthProxy](https://github.com/Cratis/AuthProxy)) that:

1. **Forwards the invite token as a Bearer credential** to `POST /_invite/exchange` immediately after OIDC login completes, with a body of `{ subject, identityProvider, providerKey?, issuer? }`.
2. **Runs an invite-claims enricher** that forwards the `jti` and `invite_type` claims from the invite token onward onto the authenticated principal, so `InvitationIdentityProvider` can resolve identity details from the current request rather than only from the recorded exchange session.
3. **Optionally stamps canonical identity claims** — `urn:cratis:identity:provider-key`, `urn:cratis:identity:issuer`, and `urn:cratis:identity:subject` — once it is configured to resolve a canonical federated identity for the provider. `SignedInIdentity` prefers these over a plain `iss`/`ClaimTypes.NameIdentifier` read, because the OpenID Connect handler deletes the raw `iss` claim by default and every federated identity is otherwise named identically after the authentication type, not the actual provider.

## Known limitation: the inbox source store

`Source/Ante/Invitations/Receiving/InboxSourceStore.cs` names the Chronicle event store Ante's inbox reactor cross-subscribes to for the three "emit" events above:

```csharp
public static class InboxSourceStore
{
    public const string Name = "Direct";
}
```

This is a **compile-time literal, not a runtime configuration value**, and that is a known limitation rather than a deliberate design choice. Chronicle's `[EventStore]` attribute is the only mechanism for pointing an observer at a store other than its own, and it requires a compile-time constant argument — there is currently no fluent or configuration-driven equivalent in the Chronicle client API. Pointing a deployment at a host store other than `"Direct"` means changing this one constant and rebuilding. The gap is tracked upstream as [Cratis/Chronicle#3951](https://github.com/Cratis/Chronicle/issues/3951).

`Ante:InboxSourceStore` (bound to `AnteOptions.InboxSourceStore`) exists alongside this constant, but it is **not** a way to retarget the subscription — it defaults to the compiled value and `AnteRoutingValidator` throws at startup if a deployment sets it to anything else, so a value this build cannot honor is rejected loudly instead of silently accepted and ignored. See [Configuration](./configuration.md#known-limitation-the-inbox-source-store-is-not-configurable) for the operator-facing detail, including the mirror-image limitation a host integrator hits when writing their own reactor against `Cratis.Ante.Contracts` types for a renamed Ante instance.

## Safe local routing

`Ante:EventStore` and `Ante:Namespace` (see [Configuration](./configuration.md)) independently select the local store and the fixed namespace within it that this Ante instance runs against — every event Ante appends locally, and everything the four reactors that forward to Ante's own outbox (`OrganizationSetupOutbox`, `JoinTenantAcceptanceOutbox`, `OrganizationRegistrationOutbox`, `LegalTermsAcceptanceOutbox`) observe, stays within that one store/namespace pair, pinned with Chronicle's `[EventLog]` attribute precisely so a renamed store can never make one of them misroute onto a nonexistent inbox sequence instead. Ante is single-tenant per deployment - `Ante:Namespace` is fixed for the whole instance, not resolved per request - see [Boundaries](./boundaries.md#multi-tenancy-of-ante-itself).

## Next steps

- [Invitation Lifecycle](./invitation-lifecycle.md) — the full flow these events belong to.
- [Configuration](./configuration.md) — the backchannel URL and every other setting.
