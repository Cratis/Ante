---
applyTo: "**/*"
---

## Commands

```bash
dotnet build
dotnet test
```

CI runs the build workflow; publishing is label-driven. The lobby and
invitation flows are event-sourced — model changes start from the event types.
