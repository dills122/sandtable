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
- Same gate, concurrent shards and the known Orleans outlier excluded: `just check-fast`
- Focused local suites: `just test-suite host` (also `core`, `runner`, `contracts`, `boundary`)
- Class/method scope: `just test-class core '*CampaignContact*'`, `just test-method host '*ForceRound*'`
- CI-compatible local chunk: `just test-shard fast-01`; list choices with `just test-suites`

During iteration, run focused tests and affected project suites instead of repeating the full
solution gate after every edit. Add boundary tests for authority/privacy changes and affected
consumers for Core/contracts changes. Use full local validation for release checkpoints or
uncertain impact; exact-head CI remains required before merge. Report selected and unrun coverage
honestly. See `docs/runbooks/local-testing.md`. These local commands build Release by default.

The repository uses .NET 10 native Microsoft.Testing.Platform mode. Always pass a solution through
`--solution` and a test project through `--project`; do not use a bare positional test path.

## Adding Tests (CI timing weights)

Adding, renaming or removing tests requires NOTHING in the CI timing manifest
(`.github/scripts/long-suite-weights/`). Rows without a committed weight are planned on a conservative
estimate and weights for deleted rows are ignored; coverage (every discovered row runs exactly once) is
still enforced strictly. Do not add or edit weight files for ordinary test changes. A genuinely heavy new
test fails loudly via the per-process deadline or memory guard; only then see
`.github/scripts/long-suite-weights.md` for the optional `long_suite_weights.py add` flow. A scheduled
refresh keeps the committed weights current.

## Branch And PR Metadata

- Use feature branches for behavior, contract, test, or documentation changes.
- Orchestrator-generated branch names (e.g. `orchestrator/<session-id>/...`) are acceptable
  alongside hand-named feature branches.
- Do not commit directly to `main`.
- When work is ready, provide the branch name, PR title, summary, and exact test evidence.

## Codebase search: use codebase-memory-mcp

This project replaced the prior "Context Engine (CCE)" setup (never actually
installed — `$HOME/.cce/hooks/cce_hook.sh` did not exist on disk) with the
`codebase-memory-mcp` MCP server, registered in this repo's `.mcp.json`. It
is a code-structure search/graph tool, not a decision ledger.

**Use the graph tools instead of reading files directly** when exploring the
codebase, answering questions about code, or understanding how something
works:

- `search_graph` — find symbols by name/kind.
- `trace_path` — find callers/callees.
- `get_code_snippet` — fetch source for a found symbol.
- `query_graph` — multi-hop structural queries.
- `get_architecture` — broad structural overview.
- `search_code` / grep — literal strings or anything the graph doesn't cover.
- `check_index_coverage` — call for every file you rely on; if incomplete,
  read the reported missed lines directly.

### Output style

This compressed style applies to chat replies only. PR descriptions, commit
messages, dispatch/worker briefs, review findings, and any text another
agent or reviewer will read must be written in plain, full sentences.
(Kept in sync with `CLAUDE.md`'s identical section; update both together.)

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
