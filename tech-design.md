Exactly. I would formalize it as an **authoritative simulation plane** plus a separate **intelligence/services plane** connected through gRPC.

The important caveat is that the shared backend should remain **optional and non-authoritative**. A game must still run entirely through scripted policies when that backend—or the model behind it—is unavailable.

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

The game engine should construct a compact, side-specific observation and a bounded set of valid candidate plans.

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

  PlanParameters parameters = 4;
  string commander_commentary = 5;
  ModelTrace trace = 6;
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
- Progress from batch evaluations
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
Weather, and stop at Organization. It validates
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

The implemented Campaign World v1 cutover records exact ruleset, setup, content, and scenario
identities; resolves immutable content before an authoritative grain turn; and projects only
mutable initial element locations into campaign history. The Umpire performs no network or
persistence I/O, and replay requires the same exact content plus the matching executable rules
manifest. The [Campaign World specification](docs/specs/campaign-world-v1.md) and
[technical design](docs/design/campaign-world-v1.md) document the superseded version-3 campaign
contract and schema-2 setup. Weather Determination v1 cuts current authority over to snapshot
contract 5, ruleset manifest contract 3, setup schema 4, Content Pack schema 2, explicit
opening-preamble and Weather policies, pair-keyed actor-order/Weather history, and an opaque public
handle while preserving resident exact-content context internally. Organization remains the next
unsupported mandatory mechanic; no
generic sequence bypass exists. See the Content Pack v1
[research](docs/research/content-pack-v1-spike.md),
[specification](docs/specs/content-pack-v1.md), and
[technical design](docs/design/content-pack-v1.md).

The implemented Campaign Observation boundary lives in `Cna.Core.Observations`. Contract 2 accepts only
a fully admitted Campaign World snapshot, its already-resolved exact content context, and a defined
viewer side. A pure projector then copies a closed allowlist of public campaign/turn/topology facts
the current source-free Weather summary, and the viewer's independently placed elements into
dedicated source-free values. Complete Content
Pack identity and all opposing-force rows, associations, counts, contacts, and placeholders remain
absent. Canonical output is an explicit compact UTF-8 JSON contract; it is derived query data, not
trusted history or command authority. The Umpire still adjudicates from complete authoritative
truth. Future adapters must authorize the viewer and preserve the same allowlist rather than map
authority into the current free-form Intelligence observation strings. See the Campaign Observation
v1 [research](docs/research/observation-and-fog-boundary-spike.md),
[specification](docs/specs/campaign-observation-v1.md), and
[technical design](docs/design/campaign-observation-v1.md).

Legal Actions v1 lives in `Cna.Core.Actions` and is the only public campaign-mutation path after
creation. System actions resolve Initiative, each admitted empty convoy checkpoint, and Weather
separately.
At Operation Stage 1 Initiative Declaration, an observation-only generator gives the holder exactly
`act-first` and `act-last`; the opponent and system receive empty sets. Submission binds campaign,
state version, position, audience, and deterministic action ID, then re-derives exact-audience
membership before translating to an internal mechanic command. Success returns a scalar receipt and
successor opaque `CampaignAuthorityHandle`, never authority state. Weather submission reaches only
the same stage's Organization barrier. `Cna.DecisionWorker` has no Core
reference; `Cna.OrleansHost` is the sole production Core consumer and receives no raw replay or
projection access. See the [research](docs/research/turn-preamble-action-boundary-spike.md),
[specification](docs/specs/legal-actions-v1.md), and
[technical design](docs/design/legal-actions-v1.md).

The proposed Exercise Harness v1 is trusted local developer instrumentation for deterministic,
freshly created campaign runs. It keeps the Umpire authoritative by introducing an opaque
Exercise-only Core capability that shares the existing creation and legal-action execution
primitives; the runner owns orchestration, transactional artifacts, diagnostics, and reports but no
rules or state mutation. The first checked-in Exercise stops at the currently implemented
Organization boundary, proves event-history reconstruction and fresh-session re-adjudication
separately, and fails closed for replay, invariant, build-identity, or artifact faults. The
Maneuvers layer is the serial batch layer, including paired variants with identical declared
initial conditions and initial role streams but no post-divergence synchronization claim. V1 bundles are
`trusted-authority`; side-safe exports, full victory runs, model controllers, and distributed
execution remain explicitly deferred. See the retained
[capability/replay research](docs/research/exercise-capability-and-replay-spike.md),
[artifact research](docs/research/exercise-evidence-artifact-spike.md),
[reproducibility research](docs/research/exercise-reproducibility-and-pairing-spike.md), proposed
[specification](docs/specs/exercise-harness-v1.md), and proposed
[technical design](docs/design/exercise-harness-v1.md).

[1]: https://learn.microsoft.com/en-us/dotnet/orleans/grains/external-tasks-and-grains?utm_source=chatgpt.com "External tasks and grains - .NET | Microsoft Learn"
[2]: https://learn.microsoft.com/en-us/aspnet/core/grpc/performance?view=aspnetcore-10.0&utm_source=chatgpt.com "Performance best practices with gRPC | Microsoft Learn"
[3]: https://learn.microsoft.com/en-us/aspnet/core/grpc/deadlines-cancellation?view=aspnetcore-10.0&utm_source=chatgpt.com "Reliable gRPC services with deadlines and cancellation | Microsoft Learn"
[4]: https://learn.microsoft.com/pt-br/aspnet/core/grpc/json-transcoding?view=aspnetcore-10.0&utm_source=chatgpt.com "Transcodificação de gRPC JSON em aplicativos gRPC do ASP.NET Core | Microsoft Learn"
