# AGENTS

AI coding guidance for Sandtable.

## Purpose

Sandtable is a deterministic campaign simulation with an optional, non-authoritative intelligence
plane. Optimize for reproducible adjudication, strict fog-of-war boundaries, versioned typed
contracts, and graceful scripted fallback when model-backed services are unavailable.

The repository is scaffolded as `Sandtable.slnx`. Treat `tech-design.md` and `naming-overview.md`
as architectural rationale, `README.md` as the current project map, and keep all three synchronized
with implementation changes.

## Architecture Boundaries

- **Umpire / `src/Cna.Core`** owns authoritative state, rules, RNG, legal-action generation, fog of
  war, plan execution, victory calculation, and emitted game events.
- **`src/Cna.OrleansHost` and `src/Cna.DecisionWorker`** own campaign activation,
  pending-decision lifecycle, and asynchronous dispatch. Never hold an authoritative grain turn
  open on model inference.
- **`src/Cna.Intelligence.Gateway`** owns provider routing, personas, structured-output validation,
  deduplication, advisory memory, narrative, and evaluation. Treat every response as an untrusted,
  versioned proposal.
- **`src/Cna.Intelligence.Contracts`** owns protobuf and transport compatibility. A Dispatch is a
  domain message; Signals is the transport carrying it. The canonical contract is
  `src/Cna.Intelligence.Contracts/Protos/intelligence.proto`.
- **`src/Cna.ServiceDefaults` and `src/Cna.AppHost`** own shared service defaults and local
  orchestration. They do not own domain behavior.
- **Chronicle and Archives** own authoritative event history and persistence respectively.
  Human-readable War Diary output is derived data.

Keep flavor names for major products and domains, and use plain technical names inside modules.
Do not introduce a microservice where a domain module is sufficient.

## Contract-First Rules

- Update protobuf contracts before generated clients, servers, or adapters.
- Preserve protobuf field numbers and wire compatibility; reserve removed fields.
- Carry `decision_id`, `state_version`, and the ruleset/configuration hash across intelligence
  requests and responses.
- Define command, event, snapshot, and persistence schemas before implementations that consume
  them.
- Never send hidden opposing state to the intelligence plane.

## Reliability Rules

- Keep all model and remote I/O outside authoritative grain turns.
- Propagate cancellation and explicit deadlines through gRPC and provider calls.
- Retry generation only through an idempotent, deduplicated decision ID.
- Reject stale, malformed, out-of-bounds, or no-longer-valid model proposals.
- A timeout or unavailable backend must produce a deterministic scripted decision, not a failed
  game turn.
- Reuse gRPC channels; use unary RPCs for decisions and streaming only where progressive output is
  useful.

## Scope And Quality

- Keep changes small and explicit; avoid unrelated refactors and generated artifact churn.
- Preserve the authority hierarchy: Command decides, Staff plans, Dispatch carries, Umpire
  adjudicates, Chronicle remembers, and Maproom shows.
- Add focused deterministic tests for behavior and contract changes.
- Prefer fake clocks, seeded RNG, in-process test hosts, and provider-realistic integration tests
  over timing-sensitive assertions or remote dependencies.
- Update the design docs whenever architecture, names, contracts, setup, or workflow changes.

## Commands

- Restore: `dotnet restore Sandtable.slnx`
- Build: `dotnet build Sandtable.slnx --no-restore`
- Test: `dotnet test --solution Sandtable.slnx --no-build`
- Format check: `dotnet format Sandtable.slnx --verify-no-changes --no-restore`
- Run locally: `dotnet run --project src/Cna.AppHost/Cna.AppHost.csproj`
- Full local gate: `just check`

The repository uses .NET 10 native Microsoft.Testing.Platform mode. Always pass a solution through
`--solution` and a test project through `--project`; do not use a bare positional test path.

## Branch And PR Metadata

- Use feature branches for behavior, contract, test, or documentation changes.
- Do not commit directly to `main`.
- When work is ready, provide the branch name, PR title, summary, and exact test evidence.

## Context Engine (CCE)

This project uses Code Context Engine for intelligent code retrieval and
cross-session memory.

### Searching the codebase

**Use `context_search` instead of reading files directly** when exploring
the codebase, answering questions about code, or understanding how things
work. `context_search` returns the most relevant code chunks with
confidence scores instead of whole files.

When to use `context_search`:
- Answering questions about the codebase ("how does X work?", "where is Y?")
- Exploring structure or architecture
- Finding related code, functions, or patterns

Other tools:
- `expand_chunk` for full source of a compressed result
- `related_context` for what calls/imports a function
- `session_recall` to recall past decisions

### Cross-session memory

Call `session_recall("topic phrase")` before answering non-trivial questions.
Call `record_decision(decision="...", reason="...")` after making choices.
Call `record_code_area(file_path="...", description="...")` after meaningful work.

### Output style

Respond in compressed style. Drop articles (a, an, the) in prose. Use
sentence fragments over full sentences. Use short synonyms (fix not resolve,
check not investigate). Pattern: [thing] [action] [reason]. [next step].
No filler, hedging, pleasantries, trailing summaries, or restating what
the user said. One sentence if one sentence is enough.

When suggesting code changes, show only the changed lines with 3 lines of
context. Never rewrite entire files. Multiple changes in one file: show each
change separately. Never echo back unchanged code the user already has.

Code blocks, file paths, commands, error messages: always written in full.
Security warnings and destructive action confirmations: use full clarity.
