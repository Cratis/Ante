---
title: Ante
description: Choose a path for integrating a host product with Ante or operating an Ante lobby.
---

Ante signs invitation tokens and runs a lobby for joining an existing tenant, creating one by invitation, or registering without an invitation. A host product supplies invitation events, handles link delivery and provisioning, and receives Ante's published outcomes. Ante also makes two optional HTTP reads back to the host.

## Integrate a host product

Start with [Host integration](./host-integration.md) to connect Chronicle event stores, handle the token and published facts, and establish the required authentication proxy boundary. Use [Contracts](./contracts.md) for event fields and HTTP shapes, and [Security and trust](./security.md) before exposing the lobby. The current inbox subscription is compiled for a host store named `Direct`; another host store requires an Ante rebuild.

## Run an instance

Use [Deployment](./deployment.md) for the prerequisites and safe network boundary, [Configuration](./configuration.md) for exact settings, and [Diagnose onboarding](./operations.md) when publication or token issuance stalls. For source-level checks, see [Local development](./local-development.md).

There is no verified, standalone host + Chronicle + MongoDB + validating-proxy fixture in this repository. These pages therefore do **not** promise a runnable first-invitation tutorial. A green health check alone cannot demonstrate a successful invitation or provisioning.

## Understand the boundary

[Architecture](./architecture.md) separates Ante's log, inbox, outbox and MongoDB state. [Invitation lifecycle](./invitation-lifecycle.md) explains recorded versus published; [Lobby journeys](./wizards.md) explains the three journeys. [Ownership and limits](./boundaries.md) names the work left to the host.
