# BRK-TASK-005 — Dormant stop adjudication

**Status:** Complete, 2026-09-05; full repository gate passed. [Independent checkpoint 3](../reviews/brk-progress-review-3.md): Ready with non-blocking follow-ups; documentation follow-up corrected.
**Branch:** `codex/breakdown-adjudication-design`; prerequisite `d511ec7` (Task 004).
**Authority:** [behavior](../specs/breakdown-adjudication-v1.md),
[wire contract](../specs/breakdown-wire-contract-v1.md), [plan](../design/breakdown-adjudication-v1.md).

## Implemented boundary

Dormant Ruleset 9 / Snapshot 11 now records deliberate stops, resolves immutable cohort batches,
completes Reaction participants, closes Reaction windows and advances idle first-side Movement
through Breakdown to the first Combat checkpoint. Public dispatch, creation and observations still
use their current identities; Task 006 owns coupled activation and privacy.

The collection resolver validates every cohort and the RNG algorithm before drawing. Checks sort by
vehicle/profile/cohort. No-roll precedence is no working points, raw BP at most three, below the
check surface, then a band no higher than checked memory. Eligible checks consume two authoritative
d6 draws with actual rejected-byte cursors, derive the printed result and exact rational loss,
update checked-band memory even for zero loss, and create only positive lots at the stop destination.
Existing lots remain stationary; working plus broken points remain conserved. Empty and no-roll
batches leave RNG untouched.

Lifecycle factories bind commands to current side/System capabilities. Participant completion clears
and resolves the active slot while preserving the open window until the reactor stop resolves.
Active timeout/unavailable closure records a closed-window reactor stop. Resolution then resumes the
phasing route or its deferred forced stop. An exhausted active reactor still requires explicit
completion or System closure. Pending stops block completion and closure. Segment completion requires
idle authority and changes neither world, ledgers, lots nor RNG.

The strict dormant dispatcher accepts exact successor event/version pairs. Its projector reconstructs
the expected event from prior certified authority and compares canonical bytes before applying any
immutable state change. Dice, cursors, loss arithmetic, checked memory, lots, provenance and continuation
are derived evidence, never caller-selected patches. Replay starts from a certified Movement
checkpoint and reaches first-side Combat entry; creation/preamble dispatch and public history wiring
remain Task 006, and Combat execution remains unsupported.

## Executable evidence

| Test class | Coverage |
| --- | --- |
| `BreakdownCheckResolutionTests` | 16 cases: four no-roll statuses, fixed dice/loss vectors, rejected bytes, zero-loss memory, single-point exception, Sandstorm half threshold, Rainstorm, ordering, malformed batches and strict check codec |
| `BreakdownLifecycleTests` | 19 cases: route/System handles, participant completion, active timeout/unavailable, final-CP completion, pending-stop guards, nested replay and forged/stale authority |
| `BreakdownResolutionReplayTests` | 12 cases: positive/empty resolution, canonical-but-wrong dice, loss/cursor/lot/memory/source/continuation mutations and two exact replays through both segment completions |

Observed executable RED: 16 check, nine lifecycle and ten resolution cases failed against stubs.
One initial test compile typo and one analyzer-required guard syntax correction preceded the integrated
build. Integration tests needed two fixture corrections: seed 0 supplies a positive loss, and surviving
Trucks move back west because east contains opposing authority. These were test assumptions, not
production rule changes. Further lifecycle/integration cases were added before the final gate.

Actual certified Reaction elements have no vehicle cohorts, so real Reaction stop histories draw no
RNG. Positive-cohort ordering/loss vectors exercise the pure collection resolver; they do not claim a
public motorized-infantry Reaction episode or bypass content certification. Public positive ZOC stays
deferred by the accepted profile. Historical fixtures and active runtime identities remain intact.

## Verification

Final gate on 2026-09-05:

| Command | Result |
| --- | --- |
| `dotnet restore Sandtable.slnx '-bl:artifacts/binlogs/brk-005-final-restore-{}.binlog'` | Pass; all projects up to date |
| `dotnet build Sandtable.slnx --no-restore '-bl:artifacts/binlogs/brk-005-final-build-{}.binlog'` | Pass; 0 warnings, 0 errors |
| `dotnet format Sandtable.slnx --verify-no-changes --no-restore` | Pass |
| `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-trait 'Boundary=UserSpace'` | 48 passed, 0 failed, 0 skipped |
| `dotnet test --solution Sandtable.slnx --no-build` | 1,509 passed, 0 failed, 0 skipped; 1m 40.563s |
| Individual `--filter-class` runs for the three classes above | 16 + 19 + 12 passed; 47 new cases total |
| `python3 docs/research/verify-breakdown-contract-freeze.py` | Pass: 14 historical fixture hashes, 15 Reaction children, 15 predecessor schemas, 10 requirements / 12 mappings, 34 links |
| `python3 docs/research/verify-breakdown-outcomes.py` | Pass: 324 cells, 9 source probes, 606 bounded proposed-loss combinations |

Local ignored binlogs: `brk-005-final-restore-20260905-182129--88645--bsq6TE.binlog`
and `brk-005-final-build-20260905-182146--88855--wLGap3.binlog`.
Changed-document local links and staged whitespace checked before commit.

## Remaining sequence and review

Task 006 owns coherent public activation and privacy. Task 007 owns checked Runner fixture migration
and end-to-end closeout. No BP reset, repair, later-stage execution or Combat adjudication is included.
Fresh independent checkpoint 3 of 3 reviewed completed progress through Task 005.
No implementation defect found; accepted documentation status correction is retained with the report.
Implementation review loop is now closed; no automatic fourth instance. Research/design review remains closed at 3 of 3.
