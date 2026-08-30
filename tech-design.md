# Sandtable Technical Design

**Status:** Active architectural rationale; implemented and proposed sections are labeled below.

Sandtable uses an **authoritative simulation plane** plus a separate
**intelligence/services plane** connected through gRPC.

The shared backend remains **optional and non-authoritative**. The current gateway and Decision
Worker are scaffolds and do not yet execute live campaign decisions. When that integration is
implemented, a game must still run entirely through scripted policies when the backend—or the model
behind it—is unavailable.

## Current local simulation harness

The repository now includes an in-process `Cna.ExerciseRunner` that is separate from the
intelligence/services plane. Each checked-in **Exercise** creates a fresh opaque
`Cna.Core.Exercises` session, queries and submits through the shared legal-action execution path,
stops at its exact declared boundary, and verifies both Core reconstruction and a second
fresh-session re-adjudication. The original profile remains fixed at Operation Stage 1 Organization;
the Stage Entry profile accepts nine actions and reaches Reserve, and the Reserve Designation
profile accepts 12 actions and reaches first-side Movement. The runner records normalized
inputs, Git/build identity,
seed ledger, accepted actions, canonical events, snapshots, checks, proofs, summaries, and optional
diagnostics in a manifest-last `trusted-authority` bundle. Compact, forensic, and debug detail tiers
are operational: forensic adds correlated query/controller/submission/check/proof evidence,
including progressively assembled failed-decision context, and payload sizing; debug retains every
available noncanonical operation/phase timing on failure plus a structured artifact-finalization
trace after mandatory reader validation. Separate checked exploratory and clean-baseline fixtures
exercise the two build-identity policies.

The checked Exercise and serial-unpaired Maneuver profiles use manifest v2, with unpaired report
scheme `sandtable.maneuver-report.v1`; the optional paired path uses
`sandtable.paired-maneuver-manifest.v1` and
`sandtable.paired-maneuver-report.v1`. All use controller-configuration v2, ruleset v7, snapshot
v9, world v4, and strict `trusted-authority` evidence admission. Retained Organization/Reserve
profiles and their baseline twins remain regression checkpoints.

The checked two-child **Maneuver** fixtures define strict canonical `serial-unpaired` parent manifests.
The Stage Entry fixture runs both admitted setups to Reserve; the Reserve Designation fixture runs
both through Movement. A checked six-child controller matrix crosses `act-first`/`act-last` with
Reserve `none`/`one`/`all` and reaches Movement in exact 10/11/12-action trajectories. Only the
parent supplies the root
seed; each ordered child receives an explicit Maneuver
ID and ordinal identity and runs synchronously through the same no-console post-admission
coordinator. The aggregate path opens each completed child bundle once, semantically validates its
retained evidence and re-adjudication proof, and checks the seed-ledger identity before counting it.
One canonical report reconciles every child state and hashes only deterministic material; elapsed
time, throughput, local paths, and artifact-manifest hashes remain separate diagnostics. Report
creation is transactional and a strict readback must succeed before the CLI claims completion.

The optional `serial-paired` fixture runs baseline then candidate in separate fresh Exercise
sessions. Pair admission and aggregation require identical declared initial conditions, campaign
creation inputs, complete initial role/domain seed ledgers, build cohort, and canonical initial
snapshot while keeping controller configuration identities separate. The parent report recomputes
the first accepted-action divergence and descriptive count/outcome deltas during strict readback.
It makes no causal, statistical-significance, gameplay-balance, recommendation, or
synchronized-post-divergence claim; trajectories and random consumption may diverge after the
first differing choice.

This harness is local developer instrumentation only. It is not registered in AppHost, performs no
model or remote I/O, cannot attach to a production campaign, and does not implement side-safe
exports, model controllers, parallel/distributed scheduling, or War College orchestration. The
governing contracts and closed v1 delivery plan are in
[Exercise Harness v1](docs/specs/exercise-harness-v1.md) and its
[technical design](docs/design/exercise-harness-v1.md).

## Proposed intelligence/services architecture

The following diagram is the target integrated architecture. The current repository contains the
local ExerciseRunner described above and service scaffolds, but not the depicted GameGrain,
decision dispatcher, or live model-provider path.

