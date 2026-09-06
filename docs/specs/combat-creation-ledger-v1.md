# Combat creation and initial ledger contract

**Status:** `CMB-TASK-003A`, input `4303004`, 2026-09-06. Contract-only slice of CON-002;
parent TASK-003 remains open for003B–D. [Checkpoint A](../reviews/combat-checkpoint-a-author-check.md)
and owner continuation permit this work. No runtime registrations, migration or simulator changes.
[Content7](combat-content-v7.md), [accepted policies](../design/combat-cycle-policy-reconciliation.md)
and [source manifest](../research/combat-source-freeze-v1.md) govern these values.

## Registry and compatibility inventory

| Family | Current implementation at input | Reserved successor / owner |
| --- | --- | --- |
| Content | [Content6](../../src/Cna.Core/Content/ContentPackV6Models.cs), schema6 / content-json.v5 | Content7 / content-json.v6 already frozen by002 |
| Setup | [CampaignSetupSnapshotV6](../../src/Cna.Core/Campaigns/CampaignSetupV6.cs), schema6; [canonical codec](../../src/Cna.Core/Campaigns/CampaignSetupV6Codec.cs) | `CampaignSetupSnapshotV7`, schema7, frozen here |
| World / elements | [World6](../../src/Cna.Core/Campaigns/CampaignWorldV6.cs); [Element5 / Operational5 / component TOE](../../src/Cna.Core/Campaigns/CampaignWorldV5.cs) | World7 wrapper in003B; `CampaignElementStateV6` / `CampaignElementOperationalStateV6` initial values here, later-state invariants in003B |
| Snapshot / creation | [Snapshot11](../../src/Cna.Core/Campaigns/CampaignSnapshotV11.cs), [Created10](../../src/Cna.Core/Campaigns/CampaignCreationV10.cs) | Snapshot12 / Created11 reserved for003C, exact envelopes not frozen here |
| Rules / sequence | [Rules9](../../src/Cna.Core/Rules/Cna1979Ruleset.cs), [sequence4/catalog4](../../src/Cna.Core/Rules/Cna1979LandSequenceV4.cs) | Rules10 reserved; exact bundle/hash in003C, implementation in005; sequence successor and receipts in003D |

No reserved C# successor names above existed in source/tests at input. These reservations are not
current constants. Preserve every prior reader, fixture byte/hash and rules artifact. New Content7
requires explicit Setup7/new-campaign selection; old readers reject new versions. No FromPredecessor
constructor may silently synthesize the new initialization policy. Disabling new admission must not
disable recovery of already-created new campaigns (003C/008).

Ruleset name equality is insufficient. Future creation must bind the exact registered Rules10
artifact containing the accepted table/amendment and policies, Content7 identity, Setup7 hash and
supported profile. Setup has no second Rules hash: the creation/snapshot envelope owns that binding.
This packet does not invent a placeholder Rules10 hash or claim that Rules9 admits Combat. Full
Rules artifact canonical bytes and hash remain an explicit003C combined-contract gate before005
implements the registered artifact; they cannot be deferred until after runtime consumers begin.

## Setup7: exact shape and hash

Reserve `CampaignContentV7Selection`, `CampaignCombatInitializationPolicy` (contract1), and
`CampaignSetupDefinitionV7` / `CampaignSetupSnapshotV7`. Definition presentation text remains outside
canonical identity, as in Setup6. Snapshot properties follow the table; constructors use those typed
fields, compute the hash, and require equality when reading an expected hash. Collections are copied
and compared structurally. Existing policy types and source reference semantics are reused.

| Object | Mandatory property order / types |
| --- | --- |
| Setup | `schemaVersion:int, setupId:id, setupHash:hash, isSynthetic:bool, capabilityProfileId:id, initialGameTurn:int, initialInitiative:Initiative, openingPreamble:Policy, weather:Policy, stageEntry:StageEntry, combatInitialization:Initialization, content:Selection, sources:Reference[]` |
| Initiative | `kind:string, holder:side` |
| Policy | `contractVersion:int, kind:string, sources:Reference[]` |
| StageEntry | `contractVersion:int, gameTurn:int, operationStage:int, organization:string, navalConvoyArrival:string, fleetAssignment:string, fleetRepair:string, sources:Reference[]` |
| Initialization | `contractVersion:int, gameTurn:int, operationStage:int, capabilityPointsExpended:Cp, cohesionLevel:int, reserveStatus:string, origin:Origin` |
| Selection | `schemaVersion:int, formatId:id, packId:id, rulesetId:id, hash:hash, scenarioId:id` |
| Cp | `numerator:long, denominator:int` |
| Reference / Origin | Exact Content7 reference/origin shapes, with the selected sources below |

