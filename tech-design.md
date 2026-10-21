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

The [optional code-review pilot](docs/runbooks/optional-code-review.md) is developer tooling
outside both planes. Its trusted, label-triggered workflow and tracked rules select PR changes
for additional scrutiny after a repository secret is configured. It has no gameplay integration
or merge authority.

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

## Accepted local-server and Maproom direction

The [implementation plan](docs/design/local-game-server-maproom-plan.md) defines small,
contract-first slices for a local developer client. The stack is Angular with Material/CDK,
Tailwind, typed Reactive Forms/Formly, NgRx SignalStore, generated Angular HttpClient contracts,
SignalR notifications and an initial SVG map. Browser rendering does not require a Rust/WASM
simulation port: the existing C# Umpire remains authoritative.

One ASP.NET Core/Orleans host will own campaign lifecycle and browser ingress. Aspire currently
launches service scaffolds; the browser client runs separately until integration. Versioned,
audience-safe HTTP contracts live separately from intelligence
protobuf. SignalR carries audience-safe change hints; HTTP readback recovers after reconnect.
The server grants seats and revalidates every submitted legal action. Browser caches, drafts and
late responses are isolated by seat generation.

The first connected test uses an explicitly temporary synthetic rules-lab session. Durable
publication and restore are later contract/provider gates, with external I/O coordinated outside
authoritative grain turns. PostgreSQL is an evaluation candidate, not a selected production store.
This lane does not activate private Combat or satisfy six-turn MVP acceptance.

The [Proving Ground runbook](docs/runbooks/proving-ground.md) now defines a containerized
Development preview: one built Angular shell and one single-silo Orleans host, both bound to host
loopback. It deliberately omits database, gateway and worker from the minimal human-order path.
Bounded local browser bootstrap, single-fixture creation, seat grants and initial read exist.
Public action submission, receipt lookup and durable save do not. Production clustering and
durable publication/restore remain gates before a Kubernetes deployment can replace this profile.

