# Combat and Cycle Combined Contract / Implementation Plan

**Status:** Contract checkpoint B and dormant Tasks005–007 implemented. Task008 is in progress through merged children A0/A1a/A1b; A1c/A2 are implemented and independently reviewed on stacked feature branches. Combat gameplay remains inactive.
`CMB-PLAN-001` and POL-001–008 were accepted on 2026-09-06 at `a10a588`. Source and static Content
work (`001`–`002`) is complete. Parent `003` is complete through the
[authority-composition handoff](../specs/combat-authority-composition-v1.md). Checkpoint B closes through the
[Task004C integration index](../specs/combat-outward-composition-v1.md). Task004A side contracts and Task004B occurrence, child and
parent evidence are accepted with strict clock privacy and versioned round/settlement authority. Dormant Tasks005–007 and bounded Task008 children A0/A1a/A1b are implemented; A1c/A2 are implemented and reviewed on stacked feature branches; B–H and Tasks009–025 remain pending.

| Delivery layer | Current boundary | Next required outcome |
| --- | --- | --- |
| Research and policy | Complete for the selected bounded profile, including `HOST-RSH-001` | Reopen only for a new source, failed assumption, or approved profile expansion |
| Authority contracts | CON-002–004 and all selected-profile inherited families are reconciled in 28 creation-rooted composition traces; parent003 complete | Preserve exact Task004 handoff while later runtime work derives boundaries from accepted history |
| Outward contracts | Task004A/B/C accepted; all 72 ACs mapped with bounded evidence and explicit runtime deferrals | Preserve accepted bytes and source/privacy boundaries during dormant implementation |
| Runtime | Dormant Tasks005–007 and Task008 creation children A0–A2 implemented; Task008 parent incomplete; Tasks009–019 pending | Finish inherited adapters and full retained-history restore before dependent authority work; retain open HOST-PUB-001 publication proof |
| Public and simulator | Tasks020–024 not started | Activate the certified side-safe profile, then prove strict Exercise/Runner reconstruction and repeatability |
| Closeout | Task025 not started | Full gate, evidence reconciliation, and authentic loop demonstration without overstating retained obligations |