```text
┌─────────────────────────────────────────────────────────────┐
│                    Authoritative Game Plane                 │
│                                                             │
│  Client ──► API Host ──► Orleans GameGrain ──► Cna.Core    │
│                                │                 │           │
│                                │                 ├─ rules    │
│                                │                 ├─ RNG      │
│                                │                 ├─ plans    │
│                                │                 └─ events   │
│                                │                            │
│                         Pending decision                    │
└───────────────────────────────┬─────────────────────────────┘
                                │
                        Decision dispatcher
                                │ gRPC
                                ▼
┌─────────────────────────────────────────────────────────────┐
│                  Intelligence / Services Plane              │
│                                                             │
│  Intelligence Gateway                                      │
│    ├─ model routing                                         │
│    ├─ prompt/persona assembly                               │
│    ├─ structured-output validation                          │
│    ├─ caching and deduplication                             │
│    ├─ memory / optional RAG                                 │
│    ├─ evaluation and telemetry                              │
│    └─ narrative generation                                 │
│             │                                               │
│       ┌─────┼───────────┐                                   │
│       ▼     ▼           ▼                                   │
│ llama.cpp  MLX/Ollama  hosted provider                      │
└─────────────────────────────────────────────────────────────┘
```

## The game should call an intelligence gateway, not a particular model

The Orleans/game side should know only about an interface such as:

```csharp
public interface IIntelligenceClient
{
    Task<DecisionResponse> ChoosePlanAsync(
        DecisionRequest request,
        CancellationToken cancellationToken);
}
```

It should not know whether the result came from:

- A 0.8B model on the player’s laptop
- A 2B model on a machine elsewhere on the LAN
- A shared GPU server
- A cloud model
- A deterministic scripted fallback
- A replayed historical decision

The intelligence backend then has provider adapters:

```csharp
public interface IModelProvider
{
    Task<ModelResult> GenerateAsync(
        ModelRequest request,
        CancellationToken cancellationToken);
}

public sealed class LlamaCppProvider : IModelProvider;
public sealed class OllamaProvider : IModelProvider;
public sealed class RemoteModelProvider : IModelProvider;
public sealed class ScriptedProvider : IModelProvider;
```

The gateway-to-model connection does not itself have to use gRPC. For example, the gateway can talk to `llama.cpp` through its native HTTP endpoint while exposing one clean gRPC contract to the rest of the system.

That makes the model runtime replaceable without touching Orleans or the game engine.

## Do not send the complete game state

The game engine should construct a compact, side-specific observation and a bounded set of valid
candidate plans. The canonical wire contract is
`src/Cna.Intelligence.Contracts/Protos/intelligence.proto`; the excerpt below matches its current
decision surface but does not replace that source file.

```protobuf
service IntelligenceService {
  rpc ChoosePlan(DecisionRequest) returns (DecisionResponse);
  rpc GenerateNarrative(NarrativeRequest)
      returns (stream NarrativeChunk);
  rpc GetCapabilities(CapabilitiesRequest)
      returns (CapabilitiesResponse);
}

message DecisionRequest {
  string decision_id = 1;
  string game_id = 2;
  int64 state_version = 3;
  string ruleset_hash = 4;

  CommanderProfile commander = 5;
  StrategicObservation observation = 6;
  repeated PlanCandidate candidates = 7;
}

message PlanCandidate {
  string plan_id = 1;
  string plan_type = 2;

  double objective_score = 3;
  double supply_risk = 4;
  double casualty_risk = 5;
  double expected_value = 6;

  repeated string relevant_facts = 7;
}

message DecisionResponse {
  string decision_id = 1;
  int64 based_on_state_version = 2;
  string selected_plan_id = 3;

  map<string, string> parameters = 4;
  string commander_commentary = 5;
  ModelTrace trace = 6;
  string ruleset_hash = 7;
}
```

The model should return:

> Select plan `axis_limited_counterattack_03`, retain one armored formation as reserve, and use a higher-than-normal retreat threshold.

It should not return:

> Move counter X through hexes A12, A13, and A14, subtract 37 gallons, roll on table 21.3, and attack at 4:1.

Those operational commands should be generated by the deterministic planner after the plan is selected.

## Keep the gRPC call outside the authoritative grain turn