The [Field Atlas design system](docs/design/maproom-design-system-v0.md) starts with minimal semantic
tokens and controls, then evolves through an operational map, order workflow and dense formation
ledger in MAP-03A/B/C. Its [portable operations preview](docs/design/maproom-style-study/index.html)
links selection and accepted example results across map, inspector, ledger and War Diary. Submitted
order identity is independent of selection; real completion must update the originating formation.
MAP-03B demonstrates this with local staged acceptance, rejection, stale and lost-reply states,
without network or authoritative adjudication. MAP-03C projects accepted fixture results into a
four-field ledger and selected/all-visible own-side War Diary, with shared selection and locate.
The synthetic operations workspace pans by pointer or touch at the selected zoom without changing
the scale; its hex overlay uses fixed-ratio screen units across normal and full-screen layouts.
It offers a full-screen map with keyboard focus containment, Escape return
and a selected-formation path back to orders. Formations and Orders own their header toggles;
the larger inspector keeps draft, review, issue and recovery controls next to selected context.
These controls alter browser presentation only and preserve the fixture authority labels.
The [MAP-04 outward contract](src/Cna.Game.Contracts/README.md) specifies development-only
browser routes, a coherent audience view, opaque action capabilities and lossless audience
revision strings. Private authority versions stay server-side. Its generated client compiles;
local bootstrap, creation and initial-view handlers exist; public action submit and receipt
lookup handlers remain pending. The Docker preview remains synthetic. The
[MAP-05 trusted host protocol](docs/specs/maproom-trusted-host-protocol-v1.md) fixes private
accepted-step/publication and restore boundaries for later host slices; no campaign grain or
durable provider is implemented by that contract. The
[MAP-06 Core adapter](src/Cna.Core/Hosting/CampaignHostAdapter.cs) now wraps admitted Rules9
creation, coherent side observation/legal cuts, System legal queries, exact accepted evidence
and private transcript re-adjudication. It does not perform seat authorization, conditional
publication or browser mapping. The
[MAP-07 temporary grain](src/Cna.OrleansHost/TemporaryCampaignGrain.cs) now owns one admitted
Rules9 fixture per local in-memory activation. Serialized grain calls install Core successors
with a process-lifetime in-memory request/receipt record that survives grain deactivation but not
host restart. The local Orleans client gateway is disabled, and an internal grain interface
requires a process-only capability before any private data or System action. Its memory commit
has no await or provider I/O, so pending is not externally observable in this slice; provider-backed
publication retains pending, fencing and reconciliation requirements. System
progression has a 1–32 step budget and explicit
human, unsupported-phase and step-limit outcomes. Its small typed side summary and canonical
legal set come from one committed MAP-06 cut. MAP-08 adds local operator and seat authorization;
MAP-09 privately binds a seat-scoped outward request to a current Core candidate and retains
exact retries. Public submission and receipt readback, coherent successor views, Core terminal
classification and durable conditional publication remain later gates. A process restart discards
this state. MAP-08A adds Development-only loopback HTTP bootstrap,
trusted-host grant storage and rotating seat sessions with same-origin and CSRF checks. Its grant
issuer now serves one admitted fixture through a Development-only creation route. Its retained
operator/request response has one grant pair; capacity is reserved before the grain commit,
inactive grants become redeemable only after committed creation, and an uncertain call retries
under the same request ID without renewing expired grants. Missing configuration identity is omitted. A
created initial fixture may return one
authorized `/view` mapped from a coherent typed cut; unsupported or successor cuts return
`503 unavailable` until a coherent public successor projection is supported; private diary
approval alone does not publish that view.
The main Angular Maproom route now offers a URL-backed `?mode=campaign` local read mode
using the same map presentation as the synthetic fixture study; `/connected` remains a
development deep link. Campaign mode uses the initial cut through a loopback-only
4300-to-5087 development proxy. It bootstraps, creates the one fixture,
redeems one grant and renders an authorized initial operations read. MAP-10 extends the bounded
read-only workspace without submitting an order or reading a later cut. A failed refresh clears
its prior audience view. The host retains one process-local creation for the exact
operator/request; a reload with the same valid seat cookie resumes through the
scoped session read, while a lost cookie or different operator conflicts until host restart.
No connected action submission, successor read, durable save or complete hot-seat
recovery is provided by this browser lane.
The [MAP-08B1 projection prerequisite](docs/specs/maproom-side-projection-prerequisites-v1.md)
first defines typed side fields, public presentation sources, diary visibility and stable opaque
action mapping; no browser view is returned until these sources are coherent.
MAP-08B2 implements a process-local private cut tracker over both committed side observations.
It binds campaign/source pins and authority version, advances contact/action identity on each
accepted transition, and fails closed on a skipped or incoherent cut. A private grain read scopes
opaque handles to the authorized session generation. Initial lab view uses a versioned,
source-pinned public layout with generic contact labels, own formation labels and reserve status.
The nine-entry en-CA copy catalog is owner-approved and the private tracker retains accepted
classified entries, but public post-step diary mapping and successor views still fail closed. MAP-09 private
ingress exists; no HTTP action submit or receipt route is enabled.
`CampaignAudienceOutcomeSource` is a private Core hosting decision proof: it recomputes an
accepted step in memory against canonical evidence and both side cuts, without a second
publication or state mutation. The temporary Orleans grain now asks for that proof before
installing an accepted successor, and its private audience tracker checks the exact prior and
successor head, canonical command/event/receipt and both side cuts in one serialized turn.
It retains approved fixed copy in private per-audience diary records with monotonic cursors and
the latest 50 entries; outward entry IDs are scoped to the seat-session generation. These private
records do not enable successor browser views or durable diary publication.
No canonical legal-action bytes or authority versions are serialized to HTTP. The
[Angular workspace](src/Cna.Maproom/README.md) provides
`projects/maproom-alpha` and the internal `@sandtable/field-atlas` library. Storybook uses the same
public component API, tokens and Material theme; feature patterns stay in the app. No shared
core/utils library exists until concrete reuse calls for it. A temporary process-lifetime campaign
host serves the admitted local fixture; durable hosting remains pending. Broad logistics
workspaces and semantic zoom await their own contracts and checks.

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
| `Cna.OrleansHost` | Development-only single-fixture temporary campaign grain; in-memory state survives activation churn but not host restart; no client gateway or production clustering/storage provider |
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

