# Overnight Combat wave 01 execution index

## Objective and boundary

Required: complete CMB-TASK-004/checkpoint B, then dormant CMB-TASK-005 with integrated evidence,
accepted story commits and one draft PR. CMB-TASK-006 is stretch only after both pass every gate.
Start 2026-09-15 02:49:17 UTC; hard deadline 2026-09-15 10:49:17 UTC.
Integration destination: `codex/overnight-combat-wave-01`, base/latest-main
`ff6b5f60219afbb12d5e06bf836f06f94de32006`. No merge to main.

**HARD STOP:** confirmed2026-09-15 03:19:38 UTC. Accepted shared clock-high-water fallback and
equal-outcome privacy conflict on genuine hidden-seal histories. No implementation resumes without
owner policy disposition. Required objective incomplete; retain unaccepted candidate/diagnostic in
one blocked draft PR. See handoff.md and evidence/clock-high-water-counterexample.py.

## Canonical sources

- [AGENTS](../../AGENTS.md)
- [Combined plan](../../docs/design/combat-cycle-implementation-plan.md)
- [Roadmap](../../docs/roadmap/pre-alpha-roadmap.md)
- [Exact Task003 handoff](../../docs/specs/combat-authority-composition-v1.md)

This execution index is structured data, not product authority. Formal independent-review budget
is exhausted. Ordinary fresh-context five-axis review only.

## Work items

| ID | Dependencies | Owner / delivery unit | Owned paths | Acceptance / verification | Status / blockers |
| --- | --- | --- | --- | --- | --- |
| W01-SETUP | none | brain | this planning directory | latest main includes ff6b5f6 and merged113/114; skill load; branch; plan attestation | complete |
| CMB-TASK-004A | 003 verified | task004_writer / internal; task004_evidence read-only; fresh reviewer per slice | docs/specs/combat-side-projection-v1.md, .schema.json, fixtures/combat-side-projection-v1.json, verify-combat-side-projection-v1.py; brain owns combined plan | CON-005 closed audience fields/errors/choices, canonical candidate bytes, equal-authorized-history and leak negatives; focused oracle plus brain just check | blocked; no accepted child |
| CMB-TASK-004A1 | 003 | task004_writer / internal | same four004A files only | closed common codec/identity/errors, selection/RBA/seals; TDD plus focused/gate/review | blocked/unaccepted: P1 clock/privacy policy conflict; focused+just check pass but independent privacy probe fails |
| CMB-TASK-004A2 | accepted004A1 | same writer / internal | same four004A files only | settlement results, own retreat/custody/replacement disclosure/candidates; TDD plus focused/gate/review | pending |
| CMB-TASK-004A3 | accepted004A2 | same writer / internal | same four004A files only | Reserve I/laterII, cycle/structural projection, exact003 handoff and corpus reconciliation; TDD plus focused/gate/review; only then004A freeze | pending |
| CMB-TASK-004B | accepted004A | writer / internal subagent; reviewer and evidence read-only | new Exercise-contract spec/schema/fixture/oracle; brain owns combined plan | CON-006 exact terminal/ordinal/continuation, strict manifest/report and divergence, negative success rejection; focused oracle plus just check | pending |
| CMB-TASK-004C | accepted004A/B | writer / internal subagent; reviewer and evidence read-only | new 72-AC index and integration oracle/fixture as needed; brain owns plan | exact003 handoff, all72 ACs task/planned-test mapped, outward versions/capacity/compatibility checked; all focused plus just check; checkpoint B only on reconciled evidence | pending |
| W01-NAV | current retained reality | brain | README.md, docs/roadmap/pre-alpha-roadmap.md, tech-design.md, naming-overview.md only where reality changes | coherent blocked status and local links | in progress for hard-stop handoff |
| CMB-TASK-005 | complete004/B | writer / internal subagent; reviewer and evidence read-only | Rules and focused Rules tests, at most five primary files per recorded slice | full normalized approved-source comparison, every selected coordinate/reachable differential, conditional capture and ordered pairs; TDD; focused tests plus just check; no campaign activation | gated |
| CMB-TASK-006 | complete005 | writer / internal subagent; reviewer and evidence read-only | Content and focused Content tests, split before edits | versioned synthetic admission, strict provenance/mutations and historical bytes; TDD; focused tests plus just check | stretch/deferred |
| W01-DELIVERY | coherent accepted retained changes | brain | planning/handoff, Git metadata and draft PR | final reconciliation, exact commits/files/tests, latest main/dirty state/children, push one branch and one draft PR | pending |

All child writes disjoint. Shared repository; preserve others' changes. Brain owns integration,
canonical combined plan, commits, full verification and PR. At most four active agents including brain.
Each story: one sole writer, one fresh-context read-only reviewer, one read-only evidence/source/test
agent. Child packets name precise paths and protected scope before changes. Never advance red gate.

## Protected paths and stop conditions

All historical contracts/fixtures/oracles, existing wire bytes, authority semantics and registration
remain protected unless a later accepted task explicitly owns a compatible successor. No runtime
activation. Stop for new gameplay/source-policy decision or ambiguous source; unexpected frozen-byte,
wire or authority change; unsplit scope over about five primary files; dependency upgrade; destructive
Git operation; deletion of user material; conflict with newer main; same material failure after three
diagnosed approaches; unavoidable permission/user choice; eight-hour deadline.

## Verification and evidence

Focused commands fixed in each story packet. Integration gate: `just check` at every accepted story
boundary. Preserve exact commands, exit status, counts and evidence paths in progress.md. Every
behavior change requires observed RED before GREEN. Child reports reconciled against actual diff.

## Errors

Broad skill lookup found no required delivery skills; harmless, cancelled. Main checkout skill
symlinks at `/Users/dsteele/repos/sandtable/.codex/skills/` are authoritative local source. No repeat.