An Orleans grain can await asynchronous library and remote calls, and Orleans explicitly supports normal `async`/`await`. Blocking synchronous remote I/O inside grain execution is what must be avoided. ([Microsoft Learn][1])

Even so, I would not have `GameGrain` sit and await model inference. Not because async gRPC blocks a .NET thread, but because it couples an authoritative grain turn to a slow and fallible external system.

Use this flow instead:

```text
1. GameGrain reaches a strategic decision barrier.

2. Cna.Core generates:
   - redacted observation
   - valid candidate plans
   - deterministic fallback selection

3. GameGrain persists:
   PendingDecision {
       decisionId,
       stateVersion,
       decisionType,
       expiresAt
   }

4. A DecisionDispatcher calls the intelligence backend.

5. The model returns a proposal.

6. DecisionDispatcher calls:
   GameGrain.SubmitDecisionProposal(...)

7. GameGrain verifies:
   - decision ID matches
   - state version matches
   - plan still exists
   - parameters are within permitted bounds
   - ruleset/configuration hash matches

8. Cna.Core converts the plan into legal commands and events.
```

That lets players continue inspecting the board while the model is working, isolates inference failures, and makes stale model responses harmless.

A response should be rejected whenever:

```text
response.based_on_state_version != game.current_state_version
```

It can then be regenerated or replaced by the scripted fallback.

## What belongs in the shared intelligence layer

### Definitely appropriate

**Model routing**

Choose the model according to decision complexity:

```text
Routine posture choice       → scripted or 0.8B
Normal strategic decision    → 2B
Major campaign decision      → optional 4B
Narrative sentence           → 0.8B
Batch simulation             → scripted only by default
```

**Persona and prompt rendering**

The backend can combine:

- Commander profile
- Side doctrine
- Current strategic posture
- Candidate plans
- Recent significant events
- Output schema

**Structured-output enforcement**

The backend should reject or repair malformed output before returning anything to the game.

**Request deduplication**

Every request gets a durable `decision_id`. A retry with the same ID should return the same stored result rather than generating a second costly answer.

**Model observability**

Record:

- Provider and model
- Quantization
- Prompt-template version
- Persona version
- Latency
- Input/output token counts
- Validation failures
- Fallback reason
- Final response hash

**Optional strategic memory**

The intelligence service can maintain compact advisory memory such as:

```text
The last two central offensives failed because armor outran water supply.
The commander has repeatedly favored mobile defense.
The Allied southern formation appears vulnerable but distant from supply.
```

This memory remains advisory. The event journal remains the actual truth.

**Narrative generation**

Commander remarks, staff briefings, after-action summaries, and replay narration are ideal jobs for this service because mistakes there cannot corrupt game state.

**Evaluation tooling**

The service can compare models against a recorded corpus of decision points:

```text
same observation
same candidate plans
different models/personas
compare selected plan and eventual result
```

That gives you a proper AI benchmark without rerunning full campaigns for every experiment.

## What must stay out of that layer

The intelligence backend should never own:

- Authoritative game state
- Legal-action generation
- Combat calculations
- Pathfinding used to execute orders
- Supply arithmetic
- Random-number generation
- Turn sequencing
- Fog-of-war enforcement
- Victory calculations
- Event persistence
- Rule interpretation at execution time

It may receive a fog-of-war-safe view. It should never have access to the opposing side’s hidden state merely because the server has it.

The game should treat the intelligence backend as an **untrusted strategic adviser**.

## gRPC behavior I would standardize immediately

### Reuse channels

.NET’s gRPC guidance recommends reusing channels because calls can be multiplexed across the existing HTTP/2 connection; creating a fresh channel for every call adds connection setup overhead. ([Microsoft Learn][2])

Register the client once:

```csharp
services.AddGrpcClient<IntelligenceService.IntelligenceServiceClient>(
    options =>
    {
        options.Address = configuration.IntelligenceEndpoint;
    });
```

### Always set deadlines

gRPC calls have no deadline by default, so an inference request can otherwise remain outstanding indefinitely. Deadlines and cancellation should propagate all the way through the gateway to the model provider. ([Microsoft Learn][3])

For example:

```csharp
var response = await client.ChoosePlanAsync(
    request,
    deadline: DateTime.UtcNow.AddSeconds(20),
    cancellationToken: cancellationToken);
```

Decision types can have different budgets:

