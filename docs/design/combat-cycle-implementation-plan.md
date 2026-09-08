# Combat and Cycle Combined Contract / Implementation Plan

**Status:** `CMB-PLAN-001` and POL-001–008 accepted by owner on2026-09-06 at `a10a588`.
`CMB-TASK-001` **complete** within its research scope, including accepted
[source ruling CMB-SRC-RUL-001](../research/combat-source-freeze-v1.md). [TASK-002 Content freeze](../specs/combat-content-v7.md) is complete as a contract packet;
[Checkpoint A author check](../reviews/combat-checkpoint-a-author-check.md) is complete, and owner
requested continuation. [TASK-003A creation/initial ledger](../specs/combat-creation-ledger-v1.md) is complete as a contract
slice; [TASK-003B World/settlement](../specs/combat-world-settlement-v1.md) is complete as a contract packet.
[Progress review5](../reviews/combat-progress-review-5.md) returned Ready with non-blocking follow-ups;
its status correction is applied. Parent003 remains open for003C/D.
[003C1 rules inputs/config](../specs/combat-rules-inputs-v1.md) is complete as a contract slice;
[review6](../reviews/combat-inputs-review-6.md) returned Ready with no actionable findings (6of7 used at that checkpoint).
[003D1 sequence/cycle packet](../specs/combat-cycle-sequence-v1.md) is complete;
[review7](../reviews/combat-sequence-review-7.md) returned Ready with no actionable findings (7of7 used).
[003C2 Rules10/creation envelopes](../specs/combat-authority-envelope-v1.md) are complete for the creation cut,
with author verification and independent review8 Ready (local report recorded below).
[003C3a selection/step contracts](../specs/combat-selection-steps-v1.md) are
complete as a bounded control fragment. [003C3b sealed round/commitment](../specs/combat-sealed-round-v1.md)
is complete with author checks. [HOST-RSH-001](../research/orleans-publication-feasibility.md) research
is complete. [003C3c result/settlement](../specs/combat-result-settlement-v1.md) and
[noninitial Snapshot12 composition](../specs/combat-snapshot-composition-v1.md) are complete as bounded
contract checkpoints with author checks. [003D2a ordinary movement](../specs/combat-ordinary-movement-v1.md)
is complete with author checks. [003D2b.1 Reserve Release](../specs/combat-reserve-release-v1.md) is
complete as a private control/history arm. [003D2b.2 guarded repeat/finish](../specs/combat-cycle-control-v1.md)
is complete for the private exhausted-ammunition continuation boundary and Movement-expiry projection;
[003D2c.1 successor declarations/first opening](../specs/combat-inherited-successors-v1.md) is complete
as an isolated contract boundary. [003D2c.2a opening provenance](../specs/combat-opening-preamble-v1.md)
is complete through Weather entry. [003D2c.2b Weather](../specs/combat-weather-v1.md) is complete
through Organization entry with author checks;003D2c.2c stage-entry successors are next.
ParentD2c.2/D2c/D2 remains open.
[Progress review9](../reviews/combat-progress-review-9.md) assessed all13 unmerged commits through
`a96d2a1`: Ready with non-blocking follow-ups; its sole P3 documentation-status finding is corrected.
All15 Python oracles and the focused Orleans build/run/format passed at that checkpoint.
[Review10](../reviews/combat-progress-review-10.md) returned Ready with non-blocking follow-ups
for D2b.2/D2c.1. Its P2 handoff ambiguity and P3 source-inventory status drift are corrected below.
All17 oracles passed independently at review10. Review11 across merged
[PR95–98](https://github.com/dills122/sandtable/pull/98) returned Ready with no findings and independently
passed24 oracle jobs plus the Orleans probe. Budget11of11 is exhausted; Weather2b is subsequent
author-verified work and is not covered by that verdict.
TASK-004–025 not started. Future maturity execution and checkpoint B remain gated.
**Original input:** `d2bc67c`. Exact contract freeze and production gates remain open.

Cross-package sequencing lives in the [roadmap checkpoint](../roadmap/pre-alpha-roadmap.md#current-checkpoint-and-next-gates).
Task005 begins dormant implementation; public activation is020–021 and Combat simulator evidence
is022–024. Owner accepted the author review's planning direction after `b8be39a`: the bounded
`HOST-RSH-001` investigation is scheduled after003C3b during remaining contract work. It can use
current Rules9 and is not a dependency of B. Production hosting still requires its own contract and
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

CON-001 now has the [Content7 contract/oracle](../specs/combat-content-v7.md); its reserved identity
is not registered in runtime. The remaining rows are required contents, **not allocated production
versions or executable schemas**. TASK-003–004 must freeze exact field names, types, bounds, tags,
canonical ordering/escaping, errors, schema/format/capability versions and golden bytes before any
consumer is implemented.
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
| `CMB-TASK-003` / M, 3–5 files | Freeze CON-002–004 world/event/snapshot/command schemas and state transitions. Allocate exact versions after registry inventory; pin causal receipts, bounds, loss/guard/CP conservation, cycle prefix, deadline and migration/recovery behavior. Freeze ordinary movement receipts that atomically debit break-off/terrain CP, apply immediate excess-CPA DP and end only affected relation memberships. Retain earned/due replacement scopes and the unresolved phase-specific maturity gate. | Field-by-field design trace, canonical positive/negative vectors and restart-cut matrix; explicit unsupported-state rejection. | 001–002; new authority contract packet and vectors under [design](./), [Campaign tests](../../tests/Cna.Core.Tests/Campaigns/). |
| `CMB-TASK-004` / M, 3–5 files | Freeze CON-005/006 side and Exercise contracts, candidate bytes and terminal evidence. Enumerate every design AC in an evidence index; mark deferred transport requirements without allocating fake production support. | Equal-authorized-history vectors, authority-leak negatives, ordinal/terminal tampering and all 72 ACs mapped to a task and planned test. | 003; new side/evidence packet, [Observation tests](../../tests/Cna.Core.Tests/Observations/), [Exercise tests](../../tests/Cna.Core.Tests/Exercises/). |

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
| `003D2c.4` | Full World/Snapshot composition, actual first-opening/continuation trace, retained history/obligations/capacity and CON-002–004 reconciliation. Close parent003 after all003 dependencies, integrated contract checks and the003-owned Task004 handoff below are complete;004 then produces the complete AC map. |

D2c.2 refinement (input `387445b`, before edits): four sequential family checkpoints,
each at most five primary files including its spec/schema/fixture/oracle/plan:

| D2c.2 slice | Contract boundary / order |
| --- | --- |
| `003D2c.2a` | Opening preamble: validated Created11→Initiative3→no-obligation convoy2→tactical2→order2→Weather entry. Actual prospective prefix/version/receipt/RNG provenance, both order choices, strict cut replay. |
| `003D2c.2b` | [Weather2](../specs/combat-weather-v1.md) complete:34 creation-rooted traces/68 cuts, all four outcomes and12 foul kind/location pairs, rejected RNG bytes, strict receipts/state and exact Organization successor. |
| `003D2c.2c` | Stage-entry successors: Organization/arrival/fleet-assignment/fleet-repair2 after2b; derive explicit no-obligation gates and actual Reserve-entry history. |
| `003D2c.2d` | Reserve2 designation/completion: consume2c, derive real designation history and compose atomic first opening fromD2c.1 without synthetic predecessor hashes. |

D2c.2b execution boundary (input `3ca453a`, before implementation):
[Weather packet](../specs/combat-weather-v1.md), schema, retained fixture, executable oracle and
this plan are the five primary files. Consume exact C2 creation and all four2a events; bind the
existing Weather artifact to Rules10, cover four Fall outcomes/all foul location rows and rejected
RNG bytes, preserve World/holder/order, publish one Weather2 event/receipt and Organization entry.
Strict input/cached-state/replay/retry/cross-history negatives are required. Current C# source and
historical canonical artifacts stay unchanged; zero effects require the explicit Setup7 policy.

These are prospective contract/oracle chains, not C# runtime replay. ParentD2c.2 stays open until2d
composes the accepted creation-to-opening chain; later Movement/continuation and full Snapshot
admission remainD2c.3–4/004/B. No additional independent-review pass is authorized.

D2c.4 owns the **Task004 handoff** section in its planned
`docs/specs/combat-authority-composition-v1.md` contract packet, within the same five-primary-file
cap. That section must identify the frozen CON-002–004 versions/hashes, composed trace and capacity
results, relevant requirement IDs, explicit capability exclusions and remaining runtime evidence
owners. This is003's contract handoff, not004's complete72-AC map. Parent003 closes after all003
slices and their integrated checks, including this handoff, are complete. Task004 then consumes the
handoff, freezes CON-005/006 and completes its72-AC evidence map. Checkpoint B requires both003 and004;
003 does not depend on004 starting or completing.

Each checkpoint retains the five-primary-file cap. These refine already-required inherited work;
no gameplay policy, runtime activation or checkpoint-B acceptance changes. User requested one
additional independent pitstop after the next checkpoint: review10of10, not a reset of the count.

D2a isolated Movement fixtures may derive a settled World from C3c, but any intervening release/repeat
boundary remains explicitly synthetic untilD2b/c. Such probes do not certify actual continuation,
released-Reserve history, runtime movement, Reaction or a reachable repeating campaign.

Checkpoint B: accept exact combined contracts and resolve any changed policy with owner. Reconcile
task sizes with frozen types. Runtime implementation remains gated until this checkpoint passes.

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
and019A with bounded scope/dependencies; these still require checkpoint B acceptance. The numbered parent rows below retain their identities;
this planning acceptance does not declare B passed or start runtime implementation.

### Checkpoint C — dormant table and fixture foundations

| Task / size | Output and acceptance criteria | Verification | Dependencies / likely paths |
| --- | --- | --- | --- |
| `CMB-TASK-005` / M, 3–5 files | Implement Rules-owned selected tables and pure arithmetic from approved manifest; preserve ordered pair coordinates and conditional capture. No campaign activation. | Full normalized source comparison, every coordinate and all reachable differential branches; RNG research goldens as supplemental vectors. | 001–004; [Rules](../../src/Cna.Core/Rules/), [Rules tests](../../tests/Cna.Core.Tests/Rules/). |
| `CMB-TASK-006` / M, 3–5 files | Implement versioned synthetic Content/scenario seed admission with explicit component and supply facts; preserve historical Content bytes. Reject profile mutations before active decisions. | `CombatContent` canonical/readback, missing/extra facts, provenance and historical compatibility tests. | 002–005; [Content](../../src/Cna.Core/Content/), [Content tests](../../tests/Cna.Core.Tests/Content/). |

Checkpoint C: focused suites and repository build/format gates pass; verified tables and fixture
exist, but no active Combat capability is advertised.

### Checkpoint D — loss-capable state and strict persistence

| Task / size | Output and acceptance criteria | Verification | Dependencies / likely paths |
| --- | --- | --- | --- |
| `CMB-TASK-007` / M, 3–5 files | Implement frozen world/obligation types and validated creation seeds. Represent ordinary integer infantry spending up to 150% CPA with immediate excess-CPA DP, stricter released-Reserve ceilings and separately valid mandatory overrun; retain distinct guard/loss/replacement provenance and existing Breakdown state. | `CombatWorld` boundary/conservation/overflow tests, including ordinary CPA10 spending 11/15 versus rejected16, mandatory E10→11, Reserve ceilings, guard transfer and malformed lots. | 003/006; [Campaigns](../../src/Cna.Core/Campaigns/), [Campaign tests](../../tests/Cna.Core.Tests/Campaigns/). |
| `CMB-TASK-008` / M, 3–5 files | Implement canonical snapshot/history codec and strict restore for new state. Historical bytes unchanged; reject missing/forged cycle or settlement evidence and unsupported migration. | `CombatPersistence` roundtrip/golden/tamper tests; recovery works when fresh admission is disabled. | 007; [Campaigns](../../src/Cna.Core/Campaigns/), [Campaign tests](../../tests/Cna.Core.Tests/Campaigns/). |

Checkpoint D: codec tests prove obligations survive serialization and fresh Core reconstruction
for the states whose handlers exist. Later lifecycle tasks add their event handlers and boundary
cuts to this same contract, without changing frozen bytes silently. This is in-process recovery
evidence, not durable storage, silo/process restart or atomic Chronicle publication; those require
the separately scoped hosting work.

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
into named tests/vectors; TASK-025 records actual results. No implementation evidence exists yet.

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
