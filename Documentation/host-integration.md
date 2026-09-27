---
title: Integrate a host product
description: Connect host invitation events, token links, provisioning, and the required proxy to Ante.
---

A host owns the invitation decision, link delivery, identity configuration and provisioning. Ante handles token issuance and lobby acceptance. Set `Ante:HostStores` to the host's actual Chronicle source stores, for example `["Direct"]` or Studio's `["Studio", "StudioAdmin"]`. Ante defaults to `["Direct"]` if neither source setting is supplied. This no longer requires rebuilding Ante. See [Configuration](./configuration.md).

## Connect the event stores

1. Mint a nonempty GUID invitation id, serialize it as lowercase hyphenated `D` format, and append `UserInvitedToJoinTenant` or `UserInvitedToCreateTenant` to **your host store's outbox**, using that GUID as the event source id. **Use a fresh invitation id for every distinct invite, including after revocation or acceptance.** Ante rejects reuse after revocation or acceptance, but a different event while the id is still pending currently produces a second receipt and token; do not rely on Ante to prevent that pending reuse. Revocation appends `InvitationRevoked` under the same id. An ordinary host event-log append is not the cross-store outbox input Ante observes. The exact fields and namespaces are in [Contracts](./contracts.md).
2. Observe Ante's outbox for `InvitationTokenIssued` **and** `InvitationRejected`. For a rejection with `Reason = InvalidInvitationId`, correct the host's noncanonical event source id and issue a new invitation. For `Reason = InvitationIdReused`, mint a new id and issue a new invitation instead of resending under the old id. Neither rejection creates a new pending invitation or issues a token. Chronicle's outbox-to-inbox forwarder may copy the same host event again under a new inbox sequence number, which can produce `InvitationIdReused` after an id is closed; ignore rejections for ids already revoked or accepted. For a token, build your public lobby URL as `/invite/{token}` and distribute it using your own channel. Treat tokens as secrets; Ante does not send mail. Generation 2 carries `ExpiresAt`, the UTC JWT `exp` instant; generation-1 facts migrated for new consumers use `DateTimeOffset.UnixEpoch` because their historical expiry is unknown. Do not announce or send a link for a token whose expiry is unknown or past. Multiple token-issued events for an id are possible. Check that a token arrives before announcing the link as usable.
3. Consume `InvitationToJoinTenantAccepted`, `InvitationToCreateTenantAccepted`, `OrganizationRegistrationCompleted` and, if legal documents are configured, `LegalTermsAccepted`. Make provisioning idempotent by event source id and event kind. The acceptance is evidence from Ante; enforce membership, role, identity-association and tenant uniqueness in your host. `Accepted` in Ante means published to its outbox, not provisioned in your system.

**Host upgrade:** add `InvitationRejected` (generation 1) and its `InvitationIdReused` reason to the event types your host observes from Ante's outbox (and to mirrored contract definitions, if used). A host that observes only `InvitationTokenIssued` will not receive rejection events. Hosts still using a generation-1 token-issued mirror must tolerate the additional `expiresAt` JSON member in cross-store deliveries.