```text
Routine choice        3–5 seconds
Normal plan choice   10–20 seconds
Major replanning     30–45 seconds
Narrative             5–10 seconds
```

A timeout should produce a scripted decision, not a failed game turn.

### Be careful with retries

Do not blindly retry a generation request because that can perform duplicate inference and produce different decisions.

Retry only when:

- The request includes an idempotent `decision_id`
- The intelligence gateway deduplicates that ID
- No accepted answer has already been recorded
- The game state version is still current

### Use unary calls for decisions

`ChoosePlan` should normally be a unary request/response.

Streaming makes sense for:

- Visible narrative text
- Long after-action reports
- Progress from long-running Maneuver evaluations
- Administrative model download/warmup status

The player does not benefit from seeing a strategic decision token by token. The game needs only the completed structured object.

## The shared backend can support three deployment modes

### Fully local

```text
Desktop application
├── local game runtime or Orleans silo
├── intelligence gateway
└── llama.cpp model process
```

Communication uses localhost, and the game works without internet access.

### Shared LAN model host

```text
Player desktops / game server
             │
             ▼
Intelligence gateway on GPU machine
             │
             ▼
Shared local model
```

This would let multiple games share one stronger machine without embedding model runtimes in every client.

### Hosted deployment

```text
Orleans cluster
      │
Decision workers
      │
Intelligence gateway replicas
      │
Model workers / external providers
```

The same protobuf contract works in all three modes.

## “Other backend stuff” should be divided into modules

I would avoid turning the intelligence gateway into a general dumping ground. Think in terms of three service areas:

### Intelligence services

- Plan selection
- Persona handling
- Narrative generation
- Memory/RAG
- Model routing
- Evaluation

### Platform services

- Accounts and identity
- Multiplayer lobbies
- Invitations
- Matchmaking
- Cloud saves
- Notifications
- Scenario/mod distribution

### Compute services

- Batch simulations
- Bot tournaments
- Model-versus-model evaluations
- Scenario balance analysis
- Replay report generation

These can begin as one **modular ASP.NET Core backend** while retaining explicit internal boundaries:

```text
Cna.Backend
├── Intelligence
├── Platform
├── SimulationJobs
├── Content
└── Observability
```

You do not need five separately deployed microservices initially. Split them only when their scaling or deployment requirements actually diverge.

The model process probably should remain a separate process from the start because it has very different memory, lifecycle, and hardware requirements.

## I would expose different protocols in different directions

```text
Browser / desktop UI
    → HTTPS + SignalR/WebSocket
    → Game/API host

Game host / decision workers
    → gRPC
    → Intelligence backend

Intelligence backend
    → provider-specific HTTP/native protocol
    → llama.cpp, Ollama, MLX, or hosted model
```

ASP.NET Core can expose gRPC-Web or JSON-transcoded gRPC endpoints when browser compatibility is necessary, but there is little reason for the browser to call the intelligence service directly. Keeping that traffic behind the game API preserves authorization, fog of war, and decision validation. ([Microsoft Learn][4])

## The practical initial version

I would start with:

```text
Cna.Core
    pure deterministic game and planners

Cna.OrleansHost
    authoritative games

Cna.DecisionWorker
    receives pending decisions and invokes intelligence

Cna.Intelligence.Contracts
    protobuf definitions

Cna.Intelligence.Gateway
    ASP.NET Core gRPC service

llama.cpp
    separate local model process
```

And provide two implementations:

```csharp
public interface IIntelligenceClient
{
    Task<DecisionResponse> ChoosePlanAsync(...);
}

public sealed class GrpcIntelligenceClient : IIntelligenceClient;
public sealed class ScriptedIntelligenceClient : IIntelligenceClient;
```

The scripted implementation is not merely an emergency hack. It is:

- The default for headless simulation
- The deterministic test baseline
- The fallback for timeouts
- The comparison point for model evaluations
- The mode used when a player disables AI services

So yes: **gRPC to a shared backend is exactly the right direction**, provided the shared backend selects and explains strategy rather than owning or resolving the game. The cleanest description is:

> Orleans hosts authoritative campaigns. The deterministic core resolves the game. A gRPC intelligence gateway provides optional strategic judgment, persona, narration, memory, and model abstraction. Every response returns as a versioned proposal that the game validates before execution.

