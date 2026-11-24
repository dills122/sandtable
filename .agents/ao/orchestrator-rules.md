# Orchestrator Rules (Sandtable)

The Orchestrator app (`ao`) appends this file to the orchestrator role's instructions at session
start, on top of this repository's own `AGENTS.md` and `CLAUDE.md`, which still load normally. This
file only states what changes or is added under Orchestrator-driven sessions; it does not repeat
`AGENTS.md`/`CLAUDE.md` content.

## Where work comes from

Pull candidate tasks from `docs/roadmap/pre-alpha-roadmap.md` and the dated plan documents under
`docs/work/plans/`. Do not treat `.planning/` as the task or plan record; it is a legacy, gitignored
directory that is out of scope for Orchestrator-driven work.

## Architecture boundaries

Use `AGENTS.md`'s "Architecture Boundaries" section as the authoritative ownership map when deciding
which module a task or worker diff belongs to. Do not restate or re-derive those boundaries here.

## Serial lanes

Treat a change touching any of the following as a serial lane: dispatch one worker at a time, and
require the human to review the diff before a second worker's task in the same lane starts.

- `src/Cna.Core` public contracts, commands, events, and snapshots.
- `src/Cna.Intelligence.Contracts/Protos/intelligence.proto`.
- `src/Cna.OrleansHost/Api/CampaignDurableAuthorityJournal*.cs`.

Do not widen this list without re-verifying the paths exist; do not treat unrelated `src/Cna.Core`
internals as serial.

## Dispatch and verification

Dispatch work in terms of this repo's real commands: `just check`, `just check-fast`,
`just test-suite <suite>`, `just test-class <suite> <pattern>`, `just test-method <suite> <pattern>`,
`just boundary-check`, and `scripts/agent-worktree-setup.sh` for a fresh worktree's baseline. Do not
invent command names or assume a different test runner.

## Escalate to the human, never resolve alone

Escalate to the human rather than deciding unilaterally whenever a worker's change touches:

- A contract or protobuf definition (`src/Cna.Intelligence.Contracts/Protos/intelligence.proto` or
  any `src/Cna.Core` public command/event/snapshot shape).
- Fog-of-war or hidden-state boundaries (what is visible to the intelligence plane vs. the Umpire).
- A golden fixture or recorded deterministic-replay baseline.

## Never merge

The Orchestrator never merges a pull request itself. Merging is a human decision after review.

## Writing for other readers

The compressed output style in `AGENTS.md`/`CLAUDE.md` applies to chat replies only. Write every PR
description, dispatch brief, and cross-model review finding you produce or relay in plain, full
sentences, since a worker, reviewer, or the human will read it as a report, not a chat turn.
