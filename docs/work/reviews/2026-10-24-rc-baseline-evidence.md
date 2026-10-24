# RC baseline evidence — October 24, 2026

Baseline: `16d76448a008ea4f16299402b8ef44e8c96a4843`, branch
`codex/rc-overnight-plan-20261023`. This reconciliation separates merged bounded
implementation from certification obligations. It grants no public admission,
original P01 support certificate, parent017–019 closure, skeleton completion or RC claim.

## Confirmed baseline gate

Retained log: `/tmp/sandtable-rc-check-20261023-unrestricted.log`.
Coordinator recovered successful completion of the existing `just check`; this
reconciliation directly inspected its final test summary. No duplicate full run started.

| Command within gate | Result |
| --- | --- |
| `dotnet restore Sandtable.slnx` | Passed, coordinator baseline evidence |
| `dotnet format Sandtable.slnx --verify-no-changes --no-restore` | Passed, coordinator baseline evidence |
| `dotnet build Sandtable.slnx --no-restore` | Passed, coordinator baseline evidence |
| `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-trait "Boundary=UserSpace"` | 81 succeeded, zero failed/skipped; 7s 455ms |
| `dotnet test --solution Sandtable.slnx --no-build` | 4,195 succeeded, zero failed/skipped; 1h 01m 24s 126ms |

Core assembly completed in 1h 01m 23s 983ms. Host, ExerciseRunner and both contract
assemblies also passed. These counts describe solution-wide baseline tests, not
4,195 Combat cases or an original-300-root certificate. Retained Python oracles,
frontend browser tests and Docker/Aspire startup are outside this baseline claim.

## Current capability and obligation matrix

Runtime summary below reconciles the current [Combat implementation plan](../../design/combat-cycle-implementation-plan.md)
and [technical design](../../../tech-design.md), with the delegated Host audit below.
It does not certify all behavior described there. New focused checks and runtime
changes belong to subsequent overnight packets.