## Rules fidelity and incremental delivery

Sandtable targets a faithful digital implementation of the original 1979 SPI game rather than a
generic campaign engine. The proposed initial authority is the original rules and component data
as corrected by the September 1979 errata. Community rulebooks, trackers, and digital modules are
comparison aids, not authority.

The first delivery path follows the game's published modular structure:

```text
rules laboratory
    -> campaign world and side-safe legal actions
    -> mandatory turn preamble
    -> replayable Land movement/contact/combat skeleton
    -> Land-only Graziani's Offensive scenario
    -> detailed Air Game
    -> detailed Logistics Game
    -> later scenarios and full campaign
```

Authoritative rules, tables, rules-owned vocabulary, and adopted rulings carry stable source
references and participate in the ruleset hash. Static topology, force assignments, and scenario
declarations carry their own provenance and participate in an independent content hash. A campaign
records both exact identities. Commands produce events; events rebuild campaign state; snapshots
are replay checkpoints. Unsupported mechanics fail explicitly rather than falling through to a
plausible approximation.

The physical game's hierarchical sequence must remain visible in the model. A weekly Game Turn
contains Operation Stages, player phases, and repeatable movement/combat segments. The engine
therefore uses versioned phase/segment identifiers instead of a monolithic `AdvanceTurn` operation.

The initial `Cna.Core` implementation makes that boundary executable. `Cna.Core.Rules` defines
source-cited normalized artifacts, complete adopted-ruling metadata, a canonical `cna-1979.1`
manifest derived from the Land sequence, Initiative Ratings, and deterministic-random artifacts,
and the Land-only sequence hierarchy from the original rules. Sequence positions expose actor roles
rather than a fixed first player: the initiative holder remains distinct from the side that later
chooses to act first or last in each Operation Stage. `Cna.Core.Setups` binds campaign creation to
recognized, provenance-bearing fixtures, while `Cna.Core.Randomness` owns the versioned SHA-256
counter stream consumed by authoritative rules. `Cna.Core.Campaigns` uses pure command decisions
and immutable events to create a campaign against the canonical manifest, resolve predetermined or
contested Initiative Determination, explicitly resolve the two admitted no-obligation Naval Convoy
checkpoints, record the pair-keyed Operation Stage 1 first/second actor order, resolve deterministic
Weather, resolve the four admitted empty Organization/Naval Convoy Arrival/Fleet obligations through
distinct commands and events, designate zero or more first-side Reserve I elements, complete the
Reserve decision, and stop at Movement. It validates
internal snapshots before adjudication and recomputes events during projection rather than trusting
caller-supplied outcomes or provenance. Canonical snapshot and event serialization plus the
internal replay harness prove reconstruction of accepted campaign history without ambient
timestamps or generated identifiers.

The current projector is a trusted-history contract, not an untrusted ingestion boundary. Before
Chronicle events can arrive from persistence, transport, or another process, that boundary must
authenticate event provenance. The projector already validates the complete contiguous creation
and every implemented preamble transition; persistence and transport remain outside this slice.

Copyrighted source scans and original component art remain outside the repository unless explicit
permission is recorded. Sandtable uses normalized, provenance-bearing rule data and original
visuals. See the [source-material spike](docs/research/cna-source-material-spike.md) and
[pre-alpha roadmap](docs/roadmap/pre-alpha-roadmap.md) for evidence, scope, and decision gates.

The implemented Initiative Determination slice resolves the first random authoritative mechanic
through a versioned repository-owned stream and stops at Naval Convoy. Its
[specification](docs/specs/initiative-determination.md),
[technical design](docs/design/initiative-determination.md), and
[research packet](docs/research/initiative-determination-spike.md) define the source correction,
contracts, replay invariants, implementation checkpoints, and verification boundary.

The implemented static-data boundary is Content Pack v1. `Cna.Core.Content` owns immutable
versioned topology, formation/element structure, scenario deployment declarations, per-datum
origins, validation, and an independent canonical content hash. Rules remain the authority for
side, terrain, edge, organization, and derived table meanings; campaigns own exact content binding
and mutable world state; player observations remain a separate redacted contract.
Printed coordinates are audit metadata and explicit edges are adjacency authority. The first pack
is an original nine-hex nonhistorical rules laboratory using the same path intended for future
source-derived content. Exact catalog lookup requires both pack ID and hash and never substitutes a
default; presentation labels remain outside authoritative equality and bytes.

