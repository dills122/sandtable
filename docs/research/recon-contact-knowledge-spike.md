# Reconnaissance, Contacts, and Dummy Knowledge Spike

**Status:** Decision-ready recommendation for future capability planning

**Date:** 2026-08-16

**Decision owner:** Project owner

**Research work item:** `RSH-RECON-001`

**Future capability umbrella:** `RECON-001` (working name)

**Immediate boundary:** This packet does not change the approved conservative `OBS-001` scope.

## Executive conclusion

Faithful opposing-force knowledge cannot be added as fields on `CampaignElementState` or derived
from the current authoritative world at query time. The source rules distinguish at least three
different things:

1. the real formations and units that exist;
2. the counters or parent formations that represent them on the map, including attached units and
   deceptive Dummy Tank Formations; and
3. the facts a particular side learned through Patrol and Reconnaissance at a particular time.

Sandtable should preserve those as separate authoritative concepts. Actual force truth remains
inside the Umpire. A replayable map-representation layer records what physical piece represents a
real or dummy formation and where it is. A side-specific knowledge ledger records only the exact
disclosures received by that side, including false dummy claims, together with when they were
learned. Player observations project from representation plus that ledger; they never rejoin live
opposing content merely because a real element ID is known internally.

`RECON-001` is therefore too broad for one implementation slice. Use it as an umbrella with these
ordered gates:

```text
source visibility/ruling gate
        |
        v
REP-001 map representation, attachments, and dummy authority
        |
        +--------------------+
        |                    |
        v                    v
CONTACT-001 apparent contacts  movement/contact authority
        |                    |
        +----------+---------+
                   v
PATROL-001 costs, losses, disclosure choice, and knowledge ledger
                   |
                   v
KNOW-001 current and historical reconnaissance knowledge
```

Before `MOVE-001` exposes opposing presence to a player, the project must resolve the physical
visibility convention that the reviewed rules do not state precisely: which counter existence,
location, and face facts an opponent may inspect without successful Reconnaissance. The project
must not silently treat the complete face of every authoritative combat element as public.

Full Patrol implementation also depends on capabilities absent from the current rules laboratory:
unit-type vocabulary, current TOE strength, Cohesion, Fuel, Ammunition, Stores, attachments, combat
losses, and source-normalized Rules 16.6 through 16.8 tables. It should not be pulled into
`OBS-001` or represented by placeholders.

## Decision question and boundaries

This spike asks:

> What authority, event, replay, and observation boundaries are required to represent map
> counters, attachments, Dummy Tank Formations, Patrol/Reconnaissance disclosures, and remembered
> opposing knowledge without leaking current force truth or inventing rules?

The answer matters before movement/contact contracts become outward-facing. Movement and Zones of
Control react to current opposing presence, while Reconnaissance reveals only bounded historical
facts and Dummy Tank Formations can make those facts false. A contract that conflates the two will
either leak hidden truth or become impossible to replay faithfully.

In scope:

- counter representation versus combat-element truth;
- assignment/attachment consequences for map representation;
- Patrol/Reconnaissance procedure, losses, disclosure choice, and source tables;
- Dummy Tank Formation creation, apparent identity, and source-defined removal conditions;
- side-specific knowledge, staleness, authority, events, and replay;
- dependencies with observation, movement, contact, combat, and content evolution; and
- implementation options, recommendation, owner choices, and explicit unknowns.

Out of scope:

- implementing any contract, rule, table, command, event, or projection;
- changing `OBS-001`, its specification, or its tests;
- transcribing copyrighted rules prose, tables, counter art, or published scenario data;
- adopting an unsupported visibility, forgetting, or confidence mechanic;
- persistence, transport, Maproom UI, notifications, Intelligence, or spectator policy; and
- committing source PDFs or rendered pages.

The stop condition is this retained decision packet. Any architecture or rules ruling it proposes
must be approved and moved into a governing specification before implementation.

## Method and source index

The research visually inspected the image-only primary scans outside Git, checked the searchable
errata, and compared those facts with the merged Core contracts at commit `743a604`.

### Primary sources inspected

