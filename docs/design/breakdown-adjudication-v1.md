# Breakdown adjudication v1 — design and task plan

**Status:** Owner accepted BRK-DEC-004–007 on 2026-09-05; BRK-TASK-001 contract freeze complete. Task 002 dormant Rules implemented; verification passed and independent review Ready. Task 003 dormant campaign contracts and certified Truck fixture implemented. Tasks 004–005 move/stop adjudication and Task 006 coherent public activation are implemented. Task 007 checked Runner migration is implemented; final verification and independent closeout are tracked separately.

**Governing contract:** [specification](../specs/breakdown-adjudication-v1.md),
[wire schemas](../specs/breakdown-wire-contract-v1.md), and
[fixture migration](../specs/breakdown-fixture-migration.v1.json). These supersede the exploratory
contract details below.

**Research:** [BRK-RSH-002 decision packet](../research/breakdown-adjudication-spike.md).

**Baseline:** ZOR-007 complete at `0512ec2`. **Initial supported profile:** Truck; bounded content
and placement scope accepted in BRK-DEC-006/007. Other profiles and full transport/repair rules are
explicit later gates. This is not a specification for all Section 21 behavior.

## Outcome and authority

The first supported vertical accepts exact BP-bearing moves, records a deliberate or forced stop,
adjudicates supported Truck cohorts once at the appropriate effective band, retains broken equipment
at an explicit location, and continues to the exact suspended authority position. Movement completion requires all stops drained; first-side Breakdown Determination completes
that segment and stops at
the existing Combat checkpoint. It neither fabricates Combat nor enables the continual cycle.

Core owns calculation, RNG, stop lifecycle, legal membership, loss projection and replay. Runner
selects only current public actions and records trusted evidence. No inference, remote service,
hosting scheduler, wall-clock deadline or new protobuf contract is required.

## Accepted decisions and next gate

BRK-DEC-001–007 are accepted. Owner accepted DEC-004–007 and their explicit capability exclusions
on 2026-09-05 after independent review 3. Task 001 froze governing requirements, acceptance IDs,
wire schemas and the activation/fixture matrix. Tasks 002–007 now follow that governing package; no runtime activation is included here.

## Shared accounting before adjudication

Extract one authority-side BP delta calculation from admitted content edge/terrain and weather
inputs. Ordinary and Reaction movement invoke the same calculation; they must not infer BP from CP.
Use reduced rationals and checked arithmetic. Preserve route transformation, directional hexside
provenance, exact cumulative BP, and exact Sandstorm subtotal. Non-cohort movement has no BP delta.
Rainstorm transforms route inputs, then contributes neutral column shift; it must not call the
current method that intentionally rejects Rainstorm as a shift.

Successor move events retain before/delta/after BP and source/rules identities alongside existing CP
and Reaction evidence. Projectors rederive the entire canonical event before atomic application.
A forged BP component rejects the whole move, leaving World, window and RNG unchanged. Move actions
and cost queries consume no RNG. Existing accepted v2/v1 move bytes retain their historical semantics.

## Stop and continuation model

Authority uses the wire contract's closed `BreakdownFlow` union: one executable stop plus an
optional deferred phasing route/stop while Reaction owns the interrupt. The separate Reaction window
continues to own frozen opportunities. Neither state can overwrite the other.

- A public owner `stop-element-movement` action records the current route's origin, last location,
  moved cohort membership, state/version/rules identity, and exact continuation. Movement Segment
  completion is available only after the one open phasing route has stopped and resolved.
- Reaction participant completion records a stop after its accepted steps. The participant cannot
  be selected again in that window; another participant cannot begin until the pending stop drains.
- System unavailable/timeout closure records an active participant's stop when needed, resolves the
  window, and retains the suspended phasing continuation. An active stop is drained before that
  continuation becomes executable. Empty/no-active closure creates no fictitious route.
