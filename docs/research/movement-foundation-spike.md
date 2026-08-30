# Movement Foundation Spike

**Status:** Historical source lock; implementation is complete through `MOV-TASK-007`;
`MOV-TASK-008` is next

**Date:** 2026-08-25

**Decision owner:** Project owner

**Research work item:** `RSH-MOVE-001`

**Proposed capability:** `MOVE-001`

**Current delivery note:** Tasks 001-004 are merged. The later
[Breakdown continuity packet](breakdown-continuity-spike.md) records the owner's approval of minimum
BP rules/content/world continuity before Task 005, sequential-d6 coordinates, and the Table 21.38
Sandstorm BP-share basis. Task 004B implements that clean cut and has passed its repository and
independent-review gates. Task 005 has since delivered the side-safe observation prerequisite, and
Task 006 has frozen dormant Movement action/submission/receipt contracts without making Movement
public or executable. Task 007 has since delivered internal adjudication, canonical events, atomic
projection, and replay; Task 008 owns public membership and completion. This does not broaden the
Movement package into Breakdown, ZOC/Reaction, Contact/Engaged, or Combat adjudication authority.

## Executive conclusion

The next gameplay package should be a bounded Movement foundation, not a jump directly to the full
continual movement/contact/combat loop. The first vertical can authentically move the acting side's
non-Reserve rules-laboratory elements across the normalized terrain subset, charge Capability
Points without exceeding base CPA, enforce stacking, emit replay-complete movement ledger events,
and explicitly finish the Movement Segment at the existing Breakdown Determination boundary.

Player-facing Movement has one prerequisite: an apparent map-representation/contact contract. The
Umpire may adjudicate from complete opposing truth, but a legal-action list that omits a destination
because of an undisclosed enemy unit or Zone of Control would itself disclose hidden information.
The existing `OBS-001` own-elements-only policy is therefore insufficient for Movement. The
smallest safe sequence is:

```text
owner visibility ruling
        |
        v
initial map representation + apparent opposing presence
        |
        v
non-contact Movement + CP/cohesion/stacking ledger
        |
        v
ZOC entry, contact, reaction, and break-off
        |
        v
Breakdown + Combat Segment
```

The first Movement implementation should remain deliberately non-contact. It may generate and
accept only moves whose legality is fully explainable from the acting side's observation and the
approved apparent-presence policy. Entering an enemy ZOC, Reaction, Contact, Engaged, break-off,
Reserve Release, vehicle breakdown, combat, and later segment repetition remain separate slices.

This package also gives the Exercise Harness its first materially different engine trajectories.
The existing Reserve `none`/`one`/`all` matrix naturally creates two/one/zero movable elements, so
the simulator can measure real world mutation and ledger behavior instead of only preamble length.

## Decision question and scope

This spike asks:

> What is the smallest source-faithful, fog-safe, replayable Movement capability that can begin at
> the implemented first-side Movement checkpoint and end at a truthful existing sequence boundary?

In scope:

- Capability Point expenditure and Cohesion consequences needed by Movement;
- the rules-laboratory terrain, road/track, slope/ridge, stacking, and adjacency subset;
- initial authoritative map representation and minimum apparent opposing presence;
- non-contact, adjacent, one-element movement with repeatable accepted moves;
- explicit completion to Breakdown Determination;
- legal actions, commands, events, snapshot/replay, observation, and simulator evidence; and
- explicit unsupported behavior for every deferred rule category.

Out of scope:

- entering or leaving an enemy Zone of Control;
- Reaction, Contact, Engaged, break-off, and Holding Off;
- Breakdown adjudication, Fuel, motorization by trucks, or vehicle classes;
- combat, Reserve Release, later Movement/Combat Segments, and second-player Movement;
- attachments, assignment changes, dummies, Patrol, historical knowledge, and published content;
- persistence, transport, Maproom UI, Intelligence, or model-backed intent parsing; and
- committing source scans, map art, tables, or copied rules expression.

