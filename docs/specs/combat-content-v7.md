# Combat Content 7 contract

**Status:** TASK-002 contract packet, prepared against `448e370` (2026-09-06).
CON-001 is frozen here for checkpoint A review; implementation remains gated by combined checkpoint B.
No runtime reader/writer, catalog registration, campaign migration or public capability is added.
[Accepted policies](../design/combat-cycle-policy-reconciliation.md) and
[source manifest](../research/combat-source-freeze-v1.md) govern this contract.

## Identity and compatibility

Inventory at the input commit: [Content 6](../../src/Cna.Core/Content/ContentPackV6Models.cs) uses
schema6 / `sandtable.content-json.v5`; [Content 5](../../src/Cna.Core/Content/ContentPackV5Models.cs)
uses schema5 / `sandtable.content-json.v4`. The legacy Content definition remains schema4.
[Rules](../../src/Cna.Core/Rules/Cna1979Ruleset.cs) is contract9 and
[Setup](../../src/Cna.Core/Campaigns/CampaignSetupV6.cs) is6. No Content7 definition, content-json.v6
format or following capability/profile was allocated in source, tests or docs at that commit.

Reserve these exact successor identifiers; do not change existing constants or historical bytes:

| Field | Required value |
| --- | --- |
| `schemaVersion` | integer7 |
| `formatId` | `sandtable.content-json.v6` |
| `rulesetId` | `cna-1979.1` |
| `capabilityProfileId` | `sandtable.capability.combat-cycle-infantry.v1` |
| New capability | `land.close-assault-inputs` |
| Exact full capability set, ordinal order | `land.close-assault-inputs`, `land.combat-components`, `land.element-mobility`, `land.formations`, `land.hex-topology`, `land.initial-deployment`, `land.weather-areas` |

`ContentPackV7Identity` has `(schemaVersion, formatId, packId, rulesetId, hash)` using the existing
identity units and `sha256:` plus64 lowercase hex grammar. Hash is SHA-256 of complete canonical
Content7 bytes, with no embedded self-hash. Setup's selected scenario additionally binds `scenarioId`;
TASK-003 freezes the successor Setup envelope and its Rules/config hash. Matching ruleset *name*
alone never admits a campaign: Rules must support this exact profile and the accepted source/policy
bundle, including CMB-SRC-RUL-001. Tables, amendments and policy hashes remain Rules/config authority,
not duplicate Content fields. Neither schema7 nor its declared capability activates Combat.

Old readers continue rejecting7. Preserve Content4/5/6 fixtures and existing catalog lookup paths.
TASK-006 adds a distinct Content7 reader/catalog path and dedicated pack. No conversion from old
packs, guessed offensive ratings, inherited Morale or inferred ammo/readiness. Existing campaigns
keep their original Content identity; new capability campaigns require explicit Content7 selection.
TASK-003/008 must preserve recovery of new campaigns even when fresh admission is disabled.

## Types and ownership