The permanent Combat fog path now begins with Core-owned
[contact knowledge](docs/specs/combat-contact-knowledge-v1.md). Authority
transitions retain private target bindings and replay into observer-scoped current
or stale contacts; the side view and visibility-qualified target candidates
omit those bindings. Creation, ordinary Movement, and Reaction movement cuts apply the permanent
adjacency rule, with exact contact-record checks against independently replayed
authority events. The Core journal advances across accepted turns without new sight,
and an internal owner movement cut binds its contact view and visible movement offers
to the same authority version. The trusted Host adapter receives canonical owner-visible
bytes separately from the private authority fence and Core action map. The normal Host's
[durable authority journal](docs/specs/campaign-durable-authority-journal-v1.md) now retains
canonical creation and accepted-step evidence in SQLite, commits each step with its head,
atomically retains the corresponding canonical contact records, and requires full Core replay
and contact comparison before a cold read or next append. Its durable side grants
and credentials are bound to revocable seat epochs. An internal owner-opening query stores
visible bytes and HMAC action references separately from the current private authority fence
and Core action map; equal visible cuts keep the same refs across hidden transitions. Internal
contact-derived movement submission checks those references against the current visible cut and
offers, accepts an unchanged opening across private authority advances, and retains a distinct replay kind,
with exact retry returning the retained receipt. The first internal native Combat decision
stage accepts a current-contact assault or a source-eligible voluntary no-attack choice at
Position Determination. Only selected assault stores a private defender binding; both choices
record the exact Barrage successor and verify the resulting V12 checkpoint through Exercise and
Host import replay. For an accepted voluntary finish, Core now records one V13 System event with
the five exact remaining no-attack structural successors through same-slot Reserve Release entry;
replay rejects forged or omitted positions and preserves prior World and contact records. The
normal Host commits owner decision and System completion in one SQLite transaction, then returns
the owner's retained receipt across restart. A selected infantry assault takes a V14 System
no-effect Barrage step to the same-slot Retreat Before Assault position. Core replay verifies
its source capability and exact route, and Host retains both steps atomically. Defender
Retreat Before Assault candidates use the bound defender's own formation, current
contacts, published map and movement costs. Hidden opposing occupancy cannot remove an
offer; an unseen encounter needs an accepted event rather than a hidden rejection.
The trusted defender draft keeps canonical visible bytes separate from private action IDs
and authority version. An explicit defender decline is a V15 owner event through the
normal durable Host seat/opening/receipt path; silence remains pending. Actual retreat
movement now uses a V16 retained Core event: the first accepted move updates World and
both contact audiences, returns to a defender RBA cut, and cold replays through the
normal durable Host seat/receipt path. Further offered moves append ordered events
through the same path; checkpoints bind their predecessor by hash without nesting each
prior World. An explicit stop from the updated cut reaches Force Assignment.
The first Force Assignment Core cut derives each owner's own full-current-TOE
infantry allocation without exposing the opposing allocation or private version.
Core V17 records two owner-only seals at Force Assignment. Core V18 System
completion then reaches Anti Armor. Normal Host retains each accepted seal
under its own seat, opaque ref and cold-replay receipt, appending V18 with the
second seal atomically. Normal campaign creation now accepts an explicit seven-window
timing configuration, serializes its exact rules/content-bound bytes into SQLite schema 7, and binds them in
the initial journal hash. Existing schema-5/6 journals remain readable; new campaign
creation in them is closed. No configuration means no Force Assignment opening or seal.
The normal Host stores a shared Force Assignment round for both seats with trusted opened,
deadline and high-water times. It gates each seal, retains a Core V19 cancellation after
deadline or clock failure, and verifies round status and evidence on cold replay. V19
preserves zero or one seal and advances to Anti Armor. Core V20 then records an
explicit no-effect Anti Armor outcome for infantry-only content, bound to that Force
event; normal Host appends it in the same transaction and verifies cold replay.
For a cancelled Force round, Core V21 completes Close Assault without a result
draw and advances to same-slot Reserve Release; normal Host retains that event in
the same transaction. Positive Close Assault adjudication remains to implement.
Source-backed disposition for
contacts lacking combat components remain before issuance. Reserve Release, assault adjudication and the public
opening route remain unfinished.
Encounter adjudication and authenticated Host owner lifecycle must precede
issuance of those offers. Complete detection policy,
coherent owner actions and public readers must join this path before activation.

