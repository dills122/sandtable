# Sandtable Naming and Domain Vocabulary

Sandtable draws on historical staff work, Kriegsspiel and campaign administration. Use flavor names
for major products and domains, and plain technical names inside modules. A name reserves a
responsibility; it does not imply a deployed service or implemented mechanic.

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

**Command** chooses strategic objectives, whether supplied by a person, scripted policy or optional
model. **Staff** converts intent into concrete deterministic plans. The Umpire validates and executes
accepted orders. **Intelligence** supplies untrusted proposals, personas, explanations and narrative;
it never owns campaign state or rules.

**Dispatch** is a domain order/report message. **Signals** is the transport carrying it. The protobuf
contract lives in `Cna.Intelligence.Contracts`; preserving message versions and wire compatibility
is a transport responsibility, while order legality stays in Core.

**Chronicle** is authoritative event history and replay. **War Diary** is its derived human-readable
account. **Archives** is campaign storage, snapshots and saves. A replay checkpoint does not by itself
constitute a durable player save/resume product.

**Maproom** is the future player application: map, orders, legal actions and audience-safe reports.
`site/` is the existing project website, with labeled concept illustrations. The first Maproom target
is local hot-seat play; it must use the same typed, side-safe action boundary as every other client.

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
