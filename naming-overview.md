# Sandtable Naming and Domain Vocabulary

Sandtable draws on historical staff work, Kriegsspiel and campaign administration. Use flavor names
for major products and domains, and plain technical names inside modules. A name reserves a
responsibility; it does not imply a deployed service or implemented mechanic.

**OpenCodeReview (OCR)** names optional external PR-review tooling, not a Sandtable product,
Intelligence provider or authoritative game component. Its proposed
[pilot](docs/runbooks/optional-code-review.md) is opt-in and has no merge authority.

> Command decides. Staff plans. Dispatch carries. Umpire adjudicates. Chronicle remembers.
> Maproom shows.

## Vocabulary

| Sandtable name                  | Technical responsibility              | Why                                                                       |
| ------------------------------- | ------------------------------------- | ------------------------------------------------------------------------- |
| **Umpire**                      | Deterministic simulation/rules engine | Direct Kriegsspiel lineage: the umpire adjudicates what actually happens. |
| **Maproom**                     | Main UI / campaign viewer             | Where you inspect the theater, units, supply, reports, and issue orders.  |
| **General Staff** / **Staff**   | Scripted planning system              | Converts strategic intent into concrete operational plans.                |
| **Command**                     | Human/AI strategic decision layer     | Decides _what we want to accomplish_.                                     |
| **Dispatch**                    | Command/event messaging               | Orders go in; reports/results come back.                                  |
| **Chronicle**                   | Event journal + replay                | Permanent authoritative history of the campaign.                          |
| **Archives**                    | Saves/snapshots/campaign storage      | Persistent campaign material.                                             |
| **Intelligence**                | Shared LLM service                    | Model routing, personas, narrative, optional memory.                      |
| **Signals**                     | gRPC/contracts/transport              | Historically appropriate and technically appropriate.                     |
| **Quartermaster**               | Logistics subsystem                   | Supply, fuel, water, ammunition, transport. Almost too perfect for CNA.   |
| **Order of Battle** / **ORBAT** | Units/formations definitions          | Actual military terminology.                                              |
| **Theater**                     | Map/world state                       | Geographic campaign environment.                                          |
| **Operations**                  | Movement/combat planning              | Operational actions and plan execution.                                   |
| **Air Staff**                   | Air subsystem                         | Straightforward historical terminology.                                   |
| **Admiralty**                   | Naval subsystem                       | More flavorful than `NavalService`.                                       |
| **Weather Bureau**              | Weather/environment                   | Period-flavored without being confusing.                                  |
| **War Diary**                   | Human-readable game history           | Narrative companion to Chronicle's machine event stream.                  |
| **Exercise**                    | Single simulation run                 | One automated simulation run.                                             |
| **Maneuvers**                   | Serial simulation/evaluation sets     | An ordered collection of Exercises.                                       |
| **War College**                 | AI evaluation/tournaments             | Run commanders against scenarios and measure performance.                 |

## Authority and application names

**Umpire** is the deterministic Core. Like a Kriegsspiel umpire, it knows the authoritative situation,
checks orders, resolves uncertainty and reports only what each side may know. Command, Staff,
Intelligence and Maproom cannot invent outcomes or mutate authority. Internally, use names such as
`CampaignEngine`, `MovementValidator` and `CombatResolver`, not a second layer of product branding.
`CampaignContactKnowledge` is a plain Core technical name for replayed side
knowledge in the permanent fog rule; `CampaignContactView` is its outward
projection, with private target binding retained only by Umpire.
`CampaignNativeCombatSelectionStateV12` is the first internal Combat authority step:
it stores the selected contact action and private defender binding for deterministic
Exercise and Host replay. It is not a public Combat route or adjudicated result.
The normal Host's internal Combat opening and submission use the same seat and durable
journal as owner Movement; they do not publish a playable endpoint yet.
The private [actual repeated Movement completion contract](docs/specs/combat-actual-repeated-movement-completion-v1.md)
uses plain command names: `stop-element-movement`, `resolve-breakdown-stop` and
`complete-movement-segment`. Its trusted System actor resolves the pending empty stop; the owner
then completes Movement. Breakdown Determination entry reserves the next phase without claiming
that phase has executed or introducing another product name. C04's private native consumer uses
`CampaignCombatActualRepeatedMovementCompletion` and its codec; these remain technical module
names inside Umpire, with public activation governed separately.
The private [actual ordinal2 Breakdown completion reference](docs/specs/combat-actual-repeated-breakdown-completion-v1.md)
uses `complete-breakdown-segment`, authorized by System even though the catalog retains symbolic
first-side actor metadata. It enters Combat Position Determination without executing a Combat step
or introducing another product name. B03 uses the plain technical names `CampaignCombatActualRepeatedBreakdownCompletion` and its codec
inside Umpire; reviewed acceptance and public activation remain separate gates.
The private [authentic moved-repeat Breakdown reference](docs/specs/combat-actual-moved-repeat-breakdown-completion-v1.md)
uses the same plain `complete-breakdown-segment` command after full R02 moved-repeat completion.
R03 is its delivery label, not a product name. Its frozen 300-row reference, vector audit and all
788 nonterminal cuts pass. The private native consumer uses the plain technical names
`CampaignCombatActualMovedRepeatBreakdownCompletion` and its codec inside Umpire. Native parity
passes all 300 terminal rows. The private consumer merged in PR #216 after independent review and
exact-head hosted CI; public activation remains a separate gate.

