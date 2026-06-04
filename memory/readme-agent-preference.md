---
name: readme-agent-preference
description: README upkeep — use the bdh-utils-readme agent, and update it proactively whenever a feature is added
metadata:
  type: feedback
---

README maintenance for this project (and bdh-utils utilities generally):

1. **Always use the `bdh-utils-readme` subagent** for README work — not the generic `claude`/general-purpose agent. The user explicitly corrected a generic-agent attempt and insisted on the dedicated agent.
2. **Update the README proactively, in the same session, as soon as any new feature is added** — do not wait to be asked. (User instruction, 2026-06-04: "Add to the readme, and do it as soon as new features are added from this point forward.")

**Why:** The dedicated agent inspects the app source and appends the standard bdh-utils boilerplate; the user wants docs to never drift behind the code.

**How to apply:** After implementing+committing a feature, invoke the bdh-utils-readme agent to fold it into README.md, then commit the README yourself (the agent only has Read/Glob/Grep/Write — it can't commit). Releases are a separate, gated step — see [[release-only-when-asked]].
