# Sandtable Naming and Domain Vocabulary

**Status:** Active vocabulary rationale. Current implementation notes and future reserved names are
labeled in their sections.

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
`reserve-designation-completed`. These projections are not Snapshot12. Chronicle retains the
accepted events; the future Archives reader must reconstruct and validate their complete chain.

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