P01 names the private ordinal-two Combat Position contract following R03; it is a delivery label,
not a new product. Its [acceptance matrix](docs/specs/combat-actual-moved-repeat-position-v1.md)
has a provisional private reference, bounded evidence and a disconnected
[owner alias draft](docs/specs/combat-p01-owner-projection-v0.md), but no supported public action.
The proposed alias is distinct from the Umpire-only `p01.` candidate identity. Any later native
implementation belongs inside Umpire under a plain technical name. The internal pure
`P01OwnerVisibleCodecV1` implements only approved private owner-visible bytes and IDs.
Accepted `CMB-POL-009` excludes voluntary surrender from
the initial RC bounded exercise; the remaining contract and proof gates still control admission.
The `combat-p01-no-attack-v0` name is a private ordinal-two contract inventory, not a new
Umpire capability or a completed Combat step path.
The `P01PrivateHostFoundation` name is a plain technical host module for disabled local
grant and fence experiments. Its probe records are not Chronicle events or Archives saves,
and they expose nothing to Maproom.
`P01A1PhysicalHostImportV1` is a private Core-to-host evidence adapter for one
pinned R03 cut; its import row is not an issued campaign or owner action.
`combat-p01-explicit-rba-v1` names a separate private successor for nonadjacent zero-assault
states with a retained defender RBA choice; it is not a public action or native consumer.
`CampaignCombatAutoSurrenderSourceV1` names the private pinned Content8/Setup8 Core reader;
it is a technical source boundary, not a campaign creator or a public Combat capability.
`CampaignCombatAutoSurrenderPreIssueSourceV1` names its canonical, unissuable source-byte seal;
its `source.` integrity label is not a creation binding or Chronicle receipt.

**Command** chooses strategic objectives, whether supplied by a person, scripted policy or optional
model. **Staff** converts intent into concrete deterministic plans. The Umpire validates and executes
accepted orders. **Intelligence** supplies untrusted proposals, personas, explanations and narrative;
it never owns campaign state or rules.

**Dispatch** is a domain order/report message. **Signals** is the transport carrying it. The protobuf
contract lives in `Cna.Intelligence.Contracts`; preserving message versions and wire compatibility
is a transport responsibility, while order legality stays in Core.

**Chronicle** is authoritative event history and replay. **War Diary** is its derived human-readable
account. **Archives** is campaign storage, snapshots and saves. The normal Host's internal
durable authority journal retains Core evidence and verifies it by replay, with private side
credential custody, owner-opening binding, atomic contact-record custody, and internal
contact-derived movement submission against the current equal-visible owner cut.
The same normal journal now retains internal contact-based Combat assault or no-attack
decisions with cold-replay receipts. Voluntary finish adds a V13 System no-attack completion
that records each remaining Combat successor through Reserve Release entry without releasing
reserves or adjudicating an assault. Selected infantry assault adds a V14 System no-effect
Barrage successor into Retreat Before Assault under the same durable owner submission. Defender
RBA move/decline candidates now come from the first side-scoped defender contact cut;
hidden occupancy does not control offer presence. Trusted Host draft keeps visible bytes
separate from private action IDs. An explicit defender decline uses a V15 owner event
and the normal durable Host receipt. A first defender retreat move uses a V16 owner
event, updates Core World/contact authority, and returns to the RBA choice through
the normal durable Host receipt. Further offered moves use the same ordered Core/Host
path. The next Core Force Assignment candidate binds only the owner's own current
infantry TOE; opponent allocation stays private. Core V17 and normal Host now
retain each owner's separate seal and cold-replay receipt. A distinct V18
System completion advances after both seals.
Normal campaign timing configuration is an explicit seven-window parent
binding retained in the durable Host journal; absent configuration blocks
Force Assignment opening and seals. Schema-5/6 journals stay readable but cannot
create new campaigns. Schema 7 retains one shared Force round for both seats;
trusted deadline/high-water checks close incomplete rounds through Core V19 without
inventing an owner seal. Core V20/normal Host complete the infantry-only empty
Anti Armor step from either Force outcome. Core V21 retains cancelled Close Assault
closure and same-slot Reserve Release;
normal Content 6 can resolve an exact-pack Combat source supplement for Basic Morale
and offensive ratings, with no positive draw until its hash is retained and replayed;
the 10-TOE adjacent-infantry assault setup is a normal catalog campaign;
positive Close Assault and non-infantry disposition remain unissued. Public
opening, Combat submission, and resume still need
their normal campaign path. A replay
checkpoint does not by itself
constitute a durable player save/resume product.

