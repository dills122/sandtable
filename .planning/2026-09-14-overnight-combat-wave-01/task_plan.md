# Overnight Combat wave 01 execution index

## Objective and boundary

Required: complete CMB-TASK-004/checkpoint B, then dormant CMB-TASK-005 with integrated evidence,
accepted story commits and one draft PR. CMB-TASK-006 is stretch only after both pass every gate.
Start 2026-09-15 02:49:17 UTC; hard deadline 2026-09-15 10:49:17 UTC.
Integration destination: `codex/overnight-combat-wave-01`, base/latest-main
`ff6b5f60219afbb12d5e06bf836f06f94de32006`. No merge to main.

**RESUMED:** owner chose strict privacy and authorized a new eight-hour work window on2026-09-15.
Renewed start11:39:39 UTC; hard deadline19:39:39 UTC (15:39:39 America/Toronto).
Same required004/checkpoint B then005 outcome,006 stretch, one existing draft PR115, no merge.
Historical clock/privacy conflict is reproduced and retained. Correct clock handling through an
explicit versioned successor preserving historical bytes; no privacy exception. First design and
record bounded successor packets, then TDD/ordinary review/integration gates before A1 acceptance.
Goal service still reports blocked; it exposes no resume operation. This user-authorized execution
resumes locally without claiming the unfinished objective complete or replacing it.

### Selected successor design

Collecting gate uses immutable public opening instant and fixed deadline; accepted private seal
instants remain replay evidence only. Preserve valid clock-loss System cancellation, exact retry
recovery, invalid-input ordering, Prepared recovery and approved common terminal disclosure.
No new public clock stream, per-side watermark, deadline renewal or historical v1 rewrite.

### Resumption packets

- W01-CLOCK-DESIGN: root owns canonical policy disposition and bounded split; writer and evidence
  agents independently advise read-only. No historical contract edits. Define clock evidence,
  privacy invariants and integration ripple before successor writes.
- W01-CLOCK-CONTRACT: accepted after12groups/ordinaryreview/root81+1670gate; sole writer owns new `combat-sealed-round-v2` spec/schema/fixture/oracle; root owns combined plan. Focused clock/privacy/replay/compatibility checks,
  ordinary fresh-context quality review, root just check before accepted commit.
- W01-CLOCK-INTEGRATE: after accepted successor, sole writer owns existing unaccepted four-file
  side packet; root owns combined plan. Preserve old failing diagnostic as historical evidence;
  prove successor outcomes and source bindings before accepting004A1.
- W01-CLOCK-RESULT: accepted after32traces/ordinaryreview/root81+1670gate; sole writer owns new
  `combat-result-settlement-v2` spec/schema/fixture/oracle; root combined plan. Corrected synthetic
  round-to-settlement causal evidence, unchanged gameplay arithmetic; must precede004A2 use.
- W01-CLOCK-CYCLE: required native Result2→emptyReserve→cyclefinish bridge after004A2 and before004A3; new combat-result-cycle-finish-v1 fourfiles plus rootplan,32lineages, exact retained obligations, synthetic Movement certificate, independent opening clock. Same story gates; no historical base-reader reuse.
- W01-CLOCK-SNAPSHOT: if needed byCON006, sole writer owns new
  `combat-snapshot-composition-v2` fourfile packet plus root plan; exact newpayload/arms frozen first.
  Corrected synthetic cuts stay distinct from unchanged00328trace creation-rooted handoff.


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
| CMB-TASK-004A | 003 verified | task004_writer / internal; task004_evidence read-only; fresh reviewer per slice | docs/specs/combat-side-projection-v1.md, .schema.json, fixtures/combat-side-projection-v1.json, verify-combat-side-projection-v1.py; brain owns combined plan | CON-005 closed audience fields/errors/choices, canonical candidate bytes, equal-authorized-history and leak negatives; focused oracle plus brain just check | in progress; round-v2,004A1,result-v2,004A2 accepted; required bridge next |
| CMB-TASK-004A1 | 003 | task004_writer / internal | same four004A files only | closed common codec/identity/errors, selection/RBA/seals; TDD plus focused/gate/review | accepted: corrected profile, ordinary review, root81+1670 gate; historicalv1 failure retained |
| CMB-TASK-004A2 | accepted004A1 and Result2 | same writer / internal | same four004A files only | settlement results, own retreat/custody/replacement disclosure/candidates; TDD plus focused/gate/review | accepted:37groups, ordinary review/source audit, root81+1670 gate |
| CMB-TASK-004A3 | accepted004A2; sequentialA3a/A3b | same writer / internal | same four004A files only | Reserve I/laterII, cycle/structural projection, exact003 handoff and corpus reconciliation; TDD plus focused/gate/review; only then004A freeze | pending |
| CMB-TASK-004B | accepted004A | writer / internal subagent; reviewer and evidence read-only | new Exercise-contract spec/schema/fixture/oracle; brain owns combined plan | CON-006 exact terminal/ordinal/continuation, strict manifest/report and divergence, negative success rejection; focused oracle plus just check | pending |
| CMB-TASK-004C | accepted004A/B | writer / internal subagent; reviewer and evidence read-only | new 72-AC index and integration oracle/fixture as needed; brain owns plan | exact003 handoff, all72 ACs task/planned-test mapped, outward versions/capacity/compatibility checked; all focused plus just check; checkpoint B only on reconciled evidence | pending |
| W01-NAV | current retained reality | brain | README.md, docs/roadmap/pre-alpha-roadmap.md, tech-design.md, naming-overview.md only where reality changes | coherent blocked status and local links | complete: blocked status synchronized, links checked |
| CMB-TASK-005 | complete004/B | writer / internal subagent; reviewer and evidence read-only | Rules and focused Rules tests, at most five primary files per recorded slice | full normalized approved-source comparison, every selected coordinate/reachable differential, conditional capture and ordered pairs; TDD; focused tests plus just check; no campaign activation | gated |
| CMB-TASK-006 | complete005 | writer / internal subagent; reviewer and evidence read-only | Content and focused Content tests, split before edits | versioned synthetic admission, strict provenance/mutations and historical bytes; TDD; focused tests plus just check | stretch/deferred |
| W01-DELIVERY | accepted packet evidence and final outcome | brain | planning/handoff, Git metadata and draft PR | exact commits/files/tests, latest main/dirty state/children, one pushed branch/draft PR | draftPR115 retained; renewed packets accepted, required004/005 objective still incomplete |

004A3 refinement before edits:004A3a covers Reserve/cycle candidate arms, frozen binary action/set
codec, own live inherited release/control cuts and synthetic ledger decision tests.004A3b adds
both-audience28trace terminal projections, exact003handoff binding, privacy pairs and capacities.
Both use the same four side-packet files plus root combined plan sequentially, with full story gates.
Actual own CP14/Cohesion-4 and three-action later-II sets must fit; never truncate side history.

004B refinement: B1newcombat-exercise-occurrence-v1, B2child-evidence-v1, B3parent-evidence-v1;
each fourfiles plus rootplan, sequential after004A. Tagged replay-backed fullWorld/RNG checkpoints
avoid a new fullSnapshotpacket; no CoreSnapshot parity claim before022. See source-audit-004b.md
and findings.md. Existingmanifest2/labelv1 and registeredreaders unchanged; new schemes unregistered.

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