The implemented Campaign World v1 capability now uses world snapshot contract 4 and records exact
ruleset, setup, content, and scenario identities; resolves immutable content before an authoritative
grain turn; and projects mutable element locations, per-element Reserve status, exact
Operation-Stage expenditure/Cohesion state, and opaque one-to-one map-representation bindings into
campaign history. The Umpire performs no network or
persistence I/O, and replay requires the same exact content plus the matching executable rules
manifest. The [Campaign World specification](docs/specs/campaign-world-v1.md) and
[technical design](docs/design/campaign-world-v1.md) retain the original version-3 campaign /
world-v1 and schema-2 setup delivery as historical context; their current-evolution notes point to
the later Reserve and Movement clean cuts. Weather Determination v1 established explicit opening-preamble and
Weather policies plus pair-keyed actor-order/Weather history. Reserve Designation's coordinated
identity lane first advanced authority to snapshot contract 7, creation contract 6, ruleset
manifest contract 5, world snapshot contract 2, setup schema 5, and Content Pack schema 2 while
preserving resident exact-content context internally. Operation-Stage Entry still admits only the
four exact empty obligations; Reserve Designation accepts only current owner candidates and advances
through one exact completion event to Movement. The current Movement foundation plus approved
Breakdown-continuity clean cut advances the ruleset to contract 7, snapshot to contract 9, Campaign
World to contract 4, creation event to contract 8, and Content Pack to schema 4 / canonical format
v3. Task 004B adds exact source-backed BP identity, synthetic Truck cohorts, and creation-seeded
replay state without a Breakdown action, result, or RNG path. No generic sequence bypass exists.
See the Content Pack v1
[research](docs/research/content-pack-v1-spike.md),
[specification](docs/specs/content-pack-v1.md), and
[technical design](docs/design/content-pack-v1.md).

The implemented Campaign Observation boundary lives in `Cna.Core.Observations`. Contract 5 accepts only
a fully admitted Campaign World snapshot, its already-resolved exact content context, and a defined
viewer side. A pure projector then copies a closed allowlist of public campaign/turn/topology facts,
the current source-free Weather summary, exact own mobility/operational ledger/Reserve status and
approved vehicle-risk continuity into dedicated source-free values. Opponents appear only as
opaque representation ID, apparent location, and the currently false-only `exertsZoc` field;
real bindings and force facts remain absent. Canonical output has an exact compact UTF-8 writer and
strict non-authoritative reader. It remains derived query data, never trusted history or command
authority. The Umpire still adjudicates from complete authoritative truth. Future adapters must
authorize the viewer and preserve the same allowlist rather than map authority into the current
free-form Intelligence observation strings. See the Campaign Observation
v1 [research](docs/research/observation-and-fog-boundary-spike.md),
[specification](docs/specs/campaign-observation-v1.md), and
[technical design](docs/design/campaign-observation-v1.md).

Legal Actions v1 lives in `Cna.Core.Actions` and is the only public campaign-mutation path after
creation. System actions resolve Initiative, each admitted empty convoy checkpoint, Weather, and
each of the four admitted empty stage-entry obligations separately.
At Operation Stage 1 Initiative Declaration, an observation-only generator gives the holder exactly
`act-first` and `act-last`; the opponent and system receive empty sets. Submission binds campaign,
state version, position, audience, and deterministic action ID, then re-derives exact-audience
membership before translating to an internal mechanic command. Success returns a scalar receipt and
successor opaque `CampaignAuthorityHandle`, never authority state. Fleet Repair completion reaches
Reserve with authoritative `ActiveSide` unset; observation and legal-action projection derive the
audience from the recorded actor order. The resolved first side now receives one subject-bound
candidate for each own `None` element plus explicit completion; opaque submissions map to closed
Reserve commands, and accepted mutations emit exact replayable designation/completion events.
`Cna.DecisionWorker` has no Core reference.
`Cna.OrleansHost` owns campaign hosting, while `Cna.ExerciseRunner` is the separate trusted local
instrumentation consumer; neither receives a public raw replay or projection mutation seam. See the
[research](docs/research/turn-preamble-action-boundary-spike.md),
[specification](docs/specs/legal-actions-v1.md), and
[technical design](docs/design/legal-actions-v1.md).