**Maproom** is the player application: map, orders, legal actions and audience-safe reports.
`site/` is the existing project website, with labeled concept illustrations. The first Maproom target
is local hot-seat play; it must use the same typed, side-safe action boundary as every other client.
The accepted [local-server plan](docs/design/local-game-server-maproom-plan.md) starts with a
bounded Angular developer client before complete scenario play. **Field Atlas** names its
[visual direction](docs/design/maproom-design-system-v0.md) and internal Angular design library
`@sandtable/field-atlas`, not a new service or authority domain. The Angular CLI workspace is
`src/Cna.Maproom`, its first application is `maproom-alpha`, and Storybook is the living component
and pattern reference. The main application route offers a synthetic fixture study and a
URL-backed `?mode=campaign` local read mode using the same map presentation. `/connected`
remains a development deep link to the read-only operations view of one authorized initial
campaign cut through the generated client and loopback proxy; it is
not a command surface, a second authority, full hot-seat play or durable campaign resume.
**Operations workspace** describes Maproom's map, order inspector and linked formation records;
it adds no authority layer. The formation ledger is a player-view table, distinct from Chronicle's
authoritative event journal. War Diary entries may locate permitted formations without exposing
raw journal records or hidden causes.
The full-screen situation map and expanded order inspector are presentation states of this same
workspace; neither creates a separate command surface or campaign authority.
`Cna.Game.Contracts` contains the drafted outward OpenAPI browser and SignalR contracts;
private authority revisions remain server-side. This module is separate from
`Cna.Intelligence.Contracts`. Archives remains a host storage module, and War Diary remains an
audience-safe derived display rather than a raw Chronicle stream.
The [MAP-05 trusted host protocol](docs/specs/maproom-trusted-host-protocol-v1.md) keeps canonical
accepted-step evidence and publication heads in Chronicle/Archives private host records; Maproom
receives only authorized projected views and outward receipt status. Protocol design does not
instantiate a new service or a durable save path.
The MAP-06 trusted Core adapter stays inside Umpire; it exposes private accepted evidence to
the host while Chronicle/Archives still owns publication and recovery records.
MAP-07's temporary campaign grain lives in `Cna.OrleansHost`; it holds an opaque Umpire
session and an in-memory request record for the local rules lab. It is not Chronicle/Archives
durability or a Maproom endpoint. The host-owned side summary is a private bridge, not a
browser contract or a new authority source.
MAP-08A's `Cna.OrleansHost/Api` is a local ingress seam for operator bootstrap and seat sessions.
MAP-08B creation now binds the admitted Rules9 fixture, operator/request identity and one
short-lived grant pair; the campaign remains process-local. A bounded `/view` adapter renders
only the initial rules-lab cut from an authorized seat; later cuts fail closed pending a
coherent public successor projection. Approved private diary copy does not publish that view.
MAP-08B1 names the host-side prerequisite: typed permitted data and pinned presentation facts
between Umpire's side cut and Maproom's outward DTO. It grants no new authority to Maproom.
MAP-08B2 calls the process-local contact/action continuity record the **audience tracker**.
Its private side cut remains host data; Maproom receives only a validated player view. The
**presentation catalog** owns public fixture labels and layout, not Umpire authority.
The **audience outcome source** is a private Core hosting proof for one accepted transition.
It classifies known silent outcomes and approved diary keys. The owner-approved nine-entry
en-CA copy catalog and host-private audience tracker retain per-audience diary cursors and the
latest 50 entries, with outward IDs scoped to the seat session. They are not a browser diary,
public receipt or second adjudicator. MAP-09 names private seat-bound action ingress and exact
retry; it is not an HTTP submit or receipt route. The
**private outcome head binding** is the Orleans tracker's check that one paired proof and
both side cuts belong to the same accepted committed head. It grants no publication right.