- Under accepted DEC-005, a phasing stop coincident with a trigger defers its Breakdown resolution
  until the triggered window closes. A reactor stop can therefore precede that deferred phasing stop.
  The contract must retain both continuations explicitly, not overwrite the deferred stop.
- Forced Movement end records a stop once. Merely exhausting current legal moves must not silently
  skip completion. Repeating the same action or resubmitting an old handle cannot make another stop.

The governing specification gives each transition an explicit diagram and closed discriminator.
One record with
arbitrary recursive continuations is not acceptable; allow only the bounded normal/phasing-deferred/
reactor-stop combinations above and reject impossible mixtures. Route-origin lifetime begins with
first movement after the previous resolved stop, so later stops cannot reuse a stale origin.

## Check transaction and RNG evidence

Use a single System `resolve-breakdown-stop` action for a committed stop. It freezes all supported
check inputs and processes eligible groups in canonical authority order in one accepted event.
The same resolution transition occurs for an admitted stop with no eligible roll, keeping outward
transition count independent of hidden group cardinality. Do not publish one action per hidden cohort.

Eligibility requires positive working points, raw BP greater than three, a non-null effective band,
and a band above retained highest effective checked band. The existing band comparison and clamping
must be explicit. A zero-loss roll still advances checked-band memory; a no-roll case consumes no
randomness. A raw 71+ cohort cannot gain repeated rolls merely by accumulating more within that
capped effective band. Checks never discharge cumulative stage BP.

For each eligible check, use two calls to `SandtableRandom.RollD6` on the authoritative stream.
Retain ordered faces, sequential coordinate, actual cursor before/after, lookup label, accepted
fraction identity, count basis, losses, checked-band before/after, and complete rules provenance.
Reconstruction reruns rejection sampling; accepting a caller-supplied die or assuming cursor+2
violates this design. RNG and every group delta commit together or not at all.

Admit only roots certified under the public capability profile below, and validate its invariant
after every accepted transition. No legal stop in an admitted history can become unsupported merely
because hidden placement/grouping facts differ. Invalid roots reject before player-visible campaign
creation; malformed/stale/forged later submissions emit no event and change no RNG or World.
A violated invariant indicates invalid authority, not an ordinary zero-loss or hidden-dependent
unsupported result. The wire contract freezes those admission/rejection results.

## Working points and broken lots

Introduce authority `BrokenVehicleLot` records with stable identity derived from check/event identity,
cohort/type, owner, count, stop location and provenance. Cohort working counts and lot totals conserve
initial admitted points while no capture/repair authority exists. A later working-cohort move never
moves old lots. Lots are not combat elements, cannot acquire legal Movement/Reaction actions, and
do not invent ZOC or stacking strength.

Under accepted DEC-006, the initial public capability profile admits at most one unladen standalone
Truck cohort per side in the entire campaign, with no passengers/cargo or commands capable of
creating, splitting or merging such cohorts. This stronger structural bound prevents grouped/mixed
checks regardless of hidden BP, eligibility or co-location. Zero-working cohorts cannot move; survivors use
only their remaining admitted capacity. Do not apply a blanket mobility change to current infantry
without implementing its transport model. Existing motorized-infantry fixtures are not admitted under this new public profile; their BP
movement accounting remains testable through dormant successor contracts. Positive public
Reaction-stop/forced-close vectors use non-cohort combat reactors and verify zero-roll continuation;
a positive motorized-infantry Reaction loss awaits transport support.

Under accepted DEC-007, the same public profile excludes any combat unit, represented formation,
or combined combat grouping larger than a single battalion. Use a conservative structural bound,
including aggregates: absence of a larger individual leaf is insufficient. Certification checks
all authoritative components at creation, and every admitted transition must preserve this bound.
Future formation/capacity-changing actions cannot enter this profile without new approval. Its
identifier and restrictions are public setup metadata; they are not inferred from secret current
positions. Thus every route-start placement exception is impossible, irrespective of hidden enemy
movement, friendly blockers or later Reaction. A legal stop always resolves through the same
supported transition; detailed invalid-root reasons remain trusted diagnostics.

