---
title: Security and trust
description: The invitation token, proxy, exchange session, ownership check, and revocation limits as implemented.
---

:::danger[Proxy verification is mandatory]
Ante's `POST /_invite/exchange` parses the JWT with `ReadJsonWebToken` and checks that `jti` is a GUID and `exp` has not passed. It does **not** verify the signature, issuer, or audience ([Ante #51](https://github.com/Cratis/Ante/issues/51)). Ante must only be reachable through a trusted proxy that verifies invitation tokens before forwarding the exchange and prevents untrusted clients from supplying identity or invitation claims. The repository does not establish that any deployment has this boundary correctly configured.
:::

## Token and identity boundaries

Ante issues RS256 JWTs with `jti` (the host-minted GUID invitation id), `invite_type` (`JoinTenant` or `CreateTenant`), issued time and expiry (seven days by default). Ante does not explicitly set `NotBefore` (`nbf`) on its token descriptor; if the JWT handler emits `nbf` by default, a verifier must honor it. Check an issued token against the deployed handler before relying on that claim. It writes `iss` and `aud` only if configured. Setting issuer or audience does **not** turn on exchange validation. Supply the matching public key to the external verifier, keep the private key secret, and configure the verifier's signature, algorithm, issuer, audience and expiry policy to match the tokens it receives. `PublicKeyPem` is not read by Ante's issuer or exchange.

After authentication the proxy is expected to POST the bearer invitation token and subject/provider details to the exchange. Ante stores the request body's `Subject` as sent, resolves the provider, and upserts a MongoDB session keyed by **subject plus provider**. Exchanging a second invitation for the same login replaces the first session. Exchange does not check that the invitation exists, is pending, or has not been revoked. Session expiry is copied from the token and checked when reading it; MongoDB's TTL index is only cleanup. The invitation-bound command validators call `IsVerifiedOwnerOf`: a matching `jti` on the forwarded principal is accepted directly, or a request subject can match a live session for that invitation. Neither path independently validates the JWT cryptography.

Arc's Microsoft identity platform handler decodes the request principal from the `x-ms-client-principal` header, with `x-ms-client-principal-id` and `x-ms-client-principal-name` alongside it. The proxy must strip any client-supplied copy of all three headers and set them itself. Inside that principal, Ante trusts the claims `jti`, `invite_type`, `iss`, `urn:cratis:identity:subject`, `urn:cratis:identity:provider-key`, `urn:cratis:identity:issuer`, `NameIdentifier` and `sub`; the `IdentityProvider` Ante publishes comes from `iss` or the canonical provider claims, so a spoofed `iss` changes the identity key the host receives. It also trusts, via Arc's `MicrosoftIdentityPlatformHeaders.IdentityIdHeader` and, for registration email, `email`, `upn`, `preferred_username`, `Name` and `MicrosoftIdentityPlatformHeaders.IdentityNameHeader` (plus .NET `ClaimTypes.Email` and `ClaimTypes.Upn`). Do not let a browser set these as trusted identity. Self-service `RegisterOrganization` has no invitation-owner check and derives email from those sign-in values; absent a plausible email, the published field can be empty.

The browser decodes the unverified URL token to choose the wizard and to prefill the invitation id it submits and polls. Neither is verification: invitation commands are gated server-side by `IsVerifiedOwnerOf`. For the exact exchange request see [Contracts](./contracts.md#http-surface).

## Generated API exposure

No endpoint in `Source/Ante` is marked `[Authorize]`. Anyone who can reach Ante can reach its generated `/api` routes. In particular, `/api/invitations/receiving/all-pending-invitations-to-join` and `/api/invitations/receiving/all-pending-invitations-to-create-organization` return pending invitations across the instance, including invitee `Email` and `Roles` (and `TenantName` for join invitations); `status-for-invitation` queries do not check ownership either. Invitation-bound **command validators** check ownership, but this does not protect those queries. Restrict the whole `/api` surface at the proxy and review what each caller may access; simply routing every signed-in user to `/api` does not impose per-invitation authorization. Track the exposed-query work in [Ante #58](https://github.com/Cratis/Ante/issues/58).

## Revocation and rotation

A host revokes by publishing `InvitationRevoked` under the original event source id. Ante removes the pending projection; invitation commands then reject a missing pending invitation once it has caught up. Revocation does **not** invalidate the JWT or an existing MongoDB exchange session at exchange time. An already issued link may still be parsed by the frontend. Do not promise immediate token invalidation.

There is no built-in key rotation or multi-key verification scheme. Whether old tokens remain usable after rotation depends on whether the external verifier still trusts the old public key. Coordinate any transition with that verifier and the host; replacing its only trusted key may invalidate outstanding links immediately. The trust/protocol work is tracked in [Ante #11](https://github.com/Cratis/Ante/issues/11) and [verified ownership in #13](https://github.com/Cratis/Ante/issues/13).