Task019F0's private actual-selection executable contract merged in PR #161; Task019F1's
private native consumer merged in PR #165. Task019F2 defines actual prepared-round entry;
Task019F3 implements its private native consumer in PR #169, requiring two independently
trusted ledgers and reaching prepared entry or cancelled no-attack Release. Task019F4 (PR #172)
authenticates full prepared entry with an empty assault ledger and derives pure arithmetic
assessment. That predecessor performs no paid assault, cost debit, RNG advancement or settlement.
Task019F5 (PR #175) adds a separate private actual paid-assault executable reference: full actual
lineage and three independent ledgers authenticate paired debit, ordered RNG/result identity,
owned World projection, immediate settlement and one CA completion into same-cycle Reserve Release.
Four genuine ordinary histories remain distinct from isolated complete selected-outcome support.
Task019F6 (merged PR #177) adds the private native C# consumer, including native admission-only proof derivation,
complete arithmetic support before paired debit, original World projection and reference byte
parity through immediate CA completion. It accepts authentic creation context and three independently
retained ledgers; imported proof claims confer no authority. Its 64 physical dependency checks retain
the unchanged 62-path paid wire manifest, with two separate delivery pins. Focused native gates,
fresh review2 Ready and exact-head CI2803/2803 validated this merged private implementation.
PAC01 defines an additive actual-source post-assault contract: full paid replay and four independent
ledgers authenticate empty Release and guarded repeat/finish, retaining original Movement-end proof,
complete member history and actual commitment receipt/hash progress. Original World/RNG/CP remains
unchanged; CP9 is a prospective witness only. Repeat opens ordinal2Movement and finish enters
same-slotTruckConvoy; neither executes its successor. Valid inherited timing variants preserve
this bounded ordinary source profile; synthetic settled readers remain excluded authority.
PAC01 (PR #178) merged after independent Ready review and exact-head CI; its separate Python
normal passed431.075s/900. CS01 now implements the private native continuation consumer with
full native paid replay, four independent ledgers, original members/Movement/progress, owned
outputs and exact reference byte parity. Native physical acquisition verifies the unchanged
64-path wire manifest plus two PAC01 schema/oracle delivery files. CS01 merged in PR #179 after independent Ready review and exact-head CI2843/2843.
The additive [actual repeated Movement contract](docs/specs/combat-actual-repeated-movement-v1.md)
replays that repeat37 ancestry with five independent ledgers and owns up to four safe ordinary
moves. Cumulative CP5→9→11→13→15, incremental DP0/1/2/2, one Contact break-off, member spending
and actual ordinal2 move progress preserve original Movement/closure/World provenance and RNG.
Reaction-adjacent destinations reject before mutation even with empty enemy ammunition. This is a
private Python reference, with final normal344.251s/900 and unchanged frozen literals; NRM01 now implements the private native move-only consumer. Full native continuation/context replay and five
independent ledgers derive original provenance and all four safe moves with exact Source/Control/Proof/event
parity. Native physical delivery checks68 paths while preserving the wire66 manifest and two separate
reference deliveries. Deferred CS01 all-prior continuation retries are now covered for repeat/finish,
with valid changed/unavailable clocks and predecessor runtime unchanged. Actual stop/resolution/completion
now have a private [actual completion executable contract](docs/specs/combat-actual-repeated-movement-completion-v1.md).
It authenticates all five ancestor ledgers before a sixth lifecycle ledger: zero derived idle
completes37→38; positive actual tracks require owner stop/System empty resolution/owner completion
through41–44 at ordinal2 Breakdown Determination. Full parent Proof, old/current Movement proofs,
World/resources/NONE members/RNG and actual move progress persist. The reference itself executes no downstream phase. C04 now consumes it with a private native
consumer/codec: six independent ledgers authenticate the full native lineage before claims,
20traces/52events/72cuts preserve exact reference bytes and all-prior retries. The stop interrupt
requires trusted System resolution even with an empty cohort; completion derives current endpoints
and historical/current exclusion union with the actual event receipt while retaining ordinal1 proof.
70physical deliveries retain unchanged68wire pins plus2frozen reference files; native outputs are
owned bytes. Native completion still has separate fresh review/CI gates, and public/downstream
activation remains open. NRM01 merged in PR #181 after independent Ready
review and exact-head CI2879/2879; full selected-branch campaign remains a subsequent gate.
Production ingress authentication,
later-II/consumed Reserve lineage and public activation remain gated. Private settled controls
still retain synthetic earlier Movement; these checkpoints do not prove the authentic continual
cycle. The [roadmap](docs/roadmap/pre-alpha-roadmap.md) owns detailed sequencing, and the
[first-release audit](docs/research/2026-10-05-first-release-audit.md) owns release gap measurements.
The [historical ledger](docs/research/2026-10-05-project-documentation-snapshot.md) preserves former
checkpoint evidence without making it current runtime truth.

[1]: https://learn.microsoft.com/en-us/dotnet/orleans/grains/external-tasks-and-grains "External tasks and grains - .NET | Microsoft Learn"
[2]: https://learn.microsoft.com/en-us/aspnet/core/grpc/performance?view=aspnetcore-10.0 "Performance best practices with gRPC | Microsoft Learn"
[3]: https://learn.microsoft.com/en-us/aspnet/core/grpc/deadlines-cancellation?view=aspnetcore-10.0 "Reliable gRPC services with deadlines and cancellation | Microsoft Learn"
[4]: https://learn.microsoft.com/en-us/aspnet/core/grpc/json-transcoding?view=aspnetcore-10.0 "gRPC JSON transcoding in ASP.NET Core gRPC apps | Microsoft Learn"


The additive [actual ordinal2 Breakdown completion reference](docs/specs/combat-actual-repeated-breakdown-completion-v1.md)
authenticates complete RMC source and six independent inputs before its seventh Breakdown ledger.
One trusted System completion advances38/41..44 to39/42..45 at canonical Combat Position Determination.
Both Movement proofs, exclusions, World/resources/RNG/progress and active ordinal2 are preserved;
local fragment closure does not close that Combat cycle. Measured70external+2own physical acquisition
and dominating128MiB own Source/depth42 envelope preserve RMC64MiB/depth40 and earlier limits.
This reference executes no Combat step. B03 now consumes it through a private native consumer/codec:
full trusted creation context and six native ancestor ledgers precede the independent seventh ledger
and owned claims. All20 literal histories/40cuts/20exactretries preserve reference bytes. Own raw128MiB/
depth42 preparse retains inherited64MiB/depth40; physical72delivery checks preserve the70wire manifest
and two separate reference files. Warm fixture replay observed~1.06–1.10GB cumulative thread allocations
at~606–622ms, not peak/live memory or near-limit/public capacity proof. Independent review/exact-head
hostedCI/public activation remain separate gates.

The private [authentic moved-repeat Breakdown reference](docs/specs/combat-actual-moved-repeat-breakdown-completion-v1.md)
replays the original CreationRequest and ten independent R02 ledgers before its own eleventh
Breakdown input. One trusted System completion enters ordinal-two Combat Position Determination
without executing Combat or closing the active cycle. It preserves both Movement proofs and adds
one receipt to the cumulative and local Breakdown ledgers. All 300 authentic terminal replays and
the final vector audit pass, covering 36 paid and 264 armed rows. All 788 authentic nonterminal
cuts reject. The frozen schema passed exact-head replay and vector/cut audit. The additive private native R03
consumer authenticates the original CreationRequest, ten independent R02 ledgers, 108 pinned runtime
sources and the frozen schema/verifier before its own Breakdown input. All 300 frozen terminal
vectors match native Source/Event/Control/Proof hashes before and after the one System event; seven
focused acceptance/rejection tests pass. Release parity ran in 8m58s locally, with five measured
CI process groups. The private consumer merged in PR #216 after independent review and exact-head
hosted CI; no public activation follows.

The [P01 ordinal-two Combat Position design](docs/specs/combat-actual-moved-repeat-position-v1.md)
starts from that exact R03 suffix and requires CreationRequest plus all eleven independent input
ledgers before private assessment. Its 44 candidate and 256 empty shapes remain provisional; all
assessments are unsupported and zero are admitted. The 44-row classification is not a proof of 44
supported transitions. The [selected-assault successor](docs/specs/combat-p01-selected-assault-v1.md)
uses two representative positive roots, one per acting side, for bounded private transitions; its
300-root audit classifies candidate shapes. The [Force Assignment successor](docs/specs/combat-p01-force-assignment-v1.md)
continues those bounded histories to Prepared or cancellation before any debit. Separately, the
[private paid-commit reference](docs/specs/combat-p01-paid-commit-v1.md) authenticates all 300 roots,
executes one atomic preflighted cost event for each of the 44 positives, and leaves 256 candidate-absent
and zero admitted/public actions. It does not roll a result or settle a World. The
[private owner projection draft](docs/specs/combat-p01-owner-projection-v0.md) separates an owner
alias from the hidden candidate. The private owner decision codec's visible
fields are approved and implemented as an internal pure Core codec. Authenticated owner
openings and equivalent-history privacy proof are still missing. Accepted `CMB-POL-009`
excludes source-legal voluntary surrender under Land §15.88 from the initial RC, so the bounded
exercise cannot claim the full physical action surface. These private slices do not enable P02
selection, a native ordinal-two consumer or a public action. Full AC-003 action exhaustiveness,
AC-006 surrender/RBA, AC-007 outward privacy, AC-008 replay/resource caps and the P01 support
certificate remain open.
The [private Content8/Setup8 source reader](docs/specs/combat-auto-surrender-core-reader-v1.md)
pins exact asymmetric source bytes inside Core without registering a campaign creation or
predecessor-ledger issuer. The original Content7/Setup7 creation path remains separate.
The [pre-issue source seal](docs/specs/combat-auto-surrender-preissue-source-v1.md) canonically
retains all three complete artifacts and their initial closed inventory. It has no actor,
Rules/configuration selection, Created event, or authority to issue predecessor ledgers.
The [Choice L host foundation](docs/specs/p01-private-host-foundation-v1.md) is an internal,
unregistered `Cna.OrleansHost` SQLite store for durable local grant custody and fenced test
probes. The [private A1 physical import](docs/specs/p01-a1-physical-host-import-v1.md)
uses Core validation of the pinned R03 archive, then atomically binds canonical
import, checkpoint and same-cut bytes to an unissuable host campaign. It does
not publish a Chronicle game event or serve an owner opening. Core's same-cut
P01 projection and an authenticated host submission path are later gates.
The [private P01 no-attack v0 boundary](docs/specs/combat-p01-no-attack-v0.md) keeps proposed
ordinal-two System inputs disconnected until a source-backed supported-empty certificate and
authentic twelfth-ledger event/receipt replay exist; provisional physical emptiness is insufficient.
The [private explicit-RBA successor](docs/specs/combat-p01-explicit-rba-v1.md) handles a narrower
nonadjacent case: source-backed absence of an **assault** target does not erase the defender's
post-Barrage RBA decision. It retains trusted actor/deadline, explicit decline or cancellation,
and ordered no-assault step receipts in a private replay. It does not admit a public/native choice
or close the broader action, privacy, surrender and restart gates.

The additive [actual Reaction-to-Combat entry](docs/specs/combat-actual-reaction-combat-entry-v1.md)
authenticates complete direct Reaction closure with separate prefix/lifecycle/Breakdown caller ledgers.
Both owner routes and decline/unavailable/timeout reach certified positive entry18 with CP4/seed1cursor2.
Owned Control composition uses strict Reaction World plus typed suffix; frozen ordinary World writer
remains unchanged. Final Breakdown event/hash/receipt are structural. No public/host/Exercise/paid
activation; fixed-version/CP0 downstream contracts need explicit successors. This entry merged in PR #187.

The private [actual moved-assault successor](docs/specs/combat-actual-moved-assault-v1.md) independently
replays the original creation request and six caller ledgers. Its own Selection/Round/Paid source family
retains the two actual moves, MovementEnd, cycle, receipts and RNG. Commit adds five CP to the moved
attacker's four and three CP to the defender, consumes ammunition once, and draws no random bytes.
Mandatory settlement reaches Reserve Release entry; no-attack and cancelled branches preserve prior
costs. Before fresh debit, all 16,616 selected catalogue records and handler/window/projection capacities
are certified against the same cumulative World. Catalogue projections are isolated support checks.
This successor remains private and merged in PR #188 after review and hosted CI.

The additive [actual moved continuation](docs/specs/combat-actual-moved-continuation-v1.md) authenticates
paid and armed N1 terminals through seven independent ledgers, completes empty Reserve Release with
two System publications, and derives immutable assessment. Original NONE membership, current spent
CP, original MovementEnd, real progress and future duties survive. Safe prospective witnesses remain
separate from Reaction-dependent unsupported witnesses; unsupported capability is never noLegal.
The prospective nextOrdinal2 certificate has executionAvailable=false and no control window. This
private boundary does not accept Repeat/finish or execute another occurrence. It merged as PR #189
and main CI, CodeQL and links passed at 2e18cfebf33a69ea9204fccd96cf7df86a9aa26e.

The additive [actual moved cycle control](docs/specs/combat-actual-moved-cycle-control-v1.md)
uses eight independent ledgers and a current-cycle decision with configuration-bound window-local
clock recovery. Owner finish or admitted System fallback closes the actual ordinal1 occurrence and
reaches same-slot Truck Convoy entry; owner repeat-intent retains that occurrence and creates only
a source-qualified non-executable pending intent. World/RNG, member history, actual progress, target
use and future duties remain intact. N2 complete support and N3 uniform terminal retention laws
are isolated certification; they never manufacture future source ancestry. Actual repeat consumption,
ordinal2 publication/reset and public activation remain separate. N3 merged in PR #190.

The private [actual moved repeat Movement successor](docs/specs/combat-actual-moved-repeat-movement-v1.md)
authenticates the original creation request and eight N3 ancestor ledgers before reading its ninth
Movement ledger. `CampaignCombatActualMovedRepeatMovement` and its codec publish ordinal2 exactly
once from a real pending intent, retaining the old occurrence, target uses and progress as history.
Only current target uses and progress reset at publication; World, CP and RNG do not. Ordinary owner
moves use current geometry and cumulative CP15, with breakoff and incremental excess-CP DP applied
atomically. Accepted-command recovery precedes fresh movement legality checks after grammar/context
validation. The original MovementEnd remains ordinal1; no ordinal2 completion proof is manufactured.
Both paid and armed native paths have reference byte parity across 56 parents, 300 events and 356 cuts.
PR #192 merged at `3a7b626` after independent review and all 3,391 hosted test rows passed.
Post-merge [main CI run 38783352632](https://github.com/dills122/sandtable/actions/runs/38783352632)
verified all 3,391 rows at `3a7b626`, attempt 1. Stop, resolution and completion are not supplied by this
R01 move-only family. Older isolated or differently rooted contracts cannot substitute for these joins.

The separate private [actual moved repeat Movement completion contract](docs/specs/combat-actual-moved-repeat-movement-completion-v1.md)
(R02), implemented by `CampaignCombatActualMovedRepeatMovementCompletion` and its codec,
authenticates all 10 independent ledgers. A genuine zero-move occurrence completes with one owner
event. A positive occurrence requires owner stop, empty System resolution and owner completion,
three events, while preserving World, resources, RNG and history. The historical ordinal1 proof and
receipt-bound ordinal2 proof remain distinct. The terminal enters Breakdown Determination; it does
not execute Breakdown. Local validation passed 325 rows across 20 fresh class processes, and
independent implementation review is complete at `07fac9f`.
[PR #194](https://github.com/dills122/sandtable/pull/194) merged at `121baa5`;
[main CI run 38830848442](https://github.com/dills122/sandtable/actions/runs/38830848442), attempt 1,
verified all 3,716 rows across 26 execution processes. Public ingress, Runner adoption, acceptance of all 72 criteria and
the first release candidate remain open.