**Proving Ground** names the local development test environment and
[runbook](docs/runbooks/proving-ground.md), not a game authority or separate service. Its first
Docker profile packages the Maproom shell and Development Orleans host; the Exercise Runner stays
a separate trusted CLI. Browser creation is limited to one admitted temporary fixture.

## Domain modules and reserved names

**Theater** owns geographic concepts and topology. **ORBAT** names immutable force and formation
definitions; campaign locations, status and losses belong to mutable world state. **Quartermaster**
names logistics rules such as supply, fuel, water, ammunition and transport. **Operations**, **Air
Staff**, **Admiralty** and **Weather Bureau** group related rule responsibilities. These names do not
justify independent microservices: keep domain modules together unless a real boundary requires one.

The pure Core uses technical namespaces for Rules, Randomness, Setups, Content, Campaigns,
Observations and Actions. Current capability status belongs to the
[roadmap](docs/roadmap/pre-alpha-roadmap.md), not this vocabulary.

## Simulation and evaluation

An **Exercise** is one fresh local simulation. A **Maneuver** is an ordered set of Exercises;
**Maneuvers** is the plural collection name. Current ExerciseRunner supports serial unpaired and
optional paired profiles with trusted local artifacts and deterministic replay checks.

**War College** reserves the future evaluation environment: commander/persona comparisons,
tournaments, rule variants and strategy benchmarking. Existing Exercise/Maneuver reports are
instrumentation, not an implemented distributed tournament or evidence of model strength or balance.
Model-backed commanders and War College orchestration remain future work.

See [technical design](tech-design.md) for project mappings and authority rationale,
[documentation index](docs/README.md) for governing domain contracts, and
[Exercise Runner runbook](docs/runbooks/exercise-runner.md) for local commands. Historical implementation
checkpoints remain in the [dated ledger](docs/research/2026-10-05-project-documentation-snapshot.md)
and the linked domain research/reviews.

CampaignCombatActualReactionCombatEntry and its codec are private technical modules within Umpire.
The [Reaction-to-Combat entry contract](docs/specs/combat-actual-reaction-combat-entry-v1.md) uses existing
Movement stop/resolution/completion and System Breakdown names. A certified positive candidate at entry
is distinct from accepting a selection, paid attack or public action.

CampaignCombatActualMovedAssault and its codec are private Umpire modules for the
[actual moved-assault contract](docs/specs/combat-actual-moved-assault-v1.md). They join authenticated
Reaction-to-Combat entry to Selection, sealed Round, paid assault and mandatory settlement.
Reserve Release entry is their endpoint; the name does not imply public campaign activation.

CampaignCombatActualMovedContinuation and its codec are private Umpire technical modules for
[paid and armed continuation assessment](docs/specs/combat-actual-moved-continuation-v1.md). They
complete empty Reserve Release and retain prospective witness facts. The module name does not imply
a cycle-control offer, executable repeat, phase finish or public campaign activation.

CampaignCombatActualMovedCycleControl and its codec are private Umpire technical modules for
[actual moved cycle control](docs/specs/combat-actual-moved-cycle-control-v1.md). Finish denotes actual
closure of the current Movement and Combat phase with same-slot Truck Convoy entry. Pending repeat
intent denotes a source-bound non-executable request; it does not imply ordinal2 publication or
progress reset. The module name does not imply public campaign activation or executable Repeat.

`CampaignCombatActualMovedRepeatMovement` and its codec are private Umpire modules for
[authentic moved repeat Movement](docs/specs/combat-actual-moved-repeat-movement-v1.md).
Repeat publication means consuming the real pending intent once and opening ordinal2 Movement;
it does not rename or upgrade the historical pending intent into executable authority.
Movement here covers ordinary owner moves only. Stop, resolution, completion, later phases and
public campaign actions require separate lineage-aware integrations.

`CampaignCombatActualMovedRepeatMovementCompletion` and its codec name the separate private
[actual moved repeat Movement completion contract](docs/specs/combat-actual-moved-repeat-movement-completion-v1.md)
(R02); the R01 family above remains move-only. R02 authenticates 10 independent ledgers. True-zero
completion denotes one owner event; positive completion denotes owner stop, empty System
resolution and owner completion, three events. Historical ordinal1 and receipt-bound ordinal2
proofs remain distinct. Entry to Breakdown Determination does not imply Breakdown execution.
Local 325-row validation and independent implementation review at `07fac9f` are complete.
[PR #194](https://github.com/dills122/sandtable/pull/194) merged at `121baa5`;
[main CI run 38830848442](https://github.com/dills122/sandtable/actions/runs/38830848442), attempt 1,
verified all 3,716 rows across 26 execution processes. These technical module
names do not imply public activation, Runner adoption, acceptance of all 72 criteria or a first
release candidate.