Use the [`Cratis.Ante.Contracts` NuGet package](https://www.nuget.org/packages/Cratis.Ante.Contracts) for the public event types. CI packs it with the image's release version when publishing is approved. The package still has an assembly-level `[EventStore("Ante")]`. A host can *already reference it* with any Ante store name: explicitly annotate its observer with `[EventStore("DirectLobby")]` (replace `DirectLobby` with the actual Ante store). That observer-level setting wins over the assembly annotation. Removing the assembly annotation is deferred to a coordinated breaking contracts release; existing observers may rely on the default. Do not duplicate discovered CLR event types with the same identities when replacing mirrors. If you keep mirrored contracts (as Direct currently does), compare event ids, generations, serialization and `[PII]` metadata; matching names alone do not establish compatibility.

Ante resolves one fixed namespace (`Ante:Namespace`, default `Default`) across all its requests and subscriptions. Chronicle forwards each host outbox into `inbox-{source}` in that *same namespace* on Ante's store; there is no namespace remapping. Publish and observe in a matching namespace, and test the full path before cutover. The host observer of Ante's outbox must likewise follow Ante's configured store and matching namespace. A multi-source list grants the same three inbound contract types to each configured store; it does **not** enforce flow-specific Core/Admin permissions. These stores must belong to one trusted host product. The three inbound contracts currently use generation 1; Chronicle's subscription builder emits generation-1 filters, so a future inbound event generation needs a coordinated adapter change. Studio's Core revocation `UserInvitationRevoked` differs from Ante's `InvitationRevoked` and needs producer-side adaptation. The [Architecture](./architecture.md) page separates each host outbox, Ante inbox and Ante outbox.

For an invited event, Ante copies the host event context's compliance `Subject` into the local receipt, and the pending invitation read model projects it as a `Guid`. On join, `SignedInIdentity.Resolve` can fall back to that subject if the request/session has none. Supply the host event with a valid compliance subject and verify projection and identity behavior in an integration test; this repository does not establish what a missing or non-GUID subject does across the cross-store boundary. `IdentityProviderSubject` in the published acceptance payload is a plain string so the host can use the signed-in subject as a user key rather than infer it from compliance metadata.

A source-checked **illustrative excerpt**, not a standalone compilable host program (the surrounding host store, typed concepts and registration are omitted), shows the append shape used by Ante's own `InvitationTokenIssuingReactor`:

```csharp
await eventStore.GetEventSequence(EventSequenceId.Outbox)
    .Append(invitationId.ToString(), new UserInvitedToJoinTenant(email, tenantName, roles));
```

Here `invitationId` is a `Guid`, `email` is `Email`, `tenantName` is `TenantName`, and `roles` is `IReadOnlyList<RoleName>`. Ensure the `eventStore` points to the host store, not Ante's. A complete host fixture with compiling setup and end-to-end tests is not included in this repository. Direct is one host implementation, but its current source does not prove a working invitation end to end; do not copy its id generation without checking the GUID requirement. For a host that needs runtime-configurable reverse routing rather than a compiled `[EventStore]` observer, use Chronicle's runtime reactor on `inbox-{configured Ante store}` and a single subscription from that store's outbox, filtering the union of events its host consumers need. Coordinate ownership of the source-name subscription id: separate consumers must not overwrite one another's filters. Neither adapter nor event-type filtering can route a token to only one of Studio's stores; each consumer must correlate the invitation id and ignore unrelated ids.

## Put a verifying proxy in front

Do not expose Ante directly. Validate the invitation at the proxy according to [Security and trust](./security.md), then call `POST /_invite/exchange` after sign-in with `Authorization: Bearer <invite-token>` and this **camelCase** JSON body (values are illustrative; substitute the signed-in identity):

```json
{
  "subject": "signed-in-subject",
  "identityProvider": "issuer-or-provider-name",
  "providerKey": "canonical-provider-key",
  "issuer": "canonical-issuer"
}
```

`providerKey` and `issuer` are optional; `IdentityProviderResolver.ResolveFrom` tries `ProviderKey`, then `Issuer`, then `IdentityProvider`, and resolves a recognized configured provider name before retaining an unknown report. The proxy's invitation-claims forwarding must supply verified `jti` and `invite_type` to the principal for request-time invitation resolution. If configured to resolve a canonical federated identity, it can also forward `urn:cratis:identity:subject`, `urn:cratis:identity:provider-key`, and `urn:cratis:identity:issuer`; Ante prefers these when resolving subject and provider. The exchange session is a fallback when the invitation claims are not forwarded. Strip or overwrite all identity/claim inputs detailed in [Security and trust](./security.md). This repository specifies Ante's expectations, **not** a tested AuthProxy setup.

For self-service onboarding, direct users to `{public-lobby-url}/register` and authenticate them through the proxy without inventing an invitation token. Ensure trusted sign-in claims include a plausible email; otherwise Ante can publish an empty registration email. See [Contracts](./contracts.md#http-surface) for exact endpoints.

## Add optional host reads

For join invitations, `Ante:IdentityBackchannelUrl` adds a GET to `/in-use?organization=...&subject=...` before acceptance. It is fail-open for network/timeouts/JSON errors, so your host must enforce uniqueness itself. `Ante:HostOutcomeUrl` adds a GET to `/outcome?attempt=...` after publication for invited journeys. Its status is informational and a failed/unknown response never reverses Ante's publication; self-service registration does not use it. Protect these endpoints and their query-string data at the network and logging boundaries. Neither adapter supplies its own application-level authentication credential. Their outage warnings log the exception, not the organization name or subject; protect access to exception logs and HTTP access logs separately. Their exact JSON shapes are in [Contracts](./contracts.md#http-surface).

If the host wants legal consent, register `ILegalDocumentSource` **inside the Ante process**, through a custom composition of `Program.cs`; registering it in a separate host's DI container cannot affect Ante. Stock startup installs `NoLegalDocumentSource` and shows no legal step. See [Customization](./customization.md).