All objects reject missing/unknown/duplicate properties. `id`, locator, Origin and canonical UTF-8
rules reuse Content7; Setup7 uses the same65,536-byte/32-depth limit. `hash` is `sha256:` plus64
lowercase hex. Int is signed32-bit; long is signed64-bit; bool/float/exponent never substitutes for
an integer. Turns1–111, stages1–3 structurally; this creation profile requires stage1. CP numerator
is nonnegative, denominator positive; reuse [CapabilityPointAmount](../../src/Cna.Core/Rules/CapabilityPointAmount.cs)
and its [codec](../../src/Cna.Core/Rules/CapabilityPointAmountCodec.cs): reduced fractions, zero0/1.
Do not change old integer/rational semantics; the selected creation value is exactly0/1.
Cohesion is a signed integer with upper cap10; creation requires0. Later cause-specific bounds and
DP/RP receipts belong to003B/003D; do not impose a new historical lower bound through this packet.

Canonical Setup order is the table order; references sort by `(sourceId,locator)`. Hash calculation
omits only `setupHash` from the top-level object; all other properties, including initialization,
Content selection and provenance, participate. Persisted Setup includes the verified hash in the
listed position. No pretty whitespace, BOM, trailing newline, escaped alternative spelling or
noncanonical ordering. Unknown tokens reject before hash comparison; no defaults or repair.

Closed creation constraints:

- `schemaVersion=7`, `isSynthetic=true`, profile exactly `sandtable.capability.combat-cycle-infantry.v1`.
  Selected Content validates through the Content7 contract; all six identity/scenario fields match.
- `initialGameTurn`, StageEntry scope, initialization scope and scenario start agree; scenario
  start/end are the same stage1. No creation at stage2/3 or arbitrary Combat entry. This narrows
  creation admission without changing Content7's structural representation or inventing prior events.
- Initiative is `predetermined`, holder `axis` or `commonwealth`. Golden uses Axis; reversing the
  holder changes Setup hash and the later role resolution, not component ownership.
- Opening policy contract1 / `no-opening-naval-convoy-obligations`, source
  `sandtable-rules-lab` / `opening-preamble.no-naval-convoy-obligations.v1`.
- Weather policy contract1 / `no-immediate-weather-effect-subjects`, source
  `sandtable-rules-lab` / `weather.no-immediate-effect-subjects.v1`.
- StageEntry contract1, all four obligation tokens `explicit-none`, source
  `sandtable-rules-lab` / `stage-entry.no-obligations.v1`.
- Initialization contract1: CP0/1, Cohesion0, Reserve `none`; synthetic Origin exactly
  `sandtable-rules-lab` / `combat.close-assault-positive.v1:initial-ledger`.
- Setup has exactly one source: `sandtable-rules-lab` / `combat.close-assault-positive.v1:setup`.
  This is the explicit synthetic scope declaration, not a claim about historical unit deployment.

Weather policy does not select Normal Weather. Ordinary Weather adjudication runs on the campaign
stream and persists its real events. Later assault certification requires actual current Normal
Weather for the relevant area; other results cannot be rewritten, rerolled or given a favorable
seed during admission. An unsupported assault can remain unavailable while the supported empty
path continues, subject to the later terminal contract. Scenario readiness says the synthetic
exercise starts with stage-scoped distribution facts; it does not fabricate prior Logistics events
or allow readiness to survive scope changes. Neither direction anchor grants ammo or other supply.

## Initial authority element values

Initial element arrays are canonical by `elementId`; components by `componentId`. Each element
occurs once and the set equals the selected scenario inventory. Names/order below extend the
existing element/operational/component representation without duplicating current TOE or CP.

| Object / C# type | Mandatory property order / types |
| --- | --- |
| Element / `CampaignElementStateV6` | `elementId:id, currentLocationId:id, reserveStatus:string, operationalState:Operational, components:Component[], sourceParentFormationId:id, currentParentFormationId:id, ammunition:Ammunition, readiness:Readiness` |
| Operational / `CampaignElementOperationalStateV6` | `ledgerGameTurn:int, ledgerOperationStage:int, capabilityPointsExpended:Cp, cohesionLevel:int, vehicleBreakdownState:null, movementEnded:null, initialLedgerOrigin:Origin` |
| Component / existing `CampaignComponentToeState` | `componentId:id, currentToe:int, initialToeOrigin:Origin` |
| Ammunition / `CampaignElementAmmunitionState` | `points:int, initialAmmunitionOrigin:Origin` |
| Readiness / `CampaignElementCombatReadinessState` | `gameTurn:int, operationStage:int, waterStatus:string, storesStatus:string, pinned:bool, initialReadinessOrigin:Origin` |

This table freezes the **initial** value shape. Null Breakdown/movement-ended fields are mandatory
for these nonmotorized new units; their existing non-null typed contracts must still be preserved
where later World7/other admitted continuity requires them. It is not a zero-only runtime validator.
World7's complete representations, lots, later relationships/settlement/causes and snapshot envelope
remain003B/003C. The fixture's standalone element array is a golden for this value fragment, not a
complete saved campaign and not a new standalone production artifact/catalog.

