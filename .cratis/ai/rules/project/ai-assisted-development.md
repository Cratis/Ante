---
applyTo: "**/*"
---

## AI-assisted development

This repository uses the managed Cratis AI corpus.

- `.cratis/ai.json` selects `cratis/application/csharp`, `cratis/application/react`, `cratis/documentation` and `cratis/review`. Ante is an application built on Cratis, so it uses the application profiles; the `cratis/engineering/*` profiles are for Cratis framework repositories and are mutually exclusive with them.
- `.cratis/ai/rules/project.md` and the files in this directory are project-owned guidance shared by every configured harness.
- `.cratis/ai.manifest.json` records only Cratis-managed files and integrations; project rules and custom skills remain user-owned.
- Run `cratis ai status` before updates and review conflicts before using `--force`.
