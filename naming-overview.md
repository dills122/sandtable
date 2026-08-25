**Sandtable** gives us a really good naming theme because we can pull from historical staff work, Kriegsspiel, military administration, and campaign terminology **without making the codebase incomprehensible**.

I'd establish one rule:

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
| **Maneuvers**                   | Simulation/evaluation batches         | A collection of Exercises.                                                |
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
Repair obligations through four distinct event types; and stops at the first-acting side's Reserve
decision. The authoritative `ActiveSide` remains unset there; observation and legal-action audiences
derive the acting side from the recorded Operation Stage order.
Walking the published sequence catalog for inspection does not create Chronicle history.

For the CNA rules domain, use **initiative holder** only for the side that wins or is assigned
Game Turn initiative. Use **first-acting side** and **second-acting side** for each Operation Stage.
They are deliberately not aliases: the initiative holder chooses first or last separately for
each stage. This plain vocabulary prevents a later rules decision from being hidden behind a
generic `FirstPlayer` name.

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
| **Campaign World** | Authoritative mutable runtime facts projected from an exact setup and scenario. Version 1 stores current element locations and joins static facts through stable Content Pack IDs. |
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
**Exercise** or one serial-unpaired **Maneuver** through an in-process opaque Umpire capability and
retains trusted evidence. An Exercise is one authoritative simulation run. A Maneuver is an ordered
collection of uniquely identified Exercises that share one parent seed contract, run serially
through the same coordinator, and produce one deterministic aggregate report after strict child
readback. Compact, forensic, and debug detail tiers describe Exercise instrumentation depth, not
different game rules; Maneuver timing and path diagnostics likewise stay outside its deterministic
fingerprint. Failed decisions retain the attempted query, controller, action, and submission
context available before the failure. Paired Maneuvers remain a later contract. **War College**
remains a later evaluation layer, and the current runner is not an Orleans workload, tournament
system, or balance-analysis environment.

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
