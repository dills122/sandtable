# Sandtable Naming and Domain Vocabulary

**Status:** Active vocabulary rationale. Current implementation notes and future reserved names are
labeled in their sections. The [pre-alpha roadmap](docs/roadmap/pre-alpha-roadmap.md#current-delivery-status)
owns current delivery status; checkpoint references here explain how accepted names map to the
domain and are not a separate completion ledger.

**Sandtable** draws its naming theme from historical staff work, Kriegsspiel, military
administration, and campaign terminology without making the codebase incomprehensible.

The governing rule is:

> Flavor names for major products/services; boring technical names underneath them.

So `Umpire` can be the deterministic simulation component, but internally it still contains `CombatResolver`, `MovementValidator`, etc.

## Proposed Sandtable vocabulary

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

There are some particularly nice relationships hiding in there.

---

# 1. Umpire — the heart of Sandtable

I **really** like this one.

Historically, the Kriegsspiel umpire knew the true situation, received orders, determined their effects, rolled/resolved uncertainty, and told players what they were allowed to know.

That's remarkably close to our deterministic kernel.

```text
                         Umpire

                     authoritative state
                            │
              ┌─────────────┼─────────────┐
              ▼             ▼             ▼
            Rules          RNG        Fog of War
              │             │             │
              └─────────────┼─────────────┘
                            ▼
                     Game Events
```

I'd make **Umpire explicitly sacred** architecturally:

> If Umpire doesn't say it happened, it didn't happen.

The LLM cannot modify state. Staff cannot modify state. UI cannot modify state. Orleans cannot invent state.

Everything ultimately becomes:

```text
Command → Umpire → Events
```

That's an excellent architectural principle _and_ flavorful terminology.

The first implementation maps these names conservatively: the internal
`Cna.Core.Campaigns.CampaignEngine` decides mechanic commands, campaign event records are the
Chronicle's authoritative input, and internal projection/replay reconstruct snapshots. Those plain
technical names stay inside the Umpire boundary; they are not additional products or services.
Public callers hold only a `CampaignAuthorityHandle`, inspect one side through Campaign Observation,
and submit a current typed Legal Action. The Umpire now resolves Operation Stage 1 Weather through
explicit Initiative, Naval Convoy Schedule, Tactical Shipping, Initiative Declaration, and Weather
events; resolves the admitted empty Organization, Naval Convoy Arrival, Fleet Assignment, and Fleet
Repair obligations through four distinct event types; then adjudicates the first-acting side's
Reserve Designation, executes supported observation-derived non-contact Movement, and completes to
the Breakdown Determination checkpoint. The authoritative `ActiveSide` remains unset at both
relative-actor positions; observation and legal-action audiences derive the acting side from the
recorded Operation Stage order.
Walking the published sequence catalog for inspection does not create Chronicle history.

For the CNA rules domain, use **initiative holder** only for the side that wins or is assigned
Game Turn initiative. Use **first-acting side** and **second-acting side** for each Operation Stage.
They are deliberately not aliases: the initiative holder chooses first or last separately for
each stage. This plain vocabulary prevents a later rules decision from being hidden behind a
generic `FirstPlayer` name.

Use **Reserve Designation** for the decision phase, **Reserve I** and **Reserve II** for the rules'
closed status vocabulary, and **completion** for the explicit event that ends designation. Do not
call completion a generic sequence advance: the Umpire emits `ReserveDesignationCompleted` and
projects the exact first-acting-side Movement successor. Movement behavior and the later Reserve
lifecycle remain separate capabilities.

The [inherited successor packet](docs/specs/combat-inherited-successors-v1.md) keeps
`reserve-designation-completed` as the atomic first-cycle opening event; it introduces no separate
product or service. Its private `OpeningState` is an oracle projection, not an Archives snapshot.
The [opening preamble packet](docs/specs/combat-opening-preamble-v1.md) similarly uses private
`PreambleState` for creation-rooted contract replay through Weather entry. The
[Weather packet](docs/specs/combat-weather-v1.md) uses private `WeatherState` through Organization
entry and retains the `weather-determined` successor name. The
[stage-entry packet](docs/specs/combat-stage-entry-v1.md) uses private `StageEntryState` through Reserve
entry and retains the four `no-obligation-…-resolved` event names. The
[Reserve designation packet](docs/specs/combat-reserve-designation-v1.md) uses private `ReserveState`
through atomic first opening, retaining `reserve-element-designated` and
`reserve-designation-completed`. The [inherited Movement packet](docs/specs/combat-inherited-movement-v1.md)
uses private `InheritedState` and freezes `element-moved`4; it remains distinct from D2a
`combat-cycle-element-moved`1. Actual progress references and moving routes are authority data,
not new products. The [inherited route lifecycle](docs/specs/combat-inherited-movement-lifecycle-v1.md)
uses private `LifecycleState`, retaining `element-movement-stopped`2, `breakdown-stop-resolved`2
and `movement-segment-completed`3. The [Breakdown completion packet](docs/specs/combat-inherited-breakdown-completion-v1.md)
uses private `CombatEntryState` and retains `breakdown-segment-completed`2; reaching that position
does not admit Combat actions. Task008 G2 implements this full-history transition with distinct
Reserve, Movement and Breakdown completion receipts. Suspended context and Movement-end proof remain authority data.
The [inherited selection packet](docs/specs/combat-inherited-selection-v1.md) uses private
`AdmissionBoundary` and `Control` projections, retaining `combat-segment-opened`2 and
`combat-selection-closed`2. Its supported zero-candidate result creates no decision and does not
mean the Combat segment or its structural steps are complete.
The [inherited no-attack packet](docs/specs/combat-inherited-no-attack-v1.md) nests that `Control`
unchanged and uses its own `Control` projection plus six `combat-step-completed`2 receipts. Outer
closure means structural arrival at Reserve Release; it does not rename or perform Reserve Release.
Task008 F1 implements the bounded trigger with persisted Reaction identities; public capability handles
belong to participant admission. F2 keeps `reacting-element-moved`3,
`reaction-participant-completed`3, `breakdown-stop-resolved`2 and `reaction-window-closed`3
as distinct causal events; completing a participant does not close its window. F3 direct closure
uses the same close3 family for player decline, scripted-unavailable and timeout, with reason derived
from command kind; direct closure creates no reactor stop. F4 active fallback instead records
`reactor-stop-closed` with `reaction-unavailable` or `reaction-timeout`, and a distinct
`breakdown-stop-resolved`2 resumes phasing Movement. F5 retains `reacting-element-moved`3 for
the second reactor move; its existing route stays open and its track extends to supply.
F6 then retains explicit completion3, stop resolution2 and noeligible closure3; only closure
resumes phasing Movement, and resolved opportunity IDs differ from the empty closed-ID list.
The accepted [inherited Snapshot12 contract](docs/specs/combat-inherited-snapshot-v1.md) defines literal retained roots.
Its `reserve-designation` and `inherited-cycle` tags describe typed `cycleState` arms;
`InheritedCommandReceipt` names the existing preamble receipt grammar. These are persisted data
shapes, not new services or public actions. Accepted H4 provides bounded Initial H runtime restore; later families retain their own gates.
H1's `CampaignCombatRetainedHistory` and `CampaignCombatHistoryProjection` are internal evidence
and typed replay results. `CampaignCombatHistoryReplay` derives causal family partitions from one
retained stream. H2 adds typed Movement, MovementLifecycle and BreakdownCompletion projections
under that same router. H3 adds corresponding typed Reaction projections; these names do not
introduce a public restore endpoint or new service. H4 adds `CampaignCombatInheritedSnapshotV12Codec`
for literal inherited roots and retained restore; `CampaignCreationSnapshotV12Codec` stays creation-only.
The [inherited Reaction-trigger packet](docs/specs/combat-inherited-reaction-trigger-v1.md) uses
private `TriggerState`, retains `element-moved`4 and opens one identity-bound `ReactionWindow`.
`ReactingPosition` suspends Movement; it is not a participant choice, move or window closure.
The [inherited Reaction-lifecycle packet](docs/specs/combat-inherited-reaction-lifecycle-v1.md) uses
private `LifecycleState` for one `reacting-element-moved`3, explicit participant completion3,
compatible empty-stop resolution2 and no-eligible closure3. `reactor-stop-open` is mandatory
authority, while terminal `moving` resumes the prior phasing route rather than naming a new route.
The [inherited direct Reaction-closure packet](docs/specs/combat-inherited-reaction-closure-v1.md)
reuses compatible `reaction-window-closed`3 for `player-decline`, `scripted-unavailable`, and
`timeout`. Distinct action identities select reason and actor authority; Core does not schedule time.
The [inherited active Reaction-fallback packet](docs/specs/combat-inherited-reaction-active-fallback-v1.md)
uses `reactor-stop-closed` after System unavailable/timeout closes an active participant. Closed
means window authority is gone, not that Breakdown adjudication is skipped; `breakdown-stop-resolved`2
must restore the retained phasing `moving` route.
The [inherited active Reaction second-move packet](docs/specs/combat-inherited-reaction-second-move-v1.md)
uses the existing `reacting-element-moved`3 family for another step by the same active participant.
“Second move” changes current World/route/track location and cumulative CP; it does not mean a
second opportunity, new route, participant completion, or window closure.
The [inherited Reaction movement-completion packet](docs/specs/combat-inherited-reaction-movement-completion-v1.md)
uses existing `reaction-participant-completed`3, `breakdown-stop-resolved`2, and
`reaction-window-closed`3 families after that second move. “Movement completion” here closes the
active Reaction participant episode and resumes phasing; it is not a phasing Movement-segment end.
The [inherited Reserve-cycle packet](docs/specs/combat-inherited-reserve-cycle-v1.md) reaches
`reserve-release` without performing Release. The
[inherited Reserve Release packet](docs/specs/combat-inherited-reserve-release-v1.md) retains
`reserve-release-opened`1, `reserve-unit-disposition-recorded`1, and
`reserve-release-completed`1. A `pending next-Movement exception` is scoped authority for ordinal2,
not a move, route, destination, or generic permission token. `release-I` changes the matched own
member from Reserve I to none; `complete` closes only the current Release window. Neither term
implies cycle repeat, phase finish, or production activation. The
[inherited armed-continuation proof](docs/specs/combat-inherited-armed-continuation-v1.md) names a
source-legal, fully supported prospective Combat path for that released-I profile. “Continuation”
does not mean opportunity selection, Combat execution, cycle repeat, or runtime admission.
The [inherited guarded cycle-control packet](docs/specs/combat-inherited-cycle-control-v1.md) then
uses `movement-combat-cycle-repeated` or `movement-combat-phase-finished` for exact owner closure.
This proves private authority composition only; it does not mean ordinal-2 Movement or Combat ran.
The [round-v2 contract](docs/specs/combat-sealed-round-v2.md) calls its immutable public timing
bound the `openingFloor`. Accepted private seal times are audit evidence, not a shared admission
watermark. [Corrected side profiles](docs/specs/combat-side-projection-v1.md) name a separately
authenticated contract family; the name does not register or activate production Combat.
The [inherited released-I Movement packet](docs/specs/combat-inherited-reserve-movement-v1.md) then
uses `combat-cycle-element-moved` for one actual ordinal-2 move. “Pending exception” remains true
until an accepted Movement-completion receipt expires it. The
[inherited released-I Movement-completion packet](docs/specs/combat-inherited-reserve-movement-completion-v1.md)
uses profile-specific `combat-cycle-element-movement-stopped`1,
`combat-cycle-breakdown-stop-resolved`1, and `combat-cycle-movement-segment-completed`1 for that
closure. These adapt the established lifecycle semantics without claiming compatibility with the
richer first-cycle envelopes. “Expired exception” means the exact D2b.2 projection bound the
one-cycle authority to that accepted completion receipt; it does not mean Breakdown ran.
These projections are not Snapshot12. Chronicle retains the
accepted events; the future Archives reader must reconstruct and validate their complete chain.
Task009A introduces `CampaignCombatParticipant`, `CampaignCombatCandidateAssessment` and
`CampaignCombatAdmissionBoundary` as internal identity/assessment values. `CampaignCombatCertification`
derives supported inherited admission from replay; binding a participant alone does not certify
combat eligibility. No new service or public capability is introduced.

The prospective [cycle-control contract](docs/specs/combat-cycle-control-v1.md) uses
`movement-combat-cycle-repeated` for closing one occurrence and opening the next Movement in the
same relative player slot, and `movement-combat-phase-finished` for entering that slot’s Truck
Convoy phase. Neither name implies stage housekeeping or production runtime admission.

---

# 2. Maproom — the player-facing application

This one also feels basically settled to me.

**Maproom** is where the player experiences Sandtable.

```text
MAPROOM

┌──────────────────────────────────────────────────────────┐
│ NORTH AFRICA       17 DEC 1940       OPERATION STAGE II │
├───────────────────────────────────────────┬──────────────┤
│                                           │ GENERAL STAFF│
│                                           │              │
│                MAP                        │ Orders       │
│                                           │ Reports      │
│             units / fronts                │ Supply       │
│             supply routes                 │ Intelligence │
│                                           │              │
│                                           │              │
├───────────────────────────────────────────┴──────────────┤
│ DISPATCHES                                                │
│ 08:10  7th Armoured reports contact near Sidi Barrani   │
│ 08:25  Supply column delayed                             │
└──────────────────────────────────────────────────────────┘
```

Then we can say:

> Open the campaign in Maproom.

That's much nicer than "open the frontend."

The static project website under `site/` is deliberately **not** Maproom. It introduces Sandtable
to developers and prospective players, explains how the eventual game is intended to work, and
links into the repository. It cannot open a campaign, inspect authoritative state, or issue an
order. Reserve **Maproom** for the future side-safe player application so the flavor name continues
to communicate an actual authority boundary rather than any web page with a tactical motif.

For future complex decisions, Maproom may be prompt-forward without becoming a chatbot or authority
boundary. A few contextual suggested approaches, natural language, map/list interaction, and direct
fields edit one visible private typed intent draft. The player corrects and confirms that draft
before Staff planning, then separately confirms the exact current legal order before the Umpire
adjudicates. See the proposed [Player Intent Composer v1](docs/specs/player-intent-composer-v1.md).

---

# 3. Staff — deterministic/scripted planning

This should be separate from Command.

I think this distinction is important:

```text
COMMAND
"What should we accomplish?"

        ↓ intent

STAFF
"How can we accomplish that?"

        ↓ concrete orders

UMPIRE
"What actually happens?"
```

For example:

### Command

> Hold Tobruk, preserve armor, and avoid exhausting our water reserves.

### Staff

Determines:

- Which formations hold which sectors
- Where reserves go
- Which units need supply
- Truck allocation
- Artillery positioning
- Withdrawal routes
- Engagement thresholds

### Umpire

Determines:

- Whether movements are legal
- Actual supply expenditure
- Breakdown results
- Combat
- Enemy reactions
- Resulting state

This is an extremely clean separation.

---

# 4. Command — strategic decision system

**Command** can encompass either:

```text
HumanCommander
ScriptedCommander
AICommander
ReplayCommander
```

Then personalities belong to Command:

```text
CommanderProfile
├── aggression
├── caution
├── logistics discipline
├── initiative
├── force preservation
├── doctrine
└── narrative persona
```

I wouldn't call the LLM itself "Command."

Rather:

```text
                    Command

             ┌─────────┼─────────┐
             ▼         ▼         ▼
           Human    Scripted      AI
                               │
                               ▼
                         Intelligence
```

**Intelligence assists Command. It doesn't command.**

That's another useful conceptual boundary.

---

# 5. Dispatch — commands and reports

This might be the most fun terminology.

Historically, commanders communicated through dispatches.

In Sandtable, a Dispatch is something flowing between actors/components.

Two broad varieties:

```text
Orders
Commander → Umpire

Reports
Umpire → Commander
```

For example:

```json
{
  "dispatch": "order",
  "from": "western_desert_force",
  "issuedAt": "1940-12-10T06:00",
  "intent": "advance",
  "formation": "7th_armoured",
  "objective": "sidi_barrani"
}
```

Then results become reports:

```text
REPORT

FROM: 7TH ARMOURED DIVISION
TIME: 1420 HOURS

Advance halted approximately six miles east
of Sidi Barrani.

Enemy armor sighted.

Fuel remaining: 61%
Water remaining: 2.8 days.
```

You could even make **Dispatches a first-class UI concept** in Maproom.

---

# 6. Signals — the communications infrastructure

I'd distinguish this from Dispatch.

**Dispatch = message/domain concept**

**Signals = transport/infrastructure**

That's historically appropriate too. Military signals organizations handle communications.

So:

```text
Sandtable.Signals
```

could contain:

```text
gRPC contracts
protobuf definitions
service discovery
serialization
streaming
client/server transport
```

A `Dispatch` travels through `Signals`.

That's wonderfully nerdy while still making sense.

---

# 7. Chronicle — authoritative event history

This is another one I'd strongly adopt.

```text
Chronicle
─────────

000381 UnitMoved
000382 FuelConsumed
000383 ContactEstablished
000384 ReactionDeclared
000385 BarrageResolved
000386 CohesionReduced
```

**Chronicle is machine truth.**

Then separately:

### War Diary

Human-readable narrative derived from Chronicle:

> **17 December 1940**

> The 7th Armoured Division continued its advance westward during the morning operation stage. Contact was established with elements of the Italian 10th Army...

That gives us:

```text
Chronicle → exact replay/audit
War Diary → human history
```

Very clean distinction.

---

# 8. Archives — storage

Chronicle records events.

**Archives** stores:

```text
campaigns
snapshots
completed games
exports
war diaries
replays
scenario results
```

So:

```text
Umpire produces Chronicle.
Chronicle is stored in Archives.
Maproom reads Archives for replay.
```

---

# 9. Quartermaster — logistics

We absolutely have to use this somewhere.

Especially considering our first game is CNA.

```text
Quartermaster
├── Supply
│   ├── water
│   ├── fuel
│   ├── ammunition
│   └── stores
│
├── Transport
│   ├── trucks
│   ├── rail
│   └── convoy
│
├── Depots
├── Consumption
└── Resupply
```

But architecturally it's still part of Umpire:

```text
Umpire
├── Operations
├── Quartermaster
├── Combat
├── Air
├── Naval
└── Weather
```

I wouldn't make Quartermaster a microservice.

It's a **domain module**.

---

# 10. Theater — map/environment

Instead of `World` or `MapState` at the high level:

**Theater**

contains:

```text
Terrain
Hexes
Roads
Rail
Ports
Airfields
Settlements
Borders
Weather regions
Off-map areas
```

Then a campaign operates within a Theater:

```text
Theater
    North Africa

Campaign
    Operation Compass

Scenario
    Graziani's Offensive
```

That terminology scales beautifully if Sandtable ever supports other games.

Use five related terms precisely in the implementation:

| Term | Meaning |
| --- | --- |
| **Content Pack** | Immutable, versioned static topology, force structure, and scenario definitions with independent canonical identity. It is a Core domain artifact, not a service or player DTO. |
| **Scenario** | One playable temporal/deployment definition inside a Content Pack. A scenario does not itself create or mutate a campaign. |
| **Setup** | Campaign admission policy selecting recognized rules, content/scenario identity, and any still-separate initialization policy. The creation command supplies the per-campaign seed; Setup is not a synonym for Content Pack. |
| **Campaign World** | Authoritative mutable runtime facts projected from an exact setup and scenario. The delivered capability began with element locations and now also carries Reserve status, exact Operation-Stage expenditure/Cohesion, and opaque internal map-representation bindings while joining static facts through stable Content Pack IDs. |
| **Content Context** | Runtime-only, already-resolved exact Content Pack and selected scenario supplied to authoritative decision/replay. It is not campaign state or a transport DTO. |
| **Campaign Observation** | Immutable side-safe derived view for one authorized side. It copies only approved public and own-force facts, carries no complete Content Pack identity, and is never authoritative state or trusted history. |
| **Campaign Authority Handle** | Opaque Core-issued reference to admitted authoritative state plus exact resident content context. It can be passed to safe facades but not inspected, serialized into authority, deconstructed, or mutated directly. |
| **Legal Action** | Immutable typed candidate currently available to one exact system or side audience. Its deterministic ID is identity only; submission must re-derive membership against the current authority handle. |

Presentation labels and original visuals are separate from authoritative Content Pack identity.
The **Theater** is the runtime geographic/world concept assembled from exact content plus campaign
state; it is not the serialized source scan or the pack catalog.

---

# 11. ORBAT — forces and formations

We have a legitimate military term available here: **Order of Battle**.

Usually abbreviated **ORBAT**.

It describes the organization, command structure, disposition, strength, and equipment of military forces.

Perfect for:

```text
ORBAT
├── Nations
├── Forces
├── Formations
├── Units
├── Equipment
├── Command hierarchy
└── Reinforcement schedules
```

I'd probably use `Orbat` in code:

```csharp
Orbat
OrbatDefinition
FormationDefinition
UnitDefinition
```

and display **Order of Battle** in Maproom.

---

# 12. War College — AI experimentation

This one is too good not to reserve.

Not part of the core gameplay.

The names now have a concrete local implementation boundary: `Cna.ExerciseRunner` runs either one
**Exercise** or one serial **Maneuver** through an in-process opaque Umpire capability and retains
trusted evidence. An Exercise is one authoritative simulation run. A Maneuver is an ordered
collection of uniquely identified Exercises that share one parent seed contract, run one child at
a time, and produce one deterministic aggregate report after strict child readback. The frozen
`serial-unpaired` contract uses manifest v2 and report v1. The separate optional `serial-paired`
contract uses paired manifest/report v1 and runs isolated baseline then candidate arms from identical
declared inputs, initial role-specific random streams, campaign creation inputs, build cohort, and
initial snapshot. Its comparison remains descriptive: it records first divergence and bounded
count/outcome deltas but makes no causal, statistical-significance, gameplay-balance,
recommendation, or synchronized-post-divergence claim. Compact, forensic, and debug detail tiers
describe Exercise instrumentation depth, not different game rules; Maneuver timing and path
diagnostics likewise stay outside its deterministic fingerprint. Failed decisions retain the
attempted query, controller, action, and submission context available before the failure. **War
College** remains a later evaluation layer, and the current runner is not an Orleans workload,
tournament system, parallel scheduler, or balance-analysis environment.

[HOST-RSH-001](docs/research/orleans-publication-feasibility.md) is a separate research probe using
a local Orleans silo and injected memory storage. It leaves Umpire adjudication and Chronicle
history ownership intact; it does not turn Runner into campaign hosting or activate durable Archives.

Closed controller-policy names describe deterministic test behavior, not commanders or authority.
The checked policy matrix says exactly `act-first`/`act-last` and Reserve `none`/`one`/`all`; its
runner-only accepted-designation count is controller history, never campaign state. These policies
produce six Movement-entry Exercises inside one Maneuver and do not make the runner the Umpire.
The checked Movement policies append `move-each-once-then-complete`. The additive
`move-each-once-by-lowest-cost-then-complete` name is explicitly a trusted simulator-selection
instrument: it orders an element's current public legal moves by exact cost and is neither a Core
rule nor a recommended player strategy. The Truck study adds
`act-first-reserve-all-move-each-once-by-lowest-cost-then-complete` and
`act-first-reserve-all-repeat-highest-cost-stops-then-complete`. The latter repeats legal moves,
stops after every edge, and lets the current System action resolve each pending stop. These names
describe bounded runner choices; **Stop Element Movement** and **Resolve Breakdown Stop** remain
Umpire actions with authoritative results.

The implemented Movement Foundation keeps **representation** as Umpire truth and exposes only an
**apparent presence** to the opposing side. A representation is the authoritative map piece and
its hidden binding; an apparent presence is the side-safe fact that a player may use when choosing
a move. Neither is a **Contact** yet: the accepted `CONTACT-001` lock derives Contact from
enemy-ZOC presence at the beginning of a Movement Segment and makes Engaged a Close Assault result;
their production contracts remain gated behind the approved dependency-ordered ZOC/Reaction
spec/design slices.
[Combat side projections](docs/specs/combat-side-projection-v1.md) now retain accepted own settlement
facts and apparent opposing representation locations in a separate version2 contract. An **own
acceptance receipt** records an actual accepted player choice; a System fallback creates none, even
when a player submitted the triggering proposal. Version3 adds own Reserve and cycle facts. An
owner complete-release intent can earn acceptance through authenticated native effect even when
System authors its completion event. Historical terminal profiles provide exact checkpoint facts with
no actions; corrected composition profiles preserve continuous own receipts/history through finish.
These contract profiles are accepted; runtime remains gated.
A [private Exercise checkpoint](docs/specs/combat-exercise-occurrence-v1.md) records an authenticated
source prefix and exact current authority state. **Source occurrence** identifies the cycle being
proved; **active occurrence** identifies the current cycle after continuation. These identities
can differ after repeat and are distinct from registered CoreSnapshot identity.
A [private Exercise child](docs/specs/combat-exercise-child-evidence-v1.md) binds one configured
execution to its native source, accepted transitions, terminal result and separate verification
proofs. Input initiator, native event author and semantic action remain distinct; a rejected owner
proposal can trigger a System fallback. Expected failure remains a failed result. This contract
evidence is accepted. A [private Exercise parent](docs/specs/combat-exercise-parent-evidence-v1.md)
aggregates authenticated children and compares actual audience/action streams only when their initial
state and required configuration agree. **First divergence** marks the earliest unequal action; it
does not establish a cause or align later randomness. The
[outward integration index](docs/specs/combat-outward-composition-v1.md) reconciles those private
contract terms with all72 planned runtime requirements. Runtime artifact publication remains pending.
**Selected Combat Rules** now name dormant C# definitions and pure explicit-dice arithmetic.
**Captured TOE** is a subset of eventual losses, while **Raw Engaged** and required retreat remain
separate result facts. These helpers do not advance a campaign or consume authoritative randomness.
**RulesInput1** is the same frozen selected-rules artifact now emitted and checked by its dormant
C# codec; creating it does not register a new live ruleset version.
**Combat Content7** names the dormant `sandtable.content-json.v6` package for the certified
`close-assault-positive-v1` synthetic scenario. **Initial Ammunition**, **Initial Combat Readiness**
and **Retreat Supply Anchor** are immutable scenario seeds/direction facts, not live balances,
resupply authority or gameplay activation. **Combat World7** now names the dormant typed creation
state built from certified Content7: original elements, map representations, cause history and
distinct custody/guard/replacement/future-obligation records. Guard TOE transfer is not a new
component or current balance on the donor. Exact validation precedes creation; Task008 owns
canonical snapshot/history persistence, and gameplay activation remains later.
**Relationship settlement** records only the original assault participants; guards do not inherit
Contact or Engaged. **Round closure** proves all immediate settlement receipts are present;
**Close Assault completion** then arrives at Reserve Release. These dormant Task016 names do not
mean Reserve Release executes, future prisoner duties run, or public Combat is activated.
**Creation Request1** fixes campaign identity, seed and trusted artifact selections before World
construction. **Created11** carries the resulting dormant creation event; **Creation Binding** is
its nonrecursive request-derived identity, not an event hash or public authority handle. Retry
selection returns retained canonical bytes; it is not Chronicle publication.
**Creation Snapshot12** names the creation-only readback value derived from trusted Created11.
**Creation Receipt1** binds request, creation identity and exact event hash; **Chronicle Prefix**
frames the creation event with its byte length. Neither checksum authenticates imported evidence
without independently trusted request/artifacts, and this reader cannot restore later game state.

**PreambleState1** is the private creation-rooted opening replay projection through Weather entry.
Its receipts identify accepted causal occurrences; retry returns the original event and current
projection. It is not a general Snapshot12 or a published Chronicle head.

**WeatherState1** extends the private opening projection with retained Weather1 and its advanced
RNG cursor. It requires trusted creation and complete opening/Weather history; it cannot independently
certify a campaign state or stand in for Snapshot12.

**StageEntryState1** preserves WeatherState1 fields through four explicit-none stage gates.
Its Reserve-entry position keeps null ActiveSide; retained turn/stage order supplies the eventual
designating side. The private projection requires complete creation-to-stage history.

**ReserveState1** adds first-side ownership, own members and designation history to retained stage
state. D1 supports only pre-completion cuts: cycle, cycle ID, opening-base hash and completion receipt
remain null. **Reserve I designation** changes only the accepted own member’s Reserve status.
**Reserve completion evidence** (D2) binds canonical completion2 bytes and cycle authority to actual
creation-rooted history. Its **OpeningBase** carries the frozen compatibility profile literal; it is
not an admission path. Task019A applies that same event to terminal **ReserveState1**, retaining
precompletion World/member authority while publishing cycle, position, prefix and receipt together
within the private projection. This is not durable Chronicle publication.
**Inherited MovementState1** adds chronological tracks, actual move-progress references and nullable
Breakdown flow to full Reserve history. Move4 makes flow moving even without vehicles; **idle** requires
an accepted stop/resolution pair. Ordered routes preserve revisits. **LifecycleState1** retains that
moving history plus captured **interruptContext** and actual **MovementEndProof**. Movement completion
and its proof receipt remain distinct from Reserve completion. Empty-cohort resolution is still a
real System event; an idle cache cannot substitute for it.

**Capability Point expenditure** is the exact amount already spent during the current Operation
Stage; it is not a replenishing UI movement allowance. **Complete Movement** is a real player
decision that advances to Breakdown Determination, not a runner stop condition. These names are
approved for Movement Foundation v1. The source/ruling lock and exact Rules
foundation are complete, including stable `land.mobility.non-motorized` and
`land.mobility.motorized` identifiers. Content schema 4 now assigns one of those IDs explicitly to
each element and adds the approved synthetic Truck Point cohorts. Opaque
`map-representation.0001`-style identities now name the Umpire's physical-map
representations without embedding real element IDs; their bindings never appear outward. Contract-5
Campaign Observation now exposes only the approved apparent representation/location/current-ZOC
shape alongside exact own Movement/Breakdown facts. `MOV-TASK-004B` implements the minimum
Breakdown Point Rules/Content/World continuity seam, `MOV-TASK-005` implements its side-safe outward
projection, and `MOV-TASK-006` freezes dormant typed **Move Element** and **Complete Movement
Segment** outputs with deterministic identities and explanatory exact costs. **Route adjustment**
means either an exact Road override of destination-terrain cost or an exact Track scale of that
terrain cost; crossed-hexside additions are then applied separately;
**hexside addition** names an ordered Ridge or directional Slope increment. These output contract
names now have internal authoritative move/event semantics and observation-derived public legal
actions. `MOV-TASK-008` publishes exact move/completion membership and submission through the
Breakdown Determination checkpoint; merged `MOV-TASK-009` supplies checked Exercise/Maneuver
evidence and merged `MOV-TASK-010` / PR #79 completes synchronization and review. **Breakdown continuity** names
replay state and rules
identity. At that historical Movement milestone, no roll, result, loss or Movement BP mutation
was implemented; the current Breakdown package supplies those mechanics.
**Enemy ZOC** is a Umpire-derived board fact; authoritative adjacency to a non-phasing represented
combat element after a committed phasing combat-element move is the **Reaction trigger**, even when
that adjacent combat representation does not exert a
positive ZOC. A **Reaction window** is the approved persisted non-phasing interrupt, and a
**Reaction participant episode** is one selected participant's one-or-more steps followed by
completion. The five `CONTACT-001` rulings for ordering, repeat eligibility, decline scope, waiting
visibility, and positive-ZOC authority are accepted inputs. Their approved production contract is
the [ZOC/Reaction specification](docs/specs/zoc-reaction-v1.md) and
[technical design](docs/design/zoc-reaction-v1.md). An **apparent enemy-
controlled location** is the approved side-safe aggregate fact that a location is controlled by at
least one apparent enemy ZOC source; it exposes neither which presence controls it nor why. Immediate enemy-ZOC
entry creates neither **Contact** nor **Engaged**: Contact
is derived from enemy-ZOC presence at the beginning of a Movement Segment, while Engaged is a Close
Assault result. The repeatable **Movement/Combat cycle** remains a Sprint 5 research-gated domain
concept. Movement is complete through `MOV-TASK-010`; `ZOR-TASK-002A`-`003A` complete dormant Rules
vocabulary/predicates, Content component/seed/readback, checked static fixtures, and direct-only
World 5/creation 9 current-TOE and Reaction identity seams without activating ZOC/Reaction or
changing ruleset 7 / Content schema 4 / World 4 / CampaignCreated 8. `ZOR-TASK-003B`-`004C` add
dormant Snapshot/event replay, the side-safe Observation 6 policy/history surface, exact own
Movement-ended and Reaction-opportunity membership, and closed topology-local ordinary Movement,
Reaction-step, completion, decline, and System-close action identities/readback/mappings, plus the
manifest-registered user-space declassification boundary that publishes audience-safe
window/state-scoped capability handles and move options rather than authoritative identities or raw reacting
element fingerprints. Admission recomputes those capability handles, verifies cost claims against
the published edge traversal, and rejects decision labels inconsistent with the observer and active
side. `ZOR-TASK-005` adds direct-only authoritative adjacency triggering, frozen local
opportunities, and topology-local ZOC entry/exit reconstruction. `ZOR-TASK-006A` adds direct-only
reason-specific `ReactionWindowClosed` authority with exact Movement resumption. `ZOR-TASK-006B`
adds direct-only `ReactingElementMoved` and `ReactionParticipantCompleted` authority for atomic
first selection, later active steps, and explicit participant resolution. `ZOR-TASK-006C` activates
these successor terms together on the public Core creation, observation, action, checkpoint, and
replay paths. `ZOR-TASK-007A` Runner adoption adds bounded Reaction controllers that select
public capabilities by action ID, close participant episodes or windows explicitly, and preserve
Umpire-owned costs on final Movement completion. A historical fifteen-child Rules 8 Maneuver
retains trusted authority evidence; `007B` strict readback reconstructs and re-adjudicates its
Reaction events. That historical package was verified through matching clean runs and a Ready
independent review. The subsequent Umpire package is [Breakdown adjudication](docs/specs/breakdown-adjudication-v1.md).
Owner accepted its decisions; Task 001 freezes movement routes, bounded pending stops, check evidence
and persistent broken-vehicle lots. Task 002 supplies dormant outcome Rules and exact loss arithmetic;
[Task 003](docs/research/breakdown-campaign-contracts.md) adds dormant campaign contracts and a certified Truck fixture.
[Task 004](docs/research/breakdown-move-accounting.md) adds shared BP accounting and dormant move replay.
[Task 005](docs/research/breakdown-stop-adjudication.md) adds stop resolution, persistent loss lots, explicit Reaction continuation and dormant replay to Combat entry.
[Task 006](docs/research/breakdown-public-activation.md) activates those contracts, side-safe Breakdown waiting,
owner lot summaries and current history/readback. Public authority stops at first-side Combat;
Task 007 implements fourteen checked successors and a Truck study. Its thirteen-child Reaction
successor retains bounded battalion episodes and recurrence; the two positive-ZOC children remain
historical/deferred. **Certified battalion input** may contain zero vehicle cohorts. The
`land.breakdown-cohorts` capability is declared exactly when static cohort data exists, while the
profile's independent formation and stacking bounds remain mandatory. Truck convoy movement is
distinct from combat/Reaction eligibility; the profile still defers public positive ZOC and
motorized-infantry losses. [Task 007 closeout](docs/research/breakdown-runner-closeout.md) tracks
verification, a passing full gate and two matching clean runs. [Review 5](docs/reviews/brk-followup-review-5.md) accepts the BRK-AC-009
current-version transcript/progress follow-up, completing bounded Tasks 006–007. Its status-only
follow-up is corrected; broader capabilities remain deferred.

**War College** is where we evaluate commanders.

```text
WAR COLLEGE

Scenario: Operation Compass
Games: 10,000

Commander A
  Aggressive Utility Bot

Commander B
  Qwen 2B / Cautious Persona

────────────────────────────

Win rate             58.7%
Mean VP              +4.2
Mean casualties      31.8%
Supply failures       4.1%
Average runtime       2.8s
```

War College handles:

- Bot tournaments
- Model evaluations
- Persona evaluations
- Balance testing
- Rule variant comparisons
- Monte Carlo simulations
- Strategy benchmarking

This is where Orleans becomes particularly interesting.

```text
War College
     │
     ├── Exercise 001 → GameGrain
     ├── Exercise 002 → GameGrain
     ├── Exercise 003 → GameGrain
     │       ...
     └── Exercise 10,000 → GameGrain
```

### Terminology

One simulation:

> **Exercise**

Collection of simulations:

> **Maneuvers**

Formal evaluation environment:

> **War College**

So you could actually run:

> **War College → Maneuver 26-004 → 10,000 Exercises**

That's delightful.

---

# Putting everything together

I think we can build a surprisingly coherent Sandtable lexicon:

```text
                         SANDTABLE
                             │
        ┌────────────────────┼────────────────────┐
        │                    │                    │
     MAPROOM              CAMPAIGN           WAR COLLEGE
   player interface      live simulation       evaluation
                             │
                       ┌─────┴─────┐
                       │           │
                    COMMAND      STAFF
                  choose intent   plan it
                       │           │
                       └─────┬─────┘
                             │
                         DISPATCH
                           orders
                             │
                             ▼
                           UMPIRE
                    authoritative engine
                             │
           ┌─────────────────┼─────────────────┐
           │                 │                 │
        THEATER       QUARTERMASTER        OPERATIONS
       map/world         logistics          maneuver
           │                 │                 │
           └─────────────────┼─────────────────┘
                             │
                           EVENTS
                             │
                         CHRONICLE
                             │
                   ┌─────────┴─────────┐
                   │                   │
                ARCHIVES           WAR DIARY
               persistence          narrative


                External / supporting

                     INTELLIGENCE
                     local/remote AI
                          │
                       SIGNALS
                    gRPC transport
```

And there's a nice **authority hierarchy** underneath all the flavor:

> **Command decides. Staff plans. Dispatch carries. Umpire adjudicates. Chronicle remembers. Maproom shows.**

That could practically become Sandtable's architectural motto.

Of all these, I'd lock in **Sandtable / Umpire / Maproom / Staff / Command / Dispatch / Signals / Chronicle / Archives / Quartermaster / Theater / ORBAT / War College**. They feel cohesive rather than like we've randomly assigned military words to microservices.

Task009B adds the frozen provisional `CampaignCombatCandidate` value.
`CertifyInitialProfileFacts` names its caller-trusted provenance explicitly; it does not authenticate
a positive history. `CalculateOpportunityId` is the pure Round2 identity calculation, whose
inputs require later selection and Base2 admission.

`CampaignCombatInheritedSelection` owns actual empty selection;
`CampaignCombatInheritedNoAttack` owns its six structural completions. Their immutable local
Control states remain distinct: traversal retains selection unchanged and owns terminal closure.

`CampaignCombatSelectionSteps` names the dormant timed C3a mechanism. Its
`ReplayTrustedBoundary` and `ApplyTrustedBoundary` APIs make caller provenance explicit;
`CombatStepsInput` carries separately authenticated actor/time, and `CombatStepsControl` is
derived replay state. These types do not imply live campaign admission or publication.

**Release base** names the immutable input ledger for the Reserve Release arm. Its isolated
codec checks canonical history against independently retained expected values; it does not mean a
Release window opened, a unit changed status, or actual campaign provenance was established.
Task017A1 owns this foundation. **Release lifecycle** names Task017A2's dormant queue, timed
choices, fallback and explicit completion. `CampaignCombatReserveRelease` reconstructs state from
independently retained base and trusted input/event history; its completion does not advance cycle
control or establish actual campaign lineage. **Settled empty Release adapter** names
`CampaignCombatResultRelease` (Task017B): authenticated native Result2 history derives an empty
Release base, then reuses native open/completion. Its retained upstream boundary remains synthetic;
this name does not imply positive Reserve lineage, cycle advancement or public campaign admission.

**Held-I cycle predecessor** names `CampaignCombatInheritedReserveCycle`: actual held Reserve I
passes through no-move Movement completion and empty Combat while preserving designation and
resources. Its terminal means arrival at Reserve Release, with no release or cycle advance. This
dormant3h adapter is separate from ordinary moved-unit traversal and positive3i Release.

**Inherited Reserve Release bridge** names `CampaignCombatInheritedReserveRelease` (Task017C):
actual held-I predecessor history authorizes first owner release-I or deterministic conversion
fallback. Its progress receipt means a Reserve disposition, not cycle advancement. Pending
next-Movement history is recorded for future ordinal2 consumption; public play remains inactive.

**Cycle Movement assessment** names `CampaignCombatCycleMovementRules` (Task018A): pure
relationship membership/cost assessment and provisional spending at a trusted current-stage
boundary. An assessment is not a move receipt, World update or continuation authorization.

**Isolated cycle Movement** names `CampaignCombatCycleMovement` and its codec (Task018B):
authenticated Result2 history supplies an explicit synthetic next-Movement boundary for atomic
ordinary moves and canonical replay. Move receipts record actual projected movement within this
isolated mechanism; they do not prove campaign repeat or grant Reserve exception authority.