The proposed, not-yet-implemented Player Intent Composer keeps future complex Maproom decisions
prompt-forward without making them prompt-only. Contextual suggested approaches, short language,
map/list interaction, and structured controls edit one private typed draft; deterministic validation
may surface at most two automatic clarification questions before explicit intent confirmation.
Deterministic Staff planning and a separate final legal-action confirmation preserve the existing
Command → Staff → Umpire hierarchy. An optional local parser such as Needle can only populate draft
fields and remains gated behind a no-model prototype and corpus evaluation. See the retained
[research](docs/research/player-intent-input-and-needle-feasibility.md), proposed
[specification](docs/specs/player-intent-composer-v1.md), and
[technical design](docs/design/player-intent-composer-v1.md).
Roadmap placement is explicit: select the representative decision after the Sprint 5 combat
skeleton, run the no-model interaction prototype before Sprint 8, integrate the deterministic slice
with Minimal Maproom, and evaluate any parser only after the deterministic MVP path works.

The implemented Exercise Harness v1 is trusted local developer
instrumentation for deterministic, freshly created campaign runs. Its Exercise path keeps the
Umpire authoritative through an opaque Exercise-only Core capability that shares the existing
creation and legal-action execution primitives; the runner owns orchestration, transactional
artifacts, diagnostics, and reports but no rules or state mutation. The checked Exercises retain
the original Organization and Reserve checkpoints and now exercise Reserve Designation through
first-side Movement, prove event-history reconstruction and
fresh-session re-adjudication separately, and fail closed for replay, invariant, build-identity, or
artifact faults. Trusted bundle readback extracts snapshot coordinates only after the Core-owned
complete snapshot/world decoder accepts the canonical bytes, including for expected replay-failure
profiles. A Maneuver adds strict parent admission, explicit child identity, synchronous
coordination, one-read semantic aggregation, and a deterministic fingerprinted report without
changing that authority boundary. The separate optional serial-paired contract adds isolated,
sequential baseline/candidate arms with strict equal-initial-evidence validation and a descriptive
first-divergence report. V1 bundles and reports are `trusted-authority`; side-safe exports, full
victory runs, model controllers, and parallel/distributed execution remain explicitly deferred.
See the retained
[controller-matrix evidence](docs/research/simulator-controller-matrix.md),
[capability/replay research](docs/research/exercise-capability-and-replay-spike.md),
[artifact research](docs/research/exercise-evidence-artifact-spike.md),
[reproducibility research](docs/research/exercise-reproducibility-and-pairing-spike.md), governing
[specification](docs/specs/exercise-harness-v1.md), and
[technical design](docs/design/exercise-harness-v1.md). The implemented Operation-Stage Entry
package retains its [research](docs/research/operation-stage-entry-spike.md),
[specification](docs/specs/operation-stage-entry-v1.md), and
[technical design](docs/design/operation-stage-entry-v1.md). Reserve Designation is now an
implemented authoritative-engine package. Its [research](docs/research/reserve-designation-spike.md),
[specification](docs/specs/reserve-designation-v1.md), and
[technical design](docs/design/reserve-designation-v1.md) define an incremental, subject-bearing
legal-action flow, per-element Reserve I authority, owner-only projection, replay events, bounded
multi-event checkpoint validation, and checked harness proof through Movement. Implementation
includes the rules artifact, world/snapshot and owner-observation contracts, subject-bearing
candidates, command mapping, exact designation/completion events, strict finite Reserve/Movement
validation, and standalone plus two-setup replay evidence. Internal non-contact Movement
adjudication and replay are now implemented; public Movement membership and completion remain
pending.
The active owner-approved engine package is the Movement Foundation
[research](docs/research/movement-foundation-spike.md),
[specification](docs/specs/movement-foundation-v1.md), and
[technical design](docs/design/movement-foundation-v1.md). It keeps representation truth inside
the Umpire, adds a minimum side-safe apparent-presence contract before outward Movement legality,
normalizes exact CP/terrain/stacking rules, accepts repeatable non-contact moves, and stops at the
existing Breakdown Determination boundary. Its source/ruling lock and `MOV-TASK-002` exact
Capability Point, mobility-vocabulary, normalized-table, canonical-artifact, and ruleset-identity
work are complete. Content schema 4 / canonical format v3 now assigns and validates one
rules-owned mobility ID plus the supported optional vehicle cohort per element. Snapshot v9/world
v4 and creation event v8 now record exact
Operation-Stage Cohesion/expenditure plus opaque one-to-one internal representation bindings.
Task 006 adds dormant typed `MoveElementAction` and `CompleteMovementSegmentAction` output values,
deterministic SHA-256 identities, pure observation-only derivation, and strict non-authoritative
readback for canonical legal-action sets, submissions, and receipts. Its move cost is an exact
explanatory value: destination terrain ID/cost; a nullable route adjustment with route ID,
`override` or `scale-underlying` behavior, and exact amount; ordered hexside additions with
feature ID, `either`, `up`, or `down` direction, and exact added cost; and one coherent exact total.
Candidate, submission, and receipt contracts remain version 1 and the action-set envelope remains
version 2. Task 007 adds the internal `MoveElement` command and canonical `ElementMoved` event,
authoritative cost/provenance recalculation, single-event engine dispatch, atomic element,
representation, and ledger projection, strict event readback, and deterministic multi-event
replay. No Movement campaign action is public or executable through the player submission boundary
yet. The
[Sprint 4-5 research-gate audit](docs/research/sprint-4-5-research-gates.md) makes
`BREAKDOWN-001` explicit: minimum Breakdown Point continuity is recorded now, sequential d6 form
the `11`-`66` coordinate, and Sandstorm eligibility uses Table 21.38's share of accumulated BP.
`MOV-TASK-004B` implements the exact Rules/Content/World seam and passed the repository gate plus
two fresh-context review instances. `MOV-TASK-005` projects the approved own mobility, ledger,
Cohesion, and BP/cohort-risk facts plus the minimum opaque apparent-opponent shape.
`MOV-TASK-006` freezes the dormant action-contract cut. `MOV-TASK-007` implements the internal
non-contact command/event/adjudication vertical while keeping public membership dormant;
`MOV-TASK-008` next exposes Movement membership and completion through the public boundary.

