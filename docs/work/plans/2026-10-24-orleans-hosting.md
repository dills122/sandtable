# Phased plan: durable Orleans campaign hosting

Recorded 2026-10-24. Status: proposed; all implementation tasks unstarted.
[Architecture record](../../design/durable-orleans-campaign-hosting.md) retains discussion,
alternatives, invariants, open decisions and pinned baseline evidence.
Related: [issue #330](https://github.com/dills122/sandtable/issues/330).
This plan does not replace RC certification or authorize production deployment.

## Delivery rules

Owner roles are responsibilities, not assigned people or active agent work. Each H task is a
focused PR/session; split further if implementation exceeds roughly five files. Define schemas
before consumers. Every completion requires commit, exact verification commands/exits/counts,
retained evidence path, review disposition and status update here. Ruflo aids retrieval; repository
documents are canonical. Use seeded fixtures/fake clocks for correctness, real clocks for latency.

## Phase 0 — workload agreement and baseline

- [ ] **H01: Define workload/SLO profiles.** Owner: product/operations. Depends: none.
  Scope S: design record and benchmark manifest. Capture concurrency/growth, live/async mix,
  players/spectators, AI share, scenario/history sizes, hosting, latency/deadline/recovery/cost goals.
  Acceptance/verification: owner-reviewed manifest; unknowns explicit; thresholds set before G0.
- [ ] **H02: Measure SQLite journal.** Owner: Host performance. Depends: H01 for final targets;
  exploratory measurements can start earlier. Scope M: harness, fixtures, report. Exercise
  create/submit/retry/deadline/reload with varied history and >16 campaigns per lane.
  Acceptance/verification: reproducible raw runs, transaction holds, deadline lag, owner-write
  latency and canonical-history checks; replay verification unchanged.

**Gate G0:** workload/thresholds reviewed and baseline reproducible. No capacity promise.

## Phase 1 — persistence and authority contracts

- [ ] **H03: Specify durable commit protocol.** Owner: Host/Archives. Depends: G0.
  Scope S: persistence spec and failure matrix. Define atomic records, expected head, dedup,
  activation recovery, commit ambiguity, pending state and stale finalizers.
  Acceptance/verification: walk every crash window and explicitly reconcile remote-I/O policy.
- [ ] **H04: Extract SQLite repository boundary.** Owner: Archives. Depends: H03.
  Scope M: interface, adapter, parity tests. Keep current paths working with unchanged bytes.
  Acceptance/verification: fixtures preserve receipts/hashes/deadlines/private views for accepted,
  rejected and retried commands; record supported schema matrix.

**Gate G1:** commit contract reviewed and SQLite parity passes; provider remains provisional.

## Phase 2 — durable ownership on one silo

- [ ] **H05: Create/recover campaign grain.** Owner: Host. Depends: G1.
  Scope M: lifecycle, repository binding, tests. Recover only verified committed authority.
  Acceptance/verification: deactivate/reactivate and fresh-process restart match head/private cuts.
- [ ] **H06: Route one submission slice.** Owner: Host. Depends: H05.
  Scope M: command path and retry tests. Implement H03 with pending limits and durable receipts.
  Acceptance/verification: identical retries match, conflicting reuse rejects, concurrent writes
  cannot fork history and pre/post-commit crashes never publish speculative state.
- [ ] **H07: Deliver committed outbox work.** Owner: Host/DecisionWorker. Depends: H06.
  Scope M: outbox adapter and tests. Atomic enqueue; bounded delivery and consumer dedup.
  Acceptance/verification: crash between commit/delivery, duplicate delivery and revoked-seat tests;
  no hidden state in outward messages or inference inside authority turns.

**Gate G2:** durable slice passes fresh-process and duplicate-delivery checks. Retain legacy paths
until remaining command families achieve equivalent contract coverage.

## Phase 3 — shared database and durable deadlines

- [ ] **H08: Select/implement shared storage for slice.** Owner: Archives/operations. Depends: G2.
  Scope M per adapter slice: provider decision, schema, adapter, integration tests.
  Acceptance/verification: compare hosting fit/recovery/cost; SQLite parity, CAS races, ambiguous
  commits and real-provider crash tests pass.
- [ ] **H09: Schedule persisted decisions through grains.** Owner: Host. Depends: H08.
  Scope M per decision family: scheduler, indexes/claims, tests. Preserve all seven lane semantics
  and high-water behavior with bounded fair dispatch.
  Acceptance/verification: before/at/after expiry, duplicate/reordered wakeups, clock failure,
  restart catch-up and command/expiry races follow frozen contract.
- [ ] **H10: Compare storage/scheduler alternatives.** Owner: Host performance. Depends: H09.
  Scope M: harness/report. Repeat H02 profiles plus simultaneous expiry and player traffic.
  Acceptance/verification: distributions/backlog/resource/cost results explain regressions;
  checkpoints/caching require explicit trust model and full replay audit evidence.

**Gate G3:** all command/deadline families in release target have parity and baseline comparison.
Shared storage is proven independently of multi-silo placement.

## Phase 4 — multiple servers and operational safety

- [ ] **H11: Configure production cluster.** Owner: operations/Host. Depends: G3.
  Scope M: membership, identities, configuration, smoke harness. Keep development defaults local.
  Acceptance/verification: multi-silo routing and idle recovery work; configuration fails closed;
  incompatible rules/content cannot acquire authority.
- [ ] **H12: Prove faults/upgrades.** Owner: Host reliability. Depends: H11.
  Scope M per failure packet: harness/evidence. Test silo kill, partition/stale writer, database
  outage/failover, graceful drain and compatible/incompatible upgrade routing.
  Acceptance/verification: no lost acknowledged commits/duplicate steps; record recovery and
  backlog drain while running on surviving capacity.

**Gate G4:** failure correctness and N-1 performance meet targets. Fresh-process Reserve/Custody/
Retreat and public certification remain separate gates; reuse evidence only for applicable exact
release head and storage path.

## Phase 5 — migration and supported capacity

- [ ] **H13: Implement import/cutover.** Owner: Archives/operations. Depends: G4.
  Scope M per supported schema: importer, verifier, runbook. Quiesce, verify, switch ownership,
  resume original deadlines and retain source read-only.
  Acceptance/verification: exact history/head/private-cut equality, overdue recovery, stale-writer
  rejection and rollback boundary proved; unsupported writable migrations reject.
- [ ] **H14: Publish capacity/operations envelope.** Owner: operations/performance. Depends: H13.
  Scope M: load/soak report, admission policy, runbook. State hardware, workload class, resident/
  stored campaigns, action/deadline rates, connections, storage growth and budget.
  Acceptance/verification: repeatable normal/burst/N-1 runs meet targets with bounded queues;
  backup restore passes; document admission/backpressure and scale-out triggers.

**Gate G5:** reviewed capacity report plus independent release/product/security gates before
deployment. Capacity approval does not admit unsupported rules, scenarios or multiplayer flows.

## Benchmark protocol

Compare identical manifests at H02/H10/H14. First vary one factor, then mixed loads and skewed
hot campaigns. Choose history/scenario sizes from fixtures and record exact accepted-step counts.
Try 1, 16, 17, 32 and larger campaign counts per lane until saturation or an agreed abort threshold;
these are experimental points, never advertised capacity. Test each lane and mixed lanes.

| Axis | Required observations |
| --- | --- |
| Core/scenario/history | CPU, allocation, replay/activation duration, checkpoint bytes |
| Idle/resident/active mix | Per-activation memory, RSS/GC, idle storage/scheduler load |
| Commands | Offered/completed/rejected rates, p50/p95/p99, per-campaign queue age |
| Persistence | Commit latency, locks/transaction hold time, conflicts, IOPS, write bytes |
| Deadlines | Due-to-commit lateness, oldest backlog, rotation/fairness, owner-write responsiveness |
| Fanout/reconnect | Projection cost, bandwidth, connections, audience isolation |
| AI | Queue/provider delay, quotas, fallback rate, cost, effect on human latency |
| Faults/upgrades | Lost acknowledged commits, duplicate steps, recovery, replay surge, stale writers |
| Soak/maintenance | Memory/storage growth, backlog drift, backup/maintenance interference |

Retain revision/build/runtime/provider versions, hardware/limits, topology, seed/config hashes,
arrival model, warmup/duration, repetitions/sample counts and raw histograms/traces. Separate
cold/warm and real-provider/fake results. Include failed/rejected/censored requests. Use scheduled
open-loop arrivals as well as closed-loop sessions so client waiting cannot hide overload.
Run load generators outside measured hosts. Exclude hidden player state and credentials from telemetry.

Performance runs also check canonical accepted history, receipts/private cuts and duplicate-sweep
no-ops. Timing/arrival differences can legitimately change accepted ordering; claim identical
histories only with matching inputs and trusted timing evidence. Do not remove verification to
improve throughput.

Capacity is the highest tested sustainable envelope meeting all targets with failure reserve,
not peak throughput. CPU, memory, database, scheduler, network and AI can bind independently.
More silos do not imply more database capacity. Define per-campaign fairness, admission and
overload behavior before queues become unbounded.

## First execution packet

Start with H01 and an H02 harness proposal. Product/operations supply concurrency/growth, cadence,
hosting constraints and latency/recovery/cost targets before G0 closes. No calendar estimate or
provider commitment until those inputs and baseline exist. Plan review precedes implementation.