| Gate | Current implementation / bounded evidence | Unmet obligation | Smallest next packet |
| --- | --- | --- | --- |
| Contact-based native loop | Current plan records initial assault plus two selected repeated assaults through normal durable owner flow, settlement, custody and cold replay. V28 Repeat requires a legal released-unit movement witness and retained Combat cursor. Third tested Release offers Finish because no witness remains. | General later-cycle shapes, vehicle accounting and broader profiles; no fixed three-cycle ceiling is asserted. | Verify a specific uncovered continuation/rejection branch; bind deterministic test to exact native history before extending behavior. |
| Original P01 catalogue | [Canonical P01 matrix](../../specs/combat-actual-moved-repeat-position-v1.md): 300 genuine ancestors; 44 provisional positive and 256 absent. Every original assessment remains unsupported; zero admitted/public. | Absent candidates do not prove supported-empty or all source-legal work complete. Contact-route tests do not substitute for original roots. | Keep original-root row identity and all independent ledgers explicit in every certificate slice. |
| P01 AC-001/002/003 | Provisional ancestor replay, terminal partition and selected binding/Weather facts; bounded private successors exist. | Complete mutation/cut/retry evidence and participant/action/obligation exhaustiveness. | Select one missing named proof and retain exact source pins, all affected rows and negative cases. |
| P01 AC-004/005 | Canonical matrix records bounded result/settlement and geometry proofs (PR234/236), including both actor orientations and retained placement cases. | These slices do not admit P01 or certify the complete profile. | Preserve their scopes; rerun applicable original verifiers separately from native tests. |
| P01 AC-006 | Private explicit decline/cancellation and selected Force paths; voluntary surrender expressly excluded by CMB-POL-009. | Full automatic-surrender trigger/settlement and complete RBA/profile authority. Post-cost zero ammunition is not sufficient surrender evidence. | Freeze source condition and typed authority before any new automatic-surrender behavior. |
| P01 AC-007/008/009 | Private ID vectors; separate all-44 TEST decline/cancellation and all-prefix rejection proofs. | Authentic owner hidden-fork privacy; all-row matching midstream recovery, faults/caps; full support review and separate P02/P03. | Prove exact-profile outward identity/replay with authenticated owner and durable restart; retain TEST-only limits. |
| Parent017–019 | Accepted isolated Release, held-I/release-I, ordinary Movement and bounded private continuation children; contact path now extends runtime coverage. | Parent-wide lineage/replay reconciliation, including later-II/consumed obligations; children are not blanket parent completion. | Map current native regression evidence to specific parent obligations and retain unsupported lineages. |
| HOST-PUB-001 | Local durable implementation exists; prior pure creation/restore and memory-CAS evidence remain bounded. | [Canonical publication matrix](../../specs/combat-authority-envelope-v1.md#runtime-evidence-ownership): selected-provider/configuration and exact bytes for concurrency, before/after commit faults, lost reply, unreadable ambiguous commit, stale writer, corrupt/missing evidence, disabled-admission recovery and retention bounds. Scheduled after020–021 and one verified023 trace. | Prove one concrete missing durable fault branch against current provider. Reconcile versioned current journal with canonical Created11/Snapshot12 obligations before claiming closure. |
| Public020–021 | Local Development loopback owner route exists. | Certified side-safe admitted profile, authenticated submission and recovery registration. Original P01 remains unadmitted. | Retain local boundary; freeze certificate-to-runtime mapping before public activation. |
| Exercise/Runner022–024 | Rules9 public path and existing Runner evidence reach first-side Combat entry; native Exercise replay does not itself prove public Runner parity. | Same admitted public actions, strict reconstruction/readjudication, repeated clean empty/assault/Movement-repeat evidence. | Inventory exact missing public action/terminal mapping after021; record oracle failures independently. |
| Closeout025 / RC | Baseline full local gate green; bounded native progress substantial. | All72 reconciliation and authentic accepted loop; six-turn source/content/victory, complete lifecycle, hot-seat privacy, repeated playthrough and distribution gates. | Deliver reviewed bounded checkpoint and honest handoff; no RC claim. |

## Delegated Host source audit

Host explorer checked [CampaignDurableAuthorityJournalTests.cs](../../../tests/Cna.OrleansHost.Tests/CampaignDurableAuthorityJournalTests.cs)
and its current journal implementation through codebase-memory Tier2. Reported
coverage: `no_recorded_issue`, `metadata_match`, index generation
`2026-10-24T01:56:13Z`; best-effort coverage, not completeness proof. Baseline
solution run passed these tests; no new isolated run is claimed here.

| Test / inspected line | Bounded assertion |
| --- | --- |
| `ContactCombatOpeningCommitsOnceAndColdReplaysSelectedAuthority`,142 | Selection/barrage and RBA decline retain canonical receipts across OpenExisting. |
| `SourceBackedReserveReleaseChoiceRetainsOwnerReceiptAfterRestart`,282 | Release and cycle receipts survive restart. |
| `SecondContactAssaultEntersBarrageAndColdReplays`,446; `SecondAssaultCanRepeatIntoThirdContactCombatAfterColdRestart`,537 | Initial plus two repeated assaults; HTTP owner selection, same-running-server duplicate acknowledgement and cold reconstruction. |
| `SeatCredentialSurvivesRestartAndRotationRevokesIt`,1093; `GrantAndCredentialExpireAtTheirStoredDeadlines`,1119 | Stored credential lifetime and rotation. |
| `OwnerOpeningRemainsByteIdenticalAcrossUnseenAuthorityAdvanceAndRestart`,1139 | One bounded unseen-authority privacy/restart history. |
| `ContactMovementReferenceCommitsOnceAndReplaysAfterRestart`,1175 | Contact movement receipt/replay. |

Audit identified missing fault-injection proof for atomic Combat batches and
fresh-server lost-response retry. Initial creation still uses unconditional
insertion rather than retained identical-request replay; this is an unmet
HOST-PUB creation obligation, not a claim that current typed create API promises
idempotence. These findings justify a bounded durable evidence packet; they do
not close the complete publication matrix. The durable-journal specification also
needs reconciliation with current schema and local route boundaries.

## Historical evidence and source pins

[Pin-maintenance research](../../research/combat-verification-pin-maintenance.md)
records a historical source-pin failure and a 20-file dependency closure. Its
semantic probes deliberately did not repair or pass the original failing oracle.
Coordinator reran two original verifiers at this baseline; both remain failed:

| Command | Current result / retained log |
| --- | --- |
| `python3 -B docs/specs/verify-combat-inherited-breakdown-completion-v1.py` | Exit1, source drift for `src/Cna.Core/Rules/Cna1979LandSequence.cs`, check at line290; `/tmp/sandtable-rc-oracle-breakdown.log` |
| `python3 -B docs/specs/verify-combat-cycle-sequence-v1.py` | Exit1, same source hash drift, check at line233; `/tmp/sandtable-rc-oracle-cycle.log` |

No source-pin edits planned. Other retained oracles remain unrun in this packet.
Never refresh hashes simply to bypass a failed preflight.

[Six-turn inventory](../../research/2026-10-18-graziani-six-turn-surface-inventory.md)
is research, not MVP-GATE-01 freeze. Exact setup/content/reachable-rule counts and
source gaps remain open; the overnight engine packet does not close them.

## Older PR disposition

Coordinator read current GitHub PR metadata, bodies and file lists; no semantic
full-diff review, merge or closure performed. These are provisional dispositions.

| PR / inspected head | Observed state | Disposition |
| --- | --- | --- |
| #306 / `6895f5b` | Conflicting; private opening codec excludes durable Host/public admission | Historical or overlapping scope possible; subsumption not established. Retain pending comparison. |
| #271 / `57a0917` | Conflicting; separate MAP10a read-only states2–10 proposal | Defer from engine push; preserve its owner-review gate. |
| #200 / `272a6ef` | Conflicting; private R03 checkpoint/capacity, `closureFrozen=false` | Historical capacity obligation is not replaced by current loop. Retain open. |
| #191 / `9aa0b75` | Mergeable; old six-job A/B/C/D partition versus current17-job measured manifest | Current relevance audit needed; mergeability is not correctness evidence. |

## Verification scope

Documentation-only reconciliation. Baseline log read directly; canonical specs and
current design summaries read directly. No original-300-root execution, public
activation, dependency upgrade or new runtime behavior performed by this packet.
Review and integrated-head validation remain later gates.

Coordinator selected next packets after this reconciliation: third-cycle cancellation
with zero/one Force seal, unchanged World/ammo, retained RNG cursor presence,
no additional assault result, stale-seal rejection, cold
replay and Finish; plus exact creation retry identity/timing and an injected
selection/Barrage transaction rollback. These are planned work, not passing
evidence at this baseline. Production Core changes require a demonstrated defect.