The [ZOC/Reaction spike](docs/research/contact-reaction-zoc-spike.md) and accepted
[CONTACT-001 ruling lock](docs/research/contact-reaction-zoc-source-ruling-lock.md) separate
immediate enemy-ZOC entry and the interrupting Reaction window from Contact and Engaged. Immediate
entry creates neither relationship: Contact is derived from enemy-ZOC presence at the beginning of
a Movement Segment, while Engaged is a Close Assault result; both remain Sprint 5 contract work. The
[Combat-cycle inventory](docs/research/combat-cycle-source-inventory.md) permits source/table
normalization now but defers contract freeze until Breakdown and ZOC/Reaction are approved. The
first bounded follow-up, [Combat rules and result surface](docs/research/combat-rules-result-surface-spike.md),
completed `CMB-RSH-001` by normalizing the admitted combat-table/result surface; it is research
evidence, not an implemented combat contract. Current proposals use trusted-Umpire sealed choices,
the same pre-state for simultaneous combat, and structural sequence positions plus cycle identity;
none is implemented or approved yet.
All five `CONTACT-001` rulings are accepted, but they remain research inputs only. No ZOC/Reaction
production task is authorized before `MOV-TASK-008`-`010` and an approved specification/design
package. The implemented paired comparison does not block that engine work.

[1]: https://learn.microsoft.com/en-us/dotnet/orleans/grains/external-tasks-and-grains "External tasks and grains - .NET | Microsoft Learn"
[2]: https://learn.microsoft.com/en-us/aspnet/core/grpc/performance?view=aspnetcore-10.0 "Performance best practices with gRPC | Microsoft Learn"
[3]: https://learn.microsoft.com/en-us/aspnet/core/grpc/deadlines-cancellation?view=aspnetcore-10.0 "Reliable gRPC services with deadlines and cancellation | Microsoft Learn"
[4]: https://learn.microsoft.com/en-us/aspnet/core/grpc/json-transcoding?view=aspnetcore-10.0 "gRPC JSON transcoding in ASP.NET Core gRPC apps | Microsoft Learn"
