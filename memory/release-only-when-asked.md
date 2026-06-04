---
name: release-only-when-asked
description: Never cut a release until the user explicitly asks
metadata:
  type: project
---

Do **not** cut a release / bump version / tag / run the deployment pipeline for RS232ools until the user **explicitly** asks. They are batching multiple features per release.

**Why:** User instruction 2026-06-04: "Do not release until I say however." Features are committed/pushed to `master` as they land, but releases are deliberately held back and rolled up.

**How to apply:** Keep implementing, testing, committing, pushing, and updating the README per [[readme-agent-preference]] — but only invoke the deployer/release pipeline when the user says "release". Releases go via the deployer agent + `v*` tag; last shipped was v1.2.0.