The [preparation matrix](combat-inherited-movement-preparation.md) remains the field/provenance
baseline. Historical checkpoints and review verdicts are retained under
[progressive execution evidence](#progressive-execution-evidence); they are not the current status
source. The [roadmap](../roadmap/pre-alpha-roadmap.md#current-checkpoint-and-next-gates) owns
cross-package sequencing and the route from Combat closeout to the playable MVP.

Cross-package sequencing lives in the [roadmap checkpoint](../roadmap/pre-alpha-roadmap.md#current-checkpoint-and-next-gates).
Task005 began dormant implementation; public activation is020–021 and Combat simulator evidence
is022–024. Owner accepted the author review's planning direction after `b8be39a`: the bounded
`HOST-RSH-001` investigation is complete; its memory CAS probe did not select storage. Production hosting still requires its own contract and
storage decision; the planning target follows020–021 and one verified023 trace. No hosted/model
capability or durable provider is authorized by this documentation update.

## Outcome and boundary

Deliver the [six bounded designs](../research/combat-cycle-source-inventory.md) through ordinary
Core authority, then Exercise and strict Runner evidence: empty completion, a fully settled
infantry assault, and a Movement/Reserve repeat. Each accepted transition has replayable authority
and separately projected side evidence. The terminal is same-slot Truck Convoy **entry**; retained
prisoner upkeep/replacement obligations prohibit calling this a complete playable campaign.

The [policy register](combat-cycle-policy-reconciliation.md) owns POL-001–008 and approvals. Existing
design ACs remain requirements; this plan does not weaken them. No claim of two runtime assaults,
general resupply, real RBA, released-Reserve offensive Combat, motorized retreat, gun/armor combat,
mandatory attacks, new phase-slot execution or hosted intelligence is included. Those require
separate admitted profiles and tasks. Sprint5's full repeating-skeleton acceptance remains open
where those dependencies are required; this package supplies bounded evidence toward it.

## Combined contract inventory

CON-001 has the [Content7 contract/oracle](../specs/combat-content-v7.md); its reserved identity
is not registered in runtime. Task003 froze CON-002–004 schema evidence and exact
versions in the [composition handoff](../specs/combat-authority-composition-v1.md); Task004 froze
CON-005/006 at checkpoint B. These executable contract oracles alone do not prove complete production
types or registered runtime versions. Exact fields, bounds, canonical bytes and compatibility must remain
frozen before any consumer is implemented.
Use new versioned contracts; never alter historical canonical bytes or infer absent fields.

| ID / owner | Required contents and invariants | Freeze / consumers |
| --- | --- | --- |
| `CMB-CON-001` Content / Rules | Reconcile [Content6](../../src/Cna.Core/Content/ContentPackV6Models.cs) component IDs/classification/ratings with maximum TOE, parent Basic Morale and explicit current seed provenance. Closed capability and dedicated synthetic fixture; tables/modifiers stay Rules-owned. Complete selected table source/coordinate manifest and approved policy digest contribute to rules/config identity. | 001–002 / 005–006 |
| `CMB-CON-002` Core world / Archives | Successor to [World6](../../src/Cna.Core/Campaigns/CampaignWorldV6.cs): current component TOE/ammo/readiness, CP including ordinary infantry up to 150% CPA and mandatory overrun, separately attributable DP/RP, participant relationships, loss/capture lots, guard provenance, escape entitlement and future obligation records. Preserve existing BP/bands/broken lots. Retain earned/due game scopes and the source-backed four-turn delay; phase-specific maturity, training and absorption remain activation gates. | 001/003 / 007–008, 014–016 |
| `CMB-CON-003` Core commands / Chronicle | Versioned segment selection/closure, real RBA decline, private two-slot round, commit/result, retreat intent, loss/retreat/custody/relationship settlement and round closure. Include causal predecessor, opportunity/unit/component bindings, rules/config/base evidence, exact receipt identity and pre/post versions. Persist budgets/deadline/high-water time, consumed decisions, accepted input and terminal cause. Result retains role-labelled rolls/cursors; costs, results and each settlement publish atomically. | 003 / 009–016 |
| `CMB-CON-004` Core cycle / Archives | First-cycle opening in ReserveDesignationCompleted, repeat/finish events, pre-event prefix, ordinal/relative slot/resolved actor, release window/dispositions, Movement-end proximity, release/offensive-use history, exception expiry, ordinary break-off CP/relationship receipts, material progress and continuation witness. Reconstruct complete active-stage history; no-history restore rejects. Phase finish preserves future obligations and is not stage end. | 003 / 017–019 |
| `CMB-CON-005` Core side projection / future Dispatch | Closed per-audience choices, errors and observation fields; own revision/action set separate from authority version; exact candidate codecs and stable public refs. Authorized disclosure only, including declared inferences. Future transport must carry decision/state/rules/config bindings without sending private authority version/hash as an outward token; specify an explicit audience mapping. | 004 / 020–021; hosted adapter deferred |
| `CMB-CON-006` Core Exercise / Runner | Versioned occurrence/ordinal/position/obligation terminal, checkpoint continuation, strict child manifest and parent report, replay/readjudication evidence and paired first-divergence semantics. Reject unsupported terminals before running; failure/step-limit cannot satisfy negative-success assertions. Trusted raw bundles remain private. | 004 / 022–024 |

Each event family must specify duplicate versus conflicting retry behavior, out-of-order rejection,
exact preconditions, no-partial-publication behavior, restore cuts and tamper cases. First seal must
not stale the other audience. Deadline equality, backward time, lost replies and restart are explicit
contract vectors. Every policy/config/codec version is pinned when its window opens.

Compatibility freeze must inventory currently registered readers/writers rather than guess next
version numbers. Preserve old admission and historical fixtures. Prefer new campaigns for the new
capability; any historical migration must validate a complete history mapping and be separately
specified before use. Old readers reject new contracts. Disabling new admission must leave recovery
of already-admitted new campaigns available. Hash framing and bounds receive checked golden bytes.

The existing [protobuf](../../src/Cna.Intelligence.Contracts/Protos/intelligence.proto) is not a
sealed-Combat transport. No adapter may repurpose its generic version fields to expose authority
state. Hosted integration requires its own contract-first task set: protobuf field allocation and
wire compatibility before generated consumers, authenticated audience mapping, durable pending
decisions, deadlines/cancellation, deduplication, off-turn I/O and provider-unavailable tests. That
set is deferred; Core and simulator controllers use typed in-process decisions for this package.

## Task graph and delivery rules

All IDs below use prefix `CMB-TASK-`. Numbers in dependency columns refer to those IDs. Entry gate
`G0` means recorded POL-001–008 decisions, owner plan review and disposition of the independent
combined-plan findings. Review4 and its bounded author correction are recorded below; owner
accepted the correction and policies on2026-09-06, satisfying G0. TASK-001–004 produce source/contract artifacts; runtime consumers start only
after checkpoint B accepts their concrete freeze. No task is complete without a commit and retained
verification evidence linked here. TASK-001 has [retained source diagnostics](../research/combat-source-freeze-v1.md)
and the accepted source ruling. TASK-002 has [canonical Content evidence](../specs/combat-content-v7.md#task-002-verification);
source research and the Content contract packet are complete, with no runtime implementation task complete.

Commit each task on a feature branch. Package one checkpoint at a time for future PRs; report file
and line counts before publication. The current documentation branch is not authorization for one
large implementation PR. Primary-file estimates include focused tests; if a task exceeds five files
or crosses an independent subsystem, split it before editing and preserve its parent ID. Shared
registration edits follow dependency order. Test aliases below name new focused suites to create,
not tests that already exist.

### Checkpoint A — resolve high-risk source and static contracts

| Task / size | Output and acceptance criteria | Verification | Dependencies / likely paths |
| --- | --- | --- | --- |
| `CMB-TASK-001` / M, 3–5 files | Produce selected-table source manifest and calendar mapping decision; resolve ordinary Contact/Engaged break-off cost precedence under Land8.15/8.24 and the applicable Contact rules. Cover all reachable coordinates/effects and record independent source cross-check; define one-game-month maturity/guard obligation boundaries or leave an explicit activation blocker. | Coordinate-domain enumeration against source, discrepancy log; month-boundary examples independently calculated. Research scripts alone cannot certify untranscribed rows. | G0; [research](../research/), [Rules](../../src/Cna.Core/Rules/) source manifest and new focused oracle fixtures. |
| `CMB-TASK-002` / M, 3–5 files | Freeze CON-001 names/versions/capability, source-parent/class vocabulary, scenario seed types and canonical fixture format. Reuse existing facts; reject partial/extra categories and absent seed/readiness provenance. | Positive canonical bytes plus single-field negative mutations; review policy/source-to-field mapping. | 001; [Content7 contract](../specs/combat-content-v7.md), [canonical fixture](../specs/fixtures/combat-content-v7.canonical.json), [rejection vectors](../specs/fixtures/combat-content-v7.vectors.json), [oracle](../specs/verify-combat-content-v7.py). Production test fixtures follow in006. |

Checkpoint A: [author check at57866a7](../reviews/combat-checkpoint-a-author-check.md) and owner
continuation permit TASK-003; no independent Ready verdict is claimed. Unresolved source/calendar facts stay
visible. No favorable-vector or synthetic arithmetic result substitutes for full selected coverage.

### Checkpoint B — authority and outward contract freeze

| Task / size | Output and acceptance criteria | Verification | Dependencies / likely paths |
| --- | --- | --- | --- |
| `CMB-TASK-003` / complete | [Authority-composition packet](../specs/combat-authority-composition-v1.md) freezes and reconciles CON-002–004 world/event/snapshot/command/cycle evidence, exact versions, causal receipts, capacity, compatibility and Task004 handoff. Production registration remains gated. | 28 creation-rooted traces across nine families; 31 direct plus embedded transitive source pins; 225 readbacks, 203 mutations, six raw and eight boundary rejects. | 001–002 complete; Task004 consumes exact handoff. |
| `CMB-TASK-004` / M, 3–5 files | Freeze CON-005/006 side and Exercise contracts, candidate bytes and terminal evidence. Enumerate every design AC in an evidence index; mark deferred transport requirements without allocating fake production support. | Equal-authorized-history vectors, authority-leak negatives, ordinal/terminal tampering and all 72 ACs mapped to a task and planned test. | 003; new side/evidence packet, [Observation tests](../../tests/Cna.Core.Tests/Observations/), [Exercise tests](../../tests/Cna.Core.Tests/Exercises/). |

TASK-004 execution refinement, 2026-09-14 overnight wave01, before implementation: side projection,
Exercise evidence and requirement integration cross independent boundaries. Preserve parent004;
deliver these sequential slices with at most five primary files each, including this plan.

| Slice | Owned contract boundary | Acceptance / dependency |
| --- | --- | --- |
| `CMB-TASK-004A` | New `combat-side-projection-v1` spec, ordered schema, retained fixture and executable oracle | Complete CON-005 closed audience fields/choices/errors and canonical candidates, own-revision mapping, equal-authorized-history and authority-leak negatives; exact003 handoff preserved |
| `CMB-TASK-004B` | New Exercise evidence spec, schema, fixture and oracle | After accepted004A; complete CON-006 occurrence/ordinal/terminal/continuation and strict manifest/report/divergence contracts, unsupported-terminal and false-success negatives |
| `CMB-TASK-004C` | Explicit72-AC index and bounded integrated readback evidence | After accepted004A/B; every AC maps to task/planned test; exact versions, capacity, compatibility and Task003 handoff reconciled before004/checkpoint B closes |

004C packet refinement before edits: four new `combat-outward-composition-v1` spec/schema/fixture/
oracle files plus this plan retain the explicit72-AC index, source/version/capacity compatibility
and accepted side/Exercise readback. This unregistered evidence index preserves partial/deferred
runtime coverage. Exact scope is in [packet004C](../../.planning/2026-09-14-overnight-combat-wave-01/packet-004c.md).

004B sizing refinement before edits: three explicitly unregistered prospective Combat families,
each spec/schema/fixture/oracle plus this plan, execute after accepted004A:

| Slice | Contract family | Required evidence |
| --- | --- | --- |
| 004B1 | `combat-exercise-occurrence-v1` | Tagged synthetic-C3 or historical003 lineage; full-World/RNG typed checkpoint; exact occurrence/position/closure/obligations; continuation and pinned dual-slot schedule |
| 004B2 | `combat-exercise-child-evidence-v1` | Strict manifest/payload inventory; source replay and separate side-decision re-adjudication; exact Int32 record framing; failure/step-limit distinct from successful terminal |
| 004B3 | `combat-exercise-parent-evidence-v1` | Validate children before counts/fingerprint; equal initial lineage for pairs; audience/action first divergence, unequal-length null arm and unavailable comparisons |

These typed checkpoints are not CoreSnapshot records. They prove contract replay within explicitly
tagged lineage; ordinary-Core/Exercise parity remains runtime022, actual publication remains023.
B1/B2 bind full source lineage separately from execution profile/start fragment/local cut. Native
C3 runs execute supported public decisions and System transitions; live inherited Release/control
starts only at admitted A3 cuts after authenticating its complete prefix. Historical003 terminal
checkpoints execute zero steps and must already satisfy their exact request with identical initial/
final checkpoint bytes. Source/proof fragments affect proof identity, not execution step count.
These checkpoint-scoped proofs do not claim public-controller replay of earlier source-only
creation/Movement actions or completion of a zero-step historical suite; Task022 owns that parity.
B1 distinguishes a family-supported requested terminal from its satisfaction by a supplied
execution prefix. Eight exact A2clock-fallback prefixes can request native closedReserveRelease1
and stop at accepted-transition limit16/20 while still in CloseAssault. B2 records failedterminal/
StepLimitExceeded, actual ownerinitiator/Systemauthor and rejectedoutward proposal; no fabricated
success, failedattempt or replayproof. No arbitrary continuation past retained source is admitted.

No full Snapshot successor is needed for this chosen004B route. Preserve old ExerciseManifest
payload2/labelv1, Checkpoint1/Snapshot11 and all registered readers. New Combat scheme labels remain
unregistered; old readers never receive prospective Rules10 manifests as current supported input.

Each slice requires focused verification, ordinary quality review and a scoped commit. User-approved
workflow adjustment,2026-09-16: preserve all acceptance requirements while sharing authenticated
source warmup across B3/C, reusing unchanged predecessor evidence, and consolidating full `just check`
at C/checkpoint B and final Task005 integration. Focused C# tests still run for each005 slice; changes
or failures trigger relevant reruns. No final integration gate or privacy/replay assertion is removed.
Navigation updates follow at C and005 checkpoints. This split adds no gameplay policy, production
registration or formal independent-review pass. Task005 implementation remains gated on accepted
004/checkpoint B. Execution evidence lives in the
[overnight plan](../../.planning/2026-09-14-overnight-combat-wave-01/task_plan.md).

004A sizing refinement before edits: implement the four new packet files sequentially as004A1
(shared closed codec/identity/errors plus selection/RBA/sealed-round projections),004A2 (settlement
disclosure, retreat/custody candidates and result facts), then004A3 (Reserve/cycle projections,
exact Task003 handoff and full corpus reconciliation). Each child has semantic RED/GREEN,
fresh-context quality review and `just check`; complete CON-005 freeze is claimed only after004A3.
004A3 is further split before edits into004A3a (Reserve/cycle closed choices, frozen binary
candidate/set IDs and own live cuts) then004A3b (both-audience28trace projections, exact003handoff,
privacy and capacity reconciliation). Both keep the same four side-packet files plus this plan;
actual CP14/Cohesion-4 and three-action later-II sets must fit without truncation.
All four new files remain under one writer; historical packets remain immutable. If one adapter
group cannot remain reviewable within this ownership, re-split before further edits.

004A2 version refinement before edits: add separate Observation2, Submission2 and Candidate2
records within the side packet. Every Candidate2 arm carries explicit version2; disjoint decision,
action, set and receipt domains bind the new codec. Keep every A1 type, numeric bound, source name
and literal exact. New settlement profiles project version2 from their first C3a frame, including
both clock policy IDs and approved budgets in their public configuration identity. Current A1
admission remains separate from A2 admission. OwnParticipant2 follows existing World primitives:
nonnegative signed64 CP with certified denominator1; signed32 Cohesion capped at10. These value
bounds do not claim all possible values are certified gameplay. Add only allowlisted typed own
settlement/custody/guard/replacement facts, bounded full history and real owner-choice receipts.
System-authored fallback never creates an own accepted receipt, even when input initiator was owner.
Old/new readers and cross-profile submissions reject; source authentication precedes projection.

004A2 accepted2026-09-15 after37focused groups, ordinary fresh-context approval, source/literal
audit and root `just check`:81boundary/1670full tests,0skips. Disjoint version2 admits96audience
traces/2080cuts with7344clock outcomes and4872retries; originalA1 records remain exact. Maximum
7503observation bytes,18history entries,4own receipts. Root independently checks240fallback
outcomes and24cache/source mutations. This accepts settlement projection only; bridge/A3/B/C
and parent004/checkpointB remain open.

004A3 evidence refinement before edits: canonical first-I release/conversion and later-II
release/retain/completion sets must be derived and exercised through actual standalone Reserve
transitions, with canonical bytes, accepted state, revision and receipts. These are explicitly
synthetic ledger behavior tests, not fullWorld observations. The accepted creation-rooted inherited
release wrapper supports only the release-I owner path; keep its conversion rejection and bytes.
Its outward actions describe that bounded replay capability, distinguished by public profile/policy
identity, not the complete Reserve gameplay legal set. Conversion in an inherited fullWorld profile
remains unsupported until a separately scoped authority adapter. This retains AC003 coverage via
the canonical ledger while preserving the existing admitted inherited profile.

004A3 version refinement before edits: separate Observation3/Submission3/Candidate3 and owned
Reserve/cycle records preserve every acceptedA1/A2 descriptor, bound, domain and literal. Public
capability identity is fixed from first frame and distinguishes bounded inherited admission from
corrected synthetic support and projection-only terminals. Closed Combat/cycle decision tags keep
new Combat references separate from frozen binary cycle action/set hashes, unsigned candidate-byte
sorting and index-bound action IDs. Candidatecapacity3; source-native CP/Cohesion; no truncation.
A3a owns live inherited release/control plus full standalone ledger behavior. A3b owns28normalized
terminal projections and accepted corrected bridge, usingv3 from first C3a frame with continuous
receipts/history throughfinish. Terminal-only sources never invent prior sidehistory or actions
from authorityreceipt counts. Exact fields and bounds are recorded in
[packet004A3](../../.planning/2026-09-14-overnight-combat-wave-01/packet-004a3.md).
Source-backed receipt distinction: canonical later-II `complete-release` is accepted owner intent
recorded by a System-authored completion with exact `owner-complete-release` reason. Native replay
and offered-candidate/owner/receipt binding determine acceptance; event author alone does not.
System completion and clock fallback create no owner acceptance. B2 retains owner semantic action
for valid accepted completion and System semantic action for rejected fallback; exact retry adds
no step. This does not expand inherited release-I-only admission.

004A3a accepted2026-09-15:52focused groups, ordinary fresh-context approval, exact source/literal
checks and root `just check`81boundary/1670full tests,0skips. Live inherited release/control and
separate canonical ledgers retain A1/A2 bytes. Root reconstructs92binary sets/190action identities;
audit checks456cuts/344observation literals/190candidate pairs/38pins. Owner complete-release
receipt follows authenticated intent even with System event author. A3b starts next:28terminal
projections and continuous corrected bridge history, separate registry and internal configuration
seed binding all three clock policies from first frame. Bridge offers finish only; historical
terminal profiles offer no actions. Parent004/checkpointB and005 remain incomplete.

004A3b accepted 2026-09-15: 62 semantic groups, fresh ordinary approval and native source audit,
root `just check` 81 boundary/1670 full tests, zero skips (3m31.596s full suite).
116 sources cover 4744 cuts, 1208 literal observations and 720 candidate pairs. All 28 historical
terminal profiles match native World and Reserve state, including both singleton releaseMember
completion records. Corrected histories preserve all clock policies, receipts and privacy through
finish. A1/A2/A3a bytes preserved. B1/B2/B3 and C remain required before checkpoint B; 005 follows.

B1/B3 identity clarification: sourceLineageHash binds authenticated root/history/proofs only through
executionStart. Full future-bearing reference transcript is authenticated and hashed separately in
child evidence; reference hash/name and future controller/schedule choices never seed initial
checkpoint identity or pair equality. Pair equality still requires identical actual initial state,
RNG, prefix/cut/profile, gameplay/build, requested terminal and bounds. Each complete reference is
independently authenticated before deriving its prefix. No arbitrary source extensions. Equal-start
defender guard/escape may pair; attacker CP-limit guard/escape starts differ and must reject.

004B1 accepted 2026-09-15: new private occurrence/checkpoint contract authenticates134 sources,
2576 cuts and588 historical event instances. Exact source/active occurrence, supported versus reached
terminal, full World/RNG/control, prefix/reference identities and public-only dual-slot scheduling
are replay-backed. Historical28 endpoints have zero execution. Eight fallback prefixes remain
unfinished.352 bridge cuts retain settled World, including native release states without World.
Fresh ordinary review/source audit and root81boundary/1670full gate pass,zero skips (3m13.738s).
Authentication returns defensive copies; fixture verification compares exact bytes. B2 execution/
proofs are accepted below; B3 parent evidence and C integrated closeout remain required before
checkpoint B and005.

004B2 accepted 2026-09-16 UTC: private child evidence binds native setup/content/scenario,
initial RNG, source/reference/checkpoint and current oracle/schema build identity. Public-only
controller preferences and schedule must match supported reference; incomplete references reject
bounds beyond retained history. Exact native transitions, owner/System fallback semantics,
separate reconstruction/re-adjudication and explicit attempted failure observations are retained.
FailedAttempt records actual copied trustedClock; artifact inventory excludes its own manifest.

Prior full native run passed134 sources,2442 transitions and44 owner-triggered System fallbacks.
Final unchanged native bodies are independently AST-checked; final focused run passed134 admissions,
7 representative readbacks and22 failure/forgery/clock checks. Regenerated fixture contains134 source
summaries and12 actual children. Fresh ordinary review/source reconciliation and root literal proof/
inventory checks pass. Root format/build and81 boundary/1670 full tests pass,zero skipped; no src/tests
changes since gate. Evidence is checkpoint-scoped and unregistered, not full fresh-session/runtime
Snapshot or publication parity. B3/C remain required before checkpoint B;005 has not started.

004B3 accepted 2026-09-18: authenticated parent manifests validate children before counts and
fingerprints, preserve failed status, enforce equal pair bindings and compare actual audience/action
streams. Final12child/10parent checks and strict rejection/readback tests pass; ordinary review and
root literal audit pass. Unequal-length null arms are authenticated-prefix helper evidence only.
C integrated closeout remains required before checkpoint B and dormant005 implementation.


004C accepted 2026-09-18: exact72 requirements and planned runtime mappings reconcile99 source
pins,12 profiles,51 accepted-reader witnesses,28 Task003 traces and2440 measured records across
11 capacity groups. Integrated rejection/readback checks pass in19.583s; ordinary source/code
review approves. Root just check passes format/build,81 boundary and1670 full tests,zero skipped.
This closes Task004 and contract checkpoint B; dormant005A/B may begin. No runtime activation,
artifact publication, hosted service, full Snapshot parity or completion of planned runtime ACs
is implied. Reviewed-versus-accepted spec hashes and all evidence are retained locally.

005A accepted: dormant complete RulesInput1 types and pure explicit-dice Combat arithmetic.
Source parity covers36 morale/360 loss cells with only3 approved gap amendments; exhaustive
1296 morale pairs,6480 joint coordinates,8840 capture/refusal rows and44208 weighted paths pass.
Focused native19/19 tests and build0warnings/errors pass; ordinary review approves. Existing
Rules9 registration and Combat code remain unchanged. Strict005B codec and final full gate remain.


005B and parent005 accepted: strict RulesInput1 codec reproduces30,395 canonical bytes and frozen
sha256:fafb24792c9e3f774c368f85c02d1d068f84c9c78e67bf8324723257d0f13029. Typed serialization
normalizes copied identity/numeric sets while preserving procedure order; raw readback accepts
only exact approved authority bytes and expected hash. Complete source/provenance/amendment/
policy metadata, malformed/forged input rejection, limits and defensive-copy tests pass.
Dormant artifact factory leaves registered Rules9 unchanged. Focused005B52/52 tests, ordinary
review and final root just check pass: format/build clean,81 boundary/1741 full tests,zero skipped.
All71 new focused tests and six legacy Rules/registration baselines reconcile. Task006 follows;
public Combat/cycle actions, hosted transport, durable publication and Runner activation remain later.

004A1 overnight hard stop, 2026-09-14 local /2026-09-15 UTC: ordinary fresh-context review found
equal waiting-side observations before/after an opposing private seal, but the same proposal at
trusted time3500 accepts with high-water3000 and cancels/rejects with high-water4000. Both seal
orders reproduce this against frozen authority. Accepted POL-004 requires regression fallback;
POL-006 and PRO-AC-010 require equal semantic outcomes. No explicit regression exception exists.
The [blocked candidate](../specs/combat-side-projection-v1.md#known-blocker-private-seal-changes-clock-regression-outcome)
and [failing diagnostic](../../.planning/2026-09-14-overnight-combat-wave-01/evidence/clock-high-water-counterexample.py)
retain the conflict. A1 is not accepted;004A/004/checkpoint B remain open and005 has not begun.
Focused candidate vectors and repository gate pass, but cannot override this failing privacy
acceptance case. Owner selected strict privacy on2026-09-15; the
[policy disposition](combat-cycle-policy-reconciliation.md#clock-privacy-correction--owner-decision-2026-09-15)
authorizes an explicit versioned clock correction preserving historical authority bytes. Owner also
renewed an eight-hour work window11:39:39–19:39:39 UTC, same scope and no merge. No formal
independent-review pass occurred; concrete successor verification remains required.

Clock correction split before edits: root owns policy/navigation disposition; `W01-CLOCK-DESIGN`
selects the minimal correction and exact version boundaries with read-only source evidence.
`W01-CLOCK-CONTRACT` then owns `combat-sealed-round-v2` spec/schema/fixture/oracle plus this plan, with clock,
privacy, replay and old-reader rejection vectors. `W01-CLOCK-INTEGRATE` follows in the four
unaccepted side-packet files plus this plan. No historical packet is rewritten; any additional
independent contract family requires a recorded bounded split first. Each behavioral packet needs
TDD, ordinary fresh-context quality review and root `just check` before its accepted commit.

W01-CLOCK-CONTRACT accepted after ordinary fresh-context review and root gate,2026-09-15:
[round-v2](../specs/combat-sealed-round-v2.md) retains10synthetictraces/68cuts,12semanticgroups,
610replay mutations,340raw rejects,288clock comparisons/retries and480lifecycle retries. Root
independent88admission comparisons pass while v1 counterexample remains reproducible. `just check`
passes81boundary and1670fulltests,0skipped,build0warnings/errors. Exact fixture-byte validation
closes review's P2 numeric-coercion finding. This accepts only the clock packet;004A1/004/B remain
open pending side integration. Evidence: [ordinary review](../../.planning/2026-09-14-overnight-combat-wave-01/evidence/clock-v2-review.md),
[full gate](../../.planning/2026-09-14-overnight-combat-wave-01/evidence/clock-v2-just-check.log).

W01-CLOCK-INTEGRATE /004A1 accepted,2026-09-15: ordinary fresh-context review approved;
root `just check` passes 81 boundary and 1670 full tests, zero skipped, build zero warnings/errors.
Side oracle passes 23 semantic groups, 58 traces/466 cuts, 207 submissions, 969 mutations,
580 raw rejects, 69 receipt/stale bindings and 672 corrected clock comparisons/retries.
Reviewer independently checked 1152 equal-outcome cases; root checked 144 serialized outcomes
across eight equal-observation pairs and 17 legacy admission rejects. Source audit verified all
201 literal goldens and 23 pins; original 18 audience traces remain exact. Historical diagnostic
still documents v1 failure. This accepts A1 only; A2/A3 and parent004/B remain open.
Evidence: [review](../../.planning/2026-09-14-overnight-combat-wave-01/evidence/side-clock-review.md),
[gate](../../.planning/2026-09-14-overnight-combat-wave-01/evidence/side-clock-just-check.log).

W01-CLOCK-RESULT accepted,2026-09-15: separate version2 command/state/event and policy binding
consume exact RoundState2 through 32 synthetic branch/side/order lineages. Fresh mandatory windows
ignore prior audit times; live-window floor/deadline and Config1 fallbacks remain explicit. Ordinary
fresh-context review approved all five axes. Frozen oracle passes10groups/304cuts/3728mutations/
1360raw/384timing/200same-owner comparisons; reviewer adds1904retry/272tamper/208clock probes.
Root independently checked192paired outcomes and `just check` passes81boundary+1670full,zero
skipped,build zero warnings/errors. Source audit verifies784literals/11pins and32unchanged historical
files. This accepts only result-v2;004A2/A3/B/C and005 remain open.
Evidence: [review](../../.planning/2026-09-14-overnight-combat-wave-01/evidence/result-v2-review.md),
[gate](../../.planning/2026-09-14-overnight-combat-wave-01/evidence/result-v2-just-check.log).

Downstream clock integration split before edits: `W01-CLOCK-RESULT` owns new
`combat-result-settlement-v2` spec/schema/fixture/oracle plus this plan after round-v2 acceptance.
It retains World7, selected Rules10 arithmetic and role-ordered RNG while validating corrected
committed-round evidence and mandatory-window timing. Complete this packet before004A2 consumes
the corrected result lane. Result-v2 timing disposition: each new mandatory window uses its own
independently trusted valid opening instant and checked fixed budget. Earlier private accepted
timestamps cannot gate that opening or trigger fallback. Choices within the live window use its
own published timing gate; any global accepted-time maximum is audit evidence only. Preserve
Config1 budget/fallback values. Every ResultCommand2, ResultState2 and ResultEvent2 binds
`resultClockPolicyId=sandtable.combat.mandatory-window-clock.v2` and the exact
`roundClockConfigurationHash`; separate v2 identity domains bind the corrected opening policy.
Retained eight-branch corpus has same-owner retreat/custody sequences: the paired10001/11000
retreat acceptance then10500 custody opening test proves prior-time isolation, not an opposing-
retreat leak. Do not invent a cross-owner gameplay branch. Retain all eight branches, both acting
sides and seal orders through exact RoundState2 bytes, including4000→3500 seals.
`W01-CLOCK-CYCLE` is a required bounded bridge before004A3/004B: new
`combat-result-cycle-finish-v1` spec/schema/fixture/oracle plus this plan. Authenticate each of32
native Result2 closed lineages, derive existing-shape empty-pending Reserve and settled cycle
kernel bases explicitly, then replay Reserve open/completion and supported cycle opening/finish
to same-slot Truck Convoy entry. Retain exact World/RNG/guards/entitlements/future obligations.
Never invoke old base constructors/readers that reconstruct Result1 or join an unrelated003 finish.
The pre-retreat Movement-end certificate stays named/pinned synthetic evidence. New empty-release
clock scope starts with null audit high-water while full Result2 audit history stays retained;
cycle timing opens independently under explicit bridge policy. Historical kernels/bytes unchanged.
No repeat, guard action, upkeep or maturity execution. This closes the required combat-then-finish
evidence seam without new gameplay policy or fullSnapshot allocation. Implement after accepted004A2
with the sole writer; review/full gate/accepted commit precede A3 consumption.

W01-CLOCK-CYCLE accepted2026-09-15:11focused groups,32native lineages/160cuts/128retries,
192prior-time comparisons and24source-fallback rejections. Fresh ordinary review approved after
P2source gate bound actual owner author/reason/disposition; valid retiming remains admitted.
Independent source audit verifies14pins/320nested literals/128retained suffixes; root1088clock/
recovery checks and `just check`81boundary/1670full tests pass,0skips. Maximum117109Bbase and
33361Bstate. Accepted bridge closes synthetic same-lineage combat-then-finish evidence only;
A3/B/C and005 remain open.

`W01-CLOCK-SNAPSHOT`, if full corrected snapshots are used by CON006,
owns new `combat-snapshot-composition-v2` spec/schema/fixture/oracle plus this plan; freeze its exact
payload/arm identity before writes. Existing00328trace creation-rooted handoff stays byte-exact;
corrected positive C3 evidence remains explicitly synthetic.004C reconciles both support sets,
so no separate positive creation-rooted assault implementation is implied.


TASK-003 sizing refinement, 2026-09-06, before authority edits: its three contract families cross
independent subsystems and cannot honestly fit one3–5-file change. Preserve parent003 and split
into the following ordered contract checkpoints, each at most five primary files including its
verification and plan update. Small navigation updates may follow separately. Parent003 closes
only after all four slices and their cross-contract checks pass; this is not a larger PR request.

| Slice | Frozen output | Verification / successor |
| --- | --- | --- |
| `CMB-TASK-003A` | Registry inventory, Setup7 binding/initialization, initial component/CP/ammo/readiness value shapes | [Setup/initial-value packet](../specs/combat-creation-ledger-v1.md) complete:63 negative vectors, hash/provenance/stage checks; implementation remains gated |
| `CMB-TASK-003B` | Durable World7, relationship/loss/capture/guard/escape/future-obligation values and settlement receipts | [World7/settlement packet](../specs/combat-world-settlement-v1.md) complete:6goldens/57 negatives/112 isolated cuts; [review5](../reviews/combat-progress-review-5.md) Ready with non-blocking follow-ups, status correction applied |
| `CMB-TASK-003C` | Exact Rules10/config bundle bytes/hash, Snapshot/creation and sealed selection/round/step/commit/result command/event envelopes | Exact version/identity/hash framing, deadline/retry suffix and restart-cut matrix; after003B |
| `CMB-TASK-003D` | Cycle/release/history and ordinary break-off movement receipts; combined CON-002–004 reconciliation | Cumulative CP/immediate DP, affected-membership endings, prefix/release/history/continuation vectors and parent003 closeout; after003C |

TASK-003C/D dependency refinement, 2026-09-06, before003C implementation:003C combines Rules,
configuration and several independent authority codecs. Its final Rules10 hash depends on the
sequence/cycle artifact assigned to003D. Preserve the parent IDs and use the following bounded
order; never insert a placeholder hash or declare the full Rules bundle frozen early.

| Ordered slice | Contract output / dependency |
| --- | --- |
| `003C1` | [Rules-input/config packet](../specs/combat-rules-inputs-v1.md) complete:3 goldens,47 mutations,39 raw-byte rejections,7 clock cases and14 kind/budget/UTC boundaries; review6 Ready. Full Rules10 manifest is assembled by003C2. |
| `003D1` | [Sequence/catalog5 and cycle codec1](../specs/combat-cycle-sequence-v1.md) complete; review7 Ready:112 positions,1 interrupt,6 cycle edges; binary preimages/prefix probes,38 mutations,3,996 scope identities and896 actor materializations. No movement/history implementation. |
| `003C2` | [Rules10/Created11/Snapshot12 creation cut](../specs/combat-authority-envelope-v1.md) complete:4 goldens,67 mutations,36 raw-byte rejections,9 recovery boundary checks,12 identity/context forks and2 turn boundaries; actual C# Rules9 capture and Rules10 hash parity. Independent review8 Ready; local report recorded below. |
| `003C3` | Freeze selection, six-step, sealed-round, commit/result/settlement event and command envelopes, noninitial Snapshot12 state arms, persisted timing/receipt suffixes and tamper/restart matrix; after003C2. |
| `003D2` | Freeze release/history/ordinary movement receipts and reconcile CON-002–004 against003C3; close parent003 only after cross-contract checks. |

Each slice retains the five-primary-file cap, including verification and plan update.003C1 inputs
are prospective Rules artifacts, not a new registered ruleset or permission to begin005. The
sequence/identity dependency moves earlier; accepted gameplay policies and checkpoint B stay intact.

TASK-003C3 sizing refinement, 2026-09-07, before event/command edits: selection/step control,
two-party private rounds, and committed result/settlement are separate transition families. Keep
parent003C3 open and freeze them in dependency order, each within five primary files:

| Slice | Contract boundary |
| --- | --- |
| `003C3a` | [Selection/step control](../specs/combat-selection-steps-v1.md) complete:5 literal traces/41 event-control cuts,5 Control goldens,246 event mutations,164 raw rejects and time/retry/weather/FA guards. Positive path stops at Force Assignment; author checks only. |
| `003C3b` | [Sealed round/commitment fragment](../specs/combat-sealed-round-v1.md) complete:4 traces/23 replay cuts,276 event/state mutations,138 raw rejections; both seal orders, deadlines/retries, FA-AA proof and CP/ammo/history. Author checks only; full Snapshot12/public projection remain C3c/D2/004. |
| `003C3c` | Complete as a bounded first-Combat contract checkpoint: result/RNG/settlement, round/CA closure and composed noninitial Snapshot12 with inherited-family audit. Synthetic pre-Combat lineage remains explicit; D2/004/B gates stay open. |

C3c execution refinement (input `7bb2d11`): finish two bounded checkpoints in order, each with
spec/schema/fixture/oracle and this plan as five primary files. `003C3c.1` [result/settlement](../specs/combat-result-settlement-v1.md) is complete:68 replay cuts,728
mutations,340 raw rejections and96 timing checks. It freezes result/RNG,
settlement choices/effects and round/CA closure. `003C3c.2` [snapshot composition](../specs/combat-snapshot-composition-v1.md)
is complete:17 traces/149 full Snapshot12 cuts,1358 mutations and596 raw rejections, plus the
inherited-family compatibility/owner audit. Both checkpoints pass; C3c/C3 close within this bounded
first-Combat contract scope. Parent003C/D stays open for D2 cross-contract reconciliation and004/B.
This is a delivery split, with no new gameplay policy or independent-review budget.

This refinement changes no gameplay policy, acceptance criterion or review budget. C3a rejects
prepared/resolved/settled states until their exact dependent contracts exist; later C3b/c freeze
the same prospective family before checkpoint B, with no production version registered early.

D2 execution refinement (input `3ac1283`, 2026-09-07): three independent transition families
require separate checkpoints, each at most five primary files including verification and this plan.
Preserve parent003D2 and all25 top-level IDs:

| D2 slice | Boundary / order |
| --- | --- |
| `003D2a` | [Ordinary integer-infantry Movement/break-off receipt](../specs/combat-ordinary-movement-v1.md) complete: terrain plus maximum applicable Contact/Engaged cost, cumulative CP ceiling, immediate excess-CPA DP and atomic affected-membership endings. 36 replay cuts,486 mutations,120 raw rejections,630 source arithmetic coordinates and14 atomic guards pass. Continuation assessment must consume this same rule. |
| `003D2b` | Reserve Release dispositions/history and guarded repeat/finish control, including timing, progress, next-Movement exception and retained obligations. AfterD2a; split further before edits if release and cycle control exceed the five-file bound. |
| `003D2c` | Exact inherited sequence5 successor inventory, first-opening/prior-history binding and full noninitial composition/reconciliation against CON-002–004. AfterD2b; bound individual inherited families before editing. Parent003 stays open until all cross-contract checks pass. |

D2b execution refinement (input `89eac24`, 2026-09-07): `003D2b.1` [Reserve Release
control/history](../specs/combat-reserve-release-v1.md) is complete:13 literal cases/48 side-slot
traces,188 cuts,2368 mutations,840 raw rejects,20 timing and27 boundary checks. `003D2b.2` [guarded cycle control](../specs/combat-cycle-control-v1.md) is complete within its declared
private boundary:19 literal cases/64 side-slot traces,164 cuts,1748 mutations,700 raw rejects,43
boundary checks and216 cost coordinates. Release-completion replay, accepted progress, D2a movement
cost witnesses, repeat/finish timing and scoped Movement exception expiry are frozen. Actual
inherited Movement/Reaction/Breakdown progress adapters and potentially armed Combat continuation
remain explicit D2c/004 capability gates; unsupported inputs reject, never imply no continuation. Each checkpoint retains the
five-primary-file cap. Release history is a private authority arm; D2c still owns its full Snapshot12
and inherited predecessor integration. Multi-unit deadline probes are isolated ledger tests, not
an expansion of the selected one-unit-per-side Combat capability. D2b private control checkpoints
are complete; full positive World/Snapshot, inherited progress/continuation and CON-002–004
reconciliation remain D2c before parent003D2/003 closes. No new policy or review round is introduced.

D2c execution refinement (input `faa3282`, before edits): inherited families and composed authority
exceed one five-file checkpoint. Preserve parent003D2c/003 and execute these bounded children:

| D2c slice | Boundary and acceptance |
| --- | --- |
| `003D2c.1` | Exact current/successor dispatch declarations, bounded008/019 first-opening owner graph, and Reserve-completion/first-cycle-opening contract. Strict source/identity/readback evidence; isolated predecessor prefix remains explicit. |
| `003D2c.2` | Actual prospective creation→preamble/Weather→Reserve provenance and initial opening input. Split preamble, Weather and Reserve field families before edits; derive headers/receipts/RNG from accepted contracts. |
| `003D2c.3` | Sequence5 Movement/Reaction/Breakdown successors, Movement-end/progress provenance, positive Reserve movement and armed Combat continuation admission. Bound each independent family before edits; unsupported input never means no continuation. |
| `003D2c.4` | [Authority composition](../specs/combat-authority-composition-v1.md) complete: 28 actual first-opening/continuation traces, full World/Snapshot/history/obligation projections, capacity proof, CON-002–004 reconciliation and exact Task004 handoff. Parent003 complete; Task004 next produces complete 72-AC map. |

D2c.2 refinement (input `387445b`, before edits): four sequential family checkpoints,
each at most five primary files including its spec/schema/fixture/oracle/plan:

| D2c.2 slice | Contract boundary / order |
| --- | --- |
| `003D2c.2a` | Opening preamble: validated Created11→Initiative3→no-obligation convoy2→tactical2→order2→Weather entry. Actual prospective prefix/version/receipt/RNG provenance, both order choices, strict cut replay. |
| `003D2c.2b` | [Weather2](../specs/combat-weather-v1.md) complete:34 creation-rooted traces/68 cuts, all four outcomes and12 foul kind/location pairs, rejected RNG bytes, strict receipts/state and exact Organization successor. |
| `003D2c.2c` | [Stage-entry2](../specs/combat-stage-entry-v1.md) complete:12 creation-rooted traces/60 cuts through Organization/arrival/fleet-assignment/fleet-repair2; explicit Setup7 gates, preserved Weather/RNG/World and actual Reserve-entry history. |
| `003D2c.2d` | Reserve2 designation/completion: consume2c, derive real designation history and compose atomic first opening fromD2c.1 without synthetic predecessor hashes. |

D2c.2b execution boundary (input `3ca453a`, before implementation):
[Weather packet](../specs/combat-weather-v1.md), schema, retained fixture, executable oracle and
this plan are the five primary files. Consume exact C2 creation and all four2a events; bind the
existing Weather artifact to Rules10, cover four Fall outcomes/all foul location rows and rejected
RNG bytes, preserve World/holder/order, publish one Weather2 event/receipt and Organization entry.
Strict input/cached-state/replay/retry/cross-history negatives are required. Current C# source and
historical canonical artifacts stay unchanged; zero effects require the explicit Setup7 policy.

D2c.2c execution boundary (input `78be145`, before implementation):
[Stage-entry packet](../specs/combat-stage-entry-v1.md), schema, retained fixture, executable oracle
and this plan are the five primary files. Consume exact Created11, four preamble events and one
Weather2 event; require all four explicit Setup7 no-obligation gates. Resolve Organization,
Naval Convoy Arrival, Fleet Assignment and Fleet Repair2 in order, preserving World/RNG/Weather,
holder/order and prior receipts. Stop at state10's unmaterialized first-side Reserve entry.
Strict replay/cached-state/retry/cross-history negatives and source pins precede completion.

D2c.2d execution boundary (input `4c10ede`, before implementation): Reserve-designation specification,
schema, retained fixture, oracle and this plan are the five primary files. Consume complete2c
history, derive real designation receipts and compose the frozenD2c.1 completion/first-opening
semantics. Preserve World/Weather/RNG/order/history; verify empty/I choices, both acting sides,
exact retry and all predecessor/identity/cache rejection boundaries. Existing contracts stay frozen.
Owner approved parallel delivery: independentCIH-IMP-004 maintenance runs on a separate branch;
one Movement field/provenance preparation document may proceed alongside2d, with final interface
references bound only after2d freezes. Preparation does not complete3 or authorize runtime work.
Shared plan/navigation edits remain with the lead; publish separate bounded checkpoints.

These are prospective contract/oracle chains, not C# runtime replay. ParentD2c.2 closes with2d
composing the accepted creation-to-opening chain; later Movement/continuation and full Snapshot
admission remainD2c.3–4/004/B. No additional independent-review pass is authorized.

D2c.3a execution boundary (input `01ded95`, before implementation): freeze inherited
`element-moved`4 in one spec/schema/fixture/oracle plus this plan. Consume completed2d history;
normal Weather, initial independent nonmotorized infantry, ordinary non-Reserve movement and
featureless Clear neighbors form the first closed profile. Preserve symbolic sequence5 position
and resolve actor through accepted cycle authority. Reconcile legacy fields with cumulative CP,
immediate excess-CPA DP and actual event/receipt/progress provenance. Source-pin no-window and
empty-cohort accounting claims; preserve active route flow and reject unsupported positive families.
Strict replay/cache/retry/cross-history negatives and literal movement outcomes precede completion.
Next bounded3b must derive owner `element-movement-stopped`2, System empty-cohort
`breakdown-stop-resolved`2 and then `movement-segment-completed`3/end proof in order. Even
nonvehicle routes require stop resolution before idle completion; never synthesize idle flow.
This future empty-cohort lifecycle does not close positive vehicle Breakdown or Reaction families.
Positive Reserve movement and armed continuation remain .3 requirements. No parent .3/.4/003/004/B
or runtime closure.
Owner requested continuation after review12 Ready; reviewed parent branches stay unchanged on this
separate feature branch. Review12 of12 covers `01ded95` and maintenance `0e815b2` only; new work
has author checks, with no further independent pass authorized.

D2c.3b execution boundary (input `d76ac3f`, after review13 Ready): one new
[spec/schema/fixture/oracle packet](../specs/combat-inherited-movement-lifecycle-v1.md) plus this plan.
Replay actual3a ordinary infantry routes into owner deliberate stop2, System empty-cohort
stop-resolved2, then owner Movement-completed3. Freeze11/15/12 retained legacy fields plus authority,
accepted input, suspended sequence5 cycle/Movement context and receipt bindings. All original World,
RNG, member, route and material-progress evidence persists. Initial ordinal1 Movement-end proof
uses actual final locations and new completion receipt; no fabricated ordinal0 predecessor proof.
Both owners, CP2/10/12/14, distance2/3 exclusions, every cut/retry, altered pending context/history,
re-signed effects, canonical caches and capability-vs-authority identity negatives form acceptance.
Profile has no release exceptions or positive vehicle checks; broader completion expiry remains open.
This child ends at Breakdown Determination. Next bounded3c must retain actual System
`breakdown-segment-completed`2 before Combat Position Determination. It cannot certify armed Combat
continuation or close Reaction/vehicle/Reserve families, .4 composition,003/004/B or runtime gates.
User authorized continuation after reading review13; no further review instance is launched.

D2c.3c execution boundary (input `a3f2f76`, before implementation): one new
[spec/schema/fixture/oracle packet](../specs/combat-inherited-breakdown-completion-v1.md) plus this plan.
Derive System `breakdown-segment-completed`2 from the complete actual3b prefix, retaining all11
legacy fields and stage1 action identity. Sources belong to the predecessor Breakdown position;
successor is the next sequence5 first Combat Position Determination. Preserve World/RNG, members,
actual Movement-end proof, progress, original receipts and opening/cycle authority. Eight both-owner
CP2/10/12/14 cases, strict canonical replay/cache, authenticated retry, re-signed effects and missing/
foreign lifecycle histories are required. One event/receipt/version/prefix increment only. This
does not admit Combat actions, a synthetic C3a base, positive Reaction/vehicle/Reserve or continuation,
or full Snapshot composition. No new independent-review pass is authorized by this continuation.

D2c.3d execution boundary (input `8219e16`, before implementation): one new
[spec/schema/fixture/oracle packet](../specs/combat-inherited-selection-v1.md) plus this plan. Consume
the complete actual3c history for both CP12/14 owners and retain its moved World, Cohesion,
Weather/RNG, proof/progress, receipts, prefix and symbolic Combat position. Derive the closed
voluntary-adjacent candidate assessment from supported facts; rejection or missing support cannot
mean an empty set. For this rear/supply profile, System opens the Combat segment without a decision
window and closes selection as no-selection in exactly two events. Preserve stepIndex0 and keep the
segment open for later structural traversal. Strict replay/readback/retry, re-signed effects,
malformed bytes, cross-history/owner and synthetic-C3a substitutions precede completion. Full
no-attack step traversal must be a separate bounded child. Positive Reaction/vehicle/Reserve
movement, armed continuation, .4 composition,004/B and runtime remain open.

D2c.3e execution boundary (input `419aae4`, before implementation): one new
[spec/schema/fixture/oracle packet](../specs/combat-inherited-no-attack-v1.md) plus this plan. Consume
only the exact closed 3d empty-selection Control for both CP12/14 owners. Nest it byte-identically,
then emit six System `combat-step-completed`2 events in catalog order from Position Determination
through Close Assault to same-slot Reserve Release. Every event derives `proofKind=no-attack`, links
the selection closure as disposition, links opening/prior-step receipt as predecessor, and advances
version/prefix once. Final outer Control closes; nested 3d segment state remains immutable. Strict
cut replay/readback/retry, re-signed event/control mutation, malformed bytes, cross-history,
incomplete predecessor, positive-candidate, order/duplicate and capacity rejection precede
completion. Arrival performs no release or material mutation. Positive families, .4 composition,
004/B and runtime remain open.

D2c.3f execution boundary (input `0c4fa07`, before implementation): one new
[spec/schema/fixture/oracle packet](../specs/combat-inherited-reaction-trigger-v1.md) plus this plan.
Consume only the exact accepted first-move prefix from the actual 3a Movement Control for both
CP12/14 owners, then return the phasing representation from its rear area to its assault origin.
The committed destination is adjacent to exactly one frozen opposing combat representation, so
System emits one `element-moved`4 event that charges the Clear2 cost once, advances
version/prefix once, opens one identity-bound Reaction window, switches current position to the
explicit Reaction interrupt and suspends the same phasing route. Replay accepted
history from creation before each fresh command; cache equality is diagnostic only. Strict
cut replay/readback/retry, re-signed event/control mutation, malformed bytes, cross-history/owner,
zero/multiple-opportunity, post-trigger movement and synthetic-prefix rejection precede completion.
Reaction participant selection/movement/completion/closure, positive vehicle/Reserve movement,
armed continuation, .4 composition,004/B and runtime remain open.

D2c.3g execution boundary (input `a6e6625`, before implementation): one new
[spec/schema/fixture/oracle packet](../specs/combat-inherited-reaction-lifecycle-v1.md) plus this plan.
Consume only the exact accepted 3f one-opportunity Reaction trigger for both owners. The reacting
independent infantry participant moves once from its assault area to its own rear, selecting the
sole frozen opportunity and opening its route; explicit participant completion then opens the
required empty-cohort Reaction-completed Breakdown stop. System resolves that actual stop before a
System no-eligible close clears the exhausted window and resumes the exact suspended phasing route.
These four events each advance version/prefix once and retain rotating public capability handles,
accepted creation/cycle authority and strict retry/readback evidence. World retains both moves;
RNG is unchanged and only the reacting move adds material progress. Player decline, timeout,
unavailable closure, another participant or move, positive vehicle/Reserve movement, armed
continuation, .4 composition,004/B, runtime and simulator remain open.

D2c.3h execution boundary (input `f75710a`, owner-approved 2026-09-11): one new
[spec/schema/fixture/oracle packet](../specs/combat-inherited-reserve-cycle-v1.md) plus this plan.
Consume only 2d's accepted seed1 Normal-Weather `I` histories for both owners. Owner directly
completes idle Movement without a fabricated route or stop; System completes idle Breakdown,
opens/closes an empty Combat selection and emits six no-attack step completions to same-slot
Reserve Release. Ten events preserve the immutable creation-rooted ReserveState, actual designation
receipt/history, `reserveStatus=I`, World, RNG, Weather, order, cycle and CP0. Movement-end proof
binds both final locations, empty ordinary-proximity exclusions and the actual completion receipt.
Public selection exposes zero candidates. Exact replay across11 cuts,20 accepted retries, deep
event/control mutation, malformed bytes, source drift, capacity and cross-history rejection form
acceptance. This checkpoint itself performs no release; 3i below consumes it. Repeat, positive
Reserve movement, vehicle continuation, armed Combat, .4,004/B, runtime and simulator remain open.

D2c.3i execution boundary (input `fbfd849`, owner-approved 2026-09-11): one new
[spec/schema/fixture/oracle packet](../specs/combat-inherited-reserve-release-v1.md) plus this plan.
Replay both 3h Normal-Weather Reserve-I terminals, derive an actual Release base at authority22,
open one Config-pinned decision, accept owner `release-I`, then complete at authority25 without
leaving same-slot Reserve Release. The matched own member changes I→none and records release cycle1,
CPA basis/voluntary ceiling10 and exact pending ordinal-2 Movement exception; World otherwise, RNG,
Weather, cycle ordinal, locations, CP0 and all prior designation/Movement/Combat history persist.
Only disposition is material progress. Exact retry/readback, valid-but-wrong mutations, malformed
bytes, cross-history authority, deadline, clock-loss/unavailable fallback, capacity and source-pin
checks form acceptance. Guarded repeat, positive Reserve movement, alternate owner conversion,
broader Reserve profiles, .4,004/B, runtime and simulator remain open.

D2c.3j execution boundary (input `765b2f9`, owner-approved 2026-09-11): one new
[spec/schema/fixture/oracle packet](../specs/combat-inherited-armed-continuation-v1.md) plus this
plan. Replay both accepted 3i release-I terminals, retain their exact ammunition10/TOE10/CP0,
pending ordinal-2 Movement exception and material-progress receipt, then prove an empty
Movement/Breakdown/prestep path reaches exactly one normal-Weather adjacent Combat candidate for
each owner. Pin the existing5 selection,4 sealed-round,8 result/settlement, and17 Snapshot
composition cases as full-result support. This pure proof emits no event and neither repeats nor
finishes the cycle. Resource, geometry, Weather, history, obligation, cross-owner, source-pin,
canonical-byte, mutation and capacity failures reject. Guarded repeat/finish composition, positive
Reserve movement, broader profiles, .4/004/B, runtime and simulator remain open.

D2c.3n execution boundary (input `e2302d2`, owner-approved 2026-09-12): one new
[spec/schema/fixture/oracle packet](../specs/combat-inherited-reaction-closure-v1.md) plus this plan.
Replay both exact3f one-opportunity trigger terminals and fork each into reacting-owner
`player-decline`, System `scripted-unavailable`, or System `timeout`. One compatible
`reaction-window-closed`3 event closes the sole unresolved opportunity, clears the window and
interrupt, restores the exact suspended sequence position and resumes the phasing route without
World/RNG/progress effects. Core timeout is reason-specific System authority, not clock scheduling;
host/worker deadlines remain deferred. Exact retry/readback, actor/reason/action mismatch,
authority-handle substitution, predecessor/event mutation, malformed bytes, competing-fork,
capacity and source-pin checks form acceptance. Active-participant fallback and second-move branches
are separate successors; multiple opportunities, positive vehicle Breakdown, .4/004/B, runtime and
simulator remain open.

D2c.3o execution boundary (owner-approved 2026-09-12): one new
[spec/schema/fixture/oracle packet](../specs/combat-inherited-reaction-active-fallback-v1.md) plus
this plan. Replay each exact3g post-first-move active Reaction state and fork it through System
`scripted-unavailable` or `timeout`. One compatible `reaction-window-closed`3 event closes the
active unresolved opportunity, clears the window, and records a reason-specific
`reactor-stop-closed` while retaining the exact phasing continuation. One mandatory
`breakdown-stop-resolved`2 event then consumes the empty-cohort infantry stop and resumes that
phasing route with unchanged World/RNG/progress. Exact retry/readback, player decline/no-eligible
rejection, reason/action/actor mismatch, predecessor/event mutation, malformed bytes,
competing-fork, capacity and source-pin checks form acceptance. Participant completion after the
second move is handled by `.3q`; multiple opportunities, positive vehicle Breakdown, .4/004/B,
runtime, host scheduling, public activation and simulator remain open.

D2c.3p execution boundary (owner-approved 2026-09-13): one new
[spec/schema/fixture/oracle packet](../specs/combat-inherited-reaction-second-move-v1.md) plus this
plan. Replay each exact3g post-first-move active Reaction state and accept one owner-authored
`move-reacting-element` command through stable public window and newly rotated opportunity handles. One
compatible `reacting-element-moved`3 event advances the same ordinary-infantry participant from
rear to supply, cumulative CP2→4, appends its inherited track, and retains exact active opportunity,
route identity, first-move version and suspended phasing continuation. Exact retry/readback,
wrong actor/action/route/history, authoritative-handle substitution, competing-fork, predecessor/event
mutation, malformed bytes, capacity and source-pin checks form acceptance. Participant completion
and closure are handled by `.3q`; multiple opportunities, positive vehicle Breakdown, .4/004/B,
runtime, public activation and simulator remain open.

D2c.3q execution boundary (owner-approved 2026-09-13): one new
[spec/schema/fixture/oracle packet](../specs/combat-inherited-reaction-movement-completion-v1.md)
plus this plan. Replay each exact3p authority15 terminal and accept owner
`complete-reaction-participant`, System `resolve-breakdown-stop`, then System
`close-reaction-window-no-eligible-reactor`. Compatible completion3/resolution2/closure3 events
advance authority15→18, preserve the supply/CP4 World, three-location reactor track, RNG and
material progress, consume the original reactor route only through mandatory empty-stop resolution,
and resume the exact suspended phasing route. Exact cut replay/readback/retry, rotated empty-option
capability, wrong actor/action/history, authoritative-handle substitution, competing-fork,
event/state mutation, malformed bytes, capacity and source-pin checks form acceptance. Multiple
opportunities, positive vehicle Breakdown, .4/004/B, runtime, public activation and simulator
remain open.

D2c.4's [Task004 handoff](../specs/combat-authority-composition-v1.md#task004-handoff) is complete
within the five-primary-file cap. It freezes CON-002–004 versions/hashes, 28 composed traces and
capacity results, six 12-AC requirement ranges, 11 capability exclusions and six remaining runtime
evidence lanes. Its integrated oracle regenerates actual predecessor histories before comparing
the retained golden: 31 direct plus embedded transitive source pins, 225 readbacks, 203 mutations,
six raw-byte rejects and eight trace/capacity/source boundaries pass. Parent003 closes on this
evidence. Task004 now consumes the exact
handoff, freezes CON-005/006 and expands all 72 ACs row by row. Checkpoint B still requires Task004;
neither the range mapping nor parent003 alone closes it.

Each checkpoint retains the five-primary-file cap. These refine already-required inherited work;
no gameplay policy, runtime activation or checkpoint-B acceptance changes. Historical review
statements below remain scoped evidence; the explicitly extended sequence is exhausted at15of15.

D2a isolated Movement fixtures may derive a settled World from C3c, but any intervening release/repeat
boundary remains explicitly synthetic untilD2b/c. Such probes do not certify actual continuation,
released-Reserve history, runtime movement, Reaction or a reachable repeating campaign.

Historical pre-B gate: Task003 authority contracts were accepted by retained composition evidence.
Task004 then froze outward contracts and reconciled all 72 ACs. Checkpoint B is now accepted;
the checks below remain constraints on runtime implementation.

[Author planning audit](../reviews/combat-delivery-plan-author-review.md) identifies an inherited
campaign-path handoff that this gate must make concrete. These are completion checks on the existing
compatibility/replay requirements, not permission to broaden the certified capability:

- 003C3c inventories creation, preamble, Weather, Reserve, Movement, Reaction and Breakdown event,
  reader and writer compatibility under Rules10/World7/Snapshot12. For each inherited family, record
  whether canonical bytes remain valid or a versioned successor is required, and the implementation
  owner.003D2 binds first-cycle opening and retained history to the actual Combat boundary.
- Assign bounded implementation children before accepting B. Task008 owns codec/restore work;
  first-cycle opening is assigned to019A after008D. The child plan schedules the new-context
  pre-Combat path and first opening before the first end-to-end predecessor-to-Combat test, without hiding those
  adapters into021 registration. Split any child crossing five primary files or an independent family.
- The contract packet must include a composed prospective trace with cross-family identity, prefix,
  version and resource checks; label assumed preconditions. In-process implementation tests must
  later derive the boundary from accepted predecessors, never promote C3a's synthetic probe hashes
  to authoritative history. Runtime proof remains unimplemented at B.
- After003 closes with its integrated contract checks and Task004 handoff,004 maps every design AC
  to named contract and implementation evidence, including composed snapshot capacity and
  unsupported-history rejection. Fragment oracles alone do not close003; neither003 alone nor the
  handoff alone closes checkpoint B.

Owner accepted the [review direction](../reviews/combat-delivery-plan-author-review.md#owner-disposition)
to split008 into codec/restore and inherited-path adapters, bringing first opening forward from019
as needed for composed tests. [D2c.1](../specs/combat-inherited-successors-v1.md#bounded-implementation-ownership) now declares008A–H
and019A with bounded scope/dependencies. Checkpoint B is now accepted; the numbered parent rows below retain their identities.

### Checkpoint C — dormant table and fixture foundations

| Task / size | Output and acceptance criteria | Verification | Dependencies / likely paths |
| --- | --- | --- | --- |
| `CMB-TASK-005` / M, 3–5 files | Implement Rules-owned selected tables and pure arithmetic from approved manifest; preserve ordered pair coordinates and conditional capture. No campaign activation. | Full normalized source comparison, every coordinate and all reachable differential branches; RNG research goldens as supplemental vectors. | 001–004; [Rules](../../src/Cna.Core/Rules/), [Rules tests](../../tests/Cna.Core.Tests/Rules/). |
| `CMB-TASK-006` / complete | Versioned synthetic Content/scenario admission loads explicit component, Ammo/readiness and retreat-supply facts through an internal certified catalog. Content4/5/6 bytes and readers remain unchanged; malformed, altered, extra and unsupported profile data reject before gameplay. | `CombatContentTests`: exact 10,339-byte/hash readback, all70 frozen negative vectors, defensive copying, structural equality, construction-order stability, provenance hash sensitivity and historical reader compatibility. | 002–005; [Content7 models](../../src/Cna.Core/Content/ContentPackV7Models.cs), [strict codec](../../src/Cna.Core/Content/ContentPackV7Serializer.cs), [Content tests](../../tests/Cna.Core.Tests/Content/CombatContentTests.cs). |

Checkpoint C complete: focused suites and repository build/format/full gates pass; verified tables
and exact Content7 fixture exist, but no active Combat capability is advertised.

### Checkpoint D — loss-capable state and strict persistence

| Task / size | Output and acceptance criteria | Verification | Dependencies / likely paths |
| --- | --- | --- | --- |
| `CMB-TASK-007` / implemented | Dormant World7, typed creation/settlement obligations and certified Content7 initial seeds retain component/ammo/readiness provenance. Integer infantry spending enforces ordinary 150% CPA and stricter released-Reserve ceilings, immediate excess-CPA DP, and separate mandatory retreat overrun; guard transfer conserves TOE and preserves existing Breakdown state. No gameplay activation or persistence. | `CombatWorldTests`: initial inventory/provenance, foreign scenario/policy rejection, CPA10 11/15 versus rejected16, mandatory E10→11, Reserve ceilings, guard transfer, malformed/dangling lots, future scope and receipt links. Review fixes cover capped victory RP, complete receipt prefixes/relationship endings, disposition distance/kind agreement, ordered repeated excess-CP causes, selected loss TOE/DP threshold, Clear-cost escape bounds, source-backed result facts, role-specific loss/capture arithmetic, exact retreat CP/DP/RP, paid pre-loss resource bounds, custody-linked guard/replacement obligations, open-prefix element/lot/cause effects, relationship publication, and custody donor/guard provenance. Full `just check`: 1,778 passed, zero skipped; 003A/003B frozen verifiers pass. | 003/006; [World7](../../src/Cna.Core/Campaigns/CampaignWorldV7.cs), [Combat values](../../src/Cna.Core/Campaigns/CampaignCombatWorldV7.cs), [Campaign tests](../../tests/Cna.Core.Tests/Campaigns/CombatWorldTests.cs). |
| `CMB-TASK-008` / parent, in progress | Implement canonical snapshot/history codec and strict restore for new state through bounded children below. Historical bytes unchanged; reject missing/forged cycle or settlement evidence and unsupported migration. No child alone enables live Combat admission. | Per-child canonical golden, tamper and readback tests; completed parent must recover full retained history when fresh admission is disabled. | 007; [Campaigns](../../src/Cna.Core/Campaigns/), [Campaign tests](../../tests/Cna.Core.Tests/Campaigns/). |

#### Task008 execution index

Merge snapshot: base `e64bed9` (planning sync #123), 2026-09-19.

Objective: implement canonical Combat creation, snapshot and inherited-history codecs with strict trusted restore. The [creation envelope](../specs/combat-authority-envelope-v1.md), [Snapshot12 composition](../specs/combat-snapshot-composition-v1.md) and [successor ownership](../specs/combat-inherited-successors-v1.md#bounded-implementation-ownership) own exact bytes and meanings. Status below is implementation and merge state, not public admission. Each pending child needs literal parity, malformed/forged/alternate-byte rejection, focused C# tests and predecessor compatibility evidence; the parent additionally needs full retained-history recovery with **fresh admission disabled**. No child alone closes that parent gate.

| Slice / objective | After | Acceptance evidence and test focus | Current status / blocker | On local main |
| --- | --- | --- | --- | --- |
| `008A0` initial World7 codec/readback | 007 | 3 C# codec tests; 3,273-byte golden, altered-byte and noninitial rejection; `just check` 1,781 pass | Implemented; noninitial reader awaits causal families | Yes, `09734ff` (#118) |
| `008A1a` dormant Rules10 manifest | A0 | Manifest C# test; 10,495-byte golden, Rules9 hash and source identity | Implemented; no gameplay registration | Yes, `5ad833a` (#119) |
| `008A1b` Setup7/configuration codecs | A1a | Creation-input C# tests; 1,655/955-byte goldens, trusted binding, variants and canonical negatives; `just check` 1,788 pass | Implemented; request/Created11 still absent | Yes, `1d5e5f5` (#122) |
| `008A1c` creation request and Created11 binding | A1b | Exact request/Created11 goldens, identity/retry conflicts, changed trusted artifact and raw-byte negatives | Implemented and reviewed on `codex/combat-task008-creation-binding`; focused52/full1,840 pass, three independent rounds complete | No |
| `008A2` Snapshot12 creation root/readback | A1c | Exact creation Snapshot12 golden; recompute from trusted request/Created11/World; disabled-admission readback and noninitial rejection | Implemented and reviewed on `codex/combat-task008-creation-snapshot`; focused34/full1,874 pass, three Ready rounds; publication evidence boundary pinned below | No |
| `008B` preamble/sequence adapter, parent | B1 and B2 below | Opening and stage event parity, prior-prefix and old-reader negatives | Implemented and reviewed; B1 and B2 accepted on stacked feature branches | No |
| `008B1` opening preamble | A2 | Six frozen creation-rooted traces through four opening events/state5 Weather entry; exact bytes at every cut, actor/retry/forgery and old-reader negatives | Implemented and reviewed on `codex/combat-task008-opening-preamble`; focused12/full1,886 pass, three Ready rounds | No |
| `008C` Weather adapter | B1 | Retained dice/RNG and source/context parity across replay cuts through state6 | Implemented and reviewed on `codex/combat-task008-weather`; focused42/full1,928 pass, three Ready rounds | No |
| `008B2` stage-entry adapter | C | Four explicit-none stage events from fully replayed Weather through state10 Reserve; all Weather kinds, nine receipts and null-side order handoff | Implemented and reviewed on `codex/combat-task008-stage-entry`; focused17/full1,945 pass, three Ready rounds | No |
| `008D` Reserve codec/adapter parent | D1 and D2 below | Designation/completion bytes, own history and no resource reset; terminal projection requires019A | D1/D2 codecs and019A terminal replay implemented and reviewed on stacked branches | No |
| `008D1` Reserve designation | B2 | Full state10 predecessor, optional own none→I designation, exact pre-completion bytes/history and unchanged resources | Implemented and reviewed on `codex/combat-task008-reserve-designation`; focused23/full1,968 pass, three Ready rounds | No |
| `008D2` Reserve completion codec | D1 | History-derived OpeningBase and exact completion2 bytes including ordinal1 identity; no fabricated terminal state | Implemented and reviewed on `codex/combat-task008-reserve-completion`; focused44/full1,989 pass, three Ready rounds | No |
| `019A` first-cycle opening projector | D2 | Apply same completion2 event atomically; terminal Reserve state11/12, complete retry/readback and Movement handoff | Implemented and reviewed on `codex/combat-task019a-first-opening`; focused67/full2,012 pass, three Ready rounds | No |
| `008E` Movement adapter, parent | E1 and E2/G1 below | Move/stop/completion event parity, CP/DP/history and old-context rejection | E1 and E2/G1 implemented and reviewed; ordinary Movement adapter scope complete | No |
| `008E1` ordinary Move4 | D and019A | Actual completed Reserve history; both owners, seven moves each, CP/DP/route/progress and strict replay | Implemented and reviewed on `codex/combat-task008-inherited-movement`; focused31/full2,020 pass, three Ready rounds | No |
| `008E2/G1` route lifecycle | E1 | Owner stop2, System empty-cohort resolution2, owner Movement completion3 and actual end proof | Implemented and reviewed on `codex/combat-task008-movement-lifecycle`; focused20/full2,032 pass, three Ready rounds | No |
| `008F` Reaction adapter | E | Trigger/participant/closure replay, position/owner privacy and unsupported-profile rejection | Pending; needs Movement | No |
| `008G` Breakdown adapter, parent | E2/G1 and G2 below | Stop/resolution/completion, BP/lot provenance and Combat-entry boundary | Open; concrete lifecycle dependencies below | No |
| `008G2` Breakdown completion | E2/G1 | System completion from actual end proof into Combat entry; preserve World/RNG and receipts | Pending | No |
| `008H` noninitial Snapshot12 and full restore | B–G and `019A` first opening | Complete trusted event/receipt ledger, each causal cut and tamper/reorder/omission rejection; recover with fresh admission disabled | Pending; needs all required families and first opening | No |

`019A` is the separately owned first-cycle-opening projector after `008D2`; it must precede inherited Movement replay, `008H` and the first predecessor-to-Combat integration test. B–G may need smaller PRs at the five-primary-file limit. A1c is accepted at `a8eed35` ([PR124](https://github.com/dills122/sandtable/pull/124)); A2 at `b67b4f9` ([PR125](https://github.com/dills122/sandtable/pull/125)); B1 at `22f8da6` ([PR126](https://github.com/dills122/sandtable/pull/126)); C at `a0babdb` ([PR127](https://github.com/dills122/sandtable/pull/127)); B2 at `3ded1eb` ([PR128](https://github.com/dills122/sandtable/pull/128)); D1 at `073423f` ([PR129](https://github.com/dills122/sandtable/pull/129)); D2 at `fa5e723` ([PR130](https://github.com/dills122/sandtable/pull/130));019A at `ba58c43` ([PR131](https://github.com/dills122/sandtable/pull/131)); E1 at `e641bd3` ([PR132](https://github.com/dills122/sandtable/pull/132)); E2/G1 route lifecycle is active. The three merged rows are ancestry-verified against the base above.

**B execution refinement.** [Opening preamble](../specs/combat-opening-preamble-v1.md#replay-retry-and-next-family-handoff) ends at state5 Weather entry; [Weather](../specs/combat-weather-v1.md) consumes all four accepted opening events, and [stage entry](../specs/combat-stage-entry-v1.md) reconstructs completed Weather before its four transitions. Therefore execution is **A2 → B1 → C → B2 → D**. B remains the parent ownership group for opening/stage adapters; C depends on B1, not completion of parent B. No wire contract changes. A supplied synthetic Weather state cannot substitute for C's creation-rooted runtime evidence in B2.

**D execution refinement.** [Reserve designation and first opening](../specs/combat-reserve-designation-v1.md) requires a single completion2 event containing atomic ordinal1 authority. Execute **B2 → D1 designation → D2 completion codec → 019A projection → E** within the five-primary-file cap. D2 derives its internal OpeningBase only from actual history and validates exact completion bytes; it does not admit a caller-supplied base or claim post-completion replay.019A applies that same event, with no additional command/event. Full terminal Reserve parity, designation retry after completion and Movement-entry handoff remain open until019A. D1 also adds bounded causal Reserve World serialization without weakening initial-only World7 readback. No wire contract or parent-task scope changes.

**E/G execution refinement.** [Inherited Movement](../specs/combat-inherited-movement-v1.md) requires completed019A history and provides ordinary Move4. [Route lifecycle](../specs/combat-inherited-movement-lifecycle-v1.md) then requires owner stop2, System stop-resolved2 and owner Movement-completed3 before [Breakdown completion](../specs/combat-inherited-breakdown-completion-v1.md). Execute **019A → E1 Move4 → E2/G1 route lifecycle → G2 Breakdown completion**. The frozen successor inventory assigns stop/resolution to Breakdown, so E2/G1 has explicit shared ownership; G does not wait for an already completed E parent before supplying mandatory resolution. Parent E/G close only with their assigned event evidence. No synthetic idle flow, skipped empty-cohort resolution, changed wire contract or positive vehicle-check claim. Reaction F and full restore H remain separate gates. Each packet retains the five-primary-file cap; split further by causal event if implementation exceeds it.

**Gate audit.** A1c→A2 is the immediate creation-binding path; B1→C→B2→D1→D2→019A→E→F/G, then H, provide the inherited replay path. The [creation contract](../specs/combat-authority-envelope-v1.md#creation-publication-and-recovery-contract) additionally asks Task008 to prove atomic uniqueness and lost-response behavior using actual persistence, while this plan's checkpoint D describes in-process recovery and [HOST-RSH-001](../research/orleans-publication-feasibility.md) leaves durable provider choice open. A2 pins the Core seam and remaining provider evidence as `HOST-PUB-001` below; its actual-persistence obligation stays open. A1c/A2 codec parity alone cannot satisfy publication or parent restore. Do not infer durable process restart from a memory CAS test.

Tasks009–019 already name identity, seal, debit, result, settlement, release, Movement and cycle witnesses with focused tests; [E/G/H](#progressive-execution-evidence) require cumulative Core replay and H needs an actual predecessor trace. Tasks020–021 own side-safe admission and recovery registration; 022–024 own Exercise/Runner readjudication and clean retained runs; 025 reconciles all 72 ACs. The cross-gate risk is promoting the [synthetic C3a lineage](../specs/combat-snapshot-composition-v1.md#trusted-composition-boundary) or a partial Task008 reader into E/H/I evidence. Close it with a recorded accepted-predecessor trace at H, unsupported-history negatives at H/I and a Task025 evidence map that cites runtime results rather than contract oracles. The [roadmap MVP gates](../roadmap/pre-alpha-roadmap.md#post-skeleton-milestones-toward-the-first-playable-mvp) retain six-turn inventory, source/rights, durable process recovery and Maproom/hot-seat proof after the skeleton.

**Research check-in.** No new spike is justified at A1c: C2 fixes creation bytes and `HOST-RSH-001` already answers the bounded Orleans direction. The later decision question is which storage contract/provider can atomically publish command, event, receipt and head while recovering an ambiguous acknowledgment. Alternatives remain a bounded whole-record conditional write or a transactional append batch with derived checkpoint. The isolated same-process memory CAS probe supports retry/failure invariants, but cannot establish process durability, provider fencing or comparative cost. Recommendation: use its proposed versioned per-campaign commit batch as the evaluation target, without selecting storage. Core/host owners pin the Task008 publication-evidence seam at A2 below and revisit the provider decision at the production host gate (public020–021 plus one verified023 trace); evaluate actual commit ambiguity, crash/restart, fencing and bounded record size there. Six-turn content inventory stays post-skeleton unless a new source or measured dependency changes that gate.

**A2 publication-evidence boundary.** The [creation contract's runtime evidence allocation](../specs/combat-authority-envelope-v1.md#runtime-evidence-ownership) pins the Core seam, expected-head per-campaign batch target and provider failure matrix as `HOST-PUB-001`. Core/OrleansHost jointly own that proof after020–021 plus one verified023 trace, before production hosting/durable save acceptance. Task008's actual-persistence obligation remains open until those results exist; A2 and H do not waive it. Checkpoint D/H evidence permits dependent dormant Core work under the existing in-process scope. Report “Core codec/replay gate complete; publication evidence open” if H passes before hosting proof. No provider, production schema or persistence implementation is selected at A2.

Checkpoint D: codec tests prove obligations survive serialization and fresh Core reconstruction
for the states whose handlers exist. Later lifecycle tasks add their event handlers and boundary
cuts to this same contract, without changing frozen bytes silently. This is in-process recovery
evidence, not durable storage, silo/process restart or atomic Chronicle publication; those require
the separately scoped hosting work.
Task025 must retain the `HOST-PUB-001` obligation explicitly if still open; completing a Core
dependency does not close the whole creation/publication contract.

### Checkpoint E — identity and private choices

| Task / size | Output and acceptance criteria | Verification | Dependencies / likely paths |
| --- | --- | --- | --- |
| `CMB-TASK-009` / M, 3–5 files | Implement bounded certification and unit/component/opportunity bindings. Separate target-hex use from directional unit history; Contact/Engaged uses original participants, not location groups. | `CombatIdentity` relocation/new-arrival/ambiguous-profile/hidden-fork cases and all-result geometry certification. | 005–008; [Campaigns](../../src/Cna.Core/Campaigns/), focused Core tests. |
| `CMB-TASK-010` / M, 3–5 files | Implement dormant segment/selection/RBA-decline controls and six exact step closures. Empty/cancelled paths produce no attack, synthetic decline or progress; Prepared continuation is reserved for valid seals. | `CombatSteps` selected/empty/timeout/stale/out-of-order traces; restart at each step. | 009; [Campaigns](../../src/Cna.Core/Campaigns/), focused Core tests. |
| `CMB-TASK-011` / M, 3–5 files | Implement two private slots, duplicate readback, pinned deadlines and Prepared continuation. First seal preserves other-side revision; incomplete cancellation consumes no costs/history/RNG. | `CombatSeals` both orders, changed retry, before/equal deadline, backward clock, unavailable controller and forged suffix. | 010; [Campaigns](../../src/Cna.Core/Campaigns/), [Decisions](../../src/Cna.Core/Decisions/), focused Core tests. |

Checkpoint E: run lifecycle/recovery and paired-authorized-input checks. No hosting or public
activation yet; new capabilities are exercised through dormant Core tests.

### Checkpoint F — paid use and deterministic results

| Task / size | Output and acceptance criteria | Verification | Dependencies / likely paths |
| --- | --- | --- | --- |
| `CMB-TASK-012` / M, 3–5 files | Implement atomic commitment: CP5/3, Ammo10 each, directional attack history and per-segment target use. Recheck frozen base/trace before debit; paid use remains valid after ammo reaches zero. | `CombatCommit` duplicate/stale/failed-publication cuts; no RNG at commit, no double charge or invalid post-debit surrender. | 011; [Campaigns](../../src/Cna.Core/Campaigns/), focused Core tests. |
| `CMB-TASK-013` / M, 3–5 files | Implement atomic result/cursor publication using both pre-loss sides and role-ordered 8/9 accepted dice. Leave mandatory consequences pending; failed resolution publishes nothing and cannot cancel/reroll. | `CombatResolution` full-table outcomes, rejection bytes/block boundaries, oracle comparisons and restart from committed cursor. | 005/012; [Campaigns](../../src/Cna.Core/Campaigns/), [Randomness](../../src/Cna.Core/Randomness/), focused Core tests. |

Checkpoint F: full selected result coverage and atomicity pass. A result receipt remains an
intermediate checkpoint; it cannot advance into Reserve Release.

### Checkpoint G — mandatory settlement

| Task / size | Output and acceptance criteria | Verification | Dependencies / likely paths |
| --- | --- | --- | --- |
| `CMB-TASK-014` / M, 3–5 files | Implement retreat/refusal intent, simultaneous loss/capture allocation and subsequent actual retreat CP/DP/RP settlement. Preserve conservation and only grant victory RP after proved evacuation. | `CombatLossRetreat` refusal/zero-loss retreat, 30% loss threshold, mandatory CP overrun, capped RP and every event cut; timers cannot override accepted intent. | 013; [Campaigns](../../src/Cna.Core/Campaigns/), focused Core tests. |
| `CMB-TASK-015` / M, 3–5 files | Implement all positive-capture branches: guarded rendezvous/guard transfer or immediate unguarded escape entitlement. Preserve source origin, guard resources and future feeding/maturity obligations; no immediate TOE reunion. | `CombatCustody` both captor roles, route/cost/guard conservation, timeout and restart, retained future obligations. | 014; [Campaigns](../../src/Cna.Core/Campaigns/), focused Core tests. |
| `CMB-TASK-016` / M, 3–5 files | Implement original-participant relationships and terminal round closure. Raw Retreat suppresses Engaged even if refused; guards/new arrivals do not inherit. Immediate obligations must be empty before CA closes. | `CombatClosure` refusal/Engaged/adjacency variants and tampered/duplicate closure; future obligations remain stored. | 015; [Campaigns](../../src/Cna.Core/Campaigns/), focused Core tests. |

Checkpoint G: exercise every reachable settlement branch and restart cut. Settlement/state ACs now have concrete Core evidence; outward transcript and coherent public
admission evidence still wait for checkpoint I.

### Checkpoint H — Reserve Release and real continuation

| Task / size | Output and acceptance criteria | Verification | Dependencies / likely paths |
| --- | --- | --- | --- |
| `CMB-TASK-017` / M, 3–5 files | Implement release window and canonical own-unit dispositions, one pinned budget, first-I conversion/later-II retention fallback and explicit completion. Status changes retain release restrictions/history. | `ReserveRelease` first/later/empty, consumed convert, duplicate/stale/expiry, cumulative CP ceilings and no auto-release/repeat. | 016; [Campaigns](../../src/Cna.Core/Campaigns/), focused Core tests. |
| `CMB-TASK-018` / M, 3–5 files | Implement retained Movement-end proximity, next-Movement exception and ordinary Contact/Engaged break-off under the frozen cost precedence and ordinary 150%-CPA ceiling (stricter for released Reserves). Charge CP and any immediate excess-CPA DP, move and update affected memberships atomically; preserve unrelated relations and CP/BP/bands/broken lots, ammo/TOE/Cohesion and offensive-use history. | `CycleMovement` Contact/no-ZOC Engaged/overlapping-cost and last-counterpart cases, Clear2: spent5+4+2=11/DP1, spent9+4+2=15/DP5 versus rejected16, stricter Reserve ceilings, atomic restart; exception expiry, changed enemy position, mandatory overspend, retained resource/Breakdown history and exhausted-assault rejection. | 017; [Campaigns](../../src/Cna.Core/Campaigns/), focused Core tests. |
| `CMB-TASK-019` / M, 3–5 files | Implement first opening, semantic progress, supported continuation witness and repeat/finish control. Use TASK-018's relation-aware move rule for witnesses; never infer no Movement from exhausted ammo or a missing break-off implementation. Prefix excludes opening event; finish enters same-slot Truck Convoy without stage housekeeping. | `CycleControl` full truth table, ordinal/prefix forks, no-op/cancel/retain-only history, reachable zero-loss Engaged repeat with break-off witness, lost reply/deadline, pending and future obligations; unsupported continuation is not “none legal.” | 018; [Campaigns](../../src/Cna.Core/Campaigns/), [Rules](../../src/Cna.Core/Rules/), focused Core tests. |

Checkpoint H: demonstrate actual Movement/Reserve repetition and settled Combat-to-finish through
dormant authority. No second assault is claimed without a separately certified strength/ammo path.

### Checkpoint I — side-safe public activation

| Task / size | Output and acceptance criteria | Verification | Dependencies / likely paths |
| --- | --- | --- | --- |
| `CMB-TASK-020` / M, 3–5 files | Implement frozen observation/action projection and authenticated-audience submission mapping. Enforce allowlisted custody/replacement inferences; hide raw authority history/counters and unapproved opponent facts. | `CombatCycleDisclosure` equal-authorized-history byte/action/outcome pairs across every window, including seal order, hidden forks and terminal reasons. | 004/019; [Observations](../../src/Cna.Core/Observations/), [Actions](../../src/Cna.Core/Actions/), focused Core tests. |
| `CMB-TASK-021` / M, 3–5 files | Activate the new certified capability coherently in ordinary Core authority. Reject unsupported initial profiles/terminals before decisions; keep old readers and new recovery available. | `CombatCycleAdmission` ordinary entry-to-terminal public traces, dormant/active separation, historical compatibility and all Core AC evidence indexed. | 020; Core admission/registration files and focused Core tests. |

Checkpoint I: full repository gate and privacy/admission review before simulator adoption. No
new runtime support is inferred from enum/catalog positions or generic Dispatch availability.

### Checkpoint J — simulator and retained evidence

| Task / size | Output and acceptance criteria | Verification | Dependencies / likely paths |
| --- | --- | --- | --- |
| `CMB-TASK-022` / M, 3–5 files | Extend opaque Core Exercise authority with the same public actions and occurrence-aware terminal; exactly one semantic event per accepted step. Reconstruct and re-adjudicate through Core, with no private test hook in production. | `CombatCycleExercise` empty, repeat and assault-settlement scenarios, restart cuts and forged ordinal/obligation terminal rejection. | 021; [Exercises](../../src/Cna.Core/Exercises/), [Exercise tests](../../tests/Cna.Core.Tests/Exercises/). |
| `CMB-TASK-023` / M, 3–5 files | Adopt decisions/terminal in Runner and strict child bundle readback. Preserve manifest-last publication and exact canonical evidence; step-limit/cancellation/invariant failure remains failure. | Runner end-to-end fixture runs and tampered child bundles, readjudication mismatch, unsupported-terminal admission and output-privacy checks. | 022; [Runner](../../src/Cna.ExerciseRunner/), [Runner tests](../../tests/Cna.ExerciseRunner.Tests/). |
| `CMB-TASK-024` / M, 3–5 files | Extend Maneuver/paired parent evidence; validate every child and parent, bind equal initial state and explicit first divergence. Compare two clean runs using the canonical evidence subset only. | Empty/Movement-repeat/Combat-finish scenarios, all selected settlement branches across suite, hidden-fork transcript pairs and invalid negative-success assertions. Retain seed/build/commands/artifact hashes. | 023; [Runner execution](../../src/Cna.ExerciseRunner/Execution/), [Runner tests](../../tests/Cna.ExerciseRunner.Tests/). |

Checkpoint J: strict child and parent readback plus repeatability pass. Report bounded terminal
and retained obligations on every result; never describe post-divergence draw purposes as synchronized.

### Closeout

| Task / size | Output and acceptance criteria | Verification | Dependencies / likely paths |
| --- | --- | --- | --- |
| `CMB-TASK-025` / M, 3–5 files | Reconcile requirement evidence, repository map/design/names and remaining Sprint5 gaps; prepare exact PR scope and test evidence. Execute Player Intent Composer Phase0 planning only if the roadmap's actual skeleton acceptance is met. | Repository full gate, clean diff, all AC evidence links, scope/deferral audit and concrete maintainer review. No unrun independent-review claim. | 024; [README](../../README.md), [technical design](../../tech-design.md), [names](../../naming-overview.md), this plan and [roadmap](../roadmap/pre-alpha-roadmap.md). |

At every checkpoint, run affected deterministic suites and relevant build/format checks; run the
full repository gate at public activation and closeout. Record command, commit, result and artifact
location when executed. Failed checks block dependent activation. User review precedes moving
beyond a checkpoint when its policy/contract/acceptance boundary changes; routine compatible fixes
and feature commits remain within authorized implementation work once G0 and B pass.

Use verification tiers so fast feedback and exhaustive evidence are both explicit:

| Tier | When | Minimum evidence |
| --- | --- | --- |
| Focused | Every edit | Changed packet/test plus direct predecessor and consumer suites, syntax/schema parse, `git diff --check` |
| Checkpoint | Before closing a bounded child or parent | All affected contract/runtime families, compatibility/recovery cuts, local Markdown targets, build and format for touched projects |
| Activation | Before Tasks021 and024 | Full repository test/build/format gate, complete contract-oracle sweep, disclosure/admission negatives, reconstruction/readjudication and two clean retained runs |
| Release | Before skeleton or MVP claims | Clean-checkout reproduction with pinned toolchain/content hashes, operational recovery drill, rights/source audit, and acceptance-criteria readback |

An environment timeout or missing optional checker is recorded as unavailable, not passing. A
document-only edit may omit runtime tests only when it changes no executable contract, source,
project, scenario, or generated input; it must still run the focused documentation checks. Avoid
rerunning every exhaustive composed oracle during the inner edit loop, but never substitute focused
checks for the Activation or Release tier.

## Progressive execution evidence

Do not wait for Runner024 to discover whether the runtime components compose. Preserve the existing
E/G/H/I/J gates and make their evidence cumulative; this clarifies verification within their assigned
scope and does not activate an incomplete public capability.

| Gate | Required composed evidence | Claim boundary |
| --- | --- | --- |
| E /010–011 | Replay no-selection/cancelled steps and both seal orders through dormant Core; record the boundary's provenance and restore at each implemented cut. | Pending/Prepared lifecycle, no settled assault claim. |
| G /016 | Join selection, seals, costs, result and mandatory settlement into a complete dormant assault; include representative positive capture and retreat cases alongside all-branch focused tests. | Real Core settlement; public privacy still gated. |
| H /019 | Join the inherited pre-Combat path, assault/no-attack path and supported repeat/finish; reconstruct each accepted event and compare final state. | Dormant cycle proof, not simulator or hosted execution. |
| I /020–021 | Exercise the same path through audience-safe queries/submissions, paired hidden-state tests and public admission. | Supported public capability. |
| J /022–024 | Runner/Exercise replay, re-adjudication and clean repeated bundles, followed by branch/seed coverage justified by observed gaps. | Bounded simulation; no balance, AI or durable-host claim. |

Use compatible fixtures and explicit trusted test inputs until the inherited-path implementation is
available; identify that limit on early E/G evidence. H must close it with a real predecessor trace.
Re-run broad simulator/timing studies when executable inputs, runtime or measured concerns change;
documentation-only changes do not require another full seed sweep.

## Requirement-to-task evidence index

Ranges include every numbered AC, not only favorable examples. TASK-004 expands this compact index
into named tests/vectors; TASK-025 records actual results. Tasks005–007 and 008A0–A2 have bounded implementation evidence above; the remaining runtime AC mapping is still planned.

| Canonical requirement / decisions | Tasks | Required evidence / current status |
| --- | --- | --- |
| [CMB-ID-AC-001–012 / ID-001–008](combat-opportunity-identity-v1.md) | 002–004, 006, 009, 016, 018–022 | Identity/profile/participant and side-fork tests; **planned**. |
| [CMB-PRO-AC-001–012 / PRO-001–007](combat-sealed-decision-protocol-v1.md) | 003–004, 008, 010–013, 020–024 | Seal/deadline/atomicity/restart/transcript and historical codec tests; **planned**. |
| [CMB-STEP-AC-001–012 / STEP-001–007](combat-step-transitions-v1.md) | 003–004, 009–011, 016, 020–024 | Six-step selected/cancelled/settled traces and no forged closure; **planned**. |
| [CMB-RES-AC-001–012 / RES-001–007](combat-cost-resolution-order-v1.md) | 001–008, 012–016, 020, 022–024 | Complete selected source/table comparison, costs/RNG/conservation and tamper cuts; **planned**. |
| [CMB-SET-AC-001–012 / SET-001–008](combat-settlement-disclosure-v1.md) | 001, 003–004, 007–009, 014–016, 020–024 | Every retreat/capture/fallback/disclosure branch and future-obligation persistence; **planned**. |
| [CYCLE-COMP-AC-001–012 / COMP-001–008](continual-cycle-reserve-composition-v1.md) | 003–004, 008, 017–024 | Identity/prefix, release, progress/continuation truth table, history, strict paired terminal evidence; **planned**. |

Research checks provide arithmetic and logical evidence only: [mutable-state](../research/verify-combat-mutable-state.py),
[RNG](../research/verify-combat-rng.py) and [Reserve](../research/verify-reserve-release.py).
They do not prove production source tables, live settlement, replay, privacy or simulator success.
[Review3](../reviews/combat-settlement-review-3.md) is Ready for DES-004/005 only.
[Review4](../reviews/combat-cycle-plan-review-4.md) assessed this plan and CYCLE-DES-001 at9f683d1:
**Not ready**, one P2 ordinary break-off handoff gap. Author accepted the finding and assigned
source/contract/implementation/witness evidence to TASK-001/003/018/019 above. This correction
has local checks only; no new independent Ready verdict is claimed. Historical user-approved limit at that checkpoint: **4/4 used**. The newly requested progress review below extends the initiating flow cap to7, preserving that count.

Planning-packet validation, 2026-09-06: 246 local path links resolved across the five changed docs;
25 ordered task IDs/dependency rows, eight policy IDs, six contract IDs and all 72 design ACs checked.
All three research scripts above passed; staged whitespace check passed. No .NET build/tests run
for this documentation-only change. CCE recall/search and memory writes returned `Transport closed`;
local files supplied the fallback evidence. These checks do not complete any implementation task.

Historical TASK-001 research checkpoint at `0f08694`, 2026-09-06: [source packet](../research/combat-source-freeze-v1.md),
[numeric fixture](../research/fixtures/combat-selected-source-v1.json) and
[diagnostic verifier](../research/verify-combat-source-freeze.py). Visual and optical extraction
agree on 357 defined loss values and three source gaps; the PDF copies contain identical scan
pixels. At that checkpoint, the proposed 10% repair was unadopted. Candidate checks cover 6,480 joint coordinates,
8,840 settlement combinations and 12 prior seeded vectors; calendar and movement arithmetic pass.
Source admission was blocked at that checkpoint. Calendar normalization is a four-turn/12-stage inference;
phase-specific maturity execution remains deferred. Ordinary break-off is highest applicable 2/4 CP
plus terrain, with immediate DP above CPA and a 150% ordinary infantry ceiling. These findings
constrain TASK-003/007/018/019; they do not certify runtime, replay or independent engineering review.

Historical TASK-001 ruling follow-up at `4f44c30`, 2026-09-06: the source packet now records wider errata, integrated-rule
and VASSAL chart checks plus all four adjacent-band repairs. Recommendation remains 10% with
moderate confidence, based on one endpoint edit and smoother neighboring probability thresholds;
no direct historical correction was found. Source admission was pending owner disposition at that checkpoint.
The source diagnostic verifier passed all four candidate comparisons and the existing 12 seeded
cross-checks; original fixture facts remained unchanged, 65 local links resolved,
and Python syntax/whitespace checks passed. No .NET runtime changed or tests were needed.

TASK-001 acceptance/closeout, 2026-09-06: owner accepted CMB-SRC-RUL-001 on the researched fidelity
basis retained in the source manifest and policy register. Verifier checks 360 normalized loss values,
357 unchanged source values, the three exact10% amendments, 6,480 joint outcomes, 8,840 settlement
combinations and12 prior seeded vectors. Raw gaps remain visible; unaccepted/altered amendment
negatives reject. Calendar and break-off findings satisfy source research with future phase-specific
maturity execution explicitly deferred to its activation gate. No production schema/runtime changed.

TASK-002 contract checkpoint, 2026-09-06 (input `448e370`): Content schema7 / format
`sandtable.content-json.v6` and closed `sandtable.capability.combat-cycle-infantry.v1` reserved after
registry inventory. Reuses existing class/component IDs; freezes offensive ratings, source-parent
Morale and explicit TOE/ammo/stage-readiness origins. Six-hex synthetic fixture adds two retreat
direction anchors without adding live supply or duplicate CP/Cohesion authority.
`python3 docs/specs/verify-combat-content-v7.py`: 10,339 canonical bytes with pinned SHA-256,
70 rejection vectors, eight initial custody/escape probes across both retreat directions,
shuffled construction and provenance hash sensitivity all pass. Policy/field mapping is retained
in the contract. No production code, existing fixture bytes or simulator support changed.
At that historical checkpoint, independent review remained open (budget4/4). TASK-003
must reconcile creation ledger/readiness/Normal Weather and Rules binding before checkpoint B.

Historical TASK-003A checkpoint, 2026-09-06 (input `4303004`): [Setup7/initial ledger contract](../specs/combat-creation-ledger-v1.md),
[retained goldens/vectors](../specs/fixtures/combat-creation-ledger-v1.json) and
[oracle](../specs/verify-combat-creation-ledger-v1.py) freeze explicit zero-ledger initialization,
source/current parent and exact Content seed/origin bindings. Stage1-only creation is explicit;
Weather remains ordinarily adjudicated and actual Normal Weather is a later assault admission gate.
Verifier passes1,655 Setup bytes,2,685 initial-element bytes,63 rejection vectors, alternate holder,
provenance hash propagation, stage2 refusal and shuffled construction. Parent003 remains open;
003B durable World/settlement values next,003C full Rules/creation/snapshot/sealed envelopes and
003D cycle/history/movement reconciliation follow. No runtime or additional independent review.

TASK-003B review checkpoint, 2026-09-06 (input `e3d1a26`): [World/settlement contract](../specs/combat-world-settlement-v1.md),
[exact schema inventory](../specs/combat-world-settlement-v1.schema.json),
[goldens/vectors](../specs/fixtures/combat-world-settlement-v1.json) and
[oracle](../specs/verify-combat-world-settlement-v1.py) complete as a contract packet. Six canonical World goldens,57
rejection vectors,112 isolated receipt-cut readbacks/20scenarios,8,840 source arithmetic cases and
eight calendar boundaries pass. Full source, Content7 and creation oracles also pass. No production
World reader, Snapshot/event schema, actual restart or public projection is claimed.003C/003D/004
remain required before combined checkpoint B and runtime consumers.

At the historical003B checkpoint, owner requested independent-review rounds on that day's remaining progress. Prior work is
already locally committed; the review target is the unmerged feature branch plus the explicit new
working-tree boundary, with unrelated main-checkout edits excluded. The initiating review flow now
permits at most7 total passes (four prior, up to three newly authorized); **5 of7 used**.
[Review5](../reviews/combat-progress-review-5.md) assessed all28 frozen branch/working-tree files
and returned **Ready with non-blocking follow-ups**. Its independent fixture sweep passed19,456
World materializations. Sole P3 finding: stale navigation/status summaries; accepted and corrected
without changing schema, goldens or verifier logic. No further pass is warranted for this status-only
correction. Reviewers receive no inherited implementation conversation and remain read-only.
Further passes require material changes; a heavy pivot returns to owner.003C was next at that checkpoint.

TASK-003C1 checkpoint, 2026-09-06 (input `8bbea59`, merged PR91):
[rules-input/config contract](../specs/combat-rules-inputs-v1.md),
[schema](../specs/combat-rules-inputs-v1.schema.json),
[goldens/vectors](../specs/fixtures/combat-rules-inputs-v1.json) and
[oracle](../specs/verify-combat-rules-inputs-v1.py) complete. Three canonical artifacts,47 isolated
mutations,39 raw-byte rejections,7 clock cases,14 window/budget/UTC boundaries and full360-cell/36-Morale
source checks pass. Existing source/Content/creation/World oracles pass unchanged.
[Independent review6](../reviews/combat-inputs-review-6.md) returned **Ready**, no actionable findings;
all five reviewed target hashes verified before/after. Author accepts the result; no material fix
or additional pass warranted. Cumulative use is **6of7**. Only plan status/navigation and retained
review evidence changed after review; contract/schema/golden/verifier bytes remain frozen.
Next003D1 supplies exact sequence/cycle artifact bytes before003C2 full Rules10 assembly. Parent003C/D,
004, checkpoint B and runtime gates remain open. This checkpoint is not full003C completion.


TASK-003D1 checkpoint, 2026-09-06 (input `4c2bd03`):
[Sequence/catalog5 and cycle codec1](../specs/combat-cycle-sequence-v1.md),
[schema](../specs/combat-cycle-sequence-v1.schema.json),
[goldens/vectors](../specs/fixtures/combat-cycle-sequence-v1.json) and
[oracle](../specs/verify-combat-cycle-sequence-v1.py) complete. Actual C# catalog4 capture anchors112
positions and one interrupt; catalog5 adds six same-slot cycle edges. Two artifact and two identity
goldens,38 mutations,3,996 scoped identities and896 actor materializations pass, including prefix
framing/fork and occurrence-binding negatives. Existing rules-input/World/creation/Content/source
oracles pass. Reviewer used explicit Homebrew Python3.14/.NET10 for checks where default PATH
selected incompatible older runtimes; details retained in report.
[Independent review7](../reviews/combat-sequence-review-7.md) returned **Ready**, no actionable findings.
All five target hashes matched before and after review. Author accepts result; no material fix or
additional pass warranted. Cumulative use is **7of7**, exhausting current independent-review budget.
Only plan status/navigation and review retention changed after review; four contract artifacts remain
byte-identical to reviewed manifest. Next003C2 assembles full Rules10/Created11/Snapshot12.
Parent003C/D,003C3,003D2,004, checkpoint B and runtime/replay/privacy evidence remain open.


TASK-003C2 checkpoint, 2026-09-07 (input `d59446e`, merged PR92):
[Rules10/creation envelope contract](../specs/combat-authority-envelope-v1.md),
[schema](../specs/combat-authority-envelope-v1.schema.json),
[goldens/vectors](../specs/fixtures/combat-authority-envelope-v1.json) and
[oracle](../specs/verify-combat-authority-envelope-v1.py) complete for creation cut. Full Rules10
manifest is10,495 bytes with11 artifacts/11 rulings; hash
`8af256c6c2bbf71cb72ea7e29922db9c4c897c2535121a68a6849bbd06e0bd18`.
Package-free temporary C# capture reproduced actual9,168-byte Rules9 hash and existing
RulesetManifest constructed from new manifest reproduced Rules10 hash. Both helper builds passed
with0warnings/errors and retained local binlogs. No production Created11/Snapshot12 C# parity claim.
Four goldens,67 mutations,36 raw-byte rejections,9 recovery boundary checks,12 seed/context forks
and2 turn boundaries plus693 nested type rejections pass. All six predecessor
source/Content/Setup/World/inputs/sequence oracles pass.
Author verification only: no independent pass started; prior review flow remains exhausted **7of7**.
Creation binding precedes World construction; snapshot binds exact validated Created11 bytes and
003D1 prefix. Recovery tests are pure contract decisions, not durable publication or actual restart.
C2 freezes only creation-state Snapshot12: noninitial variants fail closed until003C3/003D2 complete
its prospective contract before checkpoint B. No opaque saved-state payload or inferred upgrade.
Next003C3 freezes command/event envelopes, later snapshot state and causal suffixes; parent003C/D,
004, checkpoint B and all runtime/replay/privacy gates remain open.


The following checkpoint notes retain their original next-step and review-count statements as
historical evidence. Current completion and review status are recorded at the top of this plan.

TASK-003C3a checkpoint, 2026-09-07 (input `568027c`):
[Selection/step contract](../specs/combat-selection-steps-v1.md),
[schema](../specs/combat-selection-steps-v1.schema.json),
[fixture](../specs/fixtures/combat-selection-steps-v1.json) and
[oracle](../specs/verify-combat-selection-steps-v1.py) freeze no-selection traversal, selected intent,
RBA decline/cancellation and pre-assignment handoff. Five literal traces contain41 events and five
final Control goldens.41 event/control cuts,246 event mutations,164 raw rejections plus exact retry,
deadline, unavailable-clock, stale timer, actor/candidate, weather/geometry and FA guards pass.
All seven predecessor oracles pass unchanged;427 local Markdown links resolve, JSON/Python syntax
and changed-file whitespace checks pass. No .NET/runtime files changed in this slice.
Boundary fixtures are explicit isolated probes; real Breakdown/Weather/Movement history and complete
Snapshot12 remain future consumers. No CP/ammo/TOE/RNG/world changes or production activation.
C2 independent review8of8 returned Ready with no actionable findings; retained report at local
`.planning/cmb-task-003c2/review8/report.md` outside Git. Its verdict applies only to C2; C3a receives
author checks, with no additional review pass. Current review maximum remains8of8.
Next003C3b freezes sealed assignments/FA-AA trace and commitment;003C3c composes result/settlement
and noninitial Snapshot12, then003D2/004 complete checkpoint B before runtime Task005 begins.


TASK-003C3b checkpoint (input `c126f16`, merged PR94):
[Sealed-round contract](../specs/combat-sealed-round-v1.md),
[schema](../specs/combat-sealed-round-v1.schema.json),
[fixtures](../specs/fixtures/combat-sealed-round-v1.json) and
[oracle](../specs/verify-combat-sealed-round-v1.py) complete as a bounded private authority fragment.
Two slots share a pinned base and deadline; second seal derives Prepared; exact FA/AA proof permits
one atomic CP/ammo/history commitment before RNG. Zero/one-seal cancellation closes remaining steps
without attack effects. Four traces/23 event-state cuts,276 mutations,138 raw rejections and
clock/retry/CP-ceiling/commitment guards pass; all eight predecessor oracles pass unchanged.
C3a's four artifact bytes remain frozen. Golden bytes are recorded regression vectors, not independent
serializer parity; the independent literal expectations cover resources and terminal states.
Base retains the disclosed C3a isolated-probe lineage. Full pre-round Snapshot12 binding, inherited
pre-Combat history, all-result certification, public slot/revision/privacy contracts and durable
publication remain C3c/D2/004/runtime or hosting gates. No production version is registered.
Author verification only; no new independent review and current use remains8of8.
At this checkpoint the scheduled next task was HOST-RSH-001; then resume003C3c,
003D2 and004 before checkpoint B and runtime005. Parent003C3 and003 remain open.

HOST-RSH-001 checkpoint (input `6852088`): [research/probe](../research/orleans-publication-feasibility.md)
complete. Isolated Rules9 silo matched12 direct-Core accepted commands,13 writes and12 same-process
reactivations through first-side Combat entry, including duplicate/stale and failure/reply recovery.
No production provider, durable process-restart acceptance or Combat runtime claim. Publication
proposal awaits owner acceptance; next003C3c →003D2 →004 → checkpoint B. No independent review added.


TASK-003C3c checkpoint, 2026-09-07 (input `7bb2d11`):

- C3c.1 freezes eight literal result/settlement cases, mirrored roles/reversed seals, exact RNG
  cursor/purpose evidence, choice clocks, custody/escape, CP limits and round/CA closure.68 replay
  cuts,728 mutations,340 raw rejections and96 timing checks pass; commit `65d708b` plus mandatory
  regression-vector presence guard.
- C3c.2 composes149 complete Snapshot12 cuts across17 selection, round and settlement traces;
  1358 mutations and596 raw rejections pass. Full pre-round binding and inherited ledger retention
  are checked against replay-derived state. Golden bytes are regression vectors, not independent
  serializer parity. Layered counts overlap and must not be added as distinct runtime scenarios.
- Compatibility inventory assigns explicit new-context/sequence5 successors to bounded008 adapter
  families;019 supplies first-opening before predecessor-to-Combat integration. D2/004/B must settle
  exact successor declarations, actual first-opening/history provenance and whole-envelope capacity.
- All ten predecessor/source/RNG oracles pass. Frozen predecessors and runtime remain unchanged.
  Synthetic Weather/Breakdown prefixes and inherited receipts remain assumed probe inputs; this is
  neither a reachable full-campaign trace nor production restore/privacy/hosting evidence.

C3 closes as this bounded contract checkpoint. Next003D2 →004 → checkpoint B; parent003C/D and
runtime005–025 remain open. Author verification only; independent review budget remains8of8.


TASK-003D2a checkpoint, 2026-09-07 (input `3ac1283`):

- Atomic ordinary movement packet freezes World7 location/representation/CP/Cohesion changes and
  original-pair endings in one prospective receipt. Eight literal cases, both sides,36 replay cuts,
  486 mutations,120 raw negatives,630 arithmetic coordinates and14 atomic guards pass. Guard,
  entitlement, RNG, ammunition and active-stage attack history remain unchanged by movement.
- Author review corrected the former Clear1 example against original Map A chart8.37 and the
  existing Movement artifact: Clear2 gives5+4+2=11/DP1 and9+4+2=15/DP5;16 rejects. Source/design
  correction is committed separately at `a1f890b`. All72 AC IDs remain stable; AC007's example is
  explicitly corrected. This changes no accepted policy, Rules artifact or frozen assault bytes.
- All twelve predecessor/source/RNG oracles pass, including full Snapshot12 composition.
  Regression event/state goldens require exact readback. No runtime code/registration/Exercise
  changes; synthetic release/repeat gap remains explicit. This does not close D2 or prove actual
  continuation. D2b freezes release/control; D2c freezes actual first-opening/inherited adapters and
  combined CON-002–004 reconciliation;004 and checkpoint B follow. Author checks only,8of8 unchanged.


TASK-003D2b.1 checkpoint, 2026-09-07 (input `89eac24`):

- Private Release arm freezes exact opening/disposition/completion events and commands, one Config
  deadline, canonical own-unit queue and locked conversion/retention fallback. Status changes retain
  original keys, CPA/spent values, release/conversion/offensive links and next-Movement scope.
- 13 literal cases/48 side-slot traces pass188 replay cuts,2368 mutations,840 raw rejects,20 timing
  and27 boundary checks. Midway expiry preserves accepted choices. Author reproduced and fixed the
  post-last-choice/pre-completion expiry cut; it now emits completion without repeating disposition.
- Positive/multi-unit history remains synthetic ledger evidence. C3c guard/escape fixtures compose
  empty Release and preserve actual settled World/RNG/history from their cut; earlier synthetic
  pre-Combat lineage remains explicit. No positive Reserve World/Snapshot12 or runtime admission.
- All fourteen predecessor/research oracles, syntax/link/plan checks and frozen-byte checks pass.
- NextD2b.2 consumes this completion receipt for guarded repeat/finish, supported continuation,
  material progress and exception expiry. D2c still owes inherited successors/actual first opening
  and full CON-002–004 reconciliation;004/B follow. ParentD2b/D2/003 remain open. All25 task IDs,
  72 AC IDs and8 policies retained; author verification only, independent budget8of8 unchanged.


TASK-003D2b.2 checkpoint, 2026-09-07 (input `184b16c`):

- [Private cycle-control packet](../specs/combat-cycle-control-v1.md) freezes guarded repeat/finish,
  D2b.1 completion binding, derived commitment/release progress, D2a cost witnesses and scoped
  Movement expiry.19 literal cases/64 side-slot traces,164 cuts,1748 mutations,700 raw rejects,
  43 boundary checks and216 arithmetic coordinates pass. Frozen16 expiry outputs include both slots.
- Author RED test caught opening with insufficient authority-version room for mandatory closure;
  corrected before freeze. Full World/RNG, stage histories and future obligations remain unchanged.
- All15 predecessor/research oracles pass; all prior specs/src/tests/scenarios retain exact bytes.
  Actual inherited progress/Movement proof, armed Combat continuation and positive World/Snapshot
  composition remain D2c/004/B gates. No runtime, simulator, .NET or additional independent review.
- D2c is next; bound inherited successor families before editing. Parent003D2/003 stays open.


TASK-003D2c.1 checkpoint, 2026-09-07 (input `faa3282`):
[Inherited successor/first-opening contract](../specs/combat-inherited-successors-v1.md),
[schema](../specs/combat-inherited-successors-v1.schema.json),
[fixture](../specs/fixtures/combat-inherited-successors-v1.json) and
[oracle](../specs/verify-combat-inherited-successors-v1.py) freeze20 exact successor declarations
and one isolated Reserve-completion2 transition. Four traces/eight replay cuts,216 event mutations,
32 raw rejections and88 boundary checks pass; fifteen current source hashes pin the inventory.
008A–H/019A provide a bounded, acyclic inherited-path owner graph, with first opening before shared
restore composition and no registration shortcut. Full predecessor provenance, armed continuation,
nonempty obligations, general Snapshot12 and capacity remain D2c.2–4/004/B. No runtime or simulator
claim. NextD2c.2 must split preamble/Weather/Reserve families before edits. [Independent review10of10](../reviews/combat-progress-review-10.md) assessed progress since
review9, including D2b.2: Ready with non-blocking follow-ups. Both planning/status findings are
accepted and corrected; parent003 stays open.


TASK-003D2c.2a checkpoint (input `387445b`):
[Opening preamble contract](../specs/combat-opening-preamble-v1.md),
[schema](../specs/combat-opening-preamble-v1.schema.json),
[vectors](../specs/fixtures/combat-opening-preamble-v1.json) and
[oracle](../specs/verify-combat-opening-preamble-v1.py) freeze four declared inherited successors.
Six creation-rooted prospective traces reach Weather entry from actual C2-validated Created11 bytes;
30cuts/1050event mutations/3348state mutations/192raw rejections/204boundary-retry checks pass.
Both order choices preserve Initiative holder and all RNG/World bytes;60artifact entries and9source
hashes are retained. Caches reconstruct from full accepted history, with no synthetic prior-prefix
seed. This is contract evidence, not C# replay. NextD2c.2b derives Weather from this accepted chain;
2c/2d still owe stage entry/Reserve/first opening. Parent003/004/B remain open. Author verification
only; review10of10 remains exhausted, and its earlier verdict does not cover this new checkpoint.

Author closeout: all18 contract/research oracles pass, including all17 predecessors.648 local
Markdown targets/17anchors and25 task/72 AC/8 policy IDs pass; existing contract and runtime bytes
are unchanged. Self-check covers source/identity/retry/readback, scope and003→004→B ordering.

Subsequent review11of11 assessed all contents of mergedPR95–98, including2a: Ready, no findings.
Exact final tree equals`bf10c9b`;24 intermediate/final oracle jobs and focused Orleans checks passed
independently. The merge process preserved every reviewed tree/diff and reran CI after rebases.

TASK-003D2c.2b checkpoint (input `3ca453a`, contract commit `25cf4e7`):
[Weather specification](../specs/combat-weather-v1.md),
[schema](../specs/combat-weather-v1.schema.json),
[retained vectors](../specs/fixtures/combat-weather-v1.json) and
[oracle](../specs/verify-combat-weather-v1.py) freeze WeatherDetermined2 and ResolveWeather2.
34 creation-rooted traces/68 cuts,11516 leaf mutations,921 raw rejections,1651 boundary/retry checks
and3960 existing Rules coordinates pass. Thirteen source hashes and306 artifact entries are retained.
Rules10 binds the exact existing Weather artifact;17 seeds cover all outcomes and every foul-location
row for both order choices, including rejected RNG bytes. All18 predecessor oracles also pass.
Full World and initiative/order history are preserved; Weather/RNG/receipts derive from accepted
creation+four preamble events. Private WeatherState is not Snapshot12. No production source, test,
scenario, historical contract or policy bytes changed. Author verification only; no additional
independent review. Navigation closeout verifies670 local links/17anchors,25 task IDs,72 design ACs
and8 policies. Next2c stage-entry then2d Reserve/first opening; parent003/004/B stays open.

TASK-003D2c.2c checkpoint (input `78be145`, contract commit `5f68d21`):
[Stage-entry specification](../specs/combat-stage-entry-v1.md),
[schema](../specs/combat-stage-entry-v1.schema.json),
[retained vectors](../specs/fixtures/combat-stage-entry-v1.json) and
[oracle](../specs/verify-combat-stage-entry-v1.py) freeze four command/event2 successors through
Reserve entry.12 creation-rooted traces/60 cuts,11052 leaf mutations,1299 raw rejections and3552
boundary/retry checks pass;12 source hashes and228 artifact entries retained. All19 predecessors
pass. Full accepted Created11+preamble+Weather provenance derives state6; four explicit Setup7
gates lead to state10, preserving World/Weather/RNG/holder/order and all nine receipts. Both orders
retain the unmaterialized first-side Reserve successor; no System or Commonwealth fleet actor leaks
into the next side's authority. Exact retries at every later cut return the original event.
The full oracle exposed a policy-error boundary issue; checking the exact policy before inherited
creation validation fixes it, with unchanged goldens and a passing full rerun.
No runtime/source/test/scenario/historical-contract changes, public or Snapshot12 admission, or new
independent review. Next2d owns designation/completion and actual first opening; parent003/004/B
stays open. Author source/replay/identity/retry checks and the003→004→B dependency review are complete.
Navigation closeout: all20 contract/research oracles pass;692 local Markdown targets/17 anchors,
25 stable task IDs,72 design AC IDs and8 policy IDs check clean. Current navigation points to2d;
review11's scope stays limited to mergedPR95–98. Historical specs and runtime bytes are unchanged.

TASK-003D2c.2d checkpoint, 2026-09-08 (input `4c10ede`, contract commit `4d5199c`):
[Reserve designation specification](../specs/combat-reserve-designation-v1.md),
[schema](../specs/combat-reserve-designation-v1.schema.json),
[retained vectors](../specs/fixtures/combat-reserve-designation-v1.json) and
[oracle](../specs/verify-combat-reserve-designation-v1.py) compose real state10 Reserve authority
through optional designation2 and atomic completion2/ordinal1 opening.16 creation-rooted traces,
40 replay/state cuts,2342 leaf mutations,733 raw rejections,1578 boundary/retry checks and14 source
pins pass;152 literal artifact entries retained. Four compatible actual bases match frozenD2c.1
completion bytes. All20 predecessor/research oracles pass without historical artifact changes.
The adapter derives its structural OpeningBase only from full accepted history, preserving the
frozen completion shape/domain and historical profile string; it does not admit isolated bases.
Empty/I selection reaches state11/12 and10/11 receipts for either acting side. Accepted Weather,
RNG, orders and prior receipts survive; only own Reserve status/designation history changes.
Both terminal positions remain symbolic; cycle authority resolves the acting side. Exact retries
at later cuts return original events plus current state. Author integration passes eight paths
covering full-head readback, terminal retries, actor rejection and sequence/cycle-owner semantics.
Author checks caught malformed trusted policy leaking a predecessor error class. New boundary
normalizes explicit predecessor errors;34 regression probes and final full oracle pass. Four
corrupted source/literal/digest/case controls reject. No old reader or fixture was changed.
ParentD2c.2 is complete within this closed initial-infantry contract profile. D2c.3 inherited
Movement/Reaction/Breakdown, positive Reserve movement and armed continuation are next; .4 retains
full World/Snapshot/capacity and003's Task004 handoff. ParentD2c/D2/003/004/B and runtime gates remain
open. This checkpoint has author verification only; review11's earlier scope is unchanged.

Parallel Movement preparation checkpoint:
[Inherited Movement field/provenance matrix](combat-inherited-movement-preparation.md) maps all26
Move3 and12 Completion2 fields, five legacy test cases and eight proof obligations. Eleven baseline
source pins and four actual2d handoff pins bind field/API claims to source. Four entry signatures
and21 ReserveState fields match the frozen2d reader/schema. The next packet must reconcile legacy
unchanged-Cohesion validation with incremental excess-CPA DP, and materialized sequence4 Movement
with2d's symbolic sequence5 position. No successor payload, positive inherited replay or .3
implementation is completed by preparation. Runtime owner dependencies and003→004→B remain intact.
Navigation closeout: all21 contract/research oracles pass across the new packet and unchanged
predecessors.734 local Markdown targets/18 anchors,25 stable task IDs,72 design AC IDs and8 policy
IDs check clean. Offline Lychee0.24.2 also checks all160 tracked Markdown documents:1575 links,
1248 successful local checks,327 offline remote exclusions and zero errors. Movement preparation
is retained separately in `c403dfa`. Shared navigation now points to D2c.3; no independent-review,
.NET, simulator or hosted-CI evidence is inferred from these contract/document checks.

TASK-003D2c.3a checkpoint, 2026-09-08 (input `01ded95`, contract commit `dfa8385`):
[Inherited Movement specification](../specs/combat-inherited-movement-v1.md),
[schema](../specs/combat-inherited-movement-v1.schema.json),
[retained vectors](../specs/fixtures/combat-inherited-movement-v1.json) and
[oracle](../specs/verify-combat-inherited-movement-v1.py) freeze ordinary `element-moved`4 from
actual completed2d history. Exact26 legacy fields/order plus nine authority bindings are retained;
null/empty-only unsupported payloads remain explicit. Two normal-Weather traces,14 moves,16 replay
cuts,14 exact retries,384 event/cache mutations,66 raw rejections,81 boundary/admission checks and
18 source pins pass. Four temporary-fixture controls reject missing/duplicate owner, source drift
and changed golden. Direct Reserve and D2a predecessor oracles pass; their bytes remain unchanged.
Both accepted actors traverse own rear/supply route to CP14 and four excess-CPA DP; eighth cost16
rejects ceiling15. Returning toward adjacent opponent requires Reaction and rejects. Element and
representation locations, member spending, route, progress refs, receipts and prefix update together;
ammo10, TOE, RNG, Weather, order, history and opening authority persist. No CPA10 route stop is
invented under the successor ordinary policy; legacy3 remains unchanged. No vehicle cohort still
carries moving route flow. Source-backed route identity retains first version/origin across revisits.
Author integration independently checks both seven-move histories, stable route IDs, exact terminal
retry/restore, preserved authority, cost-specific eighth rejection and unsupported Reaction boundary.
Semantic RED failed literal destination/CP/version/prefix against no-op behavior; GREEN passes.
Author checks corrected a mutation harness that restored the receipt being challenged and added
exact both-owner fixture guards after finding that missing coverage could otherwise pass. Final
oracle rerun passes without relaxing transition behavior. No source/runtime/scenario/old-contract
changes or new independent review. This closes3a only; parent3/4/003/004/B remains open.
Next3b must carry actual owner stop2 → System empty-cohort stop-resolved2 → Movement-completion3,
including suspended cycle/Movement context across the generic Breakdown-stop interrupt. Its terminal
is Breakdown Determination; further Breakdown-segment completion remains required before Combat.

Navigation closeout:752 local Markdown targets/18 anchors,25 stable task IDs,72 design AC IDs
and8 policy IDs pass. Offline Lychee0.24.2 checks all161 tracked Markdown documents:1593 links,
1266 successful local checks,327 remote exclusions and zero errors. Python AST/two JSON documents
and final diff checks pass; source/runtime/scenario/predecessor bytes are unchanged.
Three relevant oracles (new inherited Movement, direct Reserve and D2a Movement) ran this turn;
no full historical-oracle, .NET, simulator, hosted-CI or new independent-review run is claimed.

TASK-003D2c.3b checkpoint, 2026-09-08 (input `d76ac3f` after review13 Ready; contract commit `9e9a324`):
[Route lifecycle specification](../specs/combat-inherited-movement-lifecycle-v1.md),
[inventory](../specs/combat-inherited-movement-lifecycle-v1.schema.json),
[vectors](../specs/fixtures/combat-inherited-movement-lifecycle-v1.json) and
[oracle](../specs/verify-combat-inherited-movement-lifecycle-v1.py) freeze owner stop2 → System
empty stop-resolved2 → owner Movement-completed3 from actual3a Move4 history. Eight both-owner
CP2/10/12/14 traces and24 events retain every11/15/12 legacy field, pending stop, capability and
accepted-input identity. Exact suspended cycle/Movement context survives the generic interrupt;
completion enters Breakdown Determination, never Combat. First end proof uses actual finalWorld,
the actual completion receipt and distance2/3 exclusion outcomes; no synthetic ordinal0 proof.
World, RNG, members, tracks, existing receipts and actual material-progress refs remain unchanged
apart from the three new lifecycle receipts/prefix increments and flow/position/proof projection.

Author evidence:32 cuts,24 terminal retries,946 event/cache mutations,152 raw rejections,304 boundary
checks and16 source pins pass. Every event leaf is challenged for both owners, including re-signed
well-typed changes; incomplete/reordered/duplicated and cross-owner/cross-cut histories reject.
Separate author integration checks four CP12/14 histories, independently hashed receipt/capability/stop
identities, exact11/15/12 legacy field order, first-proof exclusions and five failing fixture controls.
Semantic RED failed missing stop/interrupt/version behavior; GREEN passed the actual three-event flow.
Mutation testing exposed receipt canonicalization before nested type validation; typed-event checking
now precedes hashing and the original malformed-context mutation rejects normally. Valid goldens did
not change. The direct inherited Movement and cycle-control predecessor oracles also pass.

This closes3b only. No runtime/source/test/scenario or predecessor-contract changes; no .NET,
simulator, hosted-CI or entire historical-oracle rerun. Review13 covers input3a only; no further review
was launched. Next3c is the actual System Breakdown-segment completion2 into Combat Position
Determination. Positive Reaction/vehicle/Reserve movement, release exceptions, armed continuation,
full .4 composition, Task004 handoff/map and runtime gateB remain open.

Documentation closeout:778 local Markdown targets/18 anchors,25 stable ordered tasks,72 stable
design AC IDs and8 policy IDs pass. Offline Lychee0.24.2 checks162 Markdown documents:1619 links,
1292 successful local checks,327 external exclusions, zero errors. Python AST and both new JSON
documents pass; final changed-path/diff checks preserve all runtime and predecessor files.

TASK-003D2c.3c checkpoint, 2026-09-08 (input `a3f2f76`; contract commit `69a1215`):
[Breakdown completion specification](../specs/combat-inherited-breakdown-completion-v1.md),
[inventory](../specs/combat-inherited-breakdown-completion-v1.schema.json),
[vectors](../specs/fixtures/combat-inherited-breakdown-completion-v1.json) and
[oracle](../specs/verify-combat-inherited-breakdown-completion-v1.py) derive actual System
`breakdown-segment-completed`2 from full3b history. All11 legacy fields/order persist; the exact
version1 stage1 action identity is wrapped by command2. Sources come from predecessor Breakdown
position (5.2/7.11/7.14), and successor is first Combat Position Determination with null activeSide.
No World/RNG/member/proof/progress mutation or synthetic Combat admission occurs. One new event,
command receipt, completion ID, version and Chronicle prefix contribution closes this boundary.

Eight both-owner CP2/10/12/14 traces pass16 cuts,8 retries,426 event/cache mutations,92 raw rejections,
298 boundary checks and10 source pins. Every event leaf is challenged for both owners with re-signed
well-typed effects; incomplete/foreign lifecycle, altered proof/progress, stale/changed retry, owning
player System-step submission and missing-stage action IDs reject. Additional author integration
checks both CP12 owners, independent receipt/action hashes, exact11 legacy and26 inherited state
fields, preserved source/proof/World/RNG/progress, later-Barrage/proof/cross-cut rejection, and five
failing fixture controls. Semantic RED fails missing Combat-entry/version/prefix; GREEN passes.
Direct3b lifecycle oracle also passes its8 traces/24 events,32 cuts,24 retries,946 mutations,
152 raw cases,304 boundaries and16 pins. No whole historical-oracle/.NET/simulator/hosted-CI run.
Review13 applies to3a only;3b/3c retain author verification and no new review was launched.

Next bounded3d: actual Combat-entry admission and no-candidate selection opening/closure for these
rear/supply infantry histories. Frozen C3a is not a drop-in adapter: its boundary materializes the
acting side, requires original World locations and CP0..10, and fixes the C2 request seed
(`verify-combat-selection-steps-v1.py`, `boundary`). New admission must preserve moved
World, CP12/14, Cohesion, actual Weather/Breakdown receipts/prefix and symbolic sequence5 identity,
then prove no candidate from the admitted state. Boundary rejection cannot mean empty selection.
Full no-attack step traversal can follow as a separate bounded child; positive armed continuation
is still open. This is an execution choice consuming3c, not a new DAG dependency: runtime008F
Reaction and008G Breakdown still depend on008E Movement directly, as declared in the inherited
successor inventory. Positive Reaction/vehicle/Reserve families, repeat expiry, .4 composition,
Task004's handoff/map and gateB remain open.3c completion closes none of those parents.

Documentation closeout:799 local Markdown targets/18 anchors,25 stable ordered tasks,72 stable
design AC IDs and8 policy IDs pass. Offline Lychee0.24.2 checks163 Markdown documents:1640 links,
1313 successful local checks,327 external exclusions, zero errors. New Python AST and both JSON
documents pass; all predecessor contracts and runtime/source/test/scenario bytes remain unchanged.

TASK-003D2c.3d checkpoint, 2026-09-10 (input `8219e16`):
[Actual-entry selection specification](../specs/combat-inherited-selection-v1.md), inventory,
vectors and oracle compose the accepted3c history into a retained admission boundary. Both owners
at CP12/14 retain the complete moved CombatEntryState. Candidate assessment explicitly derives
Normal Weather, current locations and both CP ceilings; all four supported histories have zero
candidates because the units are nonadjacent and the acting unit exceeds voluntary CP5. Unsupported
or unauthenticated histories reject before assessment.

System `combat-segment-opened`2 records zero candidates and no decision ID; System
`combat-selection-closed`2 records no-selection and the exact opening receipt. The two events add
two versions/receipts/prefix contributions, preserve stepIndex0 and leave segmentClosed=false.
World/RNG/Weather/Cohesion, Movement/Breakdown proof/progress, inherited receipts and symbolic
sequence5 position remain byte-identical inside the admission boundary. Exact retries return the
original events; changed actor/input/history/effect/cache rejects.

Author evidence:4 traces/8 events,12 state cuts,8 retries,1126 mutations,46 raw rejections,
72 boundary checks and6 source pins pass. Direct3c predecessor evidence passes8 traces/events,
16 cuts,8 retries,426 mutations,92 raw rejections,298 boundaries and10 pins. Frozen C3a reference
passes5 literal traces,41 cuts,246 mutations and164 raw rejections. No runtime/source/test/scenario
or predecessor-contract file changes. Next bounded3e must traverse the six no-attack structural
steps from this actual closure; it cannot add positive candidates or close3/.4/003/004/B/runtime.

TASK-003D2c.3e checkpoint, 2026-09-10 (input `419aae4`):
[Actual inherited no-attack specification](../specs/combat-inherited-no-attack-v1.md), inventory,
vectors and oracle consume the exact3d empty-selection Control as immutable nested history. Four
both-owner CP12/14 histories traverse Position Determination, Barrage, Retreat Before Assault,
Force Assignment, Anti-Armor and Close Assault in six exact System step completions. All events use
the terminal no-attack proof, the3d selection receipt as disposition and opening/prior-step receipt
chaining. The final outer Control reaches same-slot Reserve Release and closes without changing the
nested3d segment flag or any World/RNG/resource/history fact.

Author evidence:4 traces/24 events,28 state cuts,24 retries,956 mutations,36 raw rejections,208
boundary checks and7 source pins pass. Fresh commands reconstruct from full accepted history; optional
cached Control must match replay exactly, and retries read original bytes from accepted event history.
Review14 at `db75340` returned Not ready on the prior boundary and an accounting omission; both accepted
findings are corrected here with author verification only. Direct3d and frozen C3a predecessor checks
remain required.
No runtime/source/test/scenario or predecessor-contract file changes. Positive Reaction/vehicle/
Reserve movement, armed continuation, repeat expiry, .4 composition, Task004 handoff/map and
runtime gateB remain open; next .3 child must be bounded before edits rather than inferred here.

TASK-003D2c.3f checkpoint, 2026-09-10 (input `0c4fa07`; contract commit `d7f9f85`):
[Reaction-trigger specification](../specs/combat-inherited-reaction-trigger-v1.md), inventory,
vectors and oracle replay each owner's exact first3a move, then commit one Clear2 return move that
opens exactly one opposing Reaction opportunity at version13. Trigger/window/opportunity identities,
the suspended phasing route, explicit Reaction position, World/CP and material progress are retained.
Author evidence passes2 traces/triggers,4 cuts,2 retries,270 mutations,60 raw rejections,30 boundaries
and11 source pins. Runtime/source/test/scenario files remain unchanged.

TASK-003D2c.3g checkpoint, 2026-09-10 (input `a6e6625`):
[Reaction-lifecycle specification](../specs/combat-inherited-reaction-lifecycle-v1.md), inventory,
vectors and oracle consume only3f's exact one-opportunity trigger. The opposing infantry participant
moves once assault→own rear, rotates its public opportunity handle over the still-legal rear→supply
option, then explicitly completes. Completion opens the mandatory empty-cohort Reaction stop;
System resolution returns to inactive Reaction before System no-eligible closure resumes the exact
suspended phasing route. Final World retains phasing CP4 and reactor CP2; RNG is unchanged and only
the reacting move adds material progress.

Author evidence passes2 traces/8 events,10 cuts,8 retries,754 mutations,72 raw rejections,
76 boundaries and16 source pins. Review caught and corrected the post-move capability-key omission
before freeze. Direct3f passes2 traces/triggers,4 cuts,2 retries,270 mutations,60 raw rejections,
30 boundaries and11 pins; compatible3b lifecycle passes8 traces/24 events,32 cuts,24 retries,
946 mutations,152 raw cases,304 boundaries and16 pins. No runtime/source/test/scenario changes.
Multiple participant moves/opportunities, alternate closure reasons, vehicle/Reserve movement,
armed continuation, .4 composition, Tasks004/021/022 and runtime/simulator remain open.

TASK-003D2c.3h checkpoint, 2026-09-11 (input `f75710a`):
[Inherited Reserve-cycle specification](../specs/combat-inherited-reserve-cycle-v1.md), inventory,
vectors and oracle branch from 2d's real both-owner Normal-Weather `I` designation histories.
Owner no-move Movement completion, System idle Breakdown completion, empty Combat selection and six
System no-attack completions form the exact ten-event path to same-slot Reserve Release. Final state
retains Reserve I, designation receipt/history, original locations, CP0, World/RNG/Weather and cycle
authority. Two traces/20 events pass22 cuts,20 retries,1103 deep mutations,46 malformed-byte cases,
196 boundary checks and13 source pins. No route/stop, release, repeat, positive Reserve move,
runtime/source/test/scenario or simulator change is claimed. Those capabilities and .4/004/B remain
open.

TASK-003D2c.3i checkpoint, 2026-09-11 (input `fbfd849`):
[Inherited Reserve Release specification](../specs/combat-inherited-reserve-release-v1.md), inventory,
vectors and oracle replay both 3h terminals and freeze System open, owner release-I and System
completion. Six events across two traces advance authority22→25, preserve cycle ordinal1 and all
prior history, change only the own member's Reserve I→none status, and record a pending ordinal-2
Movement exception with CPA basis/ceiling10. Eight cuts, six retries,658 mutations,24 malformed-byte
rejections,31 boundaries, ten recovery paths and seven source pins pass. [Independent review15](../reviews/combat-inherited-reserve-release-review-15.md)
returned Ready with non-blocking follow-ups; its P3 retained-negative-coverage finding was accepted
and corrected before commit `719ea0d`. Guarded repeat, positive Reserve movement, alternate
conversion profiles, .4/004/B, runtime and simulator remain open. The authorized Combat review
sequence is exhausted at15of15; another pass requires explicit owner authorization.

TASK-003D2c.3j checkpoint, 2026-09-11 (input `765b2f9`):
[Inherited armed-continuation specification](../specs/combat-inherited-armed-continuation-v1.md),
inventory, two proof vectors and oracle replay both 3i terminals and derive one exact candidate per
owner without mutating authority. Existing selection/seal/result/settlement/Snapshot fixtures are
hash-pinned as the full-result support boundary. Two readbacks,180 deep mutations,8 malformed or
alternate-byte cases,18 boundary rejects and18 source pins pass. This is continuation admission,
not cycle repeat, Movement, Combat execution, runtime or simulator activation. Guarded
repeat/finish composition, positive released-Reserve Movement/expiry, broader profiles, .4/004/B,
runtime and simulator remain open. Author verification only; review15 remains the exhausted limit.

TASK-003D2c.3k checkpoint, 2026-09-11 (input `36e83f8`):
[Inherited guarded cycle-control specification](../specs/combat-inherited-cycle-control-v1.md),
inventory, four vectors and oracle compose both exact 3i Release terminals with both exact 3j armed
proofs. System open plus owner repeat/finish advances authority25→27. Repeat enters ordinal-2
Movement in the same owner slot while retaining World/RNG/history, ammunition10, TOE10, CP0 and
the pending exception; finish enters Truck Convoy and expires that exception with the closure
receipt. Twenty-four readbacks,12 retries,1648 mutations,84 malformed-byte rejections,14 timing/
recovery/capacity checks and9 source pins pass. This is private contract evidence, not ordinal-2
Movement, Combat execution, Snapshot12, runtime or simulator activation. Positive released-Reserve
Movement/expiry, broader profiles, .4/004/B, runtime and simulator remain open. Author verification
only; review15 remains the exhausted limit.

TASK-003D2c.3l checkpoint, 2026-09-11 (input `9e39911`):
[Inherited released-I Movement specification](../specs/combat-inherited-reserve-movement-v1.md),
inventory, two vectors and oracle replay exact 3k repeat terminals and move each owner from assault
to own rear. One event advances authority27→28, charges Clear2 CP under released-I ceiling10,
updates World/member CP atomically, and retains ammunition10, TOE10, Cohesion, RNG, history and the
pending ordinal-2 exception. Eight readbacks,2 retries,1392 mutations,24 malformed-byte rejects,11
boundary rejects and6 source pins pass. Movement completion/exception expiry, broader profiles,
.4/004/B, runtime and simulator remain open. Author verification only; review15 remains exhausted.

TASK-003D2c.3m checkpoint, 2026-09-12 (input `91020d9`):
[Inherited released-I Movement-completion specification](../specs/combat-inherited-reserve-movement-completion-v1.md),
inventory, two vectors and oracle replay exact 3l terminals through owner deliberate stop, System
empty-cohort resolution, and owner Movement completion. Six profile-specific v1 events advance authority28→31,
retain World/RNG/attack history, route, CP2, ammunition10 and TOE10, derive the ordinal-2 end proof,
and apply unchanged D2b.2 expiry using each accepted completion receipt. Sixteen readbacks,6 retries,
1626 mutations,60 malformed-byte rejects,58 authority/order boundaries and10 source pins pass.
Broader Reaction/vehicle and remaining `.3` families, .4/004/B, runtime and simulator remain open.
Author verification only; review15 remains exhausted.

TASK-003D2c.3n checkpoint, 2026-09-12 (input `e2302d2`):
[Inherited direct Reaction-closure specification](../specs/combat-inherited-reaction-closure-v1.md),
inventory, six vectors and oracle replay both exact3f trigger terminals through owner decline or
System unavailable/timeout. One compatible `reaction-window-closed`3 event advances authority13→14,
closes the sole unresolved opportunity, clears the interrupt, restores the suspended sequence
position and resumes the exact phasing route with unchanged World/RNG/progress. Twelve cuts,6
retries,372 mutations,180 malformed-byte rejects,126 authority/fork/capacity boundaries and14
source pins pass. Core timeout proves exact System authority, not host scheduling. Active-participant
fallback and second-move branches are frozen separately; multiple opportunities, vehicle profiles,
.4/004/B, runtime and simulator remain open. Author verification only; review15 remains exhausted.

TASK-003D2c.3o checkpoint, 2026-09-12:
[Inherited active Reaction-fallback specification](../specs/combat-inherited-reaction-active-fallback-v1.md),
inventory, four vectors and oracle replay both exact3g post-first-move states through System
unavailable/timeout closure and mandatory empty-stop resolution. Eight compatible v3/v2 events
advance authority14→16, record reason-specific `reactor-stop-closed`, remove the window, and resume
the exact phasing route with unchanged World/RNG/progress. Twelve cuts,8 retries,612 mutations,200
malformed-byte rejects,132 authority/fork/capacity boundaries and17 source pins pass. Core timeout
remains authority, not host scheduling. Post-second-move participant completion is frozen separately
by `.3q` below; multiple opportunities, vehicle profiles, .4/004/B, runtime, public activation and
simulator remain open.
Author verification only; independent-review sequence remains exhausted.

TASK-003D2c.3p checkpoint, 2026-09-13:
[Inherited active Reaction second-move specification](../specs/combat-inherited-reaction-second-move-v1.md),
ordered inventory, two retained vectors and executable oracle replay both exact3g post-first-move
states through one owner-authored rear→supply step. Two events advance authority14→15 and CP2→4
while retaining active opportunity and original reactor-route identity. Four cuts,2 retries,260
mutations,60 malformed-byte rejects,48 authority/fork/capacity boundaries and14 source pins pass.
Participant completion/closure is frozen separately by `.3q` below; multiple opportunities,
vehicle profiles, .4/004/B, runtime, public activation and simulator remain open. Author verification
only; independent-review sequence remains exhausted.

TASK-003D2c.3q checkpoint, 2026-09-13:
[Inherited Reaction movement-completion specification](../specs/combat-inherited-reaction-movement-completion-v1.md),
ordered inventory, two retained vectors and executable oracle replay both exact3p authority15
terminals through owner completion, mandatory empty-stop resolution and System no-eligible closure.
Six compatible v3/v2/v3 events advance authority15→18 while retaining supply/CP4 World, exact
reactor track, RNG and progress before resuming the suspended phasing route. Eight cuts,6 retries,
506 mutations,54 malformed-byte rejects,72 authority/fork/capacity boundaries and18 source pins
pass. Multiple opportunities, vehicle profiles, .4/004/B, runtime, public activation and simulator
remain open. Author verification only; independent-review sequence remains exhausted.