Reserve immutable `ContentPackV7Definition`, `ContentCombatComponentV2`,
`ContentElementCombatFactsV2`, `ContentFormationMorale`, `ContentInitialAmmunition`,
`ContentInitialCombatReadiness`, `ContentInitialPlacementCombatFactsV2` and `ContentRetreatSupplyAnchor`.
Constructors use the fields below in listed order (JSON camelCase maps to C# PascalCase); an element
fact groups `elementId, combatClassificationId, components, combatOrigin`; placement facts group
`scenarioId, elementId, initialComponentToes, initialAmmunition, initialReadiness`. Formation morale
groups `formationId, basicMorale, basicMoraleOrigin`. Existing `ContentInitialComponentToe`, IDs,
`ContentOrigin` and reference semantics are reused. Do not add fields to old serialized types.
Collections are defensively copied, sorted and compared structurally; duplicate keys reject before
construction of an artifact. This is a new version of existing facts, not parallel assault totals.

Reuse Rules tokens from [Cna1979Combat](../../src/Cna.Core/Rules/Cna1979Combat.cs): element
`land.combat-classification.combat-unit`, component `land.combat-component.infantry`. The logical
`fixture.*` tokens in RSH-002 are superseded; organization alone never implies combat classification.
Reuse `land.organization.battalion` and `land.mobility.non-motorized`. Source-parent Morale belongs
once to the root formation. Current parent must equal that source parent at creation and admission;
attachment/detachment remains unsupported. TASK-003 binds it in authority without a second Morale copy.

Content contains immutable **initial scenario seeds**, not live balances. Existing per-component
`initialComponentToes` is the sole initial TOE source. One element-level ammo balance uses full-game
Ammo Points; the singleton component does not carry a second ammo balance. Readiness is an explicit
stage-scoped synthetic input, not perpetual supply. Water/stores distribution processes are not
implemented by reading the seed. TASK-003 must copy exact seeds/provenance once into authoritative
creation, reconcile its existing CP/Cohesion ledger (initial expenditure/DP/RP all0), pin initial
Reserve none, and reject scope/parent disagreement. No later repeat, restore or phase entry reseeds.

## Exact JSON shape

All properties below are mandatory, in listed canonical order; no others allowed. Objects referenced
by name expand recursively. Arrays have the keys/order in the final column. Integer means signed
32-bit mathematical integer, never bool, floating representation or exponent. JSON primitive type
mismatch is001; range failure is003. `id` is1–128 characters, grammar
`[a-z0-9]+(?:[-.][a-z0-9]+)*`. No adjacent separators. `locator` is1–128 safe ASCII characters,
starts alphanumeric and continues `[A-Za-z0-9._:-]`, matching existing source-atom meaning.
Only ASCII identifiers/enums/locators appear; no free presentation text or escapes are needed.

| Object | Property order and structural types | Array key / structural bounds |
| --- | --- | --- |
| Root | `schemaVersion:int, formatId:id, packId:id, rulesetId:id, capabilities:id[], capabilityProfileId:id, sourceIndex:Source[], locations:Location[], weatherAreaAssignments:Weather[], edges:Edge[], formations:Formation[], elements:Element[], scenarios:Scenario[]` | Profile counts below; byte input≤65536, JSON depth≤32 |
| Source | `sourceId:id, kind:string` | `sourceId`; selected repository-synthetic source |
| Reference | `sourceId:id, locator:locator` | `(sourceId,locator)`; exactly1 per selected origin |
| Origin | `kind:string, references:Reference[]` | selected kind `synthetic` |
| Location | `locationId:id, kind:string, terrainId:id, sourceCoordinate:null, origin:Origin` | `locationId`; kind `hex`, terrain `land.terrain.clear` |
| Weather | `locationId:id, weatherArea:string, origin:Origin` | `locationId`; area `a` |
| Edge | `firstLocationId:id, secondLocationId:id, features:[], origin:Origin` | endpoints canonical first<second; sort endpoint pair |
| Formation | `formationId:id, sideId:id, parentFormationId:null, organizationId:id, basicMorale:int, basicMoraleOrigin:Origin, origin:Origin` | `formationId`; Basic Morale -3…3 structurally, selected0 |
| Component | `componentId:id, componentClassId:id, maximumToe:int, offensiveCloseAssaultRating:int, defensiveCloseAssaultRating:int, origin:Origin` | `componentId` unique pack-wide; maximum1…Int32.MaxValue, ratings0…Int32.MaxValue |
| Element | `elementId:id, sideId:id, parentFormationId:id, organizationId:id, mobilityId:id, baseCapabilityPointAllowance:int, placementMode:string, combatClassificationId:id, combatOrigin:Origin, components:Component[], breakdownVehicleCohort:null, origin:Origin` | `elementId`; CPA1…Int32.MaxValue; selected independent |
| Boundary | `gameTurn:int, operationStage:int` | turn1…111, stage1…3 |
| ToeSeed | `componentId:id, currentToe:int, origin:Origin` | `componentId`; current0…Int32.MaxValue, then≤matching maximum |
| AmmoSeed | `points:int, origin:Origin` | points0…Int32.MaxValue |
| ReadinessSeed | `gameTurn:int, operationStage:int, waterStatus:string, storesStatus:string, pinned:bool, origin:Origin` | boundary ranges; selected `distributed-for-stage` for both statuses, pinned false |
| Placement | `elementId:id, locationId:id, initialComponentToes:ToeSeed[], initialAmmunition:AmmoSeed, initialReadiness:ReadinessSeed, origin:Origin` | `(elementId,locationId)`; each element exactly once |
| RetreatAnchor | `sideId:id, locationId:id, kind:string, origin:Origin` | `sideId`; kind `friendly-supply-direction` |
| Scenario | `scenarioId:id, start:Boundary, end:Boundary, initialPlacements:Placement[], retreatSupplyAnchors:RetreatAnchor[], origin:Origin` | `scenarioId`; one scenario, end equals start for this stage-bounded fixture |

Typed serializers sort semantic collections; readers require byte-identical canonical input.
Canonical bytes: UTF-8 without BOM, minified JSON, listed object-property order, ordinal array key
order, normal decimal integer spelling (0, never -0), lowercase booleans/null, no trailing newline.
Comments, trailing commas, duplicate/missing/extra fields, invalid UTF-8, escaped alternative
spellings, reordered objects/arrays and whitespace reject. Serialization must validate first;
hash-changing edits cannot silently become different valid inputs through defaults or repair.

## Closed profile and provenance

Validation order: JSON/shape (including the empty-feature constraint), identity, primitive bounds,
references/provenance, selected profile, then canonical comparison. The oracle below fixes check
order within each phase; vectors pin first failure for isolated mutations. Paths use JSON Pointer
with zero-based array indices. JSON decoder/byte failures use the empty root pointer. A reversed
edge endpoint pair is an invalid representation (004); reordered collections are noncanonical (008). Messages are trusted
diagnostics only; TASK-004 maps outward errors without leaking enemy data.

| Code | Failure |
| --- | --- |
| `CMB-CNT-001` | JSON, unknown/duplicate/missing property, wrong primitive/container type, depth/byte bound |
| `CMB-CNT-002` | Wrong schema/format/ruleset/profile or missing/extra/duplicate capability |
| `CMB-CNT-003` | ID/locator grammar or integer range |
| `CMB-CNT-004` | Unsupported static class/rating/size/terrain, extra category/quantity, malformed selected geometry |
| `CMB-CNT-005` | Duplicate/missing/foreign object identity, component ownership, side/parent/placement/anchor reference |
| `CMB-CNT-006` | Missing/unknown/wrong source provenance or source-kind mismatch (missing object property is001) |
| `CMB-CNT-007` | Unsupported/inconsistent initial TOE, ammo, readiness or scenario scopes |
| `CMB-CNT-008` | Otherwise-valid JSON is not byte-identical canonical form |

Exact profile: one `sandtable-rules-lab` source entry of kind `repository-synthetic`; every origin
has kind `synthetic` and one reference to that source, with a nonempty datum locator. A group origin
may cover both component ratings/maxima; formation Morale, ammo, TOE and readiness have their own
explicit origins. No fallback to enclosing-object origin. All immutable facts in the pack validate,
including elements not selected by a caller; a scenario must place the complete inventory.

There are exactly two root battalion formations and two independent battalion elements, one per
`axis`/`commonwealth` side, one same-side element per formation, one infantry component per element.
Maximum/current TOE10, offensive/defensive ratings1/1, source Morale0, base CPA10, initial Ammo10.
No vehicles/cohorts, HQ, additional force, attachment, gun, armor, fuel or cargo category. Extra or
missing inventory rejects. Structurally valid zero ratings or smaller initial TOE are unsupported,
not inferred defaults. CP/Cohesion/Reserve creation checks and current dynamic certification are
TASK-003/007/009; Content seeds do not bypass them or prove any public observation is safe.

Six Clear hexes form a connected unbranched line, five featureless edges, all in Weather area `a`.
Each side has a distinct endpoint retreat-supply anchor, two edges from its initial element;
the two initial elements occupy adjacent central hexes. Anchors declare the direction toward that
side's nearest friendly supply dump for this synthetic exercise, as required by DES-005. They are
static direction facts, not stock, resupply commands or an implementation of dump ownership/lifecycle.
No accessible non-carried ammo is admitted; an anchor never provides ammunition or proves delivery.
A future live Logistics/facility profile needs separate facts/contracts. Adding these explicit
anchors fills the earlier two-hex research fixture's geometry handoff; it does not add combatants.

In either attack orientation, initial defender has a unique Clear retreat edge toward its anchor,
farther from attacker. Prisoner relocation from either victim's pre-retreat location to the
captor's post-retreat location is≤2 edges (≤3 allowed), with no enemy in entered hexes; escapees'
original unit is≤1 edge away (≤8 CP). The verifier checks both captor roles and retreat/refusal.
These are **initial geometry checks**. TASK-009 certifies every potential result against current
state before selection, TASK-014/015 execute settlement, and TASK-004/020 prove public disclosure.
This file cannot certify legality after arbitrary movement or hidden-state changes. Normal Weather,
stage-scoped readiness and initial CP/Cohesion must be reconciled by the Setup/authority contract;
no weather result, artificial RNG seed or prior Logistics event is fabricated here.

## Evidence and delivery handoff

The [canonical fixture](fixtures/combat-content-v7.canonical.json) has pack ID
`rules-lab.content.close-assault.v1`, scenario `close-assault-positive-v1`, turn1/stage1.
Its line is `axis-supply`, `axis-rear`, `assault-west`, `assault-east`, `commonwealth-rear`,
`commonwealth-supply`; IDs are sorted in arrays, not used as geographic coordinates. The original
RSH-002 element/parent/component IDs are retained. Origins name each synthetic fact group.
The [vector manifest](fixtures/combat-content-v7.vectors.json) pins byte count/hash and one-edit
negative cases. [Verifier](verify-combat-content-v7.py) performs strict parsing, typed profile/reference
checks, canonical reconstruction, hash comparison, negative mutations and initial geometry probes.
It is a contract oracle, not the future C# implementation or independent engineering review.

| Requirement / policy | TASK-002 evidence | Later obligation |
| --- | --- | --- |
| POL-001/002, CNT-DEC-001…007/011/012; ID-AC-002/008 | Reused class/component IDs; explicit Morale/max/rating/seeds; complete synthetic pack; all mutation cases | TASK-006 C# constructor/validator/canonical tests; TASK-009 participant admission |
| POL-003/005; SET-AC-001/004/005 | Explicit Ammo/readiness scope/origin and initial two-role retreat/custody geometry | TASK-003 creation bindings/ledger/phase gates, TASK-014/015 real settlement and obligations |
| POL-006; ID-AC-011 | Content/diagnostics remain trusted; no outward fields added | TASK-004/020 equal-authorized-history privacy tests |
| POL-007/008 | Seeds never reseed; original unit inventory and side binding retained | TASK-017…019 Reserve and cycle history/witness enforcement |
| Source amendment CMB-SRC-RUL-001; RES-AC-001…012 | Required Rules/config binding, zero table data in Content | TASK-003/005 canonical Rules bundle and all source coordinates; TASK-004 full AC index |

TASK-006 must reproduce the exact golden bytes/hash, preserve all predecessor fixture hashes, reject
these vectors through real C# parsing and test defensive copying, equality, shuffled-construction
canonical stability and one-fact hash changes. This packet does not allocate Setup/World/event/
snapshot/observation/protobuf versions or pass combined checkpoint B. TASK-003 authority freeze is
next after checkpoint A review; no runtime consumer starts solely from this packet's completion.

## TASK-002 verification

`python3 docs/specs/verify-combat-content-v7.py` passes: 10,339 canonical bytes,
SHA-256 `ee4fde9638ceb61ec08612fe32f9ac81db05572aac5ca20941e402d2fed25847`,
70 single-edit rejection vectors, eight custody/escape probes covering both attack orientations
and retreat/refusal, shuffled-construction stability and a provenance-only hash change.
The fixture has no final newline. The manifest pins the literal hash; the verifier never regenerates
expected bytes or rewrites evidence. Existing Content fixture files and runtime source are unchanged.
These checks establish this contract packet only; production parser, restore and simulator gates
remain assigned to later tasks.
