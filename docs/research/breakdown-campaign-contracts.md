# BRK-TASK-003 — Dormant campaign contracts

**Status:** Complete, 2026-09-05; full repository gate passed.
**Branch:** `codex/breakdown-adjudication-design`.
**Authority:** [behavior](../specs/breakdown-adjudication-v1.md),
[wire contract](../specs/breakdown-wire-contract-v1.md),
[ordered plan](../design/breakdown-adjudication-v1.md).

## Scope and dependency boundary

Task 003 adds internal Content 6/format 5, Setup 6, World 6, Snapshot 11 and CampaignCreated 10.
Sequence/catalog 4 supplies a separate System Breakdown-stop interrupt and an exact supported
checkpoint boundary through first-side Combat position determination. A dormant Ruleset 9 manifest
now composes that catalog, Task 002's outcome artifact and four accepted rulings. Active Ruleset 8,
sequence 3, Content 5, World 5, Snapshot 10 and Created 9 remain current.

The new content fixture contains nine connected hexes, one unladen 12-point Truck convoy per side
and one nonmotorized infantry battalion per side. Every element has an independent root battalion
formation. Truck combat components and initial TOE seed arrays are empty; public V5 constructors
and validation retain their nonempty grammar. No existing checked scenario is changed or relabeled.

Fixture: [canonical Content 6](../../tests/Cna.Core.Tests/Content/Fixtures/rules-lab.content.breakdown-truck.v1.golden.json).
Canonical SHA-256: `646e76e69ecceb82216b37d84e950928099acd8a3cb04b51526d0fe631e512ee`.

## Implemented contracts and evidence mapping

| Boundary | Implementation | Executable evidence |
| --- | --- | --- |
| Static certified profile | Content V6 models/serializer/validator, explicit diagnostic classes; exact inherited capabilities, cohort/organization/source/TOE checks | `BreakdownContentTests`: strict byte identity, unsupported/mixed/unknown fields, all static diagnostic classes and legacy rejection |
| Setup 6 | Separate typed Content 6 selection, embedded profile and canonical hash, display name excluded under inherited convention | `BreakdownSetupTests`: independent predecessor-byte delta, strict readback, rehashed unsupported profile/synthetic root and content identity mutations |
| Sequence and manifest | Dormant sequence/catalog 4, System interrupt, Ruleset 9 composition, explicit successor-only shared factories | `BreakdownSequenceTests`: order/source/version/side/terminal boundary and partial artifact mixtures; World 5 rejects sequence 4 ended markers |
| World 6 | Persistent immutable lots, exact cohort/initial-point conservation, component provenance, unchanged stage ledgers, own combat aggregate bound | `BreakdownWorldTests`, `BreakdownWorldBoundaryTests`: initial state, loss balances, counterfeit ledgers/lots, dynamic stack rejection and stationary lot bytes after survivor movement |
| Finite flow and identity | Closed six-variant flow/two-variant continuation, ordered route/stop/check/lot SHA-256 domains | `BreakdownRecordTests`: byte roundtrips, malformed/recursive shapes, hash binding and mutable route-location exclusion |
| Snapshot 11 | Exact version set, route/World/owner/stop-input binding, Reaction continuation/episode causality, retained stage weather and System stop authority | `BreakdownSnapshotTests`, `BreakdownFlowTests`: canonical negatives, all authority shapes, final-CP Reaction, forced phasing continuation, weather and trigger-date mutations |
| Created 10 | Context-required factory/serializer rederive initial World and idle flow, bind canonical setup/content/rules/RNG/sequence | `BreakdownCreationTests`: coherent creation/projection and rehashed identity, state, RNG, initial point and unknown-field negatives |

These cover Task 003 portions of BRK-AC-006/007/008/011. Snapshot's context-free overload checks
canonical shape and internally bound identities; certified readback additionally requires the
artifact/scenario overload. Context validation is required for content-dependent cohort, owner,
weather, topology and conservation checks. A snapshot check is not replay of its preceding history.

## Follow-up and implementation choices

Progress review 2's P2 was accepted and fixed in `bc6fde7`: same-event forced stops apply to phasing
moves. Exhausted Reaction routes remain active until explicit completion or active System closure.
The acceptance matrix and direct finite-state vectors retain that distinction.

Lot provenance is the fixed placement source `spi-1979-land-rules / 21.41`. Check/outcome provenance
belongs to later check evidence. Sources are validated independently of lot identity, whose frozen
hash preimage intentionally omits them. New codecs preserve old serializers and create their own
versioned envelopes; shared nested types use explicit successor factories without widening old paths.

## Verification log

Executable RED runs preceded implementation: 31 Content, 8 record, 5 sequence, 3 World, 10 Setup,
10 Snapshot and 18 creation cases failed against throwing stubs. Further focused regressions exposed
and then guarded predecessor World containment, forced-stop continuation, route/trigger causality,
missing stage weather, trusted diagnostic precedence, nonadjacent frozen opportunities and
exhausted phasing routes. Foul-weather test expectations were corrected against the retained golden:
die 1 affects areas A and B, so area C supplies the outside-area probe. Compiler/analyzer fixes included fixture
API argument names, explicit window sides and an instance contract-version property. One broad MTP
wildcard selected zero tests; exact namespace/class prefixes replaced it.

Final verification on 2026-09-05:

| Command | Result |
| --- | --- |
| `dotnet restore Sandtable.slnx '-bl:artifacts/binlogs/brk-003-final-restore-{}.binlog'` | Pass; all projects up to date |
| `dotnet build Sandtable.slnx --no-restore '-bl:artifacts/binlogs/brk-003-final-build-{}.binlog'` | Pass; 0 warnings, 0 errors |
| `dotnet format Sandtable.slnx --verify-no-changes --no-restore` | Pass |
| `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-trait 'Boundary=UserSpace'` | 48 passed, 0 failed, 0 skipped |
| `dotnet test --solution Sandtable.slnx --no-build` | 1,406 passed, 0 failed, 0 skipped; 3m 10s |
| `python3 docs/research/verify-breakdown-contract-freeze.py` | 14 historical fixture hashes, 15 Reaction children, 15 predecessor schemas, 10 requirements / 12 acceptance mappings, 29 links |
| `python3 docs/research/verify-breakdown-outcomes.py` | 324 cells, 9 source probes, 606 bounded proposed-loss combinations |
| Changed documentation local-link check and `git diff --check` | Pass; 278 local links |

The full suite includes 118 new Task 003 cases across nine test classes, including the final CP,
weather and topology regressions. Local restore/build binlogs (ignored artifacts):
`brk-003-final-restore-20260905-170434--85330--Wv6PGk.binlog` and
`brk-003-final-build-20260905-170528--87145--b+Zfvt.binlog`.
An initial sandboxed restore stalled before project results and was stopped; local-access retry
passed. No dependency change was needed. Final tests and formatting also ran with local access.

## Remaining obligations

Task 004 implements actual ordinary/Reaction BP deltas and successor move events. Task 005 owns
stop/check authority, RNG consumption/replay, first-side Breakdown completion and event transition
proofs. Task 006 owns complete outward activation and the full public identity/privacy matrix.
Task 007 owns the remaining checked fixture migrations, Runner trajectories and clean-run evidence.
No new public Truck candidate, public loss trajectory, live stop handler or RNG draw is claimed here.

Peer implementation checks were performed within this task; no new independent-review instance
was started. Implementation review budget remains 2 of 3, with final independent review still
pending; the research/design loop remains closed at 3 of 3.
