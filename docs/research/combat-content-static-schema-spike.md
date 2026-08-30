# Combat Content and Static Schema Spike

**Status:** Decision-ready `CMB-RSH-002` research; project-owner approval and all production
contracts remain pending

**Date:** 2026-08-29

**Decision owner:** Project owner

**Research work item:** `CMB-RSH-002`

**Parent research:** [Combat-cycle source inventory](combat-cycle-source-inventory.md) and
[Combat rules/result surface](combat-rules-result-surface-spike.md)

## Executive conclusion

The first Close Assault path should add one bounded logical Content capability: immutable,
origin-bearing combat classification and TOE-component source facts joined to existing elements by
stable IDs. For the selected path, each participating element has one infantry component with a
maximum of 10 TOE Strength Points and explicit offensive and defensive Close Assault ratings of
`1`. Each source parent formation has Basic Morale `0`. Existing battalion organization, Clear
terrain, non-motorized mobility, parent assignment, side, and base Capability Point Allowance facts
remain the other Content inputs.

This is a logical research schema, not an authorized C# or JSON contract. It deliberately does not
choose a Content Pack schema version, canonical property names, constructor shape, migration, or
production capability token.

Content must not store current TOE strength, ammunition, Cohesion, pin, Reserve, Contact/Engaged,
force assignment, Raw or Actual Assault Points, differentials, modifiers, table coordinates,
results, or `exertsZoc`. Rules owns classification meanings, calculations, tables, and supported
input predicates. Campaign owns current strength and readiness. The Umpire joins those authorities
and derives the combat result.

The smallest positive evidence fixture is two adjacent independent infantry battalions, one per
side, in Clear terrain. Each has one 10-point infantry component rated `1/1`, source-parent Basic
Morale `0`, base CPA `10`, and provisional current strength `10`. Negative evidence is expressed as
one-field mutations of that fixture so each validation boundary fails independently. The current
strength seed, ammunition representation, attachment-sensitive Morale selection, and exact combat
participant identity remain explicit handoffs to `CMB-RSH-003`, the approved ZOC/Reaction design,
and `CMB-DES-001` through `005`.

No published unit row, source chart, scan, artwork, or complete table is retained here.

## Decision question and stop condition

This packet asks:

> Which immutable combat facts and bounded synthetic values belong in Content for the first
> source-faithful Close Assault path, and which apparent inputs must remain Rules derivations or
> mutable Campaign state?

In scope:

- logical identity and ownership of the minimum static combat facts;
- source semantics, value provenance, units, and validation boundaries;
- exact synthetic positive values and independent negative mutations;
- Content/Rules/Campaign separation;
- provisional values that depend on ZOC/Reaction or mutable-state research; and
- later design and exact-test/golden-vector consequences.

Out of scope:

- any production type, serializer, schema/canonical-format version, manifest version, or migration;
- source-derived historical unit data or reproduction of unit-characteristic/OA sheets;
- a combat rules artifact, table coordinate matrix, resolver, command, event, snapshot,
  observation, legal action, replay path, or RNG stream;
- current TOE, ammunition, loss, prisoner, captured-equipment, Disorganization, Contact, Engaged,
  retreat, attachment, or combat-opportunity contracts; and
- Barrage, Anti-Armor, armor, guns, trucks, air, fortifications, minefields, pinned/zero-rated
  defenders, Probes, Overrun, Combined Arms, or published scenarios.

The stop condition is this reviewed decision packet. It does not authorize implementation.

## Method, precedence, and source index

The official Land rules and common charts are image scans. Temporary copies were rendered and
visually inspected outside Git at the cited PDF pages. The errata text was also checked. Only
normalized semantics and locators are retained.

