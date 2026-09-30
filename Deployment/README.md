# Reference deployment

`kubernetes/ante.yaml` is a reference Kubernetes deployment of the `ghcr.io/cratis/ante` image: a `Deployment`, a `Service`, a `ConfigMap` with every setting a production host has to decide, and the `Secret` shape for the signing key. Copy it, replace every `<...>` placeholder, and apply it with `kubectl apply -f`. Hosts that deploy with Pulumi or Helm can translate it one-to-one: the environment variable names are the contract, the manifest format is not.

See [Deploy Ante](../Documentation/deployment.md) for the checks to run before routing traffic, and [Configuration](../Documentation/configuration.md) for each key.

## Checklist for the fronting proxy

Ante must only be reachable through a verifying proxy (for example Cratis AuthProxy). Configure it to:

1. Serve the lobby origin (for example `lobby.example.com`) and route every path to the `ante` service.
2. Require sign-in with the same identity providers listed in `IdentityProviders__Providers__*`.
3. Validate invitation tokens with Ante's **public** key, issuer and audience (`Ante__Invitations__Token__PublicKeyPem`, `__Issuer`, `__Audience`) before the exchange, and call `http://ante/_invite/exchange` (Legacy) or `/_invite/stage` + `/_invite/exchange` (Attested).
4. Forward `jti`, `invite_type` and the canonical identity claims, and strip any identity headers sent by the browser.
5. Allow `/register` for signed-in users without an invitation when self-service registration is enabled, and keep the query string across the external sign-in redirect.

## Compatibility

| Ante | Chronicle client | Chronicle server verified | Contracts package |
| --- | --- | --- | --- |
| 1.0.x | 19.13.1 | 19.13.1, 19.4.7 | `Cratis.Ante.Contracts` 1.0.x |
| main (unreleased) | 19.23.0 | 19.23.0 or later (older servers connect but lack event-log-only constraints, so a released organization name cannot be registered again, plus the recorded-time inbox copies and generation-aware auto-map) | `Cratis.Ante.Contracts` from main |
