# Combat and Cycle Combined Contract / Implementation Plan

**Status:** `CMB-PLAN-001` and POL-001–008 accepted by owner on2026-09-06 at `a10a588`.
`CMB-TASK-001` **complete** within its research scope, including accepted
[source ruling CMB-SRC-RUL-001](../research/combat-source-freeze-v1.md). [TASK-002 Content freeze](../specs/combat-content-v7.md) is complete as a contract packet;
checkpoint A review remains open. TASK-003 authority freeze is next; TASK-003–025 not started. Future maturity execution and checkpoint B remain gated.
**Original input:** `d2bc67c`. Exact contract freeze and production gates remain open.

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

Checkpoint A: review source coverage and Content freeze; unresolved source/calendar facts stay
visible. No favorable-vector or synthetic arithmetic result substitutes for full selected coverage.

### Checkpoint B — authority and outward contract freeze

| Task / size | Output and acceptance criteria | Verification | Dependencies / likely paths |
| --- | --- | --- | --- |
| `CMB-TASK-003` / M, 3–5 files | Freeze CON-002–004 world/event/snapshot/command schemas and state transitions. Allocate exact versions after registry inventory; pin causal receipts, bounds, loss/guard/CP conservation, cycle prefix, deadline and migration/recovery behavior. Freeze ordinary movement receipts that atomically debit break-off/terrain CP, apply immediate excess-CPA DP and end only affected relation memberships. Retain earned/due replacement scopes and the unresolved phase-specific maturity gate. | Field-by-field design trace, canonical positive/negative vectors and restart-cut matrix; explicit unsupported-state rejection. | 001–002; new authority contract packet and vectors under [design](./), [Campaign tests](../../tests/Cna.Core.Tests/Campaigns/). |
| `CMB-TASK-004` / M, 3–5 files | Freeze CON-005/006 side and Exercise contracts, candidate bytes and terminal evidence. Enumerate every design AC in an evidence index; mark deferred transport requirements without allocating fake production support. | Equal-authorized-history vectors, authority-leak negatives, ordinal/terminal tampering and all 72 ACs mapped to a task and planned test. | 003; new side/evidence packet, [Observation tests](../../tests/Cna.Core.Tests/Observations/), [Exercise tests](../../tests/Cna.Core.Tests/Exercises/). |

Checkpoint B: accept exact combined contracts and resolve any changed policy with owner. Reconcile
task sizes with frozen types. Runtime implementation remains gated until this checkpoint passes.

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

Checkpoint D: persistence tests prove stored obligations survive restart. Later lifecycle tasks add
their event handlers and boundary cuts to this same contract, without changing frozen bytes silently.

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
| `CMB-TASK-018` / M, 3–5 files | Implement retained Movement-end proximity, next-Movement exception and ordinary Contact/Engaged break-off under the frozen cost precedence and ordinary 150%-CPA ceiling (stricter for released Reserves). Charge CP and any immediate excess-CPA DP, move and update affected memberships atomically; preserve unrelated relations and CP/BP/bands/broken lots, ammo/TOE/Cohesion and offensive-use history. | `CycleMovement` Contact/no-ZOC Engaged/overlapping-cost and last-counterpart cases, spent5+4+1=CPA10, ordinary totals11/15 with DP versus rejected16, stricter Reserve ceilings, atomic restart; exception expiry, changed enemy position, mandatory overspend, retained resource/Breakdown history and exhausted-assault rejection. | 017; [Campaigns](../../src/Cna.Core/Campaigns/), focused Core tests. |
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
has local checks only; no new independent Ready verdict is claimed. User-approved limit: **4/4 used**.

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
Checkpoint A review remains open; no additional independent review has run (budget4/4). TASK-003
must reconcile creation ledger/readiness/Normal Weather and Rules binding before checkpoint B.