General origin placement stays deferred. That later package must capture immutable qualifying
threat/blocker facts or a provenance-bearing predicate immediately before the route's first move,
retain it through Reaction/snapshot/replay, and reset it only at the next route start. It must test
an enemy entering/leaving range and blocker changes after that start. Current origin location alone
cannot support broader historical replay, and current-state geometry is not a substitute. The
narrow profile needs no hidden start-time predicate because its invariant makes that predicate
uniformly false. Capture, relocation choices, towing and repair stay closed. Later stages reset BP/check memory only
through a separately defined stage-entry transition; broken lots and point conservation persist.
No such later-stage advance is claimed by the first vertical.

## Observation and artifact boundary

Normal owner projection may extend existing own risk facts only through an approved Observation
successor. Reaction projection must retain current nonlinkability: no stable cohort IDs, raw BP
ledgers, broken-lot IDs, count basis or private placement inputs appear in reacting move options.
Both player histories receive only approved stop/wait/continuation facts. Trusted Chronicle retains
all calculations. Tests compare transcript shapes for hidden group counts and compare player bytes
under hidden bindings, BP, eligibility and placement-input permutations.

Expand the versioned disclosure manifest before outward types. System action is a state-scoped opaque
capability, never an authority cohort selector exposed to a player. Extend strict Runner event
schemas, reconstruction and fresh-session re-adjudication together. Rehashed forged faces, cursor,
percentage, BP, lot locations, check memory or continuation must all fail semantic admission.

## Versioned contract freeze

Affected identities include the Breakdown rules artifact/ruleset; World and Snapshot; campaign
creation; normal/Reaction move events; stop/check/completion events; relevant action/policy and
Observation/projected-history schemes; and Runner strict event admission. Content needs a successor
only where new schemas or admission invariants require it; adding a fixture alone is not a reason
to change every content schema. The wire contract enumerates exact predecessor/successor versions
and hash dependencies, including disclosure-manifest changes, before consumers are generated.

Keep successor contracts dormant until replay, projection, actions and observation agree. Activation
rejects legacy-only and every mixed identity set on all current creation/readback/submission paths.
Historical code/bytes remain historical; old BP-zero histories cannot be treated as correctly
accounted successor histories. No inference from CP or silent history backfill. The supported rollback
is reverting the activation as a whole, with successor artifacts clearly identified and rejected by
older current readers; no dual-current downgrade mode.

## Ordered task graph

| Task | Deliverable and owner modules | Prerequisite / exit evidence |
| --- | --- | --- |
| `BRK-TASK-001` | **Complete:** accepted decisions, governing spec, finite stop states, exact schema deltas/identities, public certification/rejections and fixture migration | Owner accepted DEC-004–007 after design review 3; numeric and freeze audits pass; new freeze bytes self-checked, not covered by historical independent reviews |
| `BRK-TASK-002` | **Complete:** dormant outcome rules, exact loss arithmetic, source/ruling factories; `Cna.Core/Rules` | [Evidence](../research/breakdown-outcome-rules.md): 324 cells, exact rounding/one-point, no-roll and artifact negatives; manifest stays current until coupled activation |
| `BRK-TASK-003` | **Complete:** dormant Content/Setup/World/Snapshot/creation/lot/stop contracts, sequence/catalog 4 and bounded Truck fixture; `Content`, `Campaigns`, `Rules` | [Evidence](../research/breakdown-campaign-contracts.md): strict codecs, conservation, certified creation and state negatives, mixed-version rejection; event transition proofs remain Tasks 004–005 and public activation remains Task 006 |
| `BRK-TASK-004` | **Complete:** shared BP deltas and dormant successor ordinary/Reaction events/projectors | [Evidence](../research/breakdown-move-accounting.md): terrain/route/weather vectors, atomic forged-delta rejection, preserved CP/Reaction behavior, routes/forced-stop precedence and immutable lots |
| `BRK-TASK-005` | **Complete:** dormant stop/check authority, exact RNG replay and continuation | [Evidence](../research/breakdown-stop-adjudication.md): nested phasing/reactor/forced-close transitions, zero-roll vs zero-loss, immutable lots, no duplicate costs/draws |
| `BRK-TASK-006` | **Complete:** Observation, action membership, disclosure manifest, projected history and atomic public activation | [Evidence](../research/breakdown-public-activation.md): certified creation, privacy/forgery/identity matrix, strict Core/Runner admission and boundary gate; stop at unsupported Combat |
| `BRK-TASK-007` | **Implemented; final review pending:** checked Runner fixtures, strict bundles and research reconciliation | [Closeout evidence](../research/breakdown-runner-closeout.md): exact accounting/lots/continuation, full gate and two clean runs; independent review requires additional authorization after implementation review 3 of 3 |

