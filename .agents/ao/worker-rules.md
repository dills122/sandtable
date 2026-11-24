# Worker Rules (Sandtable)

The Orchestrator app (`ao`) appends this file to a Codex worker's instructions at session start, on
top of this repository's own `AGENTS.md` and `CLAUDE.md`, which still load normally. This file only
states what changes or is added under an Orchestrator-dispatched worker session; it does not repeat
`AGENTS.md`/`CLAUDE.md` content.

## Architecture boundaries

Use `AGENTS.md`'s "Architecture Boundaries" section as the authoritative ownership map. Stay inside
the module your task was dispatched for; do not cross into another module's ownership to "fix it
while you're there."

## Serial lanes

If your task touches any of the following, assume it is a serial lane: coordinate with the
Orchestrator before starting, and expect your diff to get human review before any other worker's
task in the same lane proceeds.

- `src/Cna.Core` public contracts, commands, events, and snapshots.
- `src/Cna.Intelligence.Contracts/Protos/intelligence.proto`.
- `src/Cna.OrleansHost/Api/CampaignDurableAuthorityJournal*.cs`.

## Verify with the repo's real commands

Use `scripts/agent-worktree-setup.sh` once per fresh worktree, then verify with focused commands
during iteration rather than the full gate on every edit: `just test-suite <suite>`,
`just test-class <suite> <pattern>`, `just test-method <suite> <pattern>`, and `just boundary-check`
for authority/privacy-boundary changes. Run `just check` or `just check-fast` before requesting
review. Report selected and unrun coverage honestly, as `AGENTS.md` requires.

## Run the boundary-checker subagent before requesting review

Before requesting review, run the `boundary-checker` subagent (`.codex/agents/boundary-checker.toml`)
against your diff. Resolve what it flags, or escalate it per the next section; do not request review
with an unresolved boundary-checker finding.

## Escalate to the human, never resolve alone

Escalate to the human rather than deciding unilaterally whenever your change touches:

- A contract or protobuf definition (`src/Cna.Intelligence.Contracts/Protos/intelligence.proto` or
  any `src/Cna.Core` public command/event/snapshot shape).
- Fog-of-war or hidden-state boundaries (what is visible to the intelligence plane vs. the Umpire).
- A golden fixture or recorded deterministic-replay baseline.

## Never merge

Never merge your own pull request. Merging is a human decision after review.

## Writing for other readers

The compressed output style in `AGENTS.md`/`CLAUDE.md` applies to chat replies only. Write your PR
description and any brief or note the Orchestrator or a reviewer will read in plain, full sentences.