| Source | Exact locator | Use in this decision |
| --- | --- | --- |
| [Original 1979 Land Game rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) | PDF pages 7-10; 3.5, 4.22, 4.25, 4.41-4.46 | TOE-component abilities, unit type, maximum TOE, source parent assignment, unit-characteristic identity, and provenance boundary |
| [Original 1979 Land Game rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) | PDF pages 18-19; 10.11-10.16, 11.0-11.38 | Combat-unit/ZOC qualification, seven combat characteristics, two Close Assault ratings, and Raw-to-Actual calculation |
| [Original 1979 Land Game rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) | PDF pages 22-25; 15.0-15.89 | Participation, ammunition exclusion, TOE assignment, offensive/defensive use, modifiers, results, and loss application |
| [Original 1979 Land Game rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) | PDF page 27; 17.0-17.28 | Basic Morale source value, parent-formation application, Cohesion adjustment, and adjusted range |
| [Charts common to both players](https://spigames.net/PDFv10/CNA_ChartsBothPlayers.pdf) | PDF pages 1 and 4; 11.4 and 17.4 | Cross-check of combat-strength calculation and Morale row semantics; no chart matrix retained here |
| [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf) | 3.4, 4.46, 10.3, 11.32, 15.25-15.27, 15.53, 15.56, 15.79, 15.88 | Parenthesized capability, corrected calculation notation, force assignment, dice reuse, size, pinned, result, and surrender boundaries |
| [Content Pack v1](../specs/content-pack-v1.md) | `CNT-001`-`CNT-016`; invariants and validation | Independent Content identity, per-datum origin, closed Rules vocabulary, canonical hashing, and separation from runtime/observations |
| [CONTACT-001 source/ruling lock](contact-reaction-zoc-source-ruling-lock.md) | `ZOR-DEC-005`; “Positive-ZOC vocabulary and first fixture” | Content primitive classification/Close Assault facts, Rules predicate, mutable capability, and rejection of a Content `exertsZoc` flag |
| [`ContentCombatElement`](../../src/Cna.Core/Content/ContentForces.cs) and current Content tests | Current read-only contracts and validators | Existing element/formation identity, organization, side, parent, mobility, CPA, origin, capability, validation, and clean-cut patterns |
| [`CampaignElementOperationalState`](../../src/Cna.Core/Campaigns/CampaignElementOperationalState.cs) and [`CampaignElementState`](../../src/Cna.Core/Campaigns/CampaignElementState.cs) | Current read-only Campaign state | Existing mutable location, Reserve, CP/Cohesion, and Breakdown boundary; absence of current combat strength/readiness |

Source semantics do not establish the selected numerical fixture as historical. Every fixture value
below is repository-synthetic and must use `sandtable-rules-lab` provenance. A future historical
pack would need a precise official component/OA locator for each source-derived value; the general
rules section that defines a field is not sufficient provenance for a published unit's number.

## Authority split

| Owner | Owns | Must not own |
| --- | --- | --- |
| Content | Immutable source classification, component identity, maximum TOE, component Close Assault ratings, source-parent Basic Morale, existing organization/terrain/CPA facts, and per-datum origin | Current TOE/readiness, derived strength, tables, results, ZOC, choices, or hidden observations |
| Rules | Closed classification/component vocabulary and meaning; eligibility predicates; rating multiplication/rounding; Morale, modifier, result, loss, capture, and retreat tables/algorithms; supported-subset admission | Historical/synthetic unit values, current state, or player choice |
| Campaign | Current TOE per stable component; current parent/attachment, location, CPA expenditure, Cohesion, Reserve, pin, ammunition/fuel, opportunity, choices, losses, and relationship state | Source maxima/ratings or table algorithms |
| Umpire | Exact join of Content + Rules + Campaign; legal participants; Raw/Actual Points; differentials; state mutation; events; replay; redacted projection | Defaults inferred from missing data or untrusted Intelligence proposals |

This preserves the repository rule: a Content Pack is immutable static source data, Campaign World
is mutable truth joined through stable Content IDs, and Rules supplies normalized meaning.

## Proposed logical static fact inventory

Property and type names below are descriptive logical names only. A later approved specification
may rename or group them, but it must preserve their identity, units, ownership, and validation
semantics.

| Logical fact | Type and unit | Stable identity | Authority owner | Value provenance and semantic locator | Validation boundary | First consumers | Disposition |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Side ownership | Existing closed side ID; no numeric unit | Existing `elementId` plus side slot | Content stores the reference; Rules/Umpire interpret audience and opposition | Exact synthetic element locator; current Content semantics | Known side; owning formation exists on the same side; selected fixture uses one element per opposing side | Participant opposition, audience/redaction, friendly-fire rejection | **Accept unchanged** |
| Source-parent assignment | Existing stable formation ID reference; no numeric unit | Existing `elementId` plus source-parent slot | Content | Exact synthetic element locator; semantics: Land 4.25 and 17.1 | Referenced formation exists on the same side; parent graph is acyclic; selected element has exactly one source parent | Source Basic-Morale lookup and later current-parent comparison | **Accept unchanged** |
| Placement mode | Existing closed placement-mode ID; no numeric unit | Existing `elementId` plus placement-mode slot | Content | Exact synthetic element locator; semantics: Land 4.25 | Known mode; selected elements are `Independent` and therefore may be placed directly | Scenario admission and initial placement only; not combat opportunity | **Accept unchanged** |
| Mobility class | Existing closed mobility ID; no numeric unit | Existing `elementId` plus mobility slot | Content stores the reference; Rules owns movement/logistics meaning | Exact synthetic element locator; current synthetic semantics and Land 6.17, 8.0-8.19 | Known Rules vocabulary; selected fixture uses the existing non-motorized value | Selected-subset admission and exclusion of fuel/breakdown behavior | **Accept unchanged** |
| Element combat classification | Closed Rules-owned stable ID; no numeric unit | Existing `elementId` plus the classification slot | Content stores the reference; Rules owns its meaning | Synthetic datum locator per element; semantics: Land 3.5, 4.22, 10.11-10.15 | Stable ID; known Rules vocabulary; selected capability admits only ordinary combat infantry, not HQ/marker/gun/armor categories | ZOC qualification, Close Assault eligibility, unsupported-category rejection | **Accept** |
| TOE component identity | Pack-stable ID | `componentId`, unique within its owning element and unambiguous pack-wide | Content | Synthetic datum locator per component; component granularity: Land 3.5, 4.46, 11.1-11.3 | Stable ID, nonempty canonical collection, no duplicate, one owning element; selected path requires exactly one component | Campaign current-strength key, assignment, loss allocation, replay joins | **Accept** |
| TOE component class | Closed Rules-owned stable ID; no numeric unit | `componentId` plus component-class slot | Content stores the reference; Rules owns its meaning | Synthetic datum locator; semantics: Land 3.5, 4.22, 4.46 | Known Rules vocabulary; selected path admits only infantry; unknown or later category fails admission | Rating interpretation, loss typing, broader-combat gate | **Accept** |
| Maximum TOE strength | Positive integer TOE Strength Points | `componentId` plus maximum-strength slot | Content | Synthetic datum locator; semantics/units: Land 3.5 and 4.46 | Greater than zero; checked arithmetic; current/assigned strength may never exceed it; selected value exactly `10` | Campaign initialization/bounds, Raw Point calculation, loss validation | **Accept** |
| Offensive Close Assault rating | Nonnegative integer Raw Close Assault Points per committed TOE Strength Point | `componentId` plus offensive-rating slot | Content | Synthetic datum locator; semantics: Land 3.5, 11.15, 11.3, 15.11-15.27 | Explicit value required when capability is declared; checked multiplication; selected path admits exactly `1`; zero and other values are representable source facts but unsupported by this first path | Attacking Raw/Actual Point calculation | **Accept** |
| Defensive Close Assault rating | Nonnegative integer Raw Close Assault Points per committed TOE Strength Point | `componentId` plus defensive-rating slot | Content | Synthetic datum locator; semantics: Land 3.5, 11.15, 11.3, 15.11-15.27 | Same structural validation as offensive rating; selected path admits exactly `1`; zero-rated special behavior remains unsupported | Defending Raw/Actual Point calculation and positive-ZOC raw-capability input | **Accept** |
| Source-parent Basic Morale | Integer Morale rating points in closed range `-3..+3` | Existing source `formationId` plus Basic-Morale slot | Content | Synthetic datum locator per formation; semantics: Land 3.5, 17.1-17.28 | Explicit and range-checked; selected value exactly `0`; current parent/attachment decides applicability outside Content | Morale calculation input after Campaign binding | **Accept** |
| Origin of each combat fact group | Existing `ContentOrigin` with one or more `RuleReference` values | The containing element/component/formation fact identity | Content | Selected values use `sandtable-rules-lab`; future source-derived values cite official component/OA locators | Existing source-kind match, nonempty/unique/canonical references, source index membership | Audit, hash review, rights boundary | **Accept** |
| Organization size | Existing closed organization ID | Existing `elementId` and `formationId` fields | Content stores the reference; Rules owns size meaning | Current synthetic battalion/regiment locators; Land 4.23 and 15.5 | Existing vocabulary/cross-reference rules; selected combat units both battalions | Organization-size differential | **Accept unchanged** |
| Base CPA | Existing positive integer Capability Points per Operation Stage | Existing `elementId` field | Content | Existing synthetic element locator; Land 3.5, 6.1, 11.2 | Existing positive validation; selected value `10`, enough for attacker `5` and defender `3` before current expenditure | Combat eligibility and later Campaign expenditure | **Accept unchanged** |
| Terrain/topology | Existing terrain ID and explicit edge | Existing location/edge IDs | Content stores facts; Rules owns effects | Existing synthetic location/edge locators; Land 8.37 and 15.3 | Existing topology/vocabulary checks; selected locations are adjacent Clear hexes with no admitted combat shift | Opportunity geometry and terrain modifier | **Accept unchanged** |

The component is the smallest safe join key. A flat element total would reproduce the selected
`10 x 1` calculation but could not later explain mixed ratings, force assignment, or loss
allocation without a breaking identity migration. Conversely, a general weapon/equipment graph is
not needed to prove this one homogeneous infantry path.

## Values deliberately excluded from Content

| Apparent fact | Correct owner | Decision and reason | Disposition |
| --- | --- | --- | --- |
| Current/committed TOE strength | Campaign | Changes with losses and player assignment; keyed by stable Content component | **Reject from Content** |
| Initial current TOE seed shape | Content scenario may declare a seed, but Campaign admission creates mutable truth | The value `10` is required for the fixture; exact declaration, event, and world handoff belongs with `CMB-RSH-003` | **Escalate** |
| Raw and Actual Close Assault Points | Rules + Umpire | Derived from current committed TOE and immutable rating using sourced rounding | **Reject from Content** |
| Basic/Final Assault Differential and all shifts | Rules + Umpire | Derived calculation and table selection, not unit source data | **Reject from Content** |
| Morale table, result rows, losses, retreat/capture/Engaged outcomes | Rules | Normalized Rules artifact with its own provenance/hash; source chart matrix must not be copied into Content | **Reject from Content** |
| Current Cohesion and Adjusted Morale | Campaign; Rules derives adjusted value | Cohesion is mutable; adjusted Morale exists only for one assault | **Reject from Content** |
| Ammunition/fuel availability or “ready” boolean | Campaign | Current readiness changes; missing data must not imply sufficient supply | **Reject from Content** |
| Generic `requiresAmmunition`, `requiresFuel`, or zero-valued unsupported rating bag | Rules semantics or later source profile | The selected infantry path does not establish a general logistics/equipment schema; optional zeros would make unsupported categories appear implemented | **Reject** |
| Pin, Reserve, gun position, fortification, minefield, attachment, prior combat | Campaign | All are current state or relationships; the selected fixture explicitly excludes them | **Reject from Content** |
| Contact, Engaged, combat opportunity, target, participants, sealed choices | Campaign/Umpire | State/decision identities depend on current ZOC, binding, and cycle state | **Reject from Content** |
| `exertsZoc` | Umpire-derived; observation may expose only approved apparent value | Topology- and state-dependent; explicitly rejected by `ZOR-DEC-005` | **Reject from Content** |
| Full Barrage/Anti-Armor/armor/AA/gun/truck/air fields | Later Content capability plus Rules meanings | Empty selected steps do not justify a nullable universal schema | **Escalate to later combat research** |

## Decision table

`Accept` recommends the choice for owner approval and later specification. `Reject` excludes the
alternative. `Escalate` leaves a named dependency open and blocks only the dependent contract,
not this research packet.

| ID | Choice | Disposition | Evidence | Limitation or handoff |
| --- | --- | --- | --- | --- |
| `CMB-CNT-DEC-001` | Model immutable combat strength at stable TOE-component granularity under an element | **Accept** | Land 3.5, 4.46, 11.3, 15.16 and 15.83 apply ratings, assignments, and losses to TOE points/components | Production grouping/type names and serialization remain design work |
| `CMB-CNT-DEC-002` | Store maximum TOE and offensive/defensive Close Assault ratings as explicit component source facts | **Accept** | Land 3.5, 11.15, 11.3; selected result-surface input is `10`, `1`, `1` | Other combat ratings and mixed components are not admitted |
| `CMB-CNT-DEC-003` | Reference an explicit Rules-owned combat classification and component class rather than infer combat ability from CPA/organization | **Accept** | Land 4.22, 10.11-10.15; `ZOR-DEC-005` rejects CPA/organization proxies | Exact vocabulary token/migration must reconcile with ZOC/Reaction design |
| `CMB-CNT-DEC-004` | Store Basic Morale as a source-parent formation fact and resolve the applicable value against current parent/attachment state | **Accept** | Land 17.1 makes a battalion/brigade use its assigned parent formation's rating subject to current attachment/out-on-own cases | Current attachment model and applicable-parent algorithm remain later work |
| `CMB-CNT-DEC-005` | Reuse existing battalion organization, Clear topology, element parent/side, non-motorized mobility, and base CPA facts | **Accept** | Current Content already owns and validates them; result-surface research requires equal battalion size, Clear, and sufficient CPA | Does not authorize current placement/opportunity or combat expenditure |
| `CMB-CNT-DEC-006` | Gate the combat facts behind one closed Content capability and fail admission when the fact set is partial or an unsupported category appears | **Accept** | `CNT-010` and current Breakdown-cohort capability tests use exact co-declaration/fail-closed semantics | Capability token and schema version are intentionally not frozen here |
| `CMB-CNT-DEC-007` | Give every synthetic combat fact group a unique `sandtable-rules-lab` locator and keep historical value provenance absent | **Accept** | `CNT-003`, `CNT-004`, `CNT-014`; actual numbers are invented for evidence | Future published values need official component/OA locators, not these rules locators |
| `CMB-CNT-DEC-008` | Put result tables, coordinate rows, modifiers, Raw/Actual Points, and differentials in Content | **Reject** | Land 11.3, 15.3-15.8, 17.2 define algorithms/tables; repository architecture assigns table meanings to Rules | `CMB-DES-004` later selects an executable Rules artifact |
| `CMB-CNT-DEC-009` | Add a flat element total or general nullable combat-stat bag | **Reject** | A flat total loses component/loss identity; nullable zeros blur unsupported categories and cannot prove source completeness | The selected capability requires explicit component/rating facts only |
| `CMB-CNT-DEC-010` | Store current strength, readiness, attachment, choices, relationships, or `exertsZoc` in Content | **Reject** | They change during play; `ZOR-DEC-005` assigns them to Campaign/Umpire and rejects Content ZOC | `CMB-RSH-003` and later designs own exact mutable fields |
| `CMB-CNT-DEC-011` | Seed provisional current TOE `10` and sufficient ammunition for both selected elements | **Escalate** | Those exact inputs are required by `CMB-RSH-001`, but the current world has no corresponding fields | `CMB-RSH-003` must choose scenario seed, current state, event/snapshot, and ammunition form |
| `CMB-CNT-DEC-012` | Freeze the classification token/property and Basic-Morale application before ZOC/Reaction and attachment identity are reconciled | **Escalate** | `CONTACT-001` fixes authority but intentionally defers field/type names; Rule 17.1 depends on current assignment | Reconcile in approved ZOC/Reaction specification and `CMB-DES-001`; no production freeze now |

## Minimal synthetic evidence fixtures

### Positive fixture `close-assault-positive-v1`

The logical fixture can later be carried by a dedicated pack or a new scenario in the existing
rules laboratory. This packet fixes its semantic values and evidence identities but not that
packaging choice. The IDs below are exact logical test-vector keys. They do not select production
property names, stable-ID syntax, serializer paths, pack IDs, or a schema version.

| Evidence object | Exact logical ID | Exact `sandtable-rules-lab` locator |
| --- | --- | --- |
| Axis source parent | `axis-assault-formation` | `combat.close-assault-positive.v1:formation.axis-assault-formation` |
| Commonwealth source parent | `commonwealth-assault-formation` | `combat.close-assault-positive.v1:formation.commonwealth-assault-formation` |
| Axis element | `axis-assault-battalion` | `combat.close-assault-positive.v1:element.axis-assault-battalion` |
| Commonwealth element | `commonwealth-assault-battalion` | `combat.close-assault-positive.v1:element.commonwealth-assault-battalion` |
| Axis infantry component | `axis-assault-battalion.toe.infantry` | `combat.close-assault-positive.v1:component.axis-assault-battalion.toe.infantry` |
| Commonwealth infantry component | `commonwealth-assault-battalion.toe.infantry` | `combat.close-assault-positive.v1:component.commonwealth-assault-battalion.toe.infantry` |
| West location | `assault-west` | `combat.close-assault-positive.v1:location.assault-west` |
| East location | `assault-east` | `combat.close-assault-positive.v1:location.assault-east` |
| Adjacency edge | `assault-west|assault-east` | `combat.close-assault-positive.v1:edge.assault-west-assault-east` |
| Axis morale fact | `axis-assault-formation.basic-morale` | `combat.close-assault-positive.v1:morale.axis-assault-formation` |
| Commonwealth morale fact | `commonwealth-assault-formation.basic-morale` | `combat.close-assault-positive.v1:morale.commonwealth-assault-formation` |

| Layer | Exact synthetic input | Provenance or status |
| --- | --- | --- |
| Topology | `assault-west` and `assault-east` joined by `assault-west|assault-east`; both are `land.terrain.clear`; no fortification, minefield, or combat-modifying hexside | Existing Content semantics; exact synthetic locators above |
| Forces | `axis-assault-battalion` at `assault-west` and `commonwealth-assault-battalion` at `assault-east`; each is an independent, non-Reserve ordinary combat infantry battalion; no other participant, attachment, gun, armor, truck, air, or special unit | Synthetic; side, source-parent, placement, mobility, organization, and classification are explicit facts |
| Organization | Both element organization IDs are `land.organization.battalion`; each retains one source parent formation | Existing Rules vocabulary and synthetic element facts |
| Capability | Base CPA `10` each; current expenditure `0`; attacker later spends `5`, defender `3` | Static base values accepted; expenditure is provisional Campaign input |
| Component | Exactly `axis-assault-battalion.toe.infantry` and `commonwealth-assault-battalion.toe.infantry`, one under each owning element; component class ordinary infantry | New logical Content facts; exact synthetic locators above |
| Static strength | Maximum TOE `10`; offensive rating `1`; defensive rating `1` for each component | New logical Content facts; repository-synthetic, not historical |
| Morale | `axis-assault-formation.basic-morale` and `commonwealth-assault-formation.basic-morale` are both `0` | New logical Content facts; repository-synthetic |
| Provisional current state | Current component TOE `10`, Cohesion `0`, unpinned, ammunition sufficient, no special state, current parent equals source parent | Required evidence input; exact contracts deferred to `CMB-RSH-003`/design |
| Fixed choices | Defender declines Retreat Before Assault; both sides commit all 10 eligible TOE to full Close Assault | Evidence input only; exact legal actions deferred |
| Rules consequence | 10 Raw and 1 Actual Point per side; Basic Differential `0`; Morale can reach Final Differential `-2..+2` | Source-locked by `CMB-RSH-001`; not Content |

Every fact group uses the exact locator of its containing object above and must remain under the
existing `sandtable-rules-lab` source index. The colon is compatible with the current source-atom
grammar; the production serializer path remains unselected.

### Independent negative mutations

Each negative starts from `close-assault-positive-v1` and changes only the stated fact. This avoids
one invalid fixture masking another issue.

| Negative ID | Single mutation | Required failure boundary |
| --- | --- | --- |
| `close-assault-negative-duplicate-component` | Change the Commonwealth component ID to `axis-assault-battalion.toe.infantry` | Content validation at component identity: duplicate pack-wide identity; no hash/admission |
| `close-assault-negative-unknown-classification` | Change only `axis-assault-battalion`'s combat classification to test sentinel `unknown.combat-classification` | Rules compatibility validation at element classification: unknown vocabulary; no hash/admission |
| `close-assault-negative-unknown-component-class` | Change only `axis-assault-battalion.toe.infantry`'s component class to test sentinel `unknown.component-class` | Rules compatibility validation at component class: unknown vocabulary; no hash/admission |
| `close-assault-negative-zero-maximum` | Change only `axis-assault-battalion.toe.infantry` maximum TOE from `10` to `0` | Local Content construction/parse rejection at maximum TOE; no semantic fallback |
| `close-assault-negative-morale-range` | Change only `axis-assault-formation.basic-morale` from `0` to `+4` | Local Content rejection at Basic Morale because it is outside `-3..+3` |
| `close-assault-negative-origin-kind` | Change only the Axis component origin kind from repository-synthetic to source-derived while retaining its `sandtable-rules-lab` source | Content origin validation at source-kind match; no canonical artifact |
| `close-assault-negative-missing-origin` | Remove only the Axis component fact-group origin | Content capability validation at mandatory origin; no implicit inherited provenance |
| `close-assault-negative-partial-capability` | Remove only the defensive rating from `axis-assault-battalion.toe.infantry` while retaining the capability | Content capability validation at defensive rating; no implicit zero/default |
| `close-assault-negative-zero-rating` | Change only the Axis component offensive rating from `1` to `0` | Structurally representable source fact, but first-path Rules admission rejects before combat mutation |
| `close-assault-negative-second-component` | Add only a second valid ordinary-infantry component `axis-assault-battalion.toe.infantry-support` with maximum `1`, ratings `1/1`, and origin locator `combat.close-assault-negative-second-component:component.axis-assault-battalion.toe.infantry-support` | First-path Rules admission rejects the second component; it does not ignore it |
| `close-assault-negative-non-infantry-component` | Change only the Axis component class from ordinary infantry to a known non-infantry class | First-path Rules admission rejects the unsupported class before combat mutation |
| `close-assault-negative-current-over-maximum` | Change only provisional Axis current TOE from `10` to `11` against maximum `10` | Campaign admission/snapshot validation rejects before opportunity creation |
| `close-assault-negative-ammunition` | Change only the Axis ammunition condition from sufficient to out of ammunition | Campaign/Rules eligibility rejects before choices or combat mutation; missing state is not “sufficient” |

The zero-rating, second-component, non-infantry, current-over-maximum, and ammunition cases are not
malformed Content. They prove that structurally valid but unsupported or currently ineligible
authority does not silently enter the selected resolver. The reserved unknown IDs are test-only
sentinels; choosing the production classification tokens remains `CMB-CNT-DEC-012`.

## Validation and canonical-identity consequences

A later Content implementation must provide these exact evidence classes:

1. **Local value tests:** stable IDs, defensive copies, structural equality, nonempty component
   collections, positive maximum TOE, explicit nonnegative ratings, Basic Morale `-3..+3`, and
   mandatory origins.
2. **Pack validation tests:** component uniqueness/ownership, capability co-declaration, source
   membership/kind, formation reference, and complete selected fact group.
3. **Rules compatibility tests:** known classification/component IDs, selected ordinary-infantry
   support, and explicit rejection of all other categories for the first path.
4. **Canonical tests:** collection insertion order cannot change bytes/hash; changing component ID,
   class, maximum TOE, either rating, Basic Morale, or origin must change bytes/hash; missing,
   duplicate, unknown, or reordered valid properties follow the current strict-reader pattern.
5. **Rights tests:** fixture origins are all repository-synthetic; canonical bytes contain no
   historical names, scan text, chart layout, artwork, or published unit rows.
6. **Boundary tests:** current strength joins by exact component ID and cannot exceed maximum;
   observations/action candidates expose no opposing exact component IDs, maximum/current TOE,
   ratings, Basic Morale, or origin.

No existing schema/hash is changed by this research. The later implementation must clean-cut the
Content identity only after the approved specification selects a contract.

## Handoffs to later work and exact planned evidence

| Later work | Required handoff from this packet | Exact tests or golden vectors later required |
| --- | --- | --- |
| Approved ZOC/Reaction specification and proposed `ZOR-TASK-002` | Explicit combat classification plus current raw defensive capability must derive from component facts; never a Content boolean | Two same-side battalion-equivalents with aggregate stacking `2` and current raw defensive Close Assault at least `10` produce positive ZOC; the named category/Cohesion/topology negatives from `CONTACT-001` each fail independently |
| `CMB-RSH-003` | Stable component IDs and maximum TOE bounds; provisional current TOE `10`; ammunition sufficient; current source-parent binding | World/event/snapshot round trip of current strength keyed by component; `11 > 10`, missing component, missing ammunition, stale parent, loss underflow, and unsupported mutable state reject atomically |
| `CMB-RSH-004` | Exact static input vector `10`, `1/1`, Basic Morale `0`; no RNG values or draw order chosen here | Golden seed/draw order must reproduce both Morale pairs, both distinguishable Close Assault pairs, any capture die, exact events/state, and cursor continuity |
| `CMB-DES-001` | Opportunity/participant identity must bind exact elements and component IDs against current state, without copying Content payloads into actions | Unknown/duplicate/cross-side/stale component, changed attachment, changed current TOE, and changed content hash all reject before choices |
| `CMB-DES-002` | Sealed submissions may reference stable opportunity/participant/assignment identities; Content facts remain server-resident | Wrong base hash/content/rules identity, duplicate envelope, wrong audience, and changed participant state emit zero events and reveal no hidden stat |
| `CMB-DES-003` | Force assignment allocates current TOE by component; selected fixture assigns all `10`; source maximum is only a bound | Exact all-strength assignment succeeds; negative, fractional, duplicate, over-current, unknown-component, withheld/mixed unsupported, and out-of-ammunition assignments reject before mutation |
| `CMB-DES-004` | Rules multiplies `rating x committed current TOE`; Content supplies only operands | `10 x 1 = 10 Raw`, total then `/10 = 1 Actual`, equal sides give Basic Differential `0`; exhaustive 36-coordinate Morale row and five `-2..+2` result columns remain Rules golden data, never Content |
| `CMB-DES-005` | Loss allocation preserves component identity and uses current TOE/rating, not source maximum | Zero attacker/nonzero defender loss and safe one-hex Retreat golden; differential `+2` two-hex pending Retreat; capture/loss rounding; no negative current strength; replay and side-redaction equivalence |
| Content implementation package | Logical fact/validation/provenance decisions only | Contract/validator/canonical/hash/golden JSON tests listed above, plus full repository format/build/test gate and source-material search |

The later `CMB-RSH-004` golden vector must not use a favorable seed to narrow the Rules surface.
All 36 ordered coordinates in every admitted column remain mandatory as specified by
`CMB-RSH-001`.

## Provisional values and unresolved owner choices

These points remain deliberately open:

1. **Owner approval of `CMB-CNT-DEC-001` through `012`.** This packet is decision-ready but cannot
   convert recommendations into production authority.
2. **Current-strength initialization.** The fixture requires current TOE `10`; `CMB-RSH-003` must
   decide whether a scenario declares component seeds and how creation events/world state retain
   them.
3. **Ammunition.** “Sufficient” is only a provisional eligibility condition. `CMB-RSH-003` must
   choose the smallest source-faithful mutable representation; absence must fail closed.
4. **Classification and attachment reconciliation.** The logical explicit classification is
   accepted here, but the vocabulary token/property, current parent/attachment identity, Morale
   selection, and ZOC consumer must reconcile with the approved ZOC/Reaction specification and
   `CMB-DES-001`.
5. **Fixture packaging.** The semantic fixture is fixed, but a dedicated synthetic pack versus a
   new scenario in the current rules laboratory is a later implementation-plan choice. It must not
   alter the values or provenance above.
6. **Broader static profiles.** Barrage, Vulnerability, Anti-Armor, Armor Protection, AA,
   parenthesized/zero-rated cases, equipment detail, published unit values, and general logistics
   profiles remain outside the first Content capability.

Until those dependencies close, no production contract may treat provisional values as defaults.

## Rejected approaches

| Approach | Reason |
| --- | --- |
| Copy the published unit-characteristics or OA tables | Rights-sensitive, unnecessary for the synthetic decision, and forbidden by the repository source-material boundary |
| Store only one precomputed element assault total | Loses current component/loss/assignment identity and changes when TOE changes |
| Store all seven combat ratings as nullable fields now | Makes empty Barrage/Anti-Armor steps appear implemented and invites missing-as-zero behavior |
| Put combat result rows in the Content Pack | Blurs independent Rules/Content identity and duplicates a protected table surface |
| Infer infantry/combat qualification from battalion organization, CPA, or positive rating | Contradicts `ZOR-DEC-005` and fails excluded unit/HQ cases |
| Store Basic Morale on every current participant | Duplicates a source-parent fact and ignores attachment-sensitive applicability |
| Treat maximum TOE as current TOE | Prevents loss/replacement state and breaks replay after the first casualty |
| Treat omitted ammunition as available | Converts absent authority into a favorable rules conclusion |
| Add `exertsZoc` to Content | Stores a current topology/state derivation as immutable source data |
| Version the production schema in this research PR | Freezes serializer/migration choices before mutable state and ZOC/Reaction consumers are designed |

## Confidence and limitations

Confidence is high in the Content/Rules/Campaign split, component granularity, rating units,
maximum/current distinction, Basic Morale range/source-parent relationship, and exact selected
synthetic values. Those conclusions were checked against the official rules, common charts, errata,
current Content contracts/validation tests, and the accepted positive-ZOC authority ruling.

Confidence is intentionally limited for current-strength initialization, ammunition, attachment,
combat-opportunity identity, broader unit categories, and production serialization. No source table
was double-entered and no executable table artifact exists. This document is research evidence,
not a proof that any combat coordinate can yet be adjudicated.