Tasks 001–006 are complete; Task 007 implementation is in place, with verification recorded in its closeout evidence and independent review pending. Safe independent research lanes
are grouped allocation,
transport consequences and origin-placement/capture/repair; they do not modify these shared contracts.

## Acceptance matrix

| ID | Required evidence |
| --- | --- |
| `BRK-AC-001` | All legal dice coordinates and nine outcome columns; illegal coordinates, gaps and duplicate outcomes reject |
| `BRK-AC-002` | Exact BP terrain/route/hexside costs, Rainstorm transformation, Sandstorm half threshold, ordinary/Reaction equality |
| `BRK-AC-003` | Raw <=3, BAR-shifted below surface, unchanged checked band and no working points produce no roll; cap and higher-band behavior exact |
| `BRK-AC-004` | Zero-loss eligible roll retains exact RNG/check memory; fractional loss/one-point exception follow accepted DEC-004 |
| `BRK-AC-005` | Same-seed events/snapshots/RNG match; rejection-sampling cursor exceeds two when appropriate; invalid submission emits nothing |
| `BRK-AC-006` | Deliberate/forced phasing stops, final-CP Reaction retaining active route until completion/System closure, active fallback and deferred phasing stop each resume exact continuation once |
| `BRK-AC-007` | Working points plus persistent lots conserve counts; later survivor movement leaves lots fixed; zero-working cohorts cannot move |
| `BRK-AC-008` | Public profile rejects multiple same-side Truck cohorts, passenger/cargo capability, and any larger combat unit/aggregate at creation; every transition preserves certification. No valid stop has hidden-dependent support. Broader start-time placement evidence explicitly deferred |
| `BRK-AC-009` | Within the admitted profile, hidden binding/BP/eligibility/position/blocker permutations preserve approved player transcript, including progress and action availability. Creation negatives cover excluded larger/grouped worlds; disclosure manifest and boundary-check pass |
| `BRK-AC-010` | Every rehashed authoritative component mutation rejects through strict readback and fresh-session re-adjudication |
| `BRK-AC-011` | All-legacy, all-successor and each partial-mixture identity vector checked across current boundaries |
| `BRK-AC-012` | Checked public Truck trajectory reaches exact Combat boundary; repeated clean artifacts match; later-stage reset remains explicitly unsupported |

The governing specification binds these acceptance criteria to requirement and task IDs.
Research experiment validates the numeric
transcription and conditional arithmetic only; it does not satisfy future Core acceptance tests.

## Frozen capability consequence

Accepted battalion-size limits exclude public positive ZOC because current ZOC needs stacking >1.
The migration inventory preserves all original fixture bytes as historical at activation, requires
bounded successors for 13 of 15 Reaction children, and explicitly defers positive local/remote ZOC.
Truck ordinary moves are admitted separately; combat-only Reaction triggers remain unchanged.
Task 006 is complete. Task 007 checked successors are implemented; final independent closeout remains pending.

[Task 001 verification evidence](../research/breakdown-contract-freeze-checks.md) records exact
specification checks and their limits.