Initial construction maps exact Content facts:

1. Copy selected placement location and each current TOE with its origin. Copy element ammo10 and
   its origin; do not recompute from maximum TOE or serialize a second component ammo balance.
2. Copy readiness scope, two distribution statuses, pinned=false and origin. Require scope equal
   to the new stage1 ledger. Changes to any seed or origin must change Content and Setup identity.
3. Copy source parent ID from Content; current parent initially equals source parent, and both
   references resolve to that element's same-side root formation. Basic Morale stays in Content;
   do not copy it into the element. Stable original side/unit binding is completed in003B/003C.
4. Copy CP/Cohesion/Reserve and initial-ledger provenance from the explicit initialization policy.
   No prior DP/RP cause exists at creation; the future creation receipt establishes this empty
   history. Do not add aggregate DP/RP balances or a second Cohesion truth.003B freezes subsequent
   cause receipts; later operations update the existing ledger and retain its initial provenance.
5. Existing initial representation factory semantics must be carried into World7 with complete
   inventory, no component duplication and zero initial broken lots; exact World7 bytes in003B.

Initial validation compares each value and its origin to the selected immutable inputs; coincidentally
matching quantities with wrong provenance reject. Creation publishes all values once in Created11.
Loss, spending or later movement must never invoke this initial constructor as a recovery repair.

## Errors, evidence and recovery handoff

Codes `CMB-INI-001` shape/JSON; `002` primitive grammar/range; `003` version/profile/Content binding;
`004` unsupported or mismatched stage/policy; `005` Setup policy/source provenance; `006` initial
ledger/seed/parent mismatch, including copied initial origins; `007` Setup hash mismatch; `008` noncanonical bytes. Decoder failures use empty JSON Pointer;
otherwise the oracle/vectors pin the first failing path. Semantic identity and initialization checks
precede hash comparison, so rehashing an unsupported change cannot make it valid.

[Fixture/vector manifest](fixtures/combat-creation-ledger-v1.json) retains literal canonical UTF-8
strings, byte counts and hashes for Setup7 and its initial element fragment, plus isolated mutations.
[Verifier](verify-combat-creation-ledger-v1.py) checks the Content7 golden independently against its
pinned hash, validates Setup/readback and exact initial mapping, and rejects all mutations. An
alternate predetermined holder and shuffled construction must be valid; a provenance-only Content
change must invalidate the old selection and, with an explicitly rebuilt selection, propagate
through Setup hash and the initial origin. This checks binding rather than pinning all packs to one
fixture hash. No Rules10 placeholder or fabricated campaign checkpoint appears in these goldens.

| Cut / input | Required behavior / consumer |
| --- | --- |
| Before creation publication | No campaign state or consumed command; retry derives same initial values.003C/008 pin exact creation request identity and atomic publication. |
| After publication, lost response | Same creation receipt; no new seed copy, RNG draw or second event.003C/008. |
| Later restore/replay | Validate exact stored Content/Setup/Rules bindings and causal history. Read retained current values; never overwrite ammo0 or losses with initial10.003B/003C/008. |
| Old save / disabled new admission | Retain historical reader; no inferred upgrade. New saves retain their own recovery reader despite admission toggle.003C/008. |
| Different stage or repeated cycle | Never apply initialization policy again; scope checks and stage history govern readiness and ledger continuity.003D/017–019. |

POL-001/002/003 map to the initial seed, source-parent and single-ledger checks here; POL-004 recovery
and POL-008 repeat boundaries have explicit future consumers above. CMB-ID-AC-002/008 and
CMB-RES-AC-001/002 gain contract evidence only.003B–D and004 still own the full field/AC matrix,
future obligation bounds, sealed deadlines, World/snapshot/events and outward privacy contract.
TASK-006/008 must reproduce these exact bytes through C# and preserve predecessor fixtures;
constructor copies, real idempotent creation/restart cuts and simulator evidence are unimplemented.

## Executed TASK-003A evidence

`python3 docs/specs/verify-combat-creation-ledger-v1.py` passes63 isolated rejection vectors,
alternate initiative-holder canonical bytes, stale Content binding rejection followed by explicit
provenance rebinding, representable Content stage2 rejected at creation, and shuffled construction.
Golden Setup is1,655 bytes, whole serialized SHA-256
`1ca736568214fc1100ab07f729cb6b5d601a80b524e51933bd754d8e0317d3fd`
(distinct from its embedded self-field-excluded Setup hash). Initial element fragment is2,685 bytes,
SHA-256 `ef3d9f03a2863c31b8f5ca5de144744cc6f5457a84b23d39367b7971ef03bc0e`.
The verifier reads literal retained bytes and expected hashes; it never regenerates its manifest.
Existing Content7 and earlier fixtures remain unchanged. No .NET runtime change requires a new
build/test claim; real creation, restart and public activation remain future validation gates.
