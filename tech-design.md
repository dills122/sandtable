# Sandtable Technical Design

**Status:** Active architectural rationale; implemented and proposed sections are labeled below.
The [pre-alpha roadmap](docs/roadmap/pre-alpha-roadmap.md#current-delivery-status) is the canonical
current-delivery ledger. Task/checkpoint references here explain architecture and must not be read as
independent completion claims.

Sandtable uses an **authoritative simulation plane** plus a separate
**intelligence/services plane** connected through gRPC.

The shared backend remains **optional and non-authoritative**. The current gateway and Decision
Worker are scaffolds and do not yet execute live campaign decisions. When that integration is
implemented, a game must still run entirely through scripted policies when the backend—or the model
behind it—is unavailable.

## Project website

The repository root contains a dependency-free static project website under `site/`, with design
tokens in `site/tokens.css`. It explains the planned play loop, the authority boundary, current
implementation status, and the local developer workflow. The site is documentation and outreach:
it does not host a campaign, read authoritative state, call Intelligence, or stand in for the
future Maproom player interface.

`.github/workflows/pages.yml` packages only `site/` and deploys that artifact to the protected
`github-pages` environment after website changes land on `main` or a maintainer runs the workflow
manually. The workflow uses short-lived OIDC identity and the minimum Pages permissions; it does not
use a deploy branch, long-lived secret, or application build output.

Keeping this distinction explicit prevents a polished concept surface from becoming an accidental
contract. Any tactical map, Chronicle feed, or legal-action surface shown by the project website is
clearly labeled as a concept or current rules-laboratory example. Maproom must later consume typed,
side-safe application contracts and remain subordinate to the Umpire.

## Local simulation harness

`Cna.ExerciseRunner` is an in-process developer harness, separate from AppHost and Intelligence.
An Exercise creates a fresh opaque Core session, queries and submits legal actions, stops at its
manifest boundary, and checks reconstruction plus fresh-session re-adjudication. Maneuvers run an
ordered serial collection of Exercises; paired profiles compare two isolated controller runs.

Bundles retain normalized inputs, build/Git identity, seed ledger, accepted actions, canonical
history, checkpoints and proofs. They are trusted local authority artifacts, not side-safe player
exports. Manifest-last finalization and strict readback must succeed before the CLI claims success.
Deterministic fingerprints exclude timings and local paths. Compact, forensic and debug tiers add
increasing diagnostic detail without changing replay equality.

Paired evidence describes divergence and count/outcome deltas. It does not establish causality,
statistical significance, balance or synchronized trajectories after divergence. Baseline identity
fails closed on an unclean checkout; exploratory profiles record that limitation explicitly.

See the [runbook](docs/runbooks/exercise-runner.md),
[specification](docs/specs/exercise-harness-v1.md), and
[design](docs/design/exercise-harness-v1.md) for exact contracts and retained profiles. Public Rules9
still stops at first-side Combat entry. Private Combat tests/contracts do not activate that path.

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

## gRPC integration rules

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

## Backend module boundaries

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

## Protocol boundaries

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

The target initial integration uses:

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

The shared backend selects and explains strategy while Core resolves the game:

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
The immutable Land sequence catalog retains its most recently requested turn to avoid rebuilding
positions during replay. This single-entry cache contains only rules data; campaign state, actor
resolution, and replay validation remain outside it. Concurrent misses may rebuild the catalog.

## Project responsibilities and current boundaries

| Project | Responsibility and current boundary |
| --- | --- |
| `Cna.Core` | Pure deterministic rules, authority, content/setup validation, RNG, events, replay, observations and legal actions |
| `Cna.ExerciseRunner` | Fresh local Exercises, serial Maneuvers, controller policies and trusted evidence |
| `Cna.OrleansHost` | Orleans development host scaffold; no live campaign grain or production clustering provider |
| `Cna.DecisionWorker` | Discovered gRPC client scaffold; no live campaign dispatcher or fallback executor |
| `Cna.Intelligence.Contracts` | Versioned protobuf and generated transport contracts |
| `Cna.Intelligence.Gateway` | Provider-status and gRPC scaffold; decision/narrative return `Unavailable` |
| `Cna.ServiceDefaults` | Discovery, resilience, health and telemetry defaults |
| `Cna.AppHost` | Local Aspire orchestration of the three service scaffolds |
| `site/` | Project website; future Maproom remains a separate player application |

The canonical intelligence wire contract is
[`intelligence.proto`](src/Cna.Intelligence.Contracts/Protos/intelligence.proto). Example interfaces
and deployment diagrams above describe the target integration, not registered live implementations.

Core keeps immutable Rules, Setups and Content separate from mutable Campaign World. Campaigns own
command decisions/events and reconstruction; Observations own audience-specific projection and
strict readback; Actions own current typed candidates, submission revalidation and receipts;
Exercises own fresh local capabilities and retained evidence. Raw authority and replay are not
player mutation seams. A public campaign handle is opaque; opposing state is disclosed only through
versioned allowlists, never by serializing full snapshots.

Task019F0's private actual-selection executable contract merged in PR #161. It requires a separately
trusted input ledger and reaches Force Assignment after defender decline, or no-attack Reserve
Release. Task019F1 implements its private native consumer in PR #165; Force Assignment completion
and production input authentication remain pending. Private settled
controls still retain synthetic earlier Movement; none of this proves public Combat or the authentic
continual cycle. The [roadmap](docs/roadmap/pre-alpha-roadmap.md) owns detailed sequencing, and the
[first-release audit](docs/research/2026-10-05-first-release-audit.md) owns release gap measurements.
The [historical ledger](docs/research/2026-10-05-project-documentation-snapshot.md) preserves former
checkpoint evidence without making it current runtime truth.

[1]: https://learn.microsoft.com/en-us/dotnet/orleans/grains/external-tasks-and-grains "External tasks and grains - .NET | Microsoft Learn"
[2]: https://learn.microsoft.com/en-us/aspnet/core/grpc/performance?view=aspnetcore-10.0 "Performance best practices with gRPC | Microsoft Learn"
[3]: https://learn.microsoft.com/en-us/aspnet/core/grpc/deadlines-cancellation?view=aspnetcore-10.0 "Reliable gRPC services with deadlines and cancellation | Microsoft Learn"
[4]: https://learn.microsoft.com/en-us/aspnet/core/grpc/json-transcoding?view=aspnetcore-10.0 "gRPC JSON transcoding in ASP.NET Core gRPC apps | Microsoft Learn"
