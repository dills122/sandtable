# Task 021 — Combat capability manifest and ordinary activation: scoping (2026-11-20)

**Status:** author scoping plus an evidence-only first slice (021A0). No product code changed. No
independent review has run. Nothing here claims Task 021 is started in product code, complete,
certified, or "Activated". The owner authorizes review passes and certification.

Predecessor: [Task 020 scoping](2026-11-20-task-020-combat-legal-actions-scoping.md) (T020-Q3
assigned the closed capability manifest here). Frozen input that this task must not change:
[combat-audience-decision-v1](../../specs/combat-audience-decision-v1.md).

## 1. What Task 021 actually requires

Three sources define it. The plan row does not use the word "manifest"; the manifest comes from the
Checkpoint I preamble and the fog spike.

| Source | Text (abridged) |
| --- | --- |
| [Plan, `CMB-TASK-021`](../../design/combat-cycle-implementation-plan.md#checkpoint-i--side-safe-public-activation) | Activate the new certified capability coherently in ordinary Core authority. Reject unsupported initial profiles/terminals before decisions; keep old readers and new recovery available. Verification `CombatCycleAdmission`: ordinary entry-to-terminal public traces, dormant/active separation, historical compatibility, all Core AC evidence indexed. Paths: Core admission/registration files and focused Core tests. |
| Plan, Checkpoint I preamble | "Activation must freeze and validate a closed rules/config capability manifest for every family reachable later, including mandatory actions; reject an uncovered family before play and revalidate on restore or rule change." Checkpoint I closes with a full repository gate and a privacy/admission review. The Activation verification tier ("before Tasks 021 and 024") requires the full gate, a complete contract-oracle sweep, disclosure/admission negatives, reconstruction/readjudication and two clean retained runs. |
| [Fog spike, "Capability admission"](../../research/combat-fog-of-war-architecture-spike.md#capability-admission) | At campaign activation Core freezes a closed manifest for the selected Land rules/config: movement and ZOC families, surprise/forced engagements, unit and weapon classes, terrain/weather/detection rules, information-sharing rules, optional variants. It lists every family that can become reachable later, not just conditions true at start. Each enabled family maps to a versioned contact/disclosure transition, a defined owner or System action, and an adjudication handler. Unknown or uncovered families reject activation with a generic owner-safe result. A rules/config change or restored campaign revalidates the same closure before producing another turn. Tests: every declared family, the first state where each becomes reachable, an unknown-family negative. |
| [Roadmap, Public Core 020–021](../../roadmap/pre-alpha-roadmap.md) | "Activate only the certified profile. Unsupported profiles reject before a decision; disabling new admission must not disable recovery." |
| [Outward composition](../../specs/combat-outward-composition-v1.schema.json) ACs planned on 021 | `CMB-PRO-AC-002` (exact retry recovers receipt), `-003` (wrong bindings reject without effects), `-010` (hidden variants preserve side artifacts), `-012` (historical codecs unchanged, old reader rejects, disabled admission still recovers), `CMB-STEP-AC-012`, `CMB-SET-AC-011`. |

### Correction to the dispatch premise

The dispatch described the manifest as a per-audience list of entitled capabilities used to
validate submissions and support restore. The design source says something different:

- The manifest is **per campaign, per rules/config**, not per audience. It lists **rule families**
  (ZOC, forced/surprise engagement, unit/weapon classes, terrain/weather/detection, sharing,
  variants) and proves each reachable one is covered by a disclosure transition, an owner or System
  action and an adjudication handler.
- Submission validation is already done by the frozen surface: refs are minted per decision and
  checked by re-projection (`CampaignCombatAudienceDecisions.Submit`). The manifest does not
  replace or extend that.
- "Closed" means: (1) the family vocabulary is closed (an unknown family is a rejection, never
  ignored); (2) every family reachable from the configuration (not only at start) has a disposition;
  (3) every enabled family is fully covered. It is fixed at activation and must be the same closure
  on restore.

## 2. What exists today

- **No manifest, no activation step.** Campaign creation (`CampaignCreationExecution.Execute`) is
  the only admission. It already has `CampaignCreationRejectionReason.UnsupportedCapabilityProfile`,
  raised when the setup's `capabilityProfileId` differs from the content's or content validation
  fails.
- **Two Combat capability identities, two authority chains.**
  - The live native contact Combat path (V12–V32 events, Host dev route, and every Task 020 audience
    stage) runs on Rules9 + Content V6 campaigns whose profile is
    `sandtable.capability.breakdown-truck-battalion.v1`. That is a Breakdown profile; no Combat
    capability identity is recorded for these campaigns.
  - `sandtable.capability.combat-cycle-infantry.v1` is reserved by Content V7
    (`ContentPackV7Definition.SupportedCapabilityProfileId`) and Rules10
    (`Cna1979CombatAdjudication.ProfileId`), used by the dormant `CampaignCombatCreationRequest` /
    `CampaignCombat*` chain (Tasks 005–019 contracts). Plan line "no active Combat capability is
    advertised" still holds.
- **Capability checks happen at runtime, not at activation.** Barrage V14 and Anti Armor V20 throw
  if the campaign content has any non-infantry component; the selection cut has the 020C ZOC
  invariant. *Checked in 021A1:* Content V6 admission does restrict the class. `ContentPackV6Validator`
  calls the V5 compatibility check, whose component vocabulary (`Cna1979Combat.ComponentClasses`)
  holds only infantry; an armored component fails with `BRK-CERT-004` and no V6 artifact can carry
  it. Creation also pins the setup's exact pack (`setup.Content.Pack == artifact.Identity`). So, as
  with ZOC in 020C, these throws are unreachable for admitted content (not a live gap).
- **Content-admission precedent for "reject before play":** 020C showed `BRK-CERT-003` and
  `CampaignWorldV6Validator` make positive ZOC unreachable, which is the kind of proof the manifest
  generalizes.
- **A candidate family register exists but is not accepted:**
  [P01 AC-003 bounded-profile proposal](../../design/combat-p01-ac003-bounded-profile-decision-proposal.md)
  "mode register" (owner disposition: *Pending*). The Task 004 handoff also lists 11 capability
  exclusions ([composition §Explicit exclusions](../../specs/combat-authority-composition-v1.md#explicit-exclusions)).
  Owner rulings already fix several dispositions: `CMB-POL-001` (full TOE, decline-only RBA, empty
  gun/armor), `CMB-POL-009` (voluntary surrender excluded), T020-Q2 (forced/ZOC out of profile),
  T020-Q7 (RBA movement excluded), T020-Q8 (non-combat contact via V19 `force-unavailable`).

## 3. Finding from slice 021A0: the frozen round kinds are not closed over the reachable profile

The spike's closure rule needs every reachable owner decision to have a disclosure and owner action.
I measured reachability on the frozen goldens' own fixture
(`rules-lab.breakdown.reaction.combat-double-fresh.v1`) through ordinary Core authority.
`CombatCycleAdmissionReachabilityTests`, 3 rows, local Debug pass:

| Test | Evidence |
| --- | --- |
| `TheSixV1StagesAreDeclaredOnThePublicSurface` | Guard: the explicit six-stage v1 list used below is distinct and every entry is a declared public stage. (Originally it required the public stages to be exactly these six; after Task 020E added three contract-v2 stages it checks the fixed v1 list instead.) |
| `FrozenReserveReleaseIsReachedOnlyThroughAPostAssaultRetreatDecisionOutsideV1` | Seed 6 (the `reserve-release-axis` / `cycle-control-axis` golden path): the assault result requires a defender retreat. Core opens a Commonwealth-owned post-assault retreat decision (Host draft has offers). No v1 stage returns a decision for any audience or subject there (since 020E1 the contract-v2 `post-assault-retreat` stage does; the test asserts its decision is version 2). After the private refusal and System settlement, the Reserve Release decision is byte-equal to the golden path's. Seed 5 (`custody-axis` path) needs no retreat; a System step closes it. |
| `FrozenCycleControlOffersRepeatIntoRepeatMovementAndSelectionOutsideV1` | Seed 6: the frozen Cycle Control decision lists `Repeat`, and the public surface accepts it. Core then opens an Axis repeated-Movement decision, then (after one move and System Breakdown) a repeated Combat selection with offers. No v1 stage serves either (020E2/020E3 later added contract-v2 stages for both). |

Seed scan (local probe, not committed): seeds 1–60 on this fixture require a defender retreat on
6, 11, 14, 16, 23, 33, 34, 37, 43, 47, 55.

Consequences:

- The owner deferred post-assault Retreat and repeat Movement/Selection to "a later phase" (020
  scoping §8). They are not optional in the bounded profile: post-assault retreat is forced by the
  result draw, and `Repeat` is offered inside a frozen decision.
- Under the spike's rule, a manifest for this content cannot close today. Any correct 021
  admission would reject every Combat-reachable campaign until those round kinds have audience
  decisions. Activating anyway would let a player reach a live decision the public surface cannot
  serve, which the spike forbids ("never reach a live turn and silently omit its mandatory action").
- Not checked: second-side Selection (V47). The [RC status](2026-11-17-rc-exhaustive-status.md)
  records that genuine second-acting-side Combat entry does not work yet; if it is unreachable in
  admitted content it needs an explicit "source-condition absent" proof rather than silence.
- The frozen contract stays untouched. Covering these round kinds would be new stages in
  `combat-audience-decision-v2` or an additive contract, which is an owner decision.

## 4. Open items (stop-and-report)

Status (2026-11-20): **Q1 resolved by the owner (option c)**, relayed. **Q2 and Q4 grounded from
existing decisions without a new ruling** (§7); the author choices inside that grounding are
listed for owner review. Q3, Q5 and Q6 stay open; they gate 021A2/021A3, not 021A1.

| ID | Gap | Why it needs a ruling | Options |
| --- | --- | --- | --- |
| **T021-Q1** (blocking) | Reachable owner decisions outside the frozen contract: post-assault Retreat, repeat Movement, repeat Combat selection (§3); second-side Selection unverified. | Closure cannot pass, so 021 cannot activate anything without changing a prior owner sequencing decision. | (a) Pull those round kinds forward into an audience contract successor before 021 activation (extends 020 scope). (b) Restrict the bounded profile so they are unreachable (needs a source-backed rule, e.g. Finish-only cycle control; post-assault retreat is result-driven and cannot be removed without a rules change). (c) Build the manifest machinery dormant now and let it correctly reject today's content; activation waits for (a). |
| **T021-Q2** | Which capability is "the certified capability". | The 020 surface exists only on the native V6 path, which records the Breakdown profile ID; `combat-cycle-infantry.v1` belongs to the dormant V7/Rules10 chain. No support certificate exists (P01 is unadmitted; the AC-003 register is pending). The plan says "certified". | (a) Native V6 path gets its own Combat capability identity (new profile ID or manifest ID), certified by owner review. (b) Activation targets the V7/Rules10 chain (no audience surface exists there). (c) Owner states what "certified" means for 021 (which evidence counts). |
| **T021-Q3** | Where the manifest is frozen. | Persisting it (creation event / snapshot successor) changes canonical bytes and needs new versions (DET-001, `CMB-PRO-AC-012`). Deriving it from already-pinned ruleset/setup/content hashes changes no bytes, but a later Core build could derive a different manifest for the same campaign, which the spike forbids. | (a) Persist a manifest record (new event/snapshot version). (b) Derive it; pin a versioned derivation ID plus manifest hash somewhere already retained (e.g. Host journal head, like timing bytes). (c) Derive it and reject restore when the derivation version changed. |
| **T021-Q4** | Family vocabulary and dispositions. | The spike lists categories; the AC-003 register lists modes (pending); the Task 004 handoff lists 11 exclusions; several owner rulings fix dispositions. An agent should not merge these into a register. | Owner adopts one register (likely AC-003's, with the four classes: supported / source-condition absent / policy-excluded / unsupported-fail-closed) or names another source. |
| **T021-Q5** | What an uncovered family rejects. | The 18 breakdown setups serve Movement, Reaction and Breakdown work that never reaches Combat. Rejecting their creation breaks those suites; the plan says "reject ... before decisions", the spike says "reject activation". "Disabling new admission must not disable recovery." | (a) Reject campaign creation. (b) Admit the campaign but record Combat as not activated; Combat entry rejects before any Combat decision. (c) A new explicit Combat activation step after creation. |
| T021-Q6 (non-blocking) | Host-calls-Core cutover (T020-Q6) was deferred to "021/022". | Decide whether 021 owns it or 022 does. | — |

## 5. Proposed breakdown (each child keeps the five-primary-file cap; review is owner-authorized)

| Child | Output | Depends on |
| --- | --- | --- |
| **021A0** (this slice) | Evidence only: reachability of owner decisions vs the frozen contract (§3). | — |
| 021A1 (dormant, internal) | Manifest model: closed family register, per-family disposition with coverage triple (disclosure stage, owner/System action, handler), canonical bytes and hash, derivation from ruleset + setup + content. Unknown-family and mutation negatives (delete family, flip class, unknown family). | Q4, Q2 |
| 021A2 | Activation admission: compute at creation/activation; uncovered or unknown family rejects with one generic result before any Combat decision. Name the Barrage V14 / Anti Armor V20 throws as bounded-profile invariants citing `BRK-CERT-004`, as 020C did for ZOC (they are unreachable, §2). | A1, Q5 |
| 021A3 | Restore/replay revalidation: `Reconstruct`, snapshot admission and Host cold replay recompute and compare the frozen closure; changed derivation or rules/config rejects before another turn. Disabled admission still recovers (`CMB-PRO-AC-012`). | A2, Q3 |
| 021B | `CombatCycleAdmission`: ordinary entry-to-terminal traces using only the public audience surface (empty, settled assault, repeat), dormant/active separation, historical-reader compatibility. | A3, **Q1** |
| 021C | AC evidence index for the 021-planned ACs; Activation-tier full gate; Checkpoint I privacy/admission review (owner). | B |

Only 021A0 was possible without rulings. 021A1 is built (§8) on the Q1 ruling and the Q2/Q4
grounding in §7.

## 6. Slice 021A0 — what was built

- `tests/Cna.Core.Tests/Actions/CombatCycleAdmissionReachabilityTests.cs` (3 tests). Test-only.
- No change to Core, Host, the frozen contract, its schema, goldens, verifier or any canonical byte.

Local evidence (Debug, 2026-11-20): the new class passes 3/3; `dotnet format --verify-no-changes`
clean on the new file. Neighbor and CI results are recorded in the PR.

## 7. Q2 and Q4 grounded from existing decisions (2026-11-20)

The coordinator asked for the same check Task 020 used for its Q2: whether an answer already follows
from decided or documented material. For the parts 021A1 needs, it does. The parts that do not are
left open and named below.

### Q2: which capability the manifest describes

- **Path: the native contact Combat path (Rules9 + Content V6).** Grounds: the fog spike makes
  contact-based fog "the only Combat visibility path"; the plan's runtime row and status line
  describe that path as the permanent Core/Host implementation; Task 021 depends on Task 020, whose
  frozen `combat-audience-decision-v1` surface exists only on that path. The dormant V7/Rules10
  chain has no audience surface, so it cannot be what 021 activates against that contract.
- **Identity: no new profile ID.** The manifest records the content's own `capabilityProfileId`
  (`sandtable.capability.breakdown-truck-battalion.v1` for every breakdown setup) and identifies
  itself by a codec ID, `sandtable.combat-capability-manifest.v1`. It does not reuse
  `sandtable.capability.combat-cycle-infantry.v1`, which belongs to Content V7.
- **Still open (not needed by 021A1):** what "certified" means for activation. The manifest has no
  certification field and makes no certification claim. That question moves to 021A2 with Q5.

### Q4: the family register

Built only from sources that are already frozen or owner-ruled. Nothing is taken from the pending
AC-003 register except its intent (named classes, unknown fails closed).

| Source | Status | What it supplies |
| --- | --- | --- |
| Host stage names, [contact side projection v2](../../specs/combat-contact-side-projection-v2.md) | Implemented route vocabulary | IDs of the native owner decision families: `combat-selection`, `retreat-before-assault`, `force-assignment`, `post-assault-retreat`, `custody`, `reserve-release`, `cycle-control`, `repeat-movement`, `repeat-combat-selection`. |
| [combat-audience-decision-v1](../../specs/combat-audience-decision-v1.md) | Frozen | Which of those have a disclosure (the six stages). |
| Task 004 handoff exclusions (`verify-combat-authority-composition-v1.py`) | Accepted at checkpoint B | 11 capability IDs and their reasons. |
| `CMB-POL-001`, `-008`, `-009`; T020-Q2, Q5, Q7, Q8 | Owner-accepted | Decline-only RBA, terminal, voluntary-surrender exclusion, ZOC/forced out of profile, custody allowlist, non-combat contact path. |
| `BRK-CERT-003`, `BRK-CERT-004` | Content V6 admission | Why ZOC and non-infantry components are unreachable for admitted content. |
| 021A0 evidence | Measured | Which owner decisions are reached without a frozen stage. |

Five dispositions: `covered` (frozen stage + action + durable step kind), `uncovered` (fails
closure), `policy-excluded` (named owner policy), `source-condition-absent` (an admission or content
fact makes it unreachable), `outside-combat-scope` (not a Combat-sequence family).

Author choices inside this grounding, flagged for owner review (none blocks 021A1, all are dormant):

1. **Two extra decision families.** `noncombat-contact-assault` records T020-Q8 option (c) as a
   covered path (Selection offer + V19 `force-unavailable`). `second-side-combat-selection` (V47) is
   `uncovered` and fail-closed because no evidence proves it unreachable. Related finding, not
   investigated further: the Host records a `native-second-combat-no-attack` System step for the
   second side's "voluntary-finish auto-continuation" (#394) with no owner opening. If that step
   stands in for an owner's finish choice, it conflicts with the separation the AC-003 proposal
   draws between an owner finish and System closure. Recorded for owner review only.
2. **Five handoff IDs are `outside-combat-scope`.** `multiple-opportunity-reaction`,
   `positive-vehicle-breakdown`, `general-resupply`, `new-phase-slot-execution` and
   `hosted-intelligence` concern pre-Combat Reaction/Breakdown (the V6 content profile's own scope),
   logistics, post-terminal phases (`CMB-POL-008` terminal is same-slot Truck Convoy entry) or
   hosting. Calling them "excluded" would be wrong for V6, where truck Breakdown is the profile's
   purpose.
3. **Two handoff IDs map to the repeat cycle.** `two-runtime-assaults` and
   `released-reserve-offensive-combat` are implemented natively (V29/V31) but have no frozen
   disclosure, so they are `uncovered` whenever an assault result is reachable. They overlap
   `repeat-movement` / `repeat-combat-selection`; closure is the same either way.
4. **Reachability predicates are content-level and fail-closed.** "Assault result reachable" means
   both sides field an element with a combat component (a component-less contact is cancelled before
   a result, per T020-Q8). `motorized-retreat` needs a motorized combat element; `gun-armor-combat`
   needs a non-infantry component. Anything not proven unreachable stays reachable.

## 8. Slice 021A1 — manifest model built (dormant, internal)

- `src/Cna.Core/Campaigns/CampaignCombatCapabilityManifest.cs` (internal; no runtime caller):
  - closed 23-family register in canonical order; each family has a fixed authority string and, when
    covered, a fixed coverage triple (frozen audience stage, action, durable accepted-step kind);
  - `Derive(rulesetHash, setup, artifact)`: requires the canonical Rules9 hash, a setup that pins
    exactly this pack and profile, and valid V6 content; dispositions come from content facts;
  - canonical compact JSON bytes and `sha256:` hash; `closed` is true only with no `uncovered` family;
  - strict reader: exact ordered fields, exactly the register (unknown, missing, renamed, reordered
    or duplicated family fails), only dispositions the register can produce for that family, only
    the registered authority and coverage, canonical re-encoding, no stale `closed` bit;
  - `Revalidate(bytes, …)`: the restore primitive for 021A3; retained bytes must equal a fresh
    derivation.
- Nothing is wired into creation, snapshot admission, `Reconstruct` or the Host. No event, snapshot,
  frozen contract or canonical byte changes.

Result on today's content (owner ruling T021-Q1 option c): every one of the 18 breakdown setups fails
closure. On `combat-double-fresh` the uncovered families are exactly `post-assault-retreat`,
`repeat-movement`, `repeat-combat-selection`, `second-side-combat-selection`,
`two-runtime-assaults` and `released-reserve-offensive-combat`.

`CombatCapabilityManifestTests`, 9 rows, local Debug + Release pass:

| Test | Evidence |
| --- | --- |
| `EveryBreakdownSetupDerivesTheClosedRegisterDeterministically` | All 18 setups: register order, deterministic bytes, reader round-trip, revalidation, hash shape, content profile recorded, coverage present iff covered. |
| `TodaysContentFailsClosureOnlyOnTheUncoveredRoundKinds` | Every setup not closed; uncovered set is within the expected set and always includes the fail-closed second side; double-fresh exact set; absent/excluded families as above. |
| `CoveredFamiliesUseExactlyTheFrozenStagesAndDurableStepKinds` | Covered stages are exactly the six frozen stages; handlers are existing `CampaignHostAcceptedStep` kinds; no uncovered family ID is a frozen stage. |
| `NonInfantryComponentIsRefusedAtAdmissionAndStillDerivesUncovered` | Armored component: V6 validation fails only with `BRK-CERT-004` on `componentClassId`; no artifact can be built; derivation still marks `gun-armor-combat` uncovered. |
| `MotorizedCombatElementMakesMotorizedRetreatReachable` | Motorized infantry fails V6 validation; derivation marks `motorized-retreat` uncovered. |
| `OneSidedCombatContentMakesAssaultResultFamiliesAbsent` | No Commonwealth combat component: assault-result families become absent; only the second side stays uncovered. |
| `StrictReaderRejectsEveryRegisterMutation` | 20 JSON mutations (delete, unknown, rename, reorder, duplicate family; policy/absent/always-reachable/uncovered disposition flips; unknown disposition; coverage changed, invented or removed; authority changed; stale `closed`; extra field; manifest ID; version; field order) plus indented, truncated and empty bytes: all rejected. |
| `RevalidationRejectsAnyDriftFromAFreshDerivation` | A canonical, register-legal flip passes the reader but fails revalidation; another setup's manifest, a foreign ruleset, another configuration and a tampered setup hash fail; mismatched content refuses derivation. |
| `DoubleFreshManifestBytesArePinned` | Draft pin of the double-fresh manifest hash (not frozen; owner decides). |

Mutation check (local, not committed): disabling the reader's disposition/authority check fails
`StrictReaderRejectsEveryRegisterMutation`; making `Revalidate` always succeed fails
`RevalidationRejectsAnyDriftFromAFreshDerivation`.

Neighbor suites: `Cna.Core.Tests.Actions.CombatCycle*`, `AuthorityBoundaryTests`,
`CombatBoundedProfileZocTests` and the new class, 66/66 local Debug. `dotnet format
--verify-no-changes` clean on both new files. Timing rows added to
`.github/scripts/long-suite-weights.json`; partition tests 35/35.
