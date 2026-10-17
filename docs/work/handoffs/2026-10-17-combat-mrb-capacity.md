# Handoff: authentic moved-repeat Breakdown capacity proof

## Objective And Boundary

Keep the frozen 268,435,456-byte Source cap and prove that every supported paid/armed R02-to-moved-repeat Breakdown ancestry can be replayed by the unchanged native consumer within that cap and an appropriate four-CPU/16-GiB hosted resource envelope. A private contract checkpoint exists, but `closureFrozen=false`. Selected profile, full 72-action closure and first-release readiness are distinct. No public PR, merge or closure claim follows from this handoff.

## Canonical Sources

Start with `AGENTS.md`, `docs/work/plans/2026-10-16-actual-moved-repeat-breakdown-capacity-evidence.md`, `docs/specs/combat-actual-moved-repeat-breakdown-completion-v1.schema.json`, the five predecessor Source schemas and frozen content fixture cited in the capacity packet. Review `docs/work/reviews/2026-10-16-mrb-capacity-review10-result.md` and the two separate review packets only for review provenance. Runtime truth is in the C# transition and codec sources.

## Current Repository State

Worktree `/Users/dsteele/.codex/worktrees/ci-runtime-optimization/sandtable`, branch `codex/combat-moved-repeat-breakdown`, code checkpoint `0d7199f153984094ef4374071d268124055cd559` plus the handoff-only commit containing this file. Tracked files are clean after that commit; only local `.serena/` is untracked and must not be staged. The primary checkout `/Users/dsteele/repos/sandtable` is a different, dirty checkout; do not switch or edit it casually. The raw archive `27d8293` remains separately blocked from publication. No PR was opened for this private milestone.

## Completed Work And Evidence

- The private Breakdown checkpoint was reviewed at `2a39f6d` (review 9): frozen 300-case fixture, 56 roots, 18 disjoint groups, 38 Release rows, 2,950 Core rows and 60 process groups. That review did not clear capacity.
- Capacity research commits `8aa7d44`, `555fe13`, `589cbb3`, `60004a5` retained authentic fixture sizing and resource observations. The largest reconstructed completed Source is 885,159 bytes with exact recorded ancestry and hash; local `long-03` ran 86 rows in eight processes in 124.3 seconds, with 724–790 MB RSS in the four Breakdown-containing processes. A 266.3-MB parser-only malformed carrier peaked at 3.39 GB RSS; it was not valid full replay.
- Commit `758ebc7` added a schema-shaped moved Assault event-size estimator, focused tests and an explicitly provisional capacity packet. Independent review 10 of 10 at that head confirmed the inspected array/ancestry counts and found one P2: supplementary Unicode can take twelve escaped bytes per scalar in generic `string` fields.
- Commit `0d7199f` accepted and corrected the P2. Five focused Python tests now pass. The revised provisional largest moved Assault event is 48,913 bytes; propagating the unchanged five successor recurrences produces a 212,034,752-byte Breakdown Source model, 56,400,704 below the frozen cap. This is a model, not native resource certification. `git diff --check` passed. The focused native Breakdown replay class passed 38/38 locally in 5m20s at source-equivalent C# head `758ebc7`; no C# changed afterward.
- The existing structural verifier `docs/specs/verify-combat-actual-moved-repeat-breakdown-capacity-v1.py` passed, reporting its looser 257,781,248-byte Source model and 108 physical paths.

## Decisions And Rationale

The user chose to keep the cap and build bounded authentic replay proof. The calculator overprices all identifier/hash code units at six escaped bytes and generic Unicode scalars at twelve bytes. It enumerates the frozen schema effect arms and fails on new event array paths. `Participant.componentIds=1` is a reachable-state assertion supported by pinned content validation, reaction-entry certification and participant binding; the standalone grammar can accept more. Setting it to 512 produces a multi-gigabyte model, so that ancestry must remain explicit. No synthetic ancestry, unsupported-as-empty, profile waiver or raw archive publication is allowed.

## Blockers And Limitations

The review 10 verdict was **Not ready for capacity certification or publication**. It found no reachable event violation but the correction at `0d7199f` has only author self-review because the 10-of-10 review budget is exhausted. A new review needs explicit user sync. The full valid-ancestry maximum and native peak RSS/latency at that maximum remain unproved. The 38/38 fixture replay is far below the cap. No hosted exact-head CI, PR, public closure, or merge exists for this milestone. The prior 266.3-MB probe tests parsing only; it cannot clear full replay.

## Immediate Next Actions

1. Use the existing capacity packet to tighten the **reachable** Source bound from authenticated transitions and pinned setup values; identify fields that can actually be varied by a legal creation request. Keep input ledgers independent and all parent event hashes authoritative. Do not replace observed maxima with an unsupported bound.
2. Construct a maximal legal predecessor with real creation and transition APIs; replay it through the unchanged native Breakdown consumer. Retain exact request, independent ledgers, Source/event hashes, control/proof cuts, retry result, process RSS and elapsed time. Start small; stop before any measured process exceeds 4 GiB RSS or aggregate exceeds 8 GiB. One heavy local suite lease at a time.
3. Run a separate isolated near-cap malformed-framing rejection. It is adversarial input evidence, not valid ancestry. Reconcile native results with the model and four-CPU/16-GiB hosted envelope. Seek user sync before an eleventh independent review or material contract/pivot; do not flip `closureFrozen` or publish before review and exact-head CI gates.

## Verification Commands

`PYTHONDONTWRITEBYTECODE=1 python3 -m unittest docs/specs/test_combat_actual_moved_repeat_breakdown_event_bound_v1.py` (5 pass at `0d7199f`). `PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-actual-moved-repeat-breakdown-event-bound-v1.py` (provisional 212,034,752). `PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-actual-moved-repeat-breakdown-capacity-v1.py` (pass). Native MTP command: `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class Cna.Core.Tests.Campaigns.CombatActualMovedRepeatBreakdownCompletionTests` (38/38 in 5m20s at source-equivalent C#). These are local checks, not exact-head hosted CI.

## Delivery Metadata

Date 2026-10-17 UTC. Branch `codex/combat-moved-repeat-breakdown`; no PR. Retained code commits `758ebc7` and `0d7199f`, after capacity packet commits listed above; this handoff is committed separately afterward. Git status must be rechecked before any continuation. Only `.serena/` was untracked at handoff; never stage it. Review chat: `01a1472b-9e06-7401-a0a4-8d80608617d0`.
