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

Chronicle reactors are **at-least-once**: a partition that pauses mid-delivery and recovers redelivers the event as an ordinary observation, not a replay. Whatever a host's own reactor does when it observes Ante's outbox — provisioning a tenant, creating a user — must be safe to run more than once for the same invitation id.

## The identity backchannel

`Ante:IdentityBackchannelUrl` is an optional pre-flight check Ante calls before accepting a join-tenant invitation, to catch "this login already has a user in this organization" before the invitee fills in the whole form. When set, Ante issues:

```
GET {url}/in-use?organization={tenantName}&subject={identityProviderSubject}
```

expecting `{ "isInUse": bool }` in response. Left empty, the check is skipped. It is also **fail-open**: any exception (unreachable host, timeout, malformed response) is logged and treated as "not in use" — it never blocks onboarding. This is a pre-flight convenience only; the host's own uniqueness constraint at the point it actually associates the identity with a user is the authoritative guard.

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

## Next steps

- [Invitation Lifecycle](./invitation-lifecycle.md) — the full flow these events belong to.
- [Configuration](./configuration.md) — the backchannel URL and every other setting.
