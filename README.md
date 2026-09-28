# Ante

[![Build](https://github.com/Cratis/Ante/actions/workflows/build.yml/badge.svg)](https://github.com/Cratis/Ante/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/Cratis.Ante.Contracts?logo=nuget)](https://www.nuget.org/packages/Cratis.Ante.Contracts)

Ante is Cratis's invitation and onboarding lobby: it signs tokens for host-issued invitations, records invitee acceptance and self-service registration, and publishes facts for a host to act on. The host owns link delivery, the authenticating proxy, and provisioning. Ante also supports two optional HTTP reads from the host.

**Status:** The backend and React lobby have source-level specifications, but this repository does not include a verified standalone host, Chronicle, MongoDB and proxy fixture for a first-invitation walkthrough. Read [Security and trust](https://github.com/Cratis/Ante/blob/main/Documentation/security.md) before deployment.

Start with [Documentation](https://github.com/Cratis/Ante/blob/main/Documentation/index.md) for host integration, operations, architecture and configuration. Configure incoming host outbox stores with `Ante:HostStores` (defaults to `["Direct"]`); changing the list requires a restart, not a rebuild. [Local development](https://github.com/Cratis/Ante/blob/main/Documentation/local-development.md) lists the full CI checks.

To build the .NET solution with the .NET 10 SDK:

```bash
dotnet build Ante.slnx --configuration Debug
```

Ante is licensed under the [MIT License](https://github.com/Cratis/Ante/blob/main/LICENSE).
