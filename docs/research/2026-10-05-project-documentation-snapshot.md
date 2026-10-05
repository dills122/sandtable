# Project documentation history — 2026-10-05

This dated snapshot preserves the former README implementation ledger during the first-release
documentation cleanup. It records historical claims, contract identities, review outcomes and
bounded evidence; it is not the current delivery ledger. Consult the
[roadmap](../roadmap/pre-alpha-roadmap.md) and
[first-release audit](2026-10-05-first-release-audit.md) for current status.

## Detailed project status


> [!IMPORTANT]
> Sandtable is pre-alpha infrastructure, not yet a playable adaptation of the published game.

Executable product and forward contract work are intentionally different. Today, public Rules9
authority and checked Runner evidence stop at first-side Combat **entry**. Parent003 frozen contract
evidence composes 28 selected future Combat/cycle histories and exact Task004 handoff, but no Combat
or Reserve Release runtime is registered on the public Rules9 path. Private Core adapters are
implemented at the bounded checkpoints summarized above. The
[pre-alpha roadmap](../../docs/roadmap/pre-alpha-roadmap.md#current-delivery-status) is the canonical
delivery ledger and defines the status vocabulary used below.

The current foundation can create a campaign from an exact ruleset, setup, Content Pack, and
scenario; project the scenario's initial mutable element locations; resolve Initiative
Determination and both admitted no-obligation Naval Convoy checkpoints; let the initiative holder
declare whether to act first or last in Operation Stage 1; resolve Weather; emit authoritative
events; explicitly resolve empty Organization, Naval Convoy Arrival, Fleet Assignment, and Fleet
Repair obligations; adjudicate the first-acting side's Reserve Designation; execute supported
first-side Movement; open, adjudicate, close, and resume bounded ZOC Reaction interrupts; complete
Movement through Breakdown Determination to unsupported first-side Combat; resolve explicit
route stops with exact BP checks and persistent broken-vehicle lots; and replay those events to byte-identical state. Reserve authority now carries per-element status,
owner-only observation, exact acting-side candidates, closed command mapping, bounded checkpoints,
and canonical designation/completion events. The Movement foundation additionally records exact
per-Operation-Stage expenditure/Cohesion state and opaque one-to-one map representations. It now
also carries typed move/completion candidates, deterministic action identities, an exact side-safe
cost breakdown, strict non-authoritative readback, internal authoritative non-contact move
adjudication/replay, and observation-derived public action membership with exact submission
revalidation. Ruleset manifest contract 9, setup schema 6, snapshot contract 11,
Campaign World snapshot contract 6, Campaign Observation contract 7, legal-action-set contract 2
with policy v3, and Content Pack schema 6 / canonical format v5 use original synthetic
rules laboratories to develop game systems without redistributing published assets.
Campaign Observation derives deterministic side-safe public topology, audience-visible turn
revision, exact own mobility/ledger/Reserve and approved vehicle-risk facts, plus only opaque
opposing representation/location rows and the source-unmapped current-ZOC aggregate. It exposes neither complete Content identity,
real opposing bindings/force facts, nor hidden Reserve counts. Legal Actions v1 exposes those
mechanics through an opaque campaign-authority handle,
deterministic system/side action sets, exact-audience membership enforcement, and side-safe
acceptance receipts. Weather Determination v1 resolves corrected source-cited Weather through that
same boundary and records pair-keyed evidence. Operation-Stage Entry v1 then resolves only the four
explicitly admitted empty obligations through mechanic-specific actions and events. Side-safe
queries derive the Reserve audience from the recorded first/second actor order while its symbolic
sequence position keeps `ActiveSide` unset; current Movement materializes that resolved side because
successor Movement and Reaction identities bind it. Raw snapshots, commands, events, content
context, projection, and replay are not public mutation seams.

Campaign Observation 7 uses the
`sandtable.observation.breakdown-side-safe.v1` policy, one canonical source-unmapped aggregate
of apparent enemy-controlled locations, exact owner-visible Movement-ended membership, and a closed
normal/phasing/reacting/Breakdown-waiting decision-state union. Pending stops expose one
System capability and generic player waiting; own lot summaries contain cohort/location counts
without lot IDs or evidence, and reactor waiting omits owner rows that could reveal bindings. Its reacting view contains only the apparent trigger,
the observer's current state-scoped capability handles with closed current move-option/cost
capabilities, and the optional active own participant. Raw element Movement, ledger, Cohesion,
Reserve, mobility, organization, and stacking inputs remain inside Core. Reacting construction and
readback reject identity-bearing root owner-element rows, so no representation-to-element binding
is published. Admission also recomputes capability-bound opportunity handles, validates published
route/hexside cost claims against their selected edge, and binds reacting/phasing decision labels
to the observer's relationship with the active side. Both sides receive the same audience-safe
window handle, never the authoritative
window identity; phasing receives only generic waiting while retaining its ordinary owner facts. A
versioned disclosure manifest and mandatory `boundary-check` gate register
this outward surface and protect retained cross-state transcripts from copied-fingerprint joins.
A distinct strict projected-history contract retains the same redacted decision state without
authority bindings, source mappings, evidence, or internal reasons. The current action layer derives
topology-local ordinary Movement and first/later Reaction movement, participant
completion, player decline, and reason-specific System close membership with canonical identities,
strict current readback, and unpublished typed submission intents. Public Core query, submission,
checkpoint, serialization, and replay paths now use this complete successor set; bounded Exercise
Runner Reaction controllers implement `ZOR-TASK-007A`: explicit bounded policies support
participant ordering, one/two-step episodes, decline/subset close, and System fallback.
Current Movement completion preserves accepted Reaction costs through the Breakdown boundary;
explicit Breakdown completion advances to unsupported Combat without another draw.
The historical `ZOR-TASK-007B` package closed with strict evidence, matching clean-run fingerprints,
and a Ready independent review; see [historical Reaction trajectories](../../docs/research/simulator-reaction-trajectories.md).
Owner accepted Breakdown decisions `BRK-DEC-004`–`007`. Tasks 001–005 supplied the frozen contracts,
certified world, BP accounting and deterministic stop lifecycle. [Task 006 public activation](../../docs/research/breakdown-public-activation.md)
activates that complete identity set, Observation 7, projected history 2 and disclosure manifest 2.
Current creation, checkpoints and event admission reject legacy or mixed contracts; retained
Initiative, Weather and preamble evidence is recomputed, while full history is verified separately
by replay. Public queries stop at first-side Combat entry. Positive ZOC, motorized-infantry losses
and later-stage reset remain outside the certified profile. Task 007 implements fourteen checked
successor manifests and a Truck study, using certified battalion, Reaction and Truck-only inputs.
The [original fixtures](../../docs/specs/breakdown-fixture-migration.v1.json) remain historical with
unchanged bytes. The profile permits zero cohorts: `land.breakdown-cohorts` is required exactly
when a pack contains a cohort; all other capability, organization and stacking checks remain strict.
[Task 007 closeout](../../docs/research/breakdown-runner-closeout.md) records 1,655 passing tests and two matching clean runs of 47 campaigns each. [AC-009 follow-up](../../docs/research/breakdown-transcript-privacy.md) adds fifteen transcript/privacy cases, bringing verification to 1,670 tests and 81 boundary cases. [Review 5](../../docs/reviews/brk-followup-review-5.md) accepts the bounded coverage; its status-only follow-up is corrected. Tasks 006–007 are complete within the certified profile.

The local `Cna.ExerciseRunner` supports that synthetic rules-laboratory path as either one
bounded, deterministic **Exercise** or one serial **Maneuver**. An Exercise uses a fresh opaque Core
capability, selects only current legal actions, stops at its exact declared boundary, proves both
event-history reconstruction and fresh-session re-adjudication, and writes a manifest-last
`trusted-authority` evidence bundle. The original Organization, Reserve and Reaction checked fixtures are historical. Current regression
tests and checked `.breakdown.v1` successors use certified battalion, Truck and contact inputs. A serial-unpaired Maneuver
strictly admits one canonical ordered `serial-unpaired` manifest,
derives explicit child identities from its sole parent root seed, and runs each child in process
through the same coordinator. Each completed child bundle is read once for semantic validation and
identity-matched aggregation; snapshot facts are accepted only after the complete Core-owned
snapshot/world decoder validates their canonical structure. The resulting transactional report
separates deterministic counts, outcomes, and fingerprint material from noncanonical timing/path
diagnostics and is strictly read back before completion is claimed. Compact, forensic, and debug
Exercise detail tiers expose progressively richer evidence without changing simulation truth. The
current two-setup serial-unpaired Maneuver retains predetermined and contested initiative paths.
A checked six-child controller matrix crosses `act-first`/`act-last` with Reserve
`none`/`one`/`all`, using two non-cohort battalions per side so all three choices remain distinct.
The corresponding Movement matrix includes explicit route stops and System resolution before
Breakdown entry. The [thirteen-child Reaction successor](../../scenarios/maneuvers/rules-lab.reaction.serial.breakdown.v1.json)
retains ordering, one/two-step episodes, decline, active System closure and later-trigger recurrence
with separated battalion reactors. The two historical positive-ZOC children remain deferred from
public authority. Optional `serial-paired` Maneuvers run
isolated baseline and candidate arms
sequentially from identical declared initial conditions, initial role-specific random streams,
campaign creation inputs, build cohort, and initial snapshot. Its strictly read-back comparison is
descriptive only: trajectories and random consumption may diverge after the first differing choice,
and it makes no causal, statistical-significance, gameplay-balance, recommendation, or
synchronized-post-divergence claim. Runner model controllers and side-safe exports are not
implemented.

The checked Exercise and serial-unpaired Maneuver profiles use manifest v2, with unpaired report
scheme `sandtable.maneuver-report.v1`; the separate paired Maneuver uses
`sandtable.paired-maneuver-manifest.v1` and
`sandtable.paired-maneuver-report.v1`. Current successors use Ruleset 9, Snapshot 11, World 6, strict
`trusted-authority` evidence admission, and deterministic v2 controller configuration identity.

The first two historical simulator studies recorded repeated Movement-terminal determinism,
counterbalanced-order timing, and contested root seeds 0-31. Every sampled run passed strict
readback; the results also show that future back-testing needs explicit act-last and Reserve
none/one/all controller profiles rather than seed variation alone. See
[Baseline 1](../../docs/research/simulator-baseline-1.md) and
[Baseline 2](../../docs/research/simulator-baseline-2.md). The follow-on
[controller-policy matrix](../../docs/research/simulator-controller-matrix.md) closes that explicit
coverage gap with 6/6 strictly read-back trajectories and a repeatable aggregate fingerprint.
The merged [Movement trajectory study](../../docs/research/simulator-movement-trajectories.md) retains its
pre-Reaction 48-trajectory baseline across six controllers and four deliberate seed probes. Under
the historical Rules 8 authority, Reserve-none repeats stopped at the opened Reaction window while
the other profiles retained exact Breakdown evidence. The historical follow-on
[Movement cost-sensitivity study](../../docs/research/simulator-movement-cost-sensitivity.md) compared
stable-route and lowest-public-cost policies: its stable arm failed at Reaction after a cost-8 move,
while its lowest-cost arm completed a 1/2-plus-1 route. Current paired cost successors use
unladen Trucks and retain exact CP, BP and stop evidence. The added
`act-first-reserve-all-move-each-once-by-lowest-cost-then-complete` policy selects one lowest-cost
public move per eligible element. `act-first-reserve-all-repeat-highest-cost-stops-then-complete`
repeats highest-cost public moves, stops after each edge, and drains each pending System resolution.
These are bounded simulator policies; Core still determines legality and every result.

Contact, combat, published scenario content, persistence, and the Maproom
player interface remain future work.

The reviewed Player Intent Composer is also future work. After the movement/contact/combat skeleton
proves one representative multi-field decision, a no-model prototype will validate contextual
suggested approaches, a private typed draft, bounded clarification, deterministic Staff planning,
and hot-seat isolation. Deterministic Maproom integration belongs in Sprint 8; Needle or any other
parser remains behind a post-MVP evidence gate and cannot block the playable campaign.

The current delivery boundary is:

See the [current checkpoint and next gates](../../docs/roadmap/pre-alpha-roadmap.md#current-checkpoint-and-next-gates)
for contract versus runtime status, the Combat simulator gate, and the accepted bounded Orleans
[investigation](../../docs/research/orleans-publication-feasibility.md), now complete with a bounded Rules9 probe.
Its production publication/storage proposal remains unapproved. Combat contract checkpoints through
003C3c/D2a/D2b private checkpoints and D2c.1 successor/opening contracts are complete;
[D2c.2a opening provenance](../../docs/specs/combat-opening-preamble-v1.md) now reaches Weather entry
from validated Created11 bytes. [D2c.2b Weather](../../docs/specs/combat-weather-v1.md) now reaches Organization
entry. [D2c.2c stage entry](../../docs/specs/combat-stage-entry-v1.md) now reaches Reserve entry;
[D2c.2d Reserve designation/completion](../../docs/specs/combat-reserve-designation-v1.md) now derives
actual first-cycle opening. [D2c.3a inherited Movement](../../docs/specs/combat-inherited-movement-v1.md)
now derives ordinary Move4 from that history. [D2c.3b route lifecycle](../../docs/specs/combat-inherited-movement-lifecycle-v1.md)
adds deliberate stop, empty-cohort resolution and Movement completion with actual end proof.
[D2c.3c Breakdown completion](../../docs/specs/combat-inherited-breakdown-completion-v1.md) now reaches
actual Combat entry. [D2c.3d actual-entry selection](../../docs/specs/combat-inherited-selection-v1.md)
now admits the moved CP12/14 state and closes its zero-candidate selection without a decision.
[D2c.3e no-attack traversal](../../docs/specs/combat-inherited-no-attack-v1.md) carries that closure through
six exact structural completions to same-slot Reserve Release without material state change.
[D2c.3f Reaction trigger](../../docs/specs/combat-inherited-reaction-trigger-v1.md) instead replays one
actual Move4 prefix and opens one frozen-opportunity Reaction interrupt for either owner.
[D2c.3g Reaction lifecycle](../../docs/specs/combat-inherited-reaction-lifecycle-v1.md) moves that sole
participant once, completes it through mandatory empty-stop resolution, and resumes phasing only
after no-eligible closure.
[D2c.3n direct Reaction closure](../../docs/specs/combat-inherited-reaction-closure-v1.md) instead closes
either exact trigger by reacting-owner decline or reason-specific System unavailable/timeout and
resumes the same suspended phasing route without material effects.
[D2c.3o active Reaction fallback](../../docs/specs/combat-inherited-reaction-active-fallback-v1.md)
instead starts after the participant's first move, closes active authority through reason-specific
System unavailable/timeout, resolves the mandatory empty stop, and only then resumes phasing.
[D2c.3p active Reaction second move](../../docs/specs/combat-inherited-reaction-second-move-v1.md)
instead advances that same active participant from rear to supply at cumulative CP2→4, preserving
route identity and active opportunity for later completion.
[D2c.3q Reaction movement completion](../../docs/specs/combat-inherited-reaction-movement-completion-v1.md)
then explicitly completes that exact CP4 participant, resolves its mandatory empty stop, closes the
exhausted window, and resumes the original phasing route.
[D2c.3h Reserve cycle entry](../../docs/specs/combat-inherited-reserve-cycle-v1.md) instead carries each
owner's real held Reserve-I unit through a no-move first cycle to same-slot Reserve Release while
preserving designation history, location and CP0.
[D2c.3i inherited Reserve Release](../../docs/specs/combat-inherited-reserve-release-v1.md) opens the
actual release window, records owner release-I and completes it with the exact pending ordinal-2
Movement exception. [D2c.3j armed continuation](../../docs/specs/combat-inherited-armed-continuation-v1.md)
proves that exact ammunition10 released-I profile reaches one supported next-cycle Combat candidate
for either owner without emitting an event. [D2c.3k guarded cycle control](../../docs/specs/combat-inherited-cycle-control-v1.md)
composes that proof with exact Release state and freezes both-owner repeat into ordinal-2 Movement
or finish into Truck Convoy. [D2c.3l released-I Movement](../../docs/specs/combat-inherited-reserve-movement-v1.md)
then moves either exact released member one Clear hex at CP0→2 under ceiling10.
[D2c.3m released-I Movement completion](../../docs/specs/combat-inherited-reserve-movement-completion-v1.md)
closes both exact routes through deliberate stop, empty resolution and completion at authority31,
then expires each pending exception with its accepted completion receipt.
[D2c.4 authority composition](../../docs/specs/combat-authority-composition-v1.md) now reconciles CON-002–004
across 28 creation-rooted traces and freezes parent003's exact Task004 handoff. Parent003 is complete;
Task004 and checkpoint B are accepted through the [outward integration index](../../docs/specs/combat-outward-composition-v1.md).
The [latest smoke check](../../docs/research/simulator-post-merge-checkin.md#combat-contract-branch-smoke-check)
verifies the existing Rules9 path; prospective Combat contracts are not executable game support.

Task019D1 adds private native [settled-continuation evidence](../../docs/specs/combat-settled-continuation-v1.md):
complete Result2/empty Release replay and pure Movement witnesses for32 owner/seal contexts.
Earlier Movement remains synthetic; the proof grants no repeat or public action.

Task019D2 adds a private [settled-control executable contract](../../docs/specs/combat-settled-control-v1.md):
full Result2 proof admission, owner repeat/finish and deterministic forced/fallback finish.
The Python contract preserves synthetic earlier trust. Task019D3 implements private native control with
full packet admission, exact replay/retry bytes and immutable source preservation; actual positive-history
selection/result consumption and repeated Movement execution remain separate gates.

Task019E1 adds private native actual positive-entry replay for the two seed1 Normal ordinary NONE
openings in [positive-entry v1](../../docs/specs/combat-positive-entry-v1.md): owner idle Movement11→12
and System empty Breakdown12→13, preserving World/resources/RNG and deriving a supported
candidate before selection. Full source, receipts and Movement-end proof authenticate entry;
initial synthetic content-origin labels remain unchanged. C3a/Result2 consumption, repeat,
public activation and parent017–019 completion remain separate gates.

Task019F0 freezes the private [actual-selection executable contract](../../docs/specs/combat-actual-selection-v1.md)
for those two original entry histories. Its positive path reaches Force Assignment after defender
decline; seven fallback variants per owner reach Reserve Release without an attack. Replay requires
an independently supplied trusted input ledger. Native execution and production actor/clock/store
authentication remain open, as do actual round/result/repeat and public Combat activation.

| Area | Executable today | Forward evidence / next gate |
| --- | --- | --- |
| Authority foundation | Versioned provenance, synthetic content, commands/events, deterministic randomness, replay, side-safe observations, and exact-audience legal actions for the admitted profile | Extend the same compatibility, recovery, and fog boundaries with each mechanic |
| Preamble and Movement boundary | Initiative through Reserve Designation, bounded Movement, ZOC/Reaction, and Breakdown through first-side Combat entry | Positive scenario-specific obligations and broader vehicle/ZOC profiles remain gated |
| Combat and continual cycle | Private Core adapters; public activation pending | Reviewed internals cover settlement, Reserve Release, bounded released-I Movement and guarded repeat/finish. Native settled control retains synthetic earlier Movement. Two native opening histories reach a supported candidate before selection. A separate private actual-selection contract is executable through Force Assignment or no-attack Reserve Release; its native consumer and production input authentication remain gated. Actual round/result, full-cycle proof and public Combat remain open. |
| Working skeleton | Not reached | One authentic movement/contact/combat/release repeat-or-finish loop plus identical replay |
| Playable MVP | Not started | Source-verified six-turn content/rules/victory, durable save/resume, hot-seat privacy, and minimal no-model Maproom |
| Exercise Harness | Current bounded Exercise/Maneuver and paired descriptive comparisons | Add Combat actions and terminals only after public Core activation |

The approved high-level path to a playable game is:

1. Implement native actual selection from its frozen contract, then join actual round/result
   settlement through bounded contracts and private adapters. Activate public side-safe actions,
   retain strict Runner evidence, and prove one authentic repeat-or-finish loop.
2. Freeze the exact six-turn scenario surface—rules, tables, content, sources, rights, termination,
   victory, and remaining decisions—before splitting later implementation tasks.
3. Implement only that measured Land surface and source-verified `Graziani's Offensive` content.
4. Add durable local save/resume and recovery, then minimal Maproom with complete no-model actions
   and hot-seat isolation.
5. Complete two deterministic six-turn playthroughs and replay/privacy/source gates before calling
   the MVP playable. Optional parsing, hosted play, and model-backed intelligence remain later work.

The serial-Maneuver portion of Exercise Harness v1 now provides validated local multi-run regression
evidence without adding game rules. The implemented Operation-Stage Entry package retains its
[research](../../docs/research/operation-stage-entry-spike.md),
[specification](../../docs/specs/operation-stage-entry-v1.md), and
[technical design](../../docs/design/operation-stage-entry-v1.md). Reserve Designation is the latest
completed player-action vertical before Movement. Its
[research](../../docs/research/reserve-designation-spike.md),
[specification](../../docs/specs/reserve-designation-v1.md), and
[technical design](../../docs/design/reserve-designation-v1.md) define an incremental designation flow
that stops at Movement. Rules, state, owner projection, legal candidates, command mapping,
designation/completion events, finite checkpoint validation, replay, and checked harness evidence
are implemented. The completed engine package is the approved Movement Foundation
[research](../../docs/research/movement-foundation-spike.md),
[specification](../../docs/specs/movement-foundation-v1.md), and
[technical design](../../docs/design/movement-foundation-v1.md). It defines a fog-safe apparent-presence
gate followed by exact CP/Cohesion state, normalized lab terrain and stacking, repeatable
non-contact moves, and explicit completion to Breakdown Determination. The plan is owner-approved;
its source/ruling lock, exact Rules foundation, `MOV-TASK-003` Content mobility contract, and
`MOV-TASK-004` replay-complete world/representation contracts are complete. Task 004 records exact
Cohesion/expenditure and opaque internal representation
bindings in the Task 004 snapshot v8/world v3 creation history. On 2026-08-29 the owner approved sequential-d6
Breakdown coordinates, continuity-now, and the Table 21.38 Sandstorm-attributed-BP basis.
`MOV-TASK-004B` implements the exact Rules/Content/World seam and passed the repository gate plus
two fresh-context review instances. `MOV-TASK-005` implements the contract-5 owner/apparent
projection and strict canonical readback. `MOV-TASK-006` freezes dormant move/completion
candidates, exact cost semantics, deterministic IDs, pure observation-derived vectors, and strict
non-authoritative action/submission/receipt readback while preserving the existing contract
versions. `MOV-TASK-007` adds the internal move command and canonical event, authoritative
cost/provenance recalculation, engine dispatch, atomic projection, and deterministic replay.
`MOV-TASK-008` atomically publishes observation-derived move and completion membership, maps only
exact current submissions, adds canonical Movement completion through the Breakdown Determination
checkpoint, and preserves deterministic fog-equivalent actions and zero/one/many-move replay.
`MOV-TASK-009` is merged in PR #78 and adopts that supported Movement path in checked
Exercise/Maneuver evidence. `MOV-TASK-010` completed synchronization and independent review and is
merged in PR #79. At that historical milestone, Breakdown public actions and adjudication were absent.
The subsequent ZOC/Reaction package follows the
[specification](../../docs/specs/zoc-reaction-v1.md) and
[technical design](../../docs/design/zoc-reaction-v1.md). `ZOR-TASK-002A`-`006B` implement dormant
Rules/Content/fixture, Campaign World 5/creation 9, Snapshot 10, and `ElementMoved` v2 successors,
including exact current-TOE provenance, nullable/empty Reaction-window truth, strict canonical
readback, atomic projection, checkpoint replay, and the side-safe Observation 6/policy and redacted
decision-history contracts described above. Dormant topology-local Movement/Reaction candidate,
strict current-readback, stable-identity, unpublished mapping, move-option capability,
manifest-registration, semantic-admission, and retained-transcript contracts are also complete.
The direct-only authority path reconstructs atomic move/window truth, freezes only individually
adjacent eligible reactors, applies topology-local enemy-ZOC entry/exit semantics, and closes
player-declined, unavailable, timed-out, or empty windows with exact Movement resumption and no
cost/RNG mutation. It also selects the first participant atomically with its move, keeps later
steps bound to that active participant, accumulates exact shared Movement CP/provenance, and resolves
participants without World or RNG mutation. `ZOR-TASK-006C` now activates the complete successor
identity set on public Core creation, observation, action, checkpoint, and replay paths; legacy
creation and Movement roots reject. Bounded Runner adoption in `ZOR-TASK-007A` is implemented; `007B` verification and independent review are complete.
The optional paired comparison is implemented Runner instrumentation and does not block
gameplay-engine progress.
Combat policies and the [25-task plan](../../docs/design/combat-cycle-implementation-plan.md) are owner-approved.
[TASK-001 source evidence](../../docs/research/combat-source-freeze-v1.md) is complete: 357 defined loss
values preserved, three source gaps filled by accepted amendment CMB-SRC-RUL-001, and calendar/
break-off findings retained. [TASK-002 Content7 contract](../../docs/specs/combat-content-v7.md) is frozen
in `c465a0f`, with canonical bytes and70 passing rejection vectors. Checkpoint A author validation
is recorded; [TASK-003A Setup/initial ledger](../../docs/specs/combat-creation-ledger-v1.md) is frozen in
`23c3fff`, with63 passing rejection vectors. [TASK-003B World/settlement packet](../../docs/specs/combat-world-settlement-v1.md)
is complete as a contract slice. [Progress review5](../../docs/reviews/combat-progress-review-5.md) returned
Ready with non-blocking follow-ups; its status correction is applied.
[003C1 rules inputs and timing](../../docs/specs/combat-rules-inputs-v1.md) is complete as a contract
slice; [review6](../../docs/reviews/combat-inputs-review-6.md) returned Ready, no actionable findings
(6of7 used at that checkpoint). [003D1 sequence/cycle contracts](../../docs/specs/combat-cycle-sequence-v1.md)
are complete; [review7](../../docs/reviews/combat-sequence-review-7.md) returned Ready, no actionable findings.
[Review9](../../docs/reviews/combat-progress-review-9.md) returned Ready with non-blocking follow-ups at
`a96d2a1`; its documentation corrections are applied. [Review10](../../docs/reviews/combat-progress-review-10.md) returned Ready with non-blocking follow-ups
for D2b.2/D2c.1; both findings are corrected. Review11 across mergedPR95–98 returned Ready with no
findings; budget11of11 is exhausted. The subsequent Weather and stage-entry slices have author verification only. [003C2 Rules10/creation envelopes](../../docs/specs/combat-authority-envelope-v1.md)
are complete for the creation cut. [003C3a selection/step control](../../docs/specs/combat-selection-steps-v1.md)
is complete with author checks; [003C3b sealed round/commitment](../../docs/specs/combat-sealed-round-v1.md)
is also complete as a bounded authority fragment. HOST-RSH-001 research,003C3c contracts and003D2a
ordinary movement contracts are complete. D2b Release and guarded control contracts are complete;
[D2c.1 inherited successors/first opening](../../docs/specs/combat-inherited-successors-v1.md) is complete
as an isolated boundary:20 event declarations, four opening traces.
[D2c.2a opening preamble](../../docs/specs/combat-opening-preamble-v1.md) freezes four of those successors
with6 creation-rooted traces/30 cuts through Weather entry. [D2c.2b Weather](../../docs/specs/combat-weather-v1.md)
adds34 traces/68 cuts through Organization entry, preserving all four outcomes and exact RNG/receipt
evidence. [D2c.2c stage entry](../../docs/specs/combat-stage-entry-v1.md) adds12 traces/60 cuts through
Reserve entry, preserving all accepted history. [D2c.2d Reserve designation/completion](../../docs/specs/combat-reserve-designation-v1.md)
closes creation-to-first-opening contracts for empty/I selection and both acting sides.
[Movement preparation](../../docs/design/combat-inherited-movement-preparation.md) maps the next inherited
successors. [D2c.3a inherited ordinary Movement](../../docs/specs/combat-inherited-movement-v1.md)
traces both sides through seven safe Clear moves, cumulative CP14 and four excess-CPA DP.
[D2c.3b route lifecycle](../../docs/specs/combat-inherited-movement-lifecycle-v1.md) adds8 traces/24 events
through Breakdown Determination, with actual first Movement-end proof and unchanged World/RNG.
[D2c.3c Breakdown completion](../../docs/specs/combat-inherited-breakdown-completion-v1.md) adds8 one-event
traces into first Combat Position Determination while retaining that proof and full state.
[D2c.3d actual-entry selection](../../docs/specs/combat-inherited-selection-v1.md) adds4 traces/8 events,
derives zero candidates from the moved World and closes selection while retaining stepIndex0.
[D2c.3e no-attack traversal](../../docs/specs/combat-inherited-no-attack-v1.md) adds4 traces/24 events and
reaches same-slot Reserve Release. [D2c.3f Reaction trigger](../../docs/specs/combat-inherited-reaction-trigger-v1.md)
adds2 actual owner traces that open one frozen opportunity.
[D2c.3g Reaction lifecycle](../../docs/specs/combat-inherited-reaction-lifecycle-v1.md) adds2 traces/8 events
through participant movement/completion, required stop resolution and exact phasing resumption.
[D2c.3h Reserve cycle entry](../../docs/specs/combat-inherited-reserve-cycle-v1.md) adds2 traces/20 events
through no-move Movement, idle Breakdown and no-attack Combat while retaining actual Reserve I.
[D2c.3i inherited Reserve Release](../../docs/specs/combat-inherited-reserve-release-v1.md) adds2 traces/6
events through owner release-I and deterministic completion. Guarded repeat/positive Reserve
movement and broader Reaction remained open at that child boundary; D2c.4 now closes selected
composition.
[D2c.3j armed continuation](../../docs/specs/combat-inherited-armed-continuation-v1.md) adds2 pure proofs
for the actual released-I ammunition10 profile, one candidate per owner, with full-result support
pins; it does not repeat the cycle or execute Combat.
[D2c.3k guarded cycle control](../../docs/specs/combat-inherited-cycle-control-v1.md) adds4 exact
repeat/finish traces while preserving private/non-runtime boundaries.
[D2c.3l released-I Movement](../../docs/specs/combat-inherited-reserve-movement-v1.md) adds2 exact
ordinal-2 Clear moves with released ceiling10.
[D2c.3m released-I Movement completion](../../docs/specs/combat-inherited-reserve-movement-completion-v1.md)
adds2 three-event stop/resolution/completion traces and applies the exact D2b.2 expiry projection
from each accepted completion receipt; broader profiles remain open while D2c.4 now closes selected
composition.
[D2c.3n direct Reaction closure](../../docs/specs/combat-inherited-reaction-closure-v1.md) adds6 one-event
forks covering both owners across player decline and distinct System unavailable/timeout authority;
[D2c.3o active Reaction fallback](../../docs/specs/combat-inherited-reaction-active-fallback-v1.md) adds4
two-event forks from exact post-first-move authority through reason-specific closed stop and
mandatory resolution. [D2c.3p active Reaction second move](../../docs/specs/combat-inherited-reaction-second-move-v1.md)
adds2 one-event owner traces from the same fork point through rear→supply at CP2→4 while retaining
active authority. [D2c.3q Reaction movement completion](../../docs/specs/combat-inherited-reaction-movement-completion-v1.md)
adds2 three-event owner/System traces through explicit completion, empty-stop resolution, and exact
phasing resumption at authority18. Multiple-opportunity Reaction and vehicle profiles remain open;
D2c.4 now closes selected composition.
[Result/settlement](../../docs/specs/combat-result-settlement-v1.md) and
[full snapshot composition](../../docs/specs/combat-snapshot-composition-v1.md) retain synthetic pre-Combat
lineage; review9 assessed these bounded artifacts. Parent003 closed through D2c.4; future
maturity execution remains open, while combined checkpoint B was subsequently accepted. The
[ordinary movement packet](../../docs/specs/combat-ordinary-movement-v1.md) freezes break-off/CP/DP
and corrects the former Clear1 example to the existing Clear2 rule. The
[Reserve Release packet](../../docs/specs/combat-reserve-release-v1.md) freezes single-deadline control
and retained history. The [cycle-control packet](../../docs/specs/combat-cycle-control-v1.md) freezes guarded
repeat/finish and Movement exception expiry:19 cases/64 traces,164 cuts. Its exhausted-ammunition
continuation surface remains private; D2c.4 now composes full history, armed Combat assessment and
Snapshot integration evidence. Later dormant World7 and codec work is summarized in the current
[delivery table](../../README.md#project-at-a-glance); Combat gameplay remains inactive.

See the [pre-alpha roadmap](../../docs/roadmap/pre-alpha-roadmap.md) for the capability-level plan and
completion criteria.
