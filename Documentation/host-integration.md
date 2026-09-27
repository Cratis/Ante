---
title: Integrate a host product
description: Connect host invitation events, token links, provisioning, and the required proxy to Ante.
---

A host owns the invitation decision, link delivery, identity configuration and provisioning. Ante handles token issuance and lobby acceptance. This guide assumes a Chronicle host store named `Direct`, the value this Ante build actually subscribes to. For another store name, change `InboxSourceStore.Name` and rebuild Ante; setting `Ante:InboxSourceStore` alone fails startup. See [Configuration](./configuration.md).

## Connect the event stores

1. Mint a GUID invitation id and append `UserInvitedToJoinTenant` or `UserInvitedToCreateTenant` to **your host store's outbox**, using that GUID as the event source id. Revocation appends `InvitationRevoked` under the same id. An ordinary host event-log append is not the cross-store outbox input Ante observes. The exact fields and namespaces are in [Contracts](./contracts.md).
2. Observe Ante's outbox for `InvitationTokenIssued`. Build your public lobby URL as `/invite/{token}` and distribute it using your own channel. Treat tokens as secrets; Ante does not send mail. Multiple token-issued events for an id are possible. Check that a token arrives before announcing the link as usable.
3. Consume `InvitationToJoinTenantAccepted`, `InvitationToCreateTenantAccepted`, `OrganizationRegistrationCompleted` and, if legal documents are configured, `LegalTermsAccepted`. Make provisioning idempotent by event source id and event kind. The acceptance is evidence from Ante; enforce membership, role, identity-association and tenant uniqueness in your host. `Accepted` in Ante means published to its outbox, not provisioned in your system.

Use the [`Cratis.Ante.Contracts` NuGet package](https://www.nuget.org/packages/Cratis.Ante.Contracts) for the public event types. CI packs it with the image's release version when publishing is approved. For an instance renamed away from store `Ante`, explicitly annotate your host observer with the actual store name: the package has an assembly-level `[EventStore("Ante")]`. If you mirror the contracts instead (as Direct currently does), match the event type names and JSON shapes exactly; a compiling mirror is not proof of wire compatibility or preservation of the package's `[PII]` annotations.

Ante resolves a fixed namespace (`Ante:Namespace`, default `Default`) across this instance's requests and subscriptions. Publish the host invitation outbox in the namespace its cross-store observer actually follows (Direct's integration uses host store `Direct`, namespace `Default`); inspect Chronicle observer registration for your deployment, especially if either namespace differs. The host observer of Ante's outbox must likewise follow Ante's configured store and namespace. The [Architecture](./architecture.md) page separates host log, host outbox and Ante outbox.

For an invited event, Ante copies the host event context's compliance `Subject` into the local receipt, and the pending invitation read model projects it as a `Guid`. On join, `SignedInIdentity.Resolve` can fall back to that subject if the request/session has none. Supply the host event with a valid compliance subject and verify projection and identity behavior in an integration test; this repository does not establish what a missing or non-GUID subject does across the cross-store boundary. `IdentityProviderSubject` in the published acceptance payload is a plain string so the host can use the signed-in subject as a user key rather than infer it from compliance metadata.

A source-checked **illustrative excerpt**, not a standalone compilable host program (the surrounding host store, typed concepts and registration are omitted), shows the append shape used by Ante's own `InvitationTokenIssuingReactor`:

```csharp
await eventStore.GetEventSequence(EventSequenceId.Outbox)
    .Append(invitationId.ToString(), new UserInvitedToJoinTenant(email, tenantName, roles));
```

Here `invitationId` is a `Guid`, `email` is `Email`, `tenantName` is `TenantName`, and `roles` is `IReadOnlyList<RoleName>`. Ensure the `eventStore` points to the host store, not Ante's. A complete host fixture with compiling setup and end-to-end tests is not included in this repository. Direct is one host implementation, but its current source does not prove a working invitation end to end; do not copy its id generation without checking the GUID requirement.

## Put a verifying proxy in front

Do not expose Ante directly. Validate the invitation at the proxy before forwarding its `jti` and `invite_type` claims; Ante independently verifies the bearer invitation token at exchange (RS256, lifetime, and configured issuer/audience) according to [Security and trust](./security.md). Then call `POST /_invite/exchange` after sign-in with `Authorization: Bearer <invite-token>` and this **camelCase** JSON body (values are illustrative; substitute the signed-in identity):

```json
{
  "subject": "signed-in-subject",
  "identityProvider": "issuer-or-provider-name",
  "providerKey": "canonical-provider-key",
  "issuer": "canonical-issuer"
}
```

`providerKey` and `issuer` are optional; `IdentityProviderResolver.ResolveFrom` tries `ProviderKey`, then `Issuer`, then `IdentityProvider`, and resolves a recognized configured provider name before retaining an unknown report. The proxy's invitation-claims forwarding must supply verified `jti` and `invite_type` to the principal for request-time invitation resolution. If configured to resolve a canonical federated identity, it can also forward `urn:cratis:identity:subject`, `urn:cratis:identity:provider-key`, and `urn:cratis:identity:issuer`; Ante prefers these when resolving subject and provider. The exchange session is a fallback when the invitation claims are not forwarded, but ownership requires the forwarded sign-in subject **and an unambiguous resolved provider** to match the stored session. A subject alone never proves ownership across providers. Keep the proxy's provider-key/issuer claims consistent across exchange and subsequent requests. Strip or overwrite all identity/claim inputs detailed in [Security and trust](./security.md). This repository specifies Ante's expectations, **not** a tested AuthProxy setup.

For self-service onboarding, direct users to `{public-lobby-url}/register` and authenticate them through the proxy without inventing an invitation token. Ensure trusted sign-in claims include a stable subject and provider for owner-verified registration status and a plausible email; otherwise Ante can publish an empty registration email. Ante records registration ownership in a local event, never the host outbox. Older registrations without that event appear unknown to the status query and cannot be resumed through status polling. See [Contracts](./contracts.md#http-surface) for exact endpoints.

## Add optional host reads

For join invitations, `Ante:IdentityBackchannelUrl` adds a GET to `/in-use?organization=...&subject=...` before acceptance. It is fail-open for network/timeouts/JSON errors, so your host must enforce uniqueness itself. `Ante:HostOutcomeUrl` adds a GET to `/outcome?attempt=...` after publication for invited journeys. Its status is informational and a failed/unknown response never reverses Ante's publication; self-service registration does not use it. Protect these endpoints and their query-string data at the network and logging boundaries. Neither adapter supplies its own application-level authentication credential. Their outage warnings log the exception, not the organization name or subject; protect access to exception logs and HTTP access logs separately. Their exact JSON shapes are in [Contracts](./contracts.md#http-surface).

If the host wants legal consent, register `ILegalDocumentSource` **inside the Ante process**, through a custom composition of `Program.cs`; registering it in a separate host's DI container cannot affect Ante. Stock startup installs `NoLegalDocumentSource` and shows no legal step. See [Customization](./customization.md).
