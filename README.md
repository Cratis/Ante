# Ante

> Ante (the initial stake in a game) is your SaaS invitation system: create signed invitations, manage the lobby where invitees register, and coordinate via events.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

## The problem

Getting people into a SaaS product starts before they have an account. Someone has to be invited, the invitation has to be trustworthy, and the moment an invitee actually registers has to connect back to the invitation that brought them in. Doing this ad hoc in every product means re-solving the same questions each time: how invitations are issued, how they are verified, and how the rest of the system learns that an invitee arrived.

Ante's scope is exactly that slice: **signed invitations**, a **lobby** where invitees register, and **event-based coordination** so the surrounding system can react to what happens.

## Status

Ante is at an early stage. **This repository does not yet contain the Ante source code.** What it holds today:

- The [MIT license](LICENSE) the project is published under.
- Development rules, skills, and prompts (under `.ai/`) for AI-assisted development on the Cratis stack.
- Repository automation workflows (under `.github/`).

There is nothing to install or run yet, so there is no quickstart. Everything in this README describes the project's intended scope, not shipped functionality. Watch the repository or join the [Cratis Discord](https://discord.gg/kt4AMpV8WV) to follow along.

## Intended scope

From the project's description:

- **Signed invitations** — invitations are created signed, so an invitation presented back to the system can be checked rather than taken on trust.
- **Lobby** — the place where invitees land and register, turning an invitation into an account.
- **Event coordination** — the invitation lifecycle is communicated through events, so other parts of a SaaS can react to invitations being created and invitees registering.

Concrete mechanics — signature scheme, storage, and APIs — are not defined here yet; they will be documented as the source code lands.

## Part of the Cratis ecosystem

Ante is a [Cratis](https://www.cratis.io) project. The repository is already scaffolded with the Cratis application-development rules — event sourcing with [Chronicle](https://github.com/Cratis/Chronicle), CQRS with [Arc](https://github.com/Cratis/Arc) for ASP.NET Core, and vertical slices — which is the stack Ante is intended to be built on. That also makes Ante relevant as a ready-made invitation building block for SaaS products built on the same stack.

## License

Ante is licensed under the [MIT License](LICENSE) and free to use.
