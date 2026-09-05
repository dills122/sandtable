# BRK-TASK-004 — Dormant move accounting

**Status:** Complete, 2026-09-05; full repository gate passed.
**Branch:** `codex/breakdown-adjudication-design`; prerequisite `c05444a` (Task 003).
**Authority:** [behavior](../specs/breakdown-adjudication-v1.md),
[wire contract](../specs/breakdown-wire-contract-v1.md), [plan](../design/breakdown-adjudication-v1.md).

## Implemented boundary

Ordinary ElementMoved 3 and ReactingElementMoved 2 now derive and replay immutable BP accounting
against Content 6 / Snapshot 11 / Ruleset 9. The event codec preserves predecessor field order,
appends the frozen ruleset/accounting/flow suffix and dispatches by exact event-type/version pair.
Existing current factories, codecs, public actions and runtime identities remain unchanged.

The shared calculator applies normalized terrain BP, Rainstorm Road-to-Track conversion, route
operation and every directional hexside addition in that order. It retains original/effective
route, exact rational before/delta/after amounts, Sandstorm subtotal and sorted source union.
Both movement paths call it; their CP calculation remains the predecessor calculation. For example,
the certified fixture's west-to-center desert/upslope step costs eight CP and 26 BP. It draws no RNG.

Ordinary moves admit certified unladen Trucks with working points, freeze route origin at the first
step, retain identity on continuation and reject switching movers before stop resolution. CP
exhaustion records a phasing stop with post-step ledger inputs. A combat/HQ adjacency trigger places
that stop behind the Reaction window when both occur. Truck movement cannot trigger Reaction.

Reaction movement rederives current public capability hashes, exact available edge costs and frozen
opportunity binding. It retains the phasing continuation, updates the single active reactor route,
never opens a nested window and remains active when its final CP is spent. Explicit participant
completion or System closure will record the reactor stop in Task 005.

Replay reconstructs the full expected event from prior authority and compares canonical bytes before
projecting a new immutable World/Snapshot. Consistent forged BP totals, provenance, weather or omitted
accounting therefore fail even when the event passes structural readback. Projection preserves
working/broken counts, checked-band memory, existing lot objects/locations and RNG state.

## Invariants and limits

- Content/World certification applies before moves and to their resulting state. Same-side combat
  stacking cannot exceed the accepted battalion bound; zero-working Trucks cannot move.
- That profile cannot produce positive ZOC: qualification requires stacking greater than one.
  The dormant factory returns no controlled locations only after enforcing profile certification.
  Existing positive-ZOC rules and historical tests remain intact; no broad-world bypass was added.
- Certified combat/HQ elements have no vehicle cohort. Actual Reaction events thus carry empty
  accounting. A direct calculator-integration vector proves positive-cohort evidence equality between
  ordinary and dormant Reaction accounting seams; it does not claim a publicly admitted Truck or
  motorized-infantry Reaction episode.
- Snapshot 11's trigger certification now admits Headquarters as well as CombatUnit, preserving
  inherited HQ trigger semantics. Frozen reacting participants remain CombatUnit only.
- Event codecs validate canonical shape. Content-derived evidence is authoritative only after
  reconstruction/projector validation. No public successor dispatcher, completion/close event,
  explicit stop action, loss transaction or RNG replay is implemented by this task.

## Executable evidence

| Test class | Coverage |
| --- | --- |
| `BreakdownAccountingTests` | 25 cases: exact terrain/routes/weather; multiple directional additions; totals and Sandstorm attribution; unsupported/binding negatives; applying twice; strict codec and equality |
| `BreakdownMovementTests` | 12 cases: Truck versus combat/HQ admission; CP/BP separation; zero working points; route identity/switching; forced/deferred stops; legacy sequence and impossible flow rejection |
| `BreakdownMoveReplayTests` | 19 cases: strict event readback and predecessor rejection; rederived forged evidence/capabilities; unchanged RNG/lots/counts/memory; shared accounting seam; final-CP Reaction |

Initial executable RED: 25 accounting, seven ordinary and eight replay cases failed against stubs.
The first build needed explicit `ContentEdgeFeature` constructors in params arguments. Integration
then exposed a mistaken prefixed ruleset-hash guard, corrected to exact unprefixed Ruleset 9.
Further RED tests caught four ordinary event sequence/flow gaps and the HQ trigger certification
mismatch. All received focused fixes. Two test expectations were corrected: source forgery must
preserve canonical sort order to exercise replay, and west-to-center uses desert/upslope BP 26.

Final gate on 2026-09-05:

| Command | Result |
| --- | --- |
| `dotnet restore Sandtable.slnx '-bl:artifacts/binlogs/brk-004-final-restore-{}.binlog'` | Pass; all projects up to date |
| `dotnet build Sandtable.slnx --no-restore '-bl:artifacts/binlogs/brk-004-final-build-{}.binlog'` | Pass; 0 warnings, 0 errors |
| `dotnet format Sandtable.slnx --verify-no-changes --no-restore` | Pass |
| `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-trait 'Boundary=UserSpace'` | 48 passed, 0 failed, 0 skipped |
| `dotnet test --solution Sandtable.slnx --no-build` | 1,462 passed, 0 failed, 0 skipped; 1m 40s |
| `python3 docs/research/verify-breakdown-contract-freeze.py` | 14 historical fixture hashes, 15 Reaction children, 15 predecessor schemas, 10 requirements / 12 mappings, 32 links |
| `python3 docs/research/verify-breakdown-outcomes.py` | 324 cells, 9 source probes, 606 bounded proposed-loss combinations |
| Changed-document local-link check and staged `git diff --check` | Pass; 285 local links |

Full suite includes all 56 new cases above. Local ignored diagnostic binlogs:
`brk-004-final-restore-20260905-174726--81092--4hmIF+.binlog` and
`brk-004-final-build-20260905-174743--81150--JR_g+R.binlog`.
No dependency, fixture-byte or public runtime change was needed.

## Remaining sequence

Task 005 owns stop/check authority, participant completion/window closure, continuation resolution,
first-side Breakdown completion and exact RNG/history replay. Task 006 owns coherent public
activation and privacy. Task 007 owns checked Runner fixture migration and end-to-end closeout.

Peer implementation review used the task's internal workers; no fresh independent-review instance
was started. Implementation review budget remains 2 of 3; research/design remains closed at 3 of 3.
