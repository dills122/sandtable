# Repository Scope And Priorities

Sandtable is a deterministic campaign simulation whose optional intelligence services advise the
game without owning or mutating authoritative state.

## Primary Deliverables

- A pure deterministic Umpire core for rules, plans, legal commands, RNG, fog of war, and events.
- An Orleans campaign host plus decision worker that keeps slow external inference outside grain
  turns.
- Versioned gRPC Signals contracts and an ASP.NET Core intelligence gateway with a first-class
  scripted fallback.
- Maproom, Chronicle, Archives, and War College capabilities built on the same authoritative event
  model.

## Core Priorities

- Determinism, replayability, and auditability.
- Stable typed contracts and explicit ownership boundaries.
- Fog-of-war isolation and least-data observations.
- Local/offline operation with graceful fallback.
- Observable, cancellable, deadline-bound external calls.

## Active Boundaries

- Umpire owns game truth. UI, Orleans infrastructure, Staff, and Intelligence cannot invent or
  directly mutate authoritative state.
- Command chooses intent; deterministic Staff converts intent into executable plans; Umpire alone
  validates and adjudicates those plans.
- Intelligence receives only redacted observations and bounded candidates. It may select or
  annotate a plan but never performs rules, pathfinding, supply arithmetic, combat, RNG, turn
  sequencing, persistence, or victory calculation.
- Chronicle is the authoritative event history. Archives persists campaigns, snapshots, replays,
  and derived artifacts. War Diary is non-authoritative narrative.
- Platform, intelligence, and compute capabilities may begin in one modular backend; split them
  only when deployment or scaling evidence requires it.

## Project Map

- `src/Cna.Core`: authoritative Umpire domain and pure validation logic.
- `src/Cna.OrleansHost`: Orleans silo and authoritative campaign host.
- `src/Cna.DecisionWorker`: out-of-turn intelligence dispatch.
- `src/Cna.Intelligence.Contracts`: protobuf source and generated transport contracts.
- `src/Cna.Intelligence.Gateway`: non-authoritative ASP.NET Core gRPC endpoint.
- `src/Cna.ServiceDefaults`: shared discovery, resilience, health, and telemetry defaults.
- `src/Cna.AppHost`: Aspire local orchestration only.
- `tests/Cna.Core.Tests`: deterministic domain tests using xUnit v3 and MTP.
- `tests/Cna.Intelligence.Contracts.Tests`: protobuf boundary compatibility tests using xUnit v3
  and MTP.

## Safe Refactor Boundaries

Do not change these without explicit intent and matching contract/documentation updates:

- the authority split between Umpire, Orleans, and Intelligence;
- protobuf field numbers, decision identity, state-version checks, or ruleset hashes;
- persisted event and snapshot semantics;
- fog-of-war filtering or who can access hidden state;
- the public names established in `naming-overview.md`;
- the requirement for a deterministic scripted fallback.

Safe defaults are feature-scoped improvements, stronger validation, focused deterministic tests,
and clearer typing within an existing boundary.