| Source | Exact locators inspected | Use |
| --- | --- | --- |
| [Original 1979 Land Game rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) | PDF pages 7 and 9; Rules 4.2, 4.21, 4.25; PDF pages 13-14, Rules 8.0, 8.14-8.25; PDF pages 26-27, Rules 16.0-16.5; PDF pages 29-30, Rules 19.1-19.5 | Counter fields, attachments, movement/contact dependency, Patrol procedure, disclosure, dummies, attachment lifecycle |
| [Charts and Tables Common to Both Players](https://spigames.net/PDFv10/CNA_ChartsBothPlayers.pdf) | Chart page 1, Rule 16.6 Patrol Survival Table; chart page 3, Rule 16.7 Patrol Reconnaissance Table and Rule 16.8 Objective Loss Table; chart page 16 index | Patrol losses and conversion of surviving patrol strength into disclosed battalion-sized equivalents |
| [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf) | Rule 16.11 addition on PDF page 2 | Additional unit types eligible to Patrol |

The source scans are evidence only. This artifact retains normalized behavior and locators, not
table matrices, artwork, or substantial rules expression.

### Repository evidence inspected

- `src/Cna.Core/Content/ContentForces.cs`
- `src/Cna.Core/Content/ContentScenarios.cs`
- `src/Cna.Core/Content/Cna1979SyntheticContentCatalog.cs`
- `src/Cna.Core/Content/ContentPackValidator.cs`
- `src/Cna.Core/Rules/Cna1979ContentVocabulary.cs`
- `src/Cna.Core/Rules/Cna1979LandSequence.cs`
- `src/Cna.Core/Campaigns/CampaignElementState.cs`
- `src/Cna.Core/Campaigns/CampaignWorldSnapshot.cs`
- `src/Cna.Core/Campaigns/CampaignWorldFactory.cs`
- `src/Cna.Core/Campaigns/CampaignEvent.cs`
- `src/Cna.Core/Campaigns/CampaignSnapshot.cs`
- the implemented Content Pack v1 and Campaign World v1 specifications/designs; and
- the approved `OBS-001` boundary research.

## Evidence

### Documented facts: counters, parent formations, and attachments

- **A counter is a representation, not the complete force record.** Rules 4.2 and 4.21 describe
  counters with visible fields such as designation, type, parent formation, organization size,
  stacking value, and Capability Point Allowance. The complete organization/TOE data remains on
  separate sheets and charts.
- **An attached unit can cease to have its own map counter.** Rule 4.25 states that a unit attached
  to another unit is not represented by a counter on the map; the parent unit's counter represents
  the attached unit. The example distinguishes attached elements from other units still using
  their own counters.
- **Assignment and attachment are different relationships.** Rules 19.1 through 19.4 distinguish a
  unit's assigned parent from the parent to which it is currently attached. A unit can have one of
  each simultaneously, subject to restrictions, and attachment/detachment can occur during named
  Reorganization or Movement windows with Capability Point costs.
- **Attaching changes physical representation.** Rule 19.42 removes the attached unit's counter
  from the map and treats it through the parent formation. Rules 19.43 and 19.44 define detach
  costs and movement consequences. Rules 19.45 and the Maximum Attachment Chart constrain what a
  parent may absorb.

These facts prohibit a permanent one-to-one relationship between `ContentCombatElement` and an
opponent-visible map piece.

### Documented facts: Patrol and Reconnaissance flow

- **Patrol is a mechanic-specific phase decision.** Rules 16.0 and 16.1 have the Phasing Player
  choose Patrol Points, their origin units, and an enemy-occupied target hex. Eligibility depends
  on unit type; the September 1979 Rule 16.11 erratum adds Italian L/6, Commonwealth Stuart, and
  mechanized-infantry/Panzergrenadier eligibility.
- **Commitment has authoritative costs and restrictions.** Rules 16.13 through 16.17 constrain
  commitment by unit and target, bar sufficiently depleted Cohesion, consume Ammunition and Fuel
  before resolution, and prohibit Patrol into or from specified foul-weather hexes.
- **Targeting depends on current map state.** Rules 16.21 and 16.22 restrict timing and allow an
  enemy-occupied target within a five-hex reconnaissance path whose Patrol Points ignore enemy
  Zones of Control and occupancy while tracing the path.
- **Patrol can change force state, not just knowledge.** Rules 16.3, 16.6, and 16.8 resolve losses
  to the patrolling and patrolled forces. The non-Phasing Player can have a source-defined choice
  over which eligible unit absorbs a loss. A completely eliminated Patrol supplies no information.
- **Successful Reconnaissance yields a bounded count, not the whole hex.** Rule 16.34 and the Rule
  16.7 table convert net surviving Patrol Points and a die result into a number of battalion-sized
  equivalents about which information is disclosed. The output can be zero or all, depending on
  the source table result.
- **The defender participates in disclosure.** Rules 16.51 through 16.55 require particular fact
  categories but let the non-Phasing Player choose qualifying units subject to size and unit-type
  restrictions. Headquarters and anti-aircraft/Flak facts are normally excluded while Engineer
  information can qualify.
- **Strength disclosure is explicitly time-bound and approximate.** Rule 16.51 requires exact
  historical designation, type, motorization status, and a TOE strength within two of the actual
  strength **at the time of the Patrol**. A later query cannot safely replace that disclosure with
  the unit's current strength.

The procedure therefore contains at least two human decision barriers: Patrol commitment by the
Phasing Player and qualifying disclosure/loss choices by the non-Phasing Player. A single automatic
`ObserveCampaign` query cannot adjudicate it.

### Documented facts: Dummy Tank Formations

- **A dummy is intentional authoritative deception.** Rule 16.41 treats Dummy Tank Formations as
  tank-battalion-sized representations with no combat value and zero CPA.
- **Creation is a state change with a cost and timing rule.** Rule 16.42 creates a Dummy Tank
  Battalion during the Construction Segment in a selected hex by spending Stores. The published
  game provides no dedicated dummy counters, which reinforces the difference between the game
  object and the physical component used to represent it.
- **Quantity and ownership are constrained.** Rule 16.43 caps simultaneous dummies, restricts a
  hex to one, and excludes Italian formations even when mixed with German formations.
- **Apparent identity may duplicate a real unit.** Rule 16.44 permits a dummy to take an identity
  already used by another tank battalion in play.
- **Reconnaissance can receive a deliberately false claim.** Rule 16.45 lets the defending player
  disclose an apparent tank battalion and a chosen TOE strength for a dummy; the patrolling player
  cannot determine from that disclosure whether the reported unit is real.
- **Removal depends on later combat state.** Rules 16.46 and 16.47 define automatic or conditional
  removal in Close Assault and Anti-Armor circumstances and special historical representations.

A dummy cannot be modeled as a hidden boolean on a real combat element. It can exist without a real
element, impersonate an existing designation, produce a false disclosure, and be removed by its
own source-defined lifecycle.

### Documented facts: movement and contact dependency

- **Movement reacts to enemy presence.** Rules 8.14 and 8.15 constrain entry into or exit from an
  enemy Zone of Control and can stop movement immediately.
- **Movement and combat repeat inside a continual sequence.** Rules 8.21 through 8.25 permit
  repeated unit movement and combat selections, track whether a unit finished moving, and connect
  enemy ZOC presence with Break Contact, Engaged, and combat eligibility.
- **Attachment can change during Movement.** Rules 19.42 through 19.44 allow some attachment and
  detachment during Movement with costs and stop conditions, changing which counter represents
  the underlying force inside the same broader gameplay slice.

Authority may use complete world truth to adjudicate a ZOC. The player-facing legal-action and
observation boundary still needs a source-approved apparent-presence policy; otherwise an action
list can disclose a hidden opposing piece even when the observation does not.

### Repository observations

- `ContentCombatElement` has stable element, side, parent-formation, organization, base CPA,
  placement-mode, and origin fields. It has no unit-type, reconnaissance-eligibility, historical
  designation, motorization, HQ/AA/Engineer classification, current TOE strength, or dummy facts.
- `ContentPlacementMode.AttachmentOnly` is a static initial-deployment restriction. It does not
  represent runtime assignment, attachment, detachment, or the current parent counter.
- `CampaignWorldSnapshot` contains one exact `CampaignElementState(elementId,
  currentLocationId)` for every independently placed scenario element. Initial validation rejects
  attachment-only elements and requires exact element/location truth.
- The content vocabulary currently contains only side, terrain, edge-feature, and organization
  kinds. Its capabilities stop at topology, formations, and initial deployment.
- The campaign snapshot has no current TOE strength, Cohesion, Ammunition, Fuel, Stores, attachment,
  loss, counter, dummy, contact, disclosure, or side-knowledge state.
- `Cna1979LandSequence` places Patrol after Repair at the end of each acting player's phase. It
  places Construction in the Operation prelude and Movement/Combat earlier in each player phase,
  so dummy creation, movement, combat removal, and Patrol disclosures cross multiple sequence
  positions.
- Chronicle currently has creation, initiative, and a retained trusted-history sequence event.
  It has no mechanic capable of producing or replaying representation or knowledge changes.
- The synthetic rules laboratory has four independent battalion elements. It can prove a safe
  observation boundary but cannot prove attachments, hidden stacks, dummy impersonation, Patrol
  eligibility, resource costs, losses, or staleness.

### Inferences

- **Representation needs its own identity.** A map representation needs a stable ID distinct from
  combat-element ID. Its authoritative binding can reference one or more real elements, a parent
  formation, or a dummy object. Which binding facts are public is an observation policy, not a
  property of the binding itself.
- **Knowledge is historical authority.** Each successful disclosure should become an immutable
  side-addressed fact carrying the observing side and observed-at campaign position/state version.
  Reprojection must return that recorded disclosure, including any dummy claim, rather than join
  against current opposing truth.
- **No automatic freshness is safe.** Historical designation and type may remain stable, while
  approximate strength and location can become stale immediately after later state changes. A
  digital record may label when a fact was learned, but must not silently refresh, expire, or
  assign confidence without a source or adopted ruling.
- **Patrol needs a command workflow.** Patrol commitment, deterministic/random resolution, enemy
  loss allocation, and enemy disclosure choice cannot be collapsed into one command if a human
  decision exists between them. Each accepted choice must validate state version and legal options.
- **Chronicle can retain truth; outward history cannot.** Authoritative events may need actual loss,
  dummy binding, and chosen disclosure values for replay. Player Chronicle/War Diary and Maproom
  output must be separate redacted projections so an event contract never becomes a player DTO.
- **Movement cannot derive visibility from legality.** The Umpire may reject a move because of an
  enemy ZOC, but a candidate-action query or rejection message must not reveal a piece the side
  was not entitled to observe. Base apparent-presence policy must be settled before movement
  actions are exposed.

### Unknowns and evidence gaps

- The reviewed rules define what Reconnaissance reveals but do not state a complete convention for
  what an opponent may inspect on a counter or in a stack without Patrol. In particular, they do
  not resolve which existence, location, top-counter, face, and under-stack facts are public in a
  digital implementation.
- The reviewed rules do not define an initial side-knowledge record for a scenario setup.
- The reviewed rules do not instruct a player to forget a prior disclosure or define confidence
  decay. Rule 16.51 proves that strength is observed at a point in time, but not how a digital UI
  should retain or label it.
- The exact interaction among a dummy's apparent identity, physical substitute counter, stack
  position, attachment, and movement needs a broader source pass before fields are fixed.
- Rules 16.6 through 16.8 were visually confirmed, but their matrices have not been independently
  normalized or double-entered. They are not implementation-ready rule data.
- The first published scenario's initial dummies, attachments, Patrol-eligible types, and player
  private records were not inventoried in this bounded spike.
- Air Reconnaissance under Rule 42.27 is a separate procedure and was not folded into this Land
  Patrol model merely because both reveal information.

## Options considered

| Option | Fidelity | Fog safety | Replay quality | Main failure mode | Decision |
| --- | --- | --- | --- | --- | --- |
| Filter live world/content on every observation | Poor | Poor | Misleading | Refreshes historical disclosures from hidden current truth; cannot represent dummies or attachments | Reject |
| Add visible/discovered fields to each real element | Poor | Medium | Poor | Couples one side's changing knowledge to shared truth and assumes one element per counter | Reject |
| Store disclosure events only, with no representation layer | Medium for Patrol | Medium | Good for knowledge | Cannot explain parent counters, dummies, ZOC/contact, or attachment-driven piece changes | Reject as complete design |
| Keep separate full world snapshots per side | Medium | Medium | Expensive | Duplicates authority, invites divergence, and still needs dummy/knowledge semantics | Reject |
| Separate force truth, map representation, and side knowledge ledger | High | High | High | More explicit contracts and staged work | **Recommend** |

## Recommended capability boundaries

### `REP-001` - authoritative map representation

Define this before player-facing movement/contact:

- stable map-representation identity independent of element identity;
- binding to the real parent/element set or to an authoritative dummy;
- current location and current represented parent after attach/detach;
- attachment/assignment state needed to reconstruct that binding;
- source-defined dummy creation, identity, quantity, ownership, and removal invariants; and
- pure projection of only the source-approved apparent presence for one side.

Do not expose the binding itself in an observation. Do not make a dummy a fake
`ContentCombatElement`. Do not copy static presentation labels into authority.

`REP-001` may need a Content Pack schema/capability extension for unit classifications and
historical identity. That extension must be independently sourced and hash/versioned rather than
adding optional strings to Content Pack v1.

### `PATROL-001` - patrol adjudication and disclosure

Implement only after the required force/resource state exists:

- source-normalized eligibility, range, weather, Cohesion, Ammunition, Fuel, and target rules;
- deterministic command stages for commitment, loss allocation, disclosure choice, and completion;
- Rule 16.6-16.8 table data with per-datum provenance and independent vectors;
- versioned random draws and exact replay of casualties and table results;
- legal disclosure candidates that contain no information unavailable to the defending side;
- explicit dummy-claim handling without revealing the dummy binding to the observer; and
- typed unsupported results for unimplemented unit types or special cases.

The authoritative turn must not stay open awaiting a remote/model choice. A human or deterministic
Staff choice resumes through a state-versioned command, consistent with the existing Umpire
boundary.

### Side-specific knowledge ledger and `KNOW-001`

The ledger should record semantic disclosure facts, not prose:

- recipient side;
- source mechanic and authoritative event identity;
- observed-at state version and Land sequence position;
- apparent representation/hex identity as permitted by the adopted visibility policy;
- exactly the fact categories disclosed under Rule 16.5; and
- whether the value is a source-permitted approximate claim, without recording hidden truth in the
  player-facing value.

The observation may label a fact as last learned at a prior state/version. It must not calculate a
current value from opponent content, reveal a dummy discriminator, or imply probability/confidence
unless a later ruling defines one.

## Spec-ready invariants and acceptance consequences

A later specification should make these testable:

1. One real element can be represented by a parent piece and have no independent visible piece.
2. Attachment/detachment changes representation only through accepted versioned events and replay
   produces the same representation graph.
3. A dummy can exist without a real element, duplicate an apparent identity, and be removed only
   through a supported source-defined transition.
4. No side observation or legal action exposes a dummy discriminator, real binding, undisclosed
   element ID, current hidden strength, or hidden resource state.
5. A Patrol command validates phase, acting side, state version, eligibility, target, range,
   weather, Cohesion, and resources before emitting any event.
6. Ammunition/Fuel commitment, random draws, casualties, net Patrol Points, table coordinates,
   disclosure count, defending choices, and resulting knowledge are inspectable and replayable.
7. A completely eliminated Patrol produces no disclosure.
8. When more eligible defenders exist than disclosures, the non-Phasing side's valid choice is
   preserved; the engine does not choose from hidden opponents on the Phasing side's behalf.
9. Replaying a historical disclosure after the actual unit moves or changes strength returns the
   same historical fact with the same observed-at marker, not refreshed truth.
10. Two side projections from the same authoritative history can contain different knowledge
    without creating divergent authoritative worlds.
11. Canonical knowledge/representation serialization is deterministic under input reordering,
    culture changes, and equal replay history.
12. Negative serialization and dependency tests prove full Content Pack, Campaign World, hidden
    bindings, random state, opposing resources, and raw source expression never become outward
    DTOs.
13. Movement/action tests prove legal-action generation and rejection messages do not reveal
    presence beyond the approved apparent-contact policy.
14. Unsupported visibility, Patrol type, dummy, attachment, table, or combat-removal cases stop
    with typed rejection and zero partial state change.

## Delivery recommendation

1. Keep `OBS-001` conservative: own-side force facts and no opponent contacts.
2. Before specifying `MOVE-001`, run a narrow source/ruling gate for base counter/stack visibility,
   initial scenario knowledge, and digital retention labeling.
3. Specify and implement `REP-001` with one synthetic attachment and one synthetic dummy only if
   the approved rules-laboratory capability needs them. Do not expand the published scenario yet.
4. Add `CONTACT-001` apparent-contact projection and movement/legal-action negative fog tests.
5. Implement movement/contact against authoritative truth while exposing only `CONTACT-001`.
6. Implement the missing force/resource/loss capabilities in their source sequence.
7. Normalize and double-check Rules 16.6-16.8, then deliver `PATROL-001` and the side-knowledge
   ledger before the engine advances through a Patrol phase.
8. Add `KNOW-001` historical reconnaissance knowledge without changing earlier observation meaning;
   use a new contract/policy version.

This ordering permits a safe movement skeleton without pretending that Patrol already exists. It
also prevents the current roadmap's later Patrol milestone from becoming an after-the-fact retrofit
of map identity and player knowledge.

## Risks and maintenance costs

| Risk | Consequence | Mitigation |
| --- | --- | --- |
| Treating real element IDs as contact IDs | Correlation leaks across turns and defeats dummies | Separate representation/contact identity from truth binding |
| Recomputing old knowledge from current world | Historical replay changes as units move or lose strength | Event-source the exact disclosure and observed-at position |
| One command hides defender choice | Removes an original decision and can reveal eligibility | Multi-stage legal commands with state-version checks |
| Public Chronicle uses authoritative events | Dummies, losses, and hidden bindings leak | Chronicle authority stays internal; War Diary is a redacted projection |
| Full Patrol pulled forward before prerequisites | Placeholder resources/types become false rules | Preserve explicit dependency gates and unsupported transitions |
| Persistent digital memory exceeds physical convention | Product advantage or rules distortion | Retain learned-at data only after owner-approved ruling/UX policy |
| Fog policy embedded in movement resolver | Future observation changes alter authority | Keep adjudication truth and apparent-contact projection separate |
| Large knowledge ledger | Snapshot size and migration cost | Store normalized semantic facts; measure before introducing compaction |

## Confidence and limitations

**High confidence:** real force truth, map representation, and side knowledge must be distinct. The
primary rules explicitly remove attached-unit counters, permit dummies with false identities, and
make Patrol disclosure a bounded, defending-player-controlled result.

**High confidence:** Patrol is an authoritative multi-step mechanic with resource costs, seeded
table outcomes, casualties, and human choices. It cannot be implemented as a read-only observation
filter.

**High confidence:** current Core contracts lack the force classifications, mutable resources,
loss state, attachment state, representation, and knowledge needed for faithful Patrol.

**Moderate confidence:** a stable representation ID is the smallest maintainable software model.
The exact public subset cannot be finalized until base counter/stack visibility is resolved.

**Low confidence / unresolved:** default digital memory duration and what physical counter facts
are inspectable without Patrol. No reviewed primary rule settled those conventions, so this packet
does not invent them.

This was a bounded Land-rules review, not an exhaustive audit of scenario booklets, player-private
OA sheets, Air Reconnaissance, every dummy interaction, or every erratum.

## Owner decisions requested

1. Approve `RECON-001` as an umbrella split into `REP-001`, apparent-contact observation, and later
   `PATROL-001` plus a side-specific knowledge ledger.
2. Require a recorded source/ruling decision on base counter/stack visibility, initial knowledge,
   and digital learned-at retention before `MOVE-001` exposes opponent presence.
3. Approve the sequencing rule that full Patrol waits for real force/resource/loss prerequisites
   but map representation and apparent-contact safety cannot wait until after movement.
4. Confirm that no probabilistic confidence, automatic forgetting, or current-truth refresh will be
   added without a later source or adopted ruling.

After approval, update the roadmap dependency graph and create a focused visibility/ruling spike.
Do not implement `RECON-001` directly from this research packet.