The stop condition is an owner-reviewed research/specification/design package. Production Movement
code must not begin until the visibility ruling and the governing contracts are approved. That
research gate is satisfied; the exact Rules foundation is implemented and Content mobility begins
with `MOV-TASK-003`.

## Method and source index

The research visually inspected temporary image-only scans, searched the official errata, and
compared the retained source facts with the current Core, Observation, Legal Actions, Content Pack,
and Exercise Harness contracts.

### Primary sources inspected

| Source | Exact locators | Use |
| --- | --- | --- |
| [Original 1979 Land Game rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) | Rules 5.2, 6.11-6.24, 8.0-8.35, 8.43-8.47, 8.62-8.66, 9.0-9.4, and 10.0-10.3 | Sequence, CPA, Cohesion, movement, terrain, stacking, ZOC, and contact behavior |
| [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf) | Corrections to Rules 8.17, 8.23, and 8.37 | Non-motorized voluntary limit and corrected Track behavior |
| [Original Map A scan](https://spigames.net/PDFv10/CNA_Maps.pdf) | Map A, Rule 8.37 Terrain Effects Chart | Exact terrain/hexside Movement costs and stacking limits |
| [Charts common to both players](https://spigames.net/PDFv10/CNA_ChartsBothPlayers.pdf) | Chart page 1, Rule 6.3; chart page 2, Rule 9.4 | Capability cost summary and stacking-point values |

The scans are evidence only. This artifact retains normalized facts and locators, not source layout,
artwork, or substantial rules prose.

### Repository evidence inspected

- `src/Cna.Core/Rules/Cna1979LandSequence.cs`
- `src/Cna.Core/Content/ContentTopology.cs`
- `src/Cna.Core/Content/ContentForces.cs`
- `src/Cna.Core/Content/Cna1979SyntheticContentCatalog.cs`
- `src/Cna.Core/Campaigns/CampaignElementState.cs`
- `src/Cna.Core/Campaigns/CampaignWorldSnapshot.cs`
- `src/Cna.Core/Campaigns/CampaignSnapshot.cs`
- `src/Cna.Core/Observations/CampaignObservation.cs`
- `src/Cna.Core/Observations/ObservedOwnElement.cs`
- `src/Cna.Core/Actions/CampaignLegalActions.cs`
- `src/Cna.Core/Exercises/CampaignExercises.cs`
- the retained Content Pack, Observation/Fog, Recon/Contact Knowledge, Reserve, and Exercise Harness
  research/specification/design packets.

## Source-backed facts

### Capability and Cohesion

- A unit's CPA is the number of Capability Points it can spend across both players' portions of one
  Operation Stage without earning Disorganization Points (`6.11-6.14`). Unspent CP does not carry
  into the next stage (`6.16`).
- Each full CP spent over CPA earns one Disorganization Point, and each DP immediately reduces
  Cohesion by one (`6.21-6.22`). Cohesion `0` is normal; positive values are Reorganization and
  negative values are Disorganization (`6.2`).
- Cohesion cannot be raised above `+10`; a unit at `-26` or worse cannot move and may surrender when
  an enemy combat unit moves adjacent (`6.23`, `6.26`).
- A unit that spends no CP during the entire Operation Stage can earn five Reorganization Points,
  but this cannot raise it above `0` (`6.24`). This is an end-stage rule and is not part of the
  first Movement vertical.
- Capability costs include halves. The authoritative amount contract must therefore be exact; a
  binary floating-point or rounded integer ledger would be unsuitable.

### Movement and sequence

- Movement is paid when entering each adjacent hex or crossing its hexside; units move one at a
  time or in stacks along a contiguous path (`8.0`, `8.11`).
- The rules use continual movement: a unit may move multiple times and the player may repeat
  Movement and Combat Segments, subject to later restrictions (`8.2`, `8.21-8.25`).
- A unit may exceed CPA, but that immediately affects Cohesion. A unit at `-26` or worse cannot
  move (`8.16-8.17`, corrected by the September 1979 errata).
- Reserve units are unavailable to ordinary Movement until the later Reserve Release procedure.
- The current catalog already has the truthful successor after Movement:
  `land.segment.breakdown-determination`. An explicit completion event can advance exactly once to
  that boundary without pretending Breakdown or Combat is implemented.

### Normalized rules-laboratory table subset

| Input | Non-motorized CP | Motorized CP | Stacking limit | Source |
| --- | ---: | ---: | ---: | --- |
| Clear hex | 2 | 2 | 6 | Map A, Rule 8.37 |
| Desert hex | 3 | 4 | 6 | Map A, Rule 8.37 |
| Road connection | 1 | 1/2 | 5 while using road | Map A, Rule 8.37 |
| Track connection | one-half underlying terrain/hexside cost | one-half underlying terrain/hexside cost | 5 while using track | September 1979 correction to Rule 8.37 |
| Ridge hexside | +2 | +4 | n/a | Map A, Rule 8.37 |
| Up-slope hexside | +2 | +4 | n/a | Map A, Rule 8.37 |
| Down-slope hexside | +1 | +2 | n/a | Map A, Rule 8.37 |

Road use negates other terrain costs in the chart's supported conditions. Track halves terrain and
hexside costs; the printed flat `1` Track entry is explicitly superseded by the errata. The first
implementation must normalize these as rules data with provenance, not resolver constants.

A battalion has stacking value `1` under the Rule 9.4 chart. Stacking limits apply when movement
ceases at the end of a Movement Segment, while road/track traversal has its own limit. The first
vertical should conservatively reject a destination whose applicable supported limit would be
exceeded and defer transient overstacking semantics until stack movement is designed.

### Zones of Control and fog

- A qualifying combat unit or qualifying combined stack exerts a ZOC into adjacent permitted
  hexes (`10.1`). Enemy occupancy and ZOC change Movement legality (`8.13-8.15`, `10.2`).
- The current observation deliberately exposes no opposing elements. Therefore a side-visible
  legal-action set cannot faithfully pre-filter enemy occupancy or ZOC without a new apparent
  representation/contact policy.
- Existing research already separated real force truth, map representation, and side knowledge.
  Movement should consume that separation rather than make element truth outward-facing.

## Repository findings

- The synthetic content already has the exact nine-location topology, Clear/Desert terrain,
  Road/Track, directional Slope, nondirectional Ridge, battalion organization, and base CPA needed
  by the first vertical.
- Content does not say whether an element is motorized. CPA alone does not determine motorization.
  A versioned rules-owned mobility classification is required before cost lookup.
- `CampaignElementState` currently stores only element ID, location, and Reserve status. It has no
  Cohesion, Operation-Stage CP expenditure, representation binding, segment participation, or
  contact state.
- `CampaignWorldSnapshot` and canonical snapshot/event formats must version with any new state;
  changing only the resolver would make replay incomplete.
- The observation already exposes public topology and own element CPA/location/Reserve status, but
  no opposing apparent presence or own Cohesion/expenditure.
- Legal Actions has no Movement candidate. Reserve completion is the last supported side command.
- Exercise manifests stop only at sequence boundaries. An explicit Movement completion to the
  existing Breakdown Determination boundary therefore produces a checked simulator terminal with
  no harness terminal-schema expansion.

## Recommended decisions

| Decision | Recommendation | Status |
| --- | --- | --- |
| `MOV-DEC-001` | Treat initial map representation/apparent presence as a prerequisite inside the Movement foundation delivery graph, not as a resolver side effect. | Approved |
| `MOV-DEC-002` | Adopt a Sandtable visibility ruling that exposes apparent opposing map presence and whether that presence exerts a ZOC, while hiding real element bindings and unsupported face/stack facts. | Approved |
| `MOV-DEC-003` | Add rules-owned mobility classification to content and derive stacking values from organization; do not infer motorization from CPA or persist a second stacking authority. | Approved |
| `MOV-DEC-004` | Represent Capability Points as an exact reduced rational amount in contracts and canonical bytes; do not use floating point. | Approved |
| `MOV-DEC-005` | The first gameplay vertical supports non-contact movement only, permits repeat accepted moves, charges the normalized subset, and completes exactly to Breakdown Determination. | Approved |
| `MOV-DEC-006` | Keep authoritative representation bindings and movement events internal; observations and action candidates carry only side-safe semantic facts. | Approved |
| `MOV-DEC-007` | Explicitly reject unsupported terrain, mobility, ZOC/contact, stack movement, and later sequence behavior with zero events. | Approved |
| `MOV-DEC-008` | Reuse the Reserve controller matrix as the first Movement Maneuver: `none`/`one`/`all` produces two/one/zero eligible movers before completion. | Approved |

## First vertical behavior

At the implemented first-side Movement checkpoint:

1. the acting side sees own movable elements, public topology, own CP/Cohesion state, and only the
   approved opposing apparent-presence facts;
2. the Umpire generates adjacent non-contact moves whose terrain, edge, stacking, Reserve, side,
   mobility, Cohesion, and resulting cumulative-expenditure rules are in the supported subset;
3. the player submits one exact action ID or an equivalent validated action submission;
4. the Umpire revalidates expected state version and position, resolves the exact CP breakdown,
   rejects a resulting cumulative expenditure above base CPA, otherwise updates location and
   expenditure while preserving Cohesion, and emits one replay-complete event;
5. the player may move again or explicitly complete Movement; and
6. completion emits one event and advances exactly to Breakdown Determination, where this package
   stops.

No remote or model call occurs in authority. A later Needle-backed intent layer may select among
these side-safe action candidates, but it cannot create a new move or bypass adjudication.

## Open rulings and explicit deferrals

- **Approved visibility ruling:** `MOV-DEC-002` exposes apparent opposing location and supported ZOC
  exertion while hiding real bindings and unsupported force facts. The primary rules assume a
  physical board and do not fully define digital inspection of stack faces, identity, or persistent
  memory, so later contact/knowledge fields require their own versioned ruling rather than reopening
  this v1 decision.
- **Over-CPA expenditure and Disorganization:** the first vertical excludes every candidate and
  rejects every submission whose resulting cumulative expenditure would exceed base CPA. This
  covers both integral and fractional excess, keeps Cohesion unchanged in v1, and avoids silently
  inventing a partial Disorganization rule. A later version may admit overrun only after its exact
  Cohesion conversion and canonical event contract are approved.
- **Transient stacking:** the first vertical rejects destination overstacking. Later stack movement
  must settle when temporary passage is legal and which road/track limit applies.
- **Contact entry:** enemy ZOC entry and immediate stop require contact state, Reaction, and later
  combat obligations. They are not silently treated as an ordinary move.
- **Published content:** all initial Cohesion, mobility, representation, attachment, and scenario
  facts require separate sourced ingestion. The synthetic lab does not establish published data.

## Risks and mitigations

| Risk | Consequence | Mitigation |
| --- | --- | --- |
| Legal actions derive from hidden truth | Candidate presence/absence leaks an opponent | Apparent representation prerequisite and negative cross-observation tests |
| Flat Track cost follows the misprinted chart | Incorrect movement ledger | Normalize the September 1979 correction and retain a golden vector |
| CPA used as mobility classification | Wrong terrain costs and movement restrictions | Versioned rules vocabulary plus content assignment |
| Location changes without a ledger | Replay cannot explain CP/Cohesion | One semantic movement event carrying before/after and cost components |
| Generic decimal arithmetic | Canonical drift or rounding defects | Reduced exact rational value object and canonical vectors |
| Movement completion skips mechanics | Authority advances past unsupported decisions | Stop at Breakdown Determination and expose no Breakdown action |
| Full continual loop pulled into one task | Contact, reaction, combat, and fog become inseparable | Deliver non-contact foundation first and retain typed unsupported boundaries |

## Owner decision

On 2026-08-25, the project owner approved all eight decisions, the first vertical terminal at
Breakdown Determination, exact rational CP amounts, the all-over-CPA/Disorganization deferral
(including fractional excess), and the linked specification/task graph. This authorizes the
bounded implementation tasks in the linked design. It does not authorize contact entry, Reaction,
Breakdown, Combat, published scenario ingestion, or player-intent/model integration.
