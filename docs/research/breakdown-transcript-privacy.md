# BRK-AC-009 — Current transcript and progress privacy

**Status:** Review 4 P2 follow-up implemented; independent review 5 requested by user and pending. P3 activation status was corrected in `77a3203`. This change adds tests only; no runtime contracts, fixtures or authority behavior change.

The [governing disclosure requirement](../specs/breakdown-adjudication-v1.md#brk-req-008--disclosure-and-action-membership) conditions equality on identical approved audience facts. [Review 4](../reviews/brk-final-review-4.md) found isolated Observation 7 tests insufficient to establish current transcript/progress equivalence.

## Executable coverage

`tests/Cna.Core.Tests/Observations/CampaignObservationV7TranscriptTests.cs` adds fifteen deterministic `Boundary=UserSpace` cases. Each scenario begins with public creation and public preamble submissions, then compares admitted current checkpoint continuations. Complete-equivalence cases include the retained audience prefix; release-boundary cases inspect every continuation frame through disclosure. Each continuation uses public query/submission through first-side Combat entry, with a finite 40-action bound. Every captured audience frame includes canonical Observation 7, projected History 2, complete legal-action envelope/candidate bytes, state version and public position/actor progress. Frame sequence lengths must also match.

| Cases | Hidden variation | Audience comparison / progress |
| --- | --- | --- |
| 6 | Phasing route age/identity; deferred stop identity rebuilt from changed route | Both full retained audience transcripts; normal resume and last-CP deferred stop; participant completion, active unavailable and active timeout |
| 2 | Suppressed inactive frozen opportunity absent vs present; alternate frozen trigger-time location and opportunity identity | Both full retained audience transcripts; active other reactor, reactor stop, no-eligible closure, resumed phasing stop and Combat |
| 4 | Opposing Truck BP; inactive reactor ineligible due to spent CP vs cohesion | Full phasing-player transcript through Combat; all three closure paths for BP. Opponent's own intentionally released ledger is a negative equality control |
| 3 | Suppressed reacting-owner Truck location/occupancy | Equality through every Reaction and pending-stop frame for complete/unavailable/timeout; same progress throughout; divergence allowed only once approved own rows return. Axis sees opposing locations, so it is explicitly outside this equivalence class |

Exact flow assertions require reactor-stop-open or reactor-stop-closed as appropriate, phasing stop, and either resumed Moving or last-CP deferred-stop continuation. Public submission success and terminal action closure are checked. Hidden-state variants must differ canonically and pass `CampaignSnapshotV11Admission`; invalid or out-of-profile snapshots cannot make these tests pass vacuously.

## Scope of the proof

These are paired **admitted-checkpoint continuation** tests with a retained public preamble, not claims that a modified checkpoint can be reconstructed from the unmodified creation event history. Checkpoint admission and full history reconstruction are separate contracts. Tests exercise every subsequent transition through public authority; they do not hand-construct successor snapshots or suppress failed submissions. Existing replay tests remain responsible for canonical historical provenance.

The equalities do not treat an observer's published own ledger or apparent opposing location as secret. Current blockers from enemy occupancy use those apparent locations; changing a visible blocker location is outside equal-approved-facts comparisons. Reacting own raw rows remain suppressed through pending stop and are intentionally released on resumption. The hidden own-location cases check that precise release boundary, including every intervening frame, rather than claiming equality after release.

Positive-cohort Reaction, positive ZOC, larger worlds and general placement remain excluded by the certified profile. Suppressed frozen-opportunity variants remain within admitted current authority, with fixed current world geometry; they do not introduce positive ZOC or motorized Reaction fixtures.

## Failure sensitivity and verification

Initial helper build caught incorrect stop property names and analyzer/nullability issues; corrected before behavioral execution. Initial thirteen cases passed, then coverage was refined to fifteen passing cases.

A temporary production mutation added hidden route age to the public Reaction window identity. Three normal-resumption cases failed during public action acceptance; the three last-CP controls passed. A narrower mutation then changed only the waiting player's published window identity. All three normal-resumption cases failed at `AssertTraces` with unequal retained collections, while the three controls passed. Output retained locally at `/tmp/brk-ac009-observation-mutation-test.log`. Both mutations were removed; production source matches `77a3203` byte-for-byte. No mutant is committed.

Final verification:

- `dotnet restore Sandtable.slnx` with unique binlog: all projects up to date.
- `dotnet build Sandtable.slnx --no-restore` with unique binlog: zero warnings/errors.
- `dotnet test --solution Sandtable.slnx --no-build`: **1,670 passed**, zero failures/skips.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-trait 'Boundary=UserSpace'`: **81 passed**, zero failures/skips.
- `dotnet format Sandtable.slnx --verify-no-changes --no-restore`: passed.
- Numeric audit, freeze audit (45 links) and `git diff --check`: passed.

Final build binlog: `artifacts/binlogs/brk-ac009-final-20260905-231156--41057--XVE0CW.binlog`. These execute all constituents of the local gate; the literal `just check` wrapper was not run. Prior Runner clean-run evidence remains tied to `5967949`; test-only follow-up does not relabel those artifacts.
