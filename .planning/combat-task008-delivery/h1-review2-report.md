# H1 independent engineering review

Review instance: 2 of 3. Frozen working-tree target at base `4139675fc9b775c5e7c0d355c06756c17ea1f2f9`.

## Blind preliminary ledger

Recorded before author packet, checks packet, session recall, or other reviewer reports.

- All five `h1-source.sha256` entries match; HEAD matches bootstrap. Supporting documentation changes stay within H1 scope.
- Canonical H1 acceptance requires bounded owned Created11 plus one ordered stream through first opening, actual causal readers, every selected prefix, and unsupported-tail rejection. H2/H3/H4 and host publication remain explicitly open.
- Router uses fixed profile offsets only to select candidate partitions; preamble, Weather, stage and Reserve readers each independently replay and compare canonical event bytes. Count alone cannot admit an event. State10 routes to Reserve with zero local events, materializing membership.
- Capture checks count before indexing and all byte limits before payload ownership copies; retained input/output buffers are copied. Count is capped at512, each payload at1MiB, post-Created aggregate at16MiB.
- Tests exercise all selected H0 pre-cycle history identities, causal projection fields, predecessor byte goldens, every event occurrence tampering, canonical RNG forgery, true future Movement tail, capacity edges and buffer mutation.
- Preliminary concern to check: coverage inventory is identity-based, whereas complete root codec is intentionally absent. Confirm retained focused execution proves assertions ran, and avoid promoting this into H4/full-root parity.
- No actionable defect identified in blind source/plan pass. Await retained execution evidence and author-claim reconciliation.

## Findings

No actionable findings. No P0/P1/P2/P3 defect or heavy pivot identified within frozen H1 scope.

`CampaignCombatHistoryReplay.Replay` reaches later readers only after earlier readers accept exact retained bytes. Fixed offsets are justified by accepted profile's four preamble, one Weather and four stage events. `CampaignOpeningPreamble.Replay`, `CampaignCombatWeather.Replay`, and `CampaignCombatStageEntry.Replay` regenerate canonical events from predecessor authority and compare whole bytes. `CampaignCombatReserveOpening.Replay` validates designation/completion and rejects events after completion. State10 is correctly represented by ReserveOpening even without a designation event.

`CampaignCombatRetainedHistory.Capture` rejects excessive collection count before indexing, validates lengths and aggregate size before copying payloads, snapshots each indexed reference once, and retains private byte arrays. Copying on export prevents later caller mutation from corrupting accepted evidence. Canonical validation occurs in replay, not in capture itself, as intended.

## Plan Review

Canonical plan's Task008 execution index and Initial H/H refinement agree with implementation and documentation. H1 remains a bounded dormant Core child: no public registration, wire schema, synthetic state injection, persistence choice, or admission switch introduced. H2 ordinary Movement/Breakdown, H3 Reaction, H4 full-root codec/admission-disabled restoration, and HOST-PUB-001 remain distinct open gates. Dependency ordering is credible; no omitted H1 prerequisite or accidental parent completion claim found.

Tests target meaningful boundaries: all selected prefix identities, state10 Reserve materialization, optional designation, exact predecessor goldens, occurrence omission/duplication/reordering, altered canonical spelling, mismatched creation/request, canonical but causally forged Weather RNG, and an actually legal Movement tail rejected by H1. Terminal omission alone is appropriately accepted as a shorter trusted prefix; this API does not authenticate archive head or commitment.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Mandatory independent Created11, owned bounded history | Capture, replay, missing-creation and buffer mutation tests | Confirmed | No regeneration fallback or retained buffer alias |
| Counts locate partitions but do not authenticate events | Router plus four actual strict predecessor replay methods | Confirmed | Malformed or wrong-family events cannot advance solely through count |
| Reserve membership exists at state10 | Router count9 path and per-prefix Reserve assertions | Confirmed | B2/D shared cut has actual membership authority |
| All256 H0 pre-cycle vectors/208 distinct histories covered | Independent JSON inventory and identity equality test | Confirmed | Inventory is exhaustive for selected frozen profile |
| Exact predecessor byte parity plus H0 projection comparisons | Four golden variants and42 parameterized selected-history cases | Confirmed | H1 preserves family authority without claiming literal full-root serialization |
| Focused53/full2157/boundary81 pass | Retained logs inspected directly | Confirmed | Runtime assertions executed successfully; reviewer did not rerun .NET |
| Full-root restore and disabled-admission seam remain H4 | Canonical plan, author packet, source/API and supporting docs | Confirmed | H1 Ready does not close Initial H or Task008 |

## Verification Performed

- Read neutral bootstrap first; inspected exact source/tests/canonical plan and predecessor context before recording blind ledger. Applied independent-review skill in reviewer mode. Session recall occurred only after ledger; no prior review reports or aggregate H1 evidence read.
- Ran `shasum -a 256 -c .planning/combat-task008-delivery/h1-source.sha256` twice: all five entries OK both times. `git rev-parse HEAD` returned `4139675fc9b775c5e7c0d355c06756c17ea1f2f9`. Read-only status and diff matched declared target/supporting changes.
- Ran independent `python3 -B` JSON inventory against `docs/specs/fixtures/combat-inherited-snapshot-v1.json`:368 total rows;256 pre-cycle rows;208 distinct `(creationHash,eventHashes)` histories. Event-count distribution:0:20,1:20,2:20,3:20,4:40,5:46,6:12,7:12,8:12,9:28,10:18,11:8.
- Ran `git diff --check`: no errors.
- Inspected `/tmp/h1-green-4.log`:53 passed,0 failed,0 skipped,6.923s. Checks packet records command `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-class '*CombatHistoryReplayTests' --no-restore '-bl:/tmp/h1-focused-{}.binlog'`.
- Inspected `/tmp/h1-suite.log`:2157 passed,0 failed,0 skipped,4m57.652s. Inspected `/tmp/h1-boundary.log`:81 passed,0 failed,0 skipped,10.093s. These are root-run checks, not reviewer reruns.
- Inspected `/tmp/h1-build.log`: build succeeded,0 warnings,0 errors,4.02s. Full solution format exit0 is root-observed evidence from checks packet; reviewer did not independently execute format or assert that empty output alone proves exit status.
- No builds, .NET test runs, TRX writes, implementation edits, Git mutations, delegation, or additional review rounds performed. Only this report written.

## Open Questions And Residual Risks

No blocking open question. Full-root canonical byte equality, later family routing, actual fresh-admission-disabled restoration, authenticated committed-head/truncation checks, and durable host publication are deliberately outside H1. Future routing extensions must preserve strict causal validation and current shared-cut semantics. Repeated bounded prefix replay/copying remains a modest known cost; no broad campaign-scale performance claim evaluated.

## Verdict

**Ready** for frozen H1 child only. Evidence supports implementation and canonical H1 plan; no parent Task008/full-restore or publication acceptance implied.

## Recommended Next Actions

Root may reconcile this report with remaining authorized review and gate evidence, then finalize H1 status. Continue H2–H4 under canonical dependency plan; do not start another review instance from this report.
