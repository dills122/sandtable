# Durable Orleans campaign hosting

Status: proposed architecture, recorded 2026-10-24. Documentation only; no production
hosting, provider choice, capacity limit or public campaign admission is established.
The [phased plan](../work/plans/2026-10-24-orleans-hosting.md) owns implementation sequencing.

## Baseline and scope

Evidence baseline: `fc4cb9255dd6d891a2e333fc82f42d8e7afe5a2b`, merged
[PR #329](https://github.com/dills122/sandtable/pull/329).
[Issue #330](https://github.com/dills122/sandtable/issues/330) tracks scaling and separate
reliability, certification, schema compatibility and product gates.

At that revision:

- Core owns deterministic adjudication, RNG and private observations.
- [TemporaryCampaignGrain](../../src/Cna.OrleansHost/TemporaryCampaignGrain.cs) uses a
  process-local dictionary; its receipts do not survive process loss.
- [Program](../../src/Cna.OrleansHost/Program.cs) registers the SQLite journal separately
  and rejects production startup without a production clustering provider.
- [Timeout service](../../src/Cna.OrleansHost/Api/CampaignCombatTimeoutService.cs) sweeps
  seven sequential lanes on a one-second timer, up to 16 campaigns per lane with rotation.
  Sixteen is a batch size, not supported campaign capacity.
- [Selection ticks](../../src/Cna.OrleansHost/Api/CampaignDurableAuthorityJournal.SelectionRounds.cs)
  acquire an immediate transaction before loading authority and checking expiry.
  [Journal Load](../../src/Cna.OrleansHost/Api/CampaignDurableAuthorityJournal.cs) reads
  retained accepted history. History growth and write-lock contention need measurement.

Links resolve to repository files; factual baseline is pinned to the commit above. Graph
coverage lacked recent deadline files and journal parsing was partial, so relevant source
was checked directly at that revision. Fresh-process Reserve recovery remains separate work.

## Proposed topology and alternatives

One logical `CampaignGrain` per campaign ID, many activations per Orleans silo. Initially
keep campaign transitions serialized and non-reentrant; different campaigns run concurrently.
Activation memory is a verified cache of committed authority. Idle campaigns deactivate and
reload verified state on demand.

Authenticated API requests, durable scheduler wakeups and untrusted AI proposals converge on
campaign ownership. Core adjudicates. Committed outbox records drive authorized player updates
and optional AI dispatch. Chronicle retains authoritative history; Archives persists it.
Grain, journal, scheduler and outbox remain technical modules, not mandatory separate services.

| Option | Benefit | Cost / reason to reconsider |
| --- | --- | --- |
| Shared Orleans hosts | Campaign ownership, placement, shared resources | Shared storage and recovery must be proven |
| Host/database shards | Explicit capacity and failure boundaries | Routing, ownership transfer and balancing |
| Container per campaign | Strong resource isolation | Per-game overhead and lifecycle complexity |

Orleans is the candidate to evaluate. Add storage shards only when measurements or failure
isolation justify them. Grain identity is logical isolation, not a process sandbox. Placement
distributes compute; it does not automatically partition storage.

## Persistence and command protocol

Prototype a shared relational journal behind a narrow repository contract. PostgreSQL is a
candidate, not a selected dependency. Hosting fit, transaction semantics, recovery and measured
cost decide. SQLite remains a local backend and parity reference; a network-shared SQLite file
is not the proposed multi-server design.

Define schemas before adapters. One campaign transaction must atomically record:

- Expected-head compare-and-swap, new sequence/hash and ownership epoch if leases are used.
- Canonical accepted command/events/receipt and any checkpoint produced by that transition.
- Request identity, authorized actor/seat binding and payload fingerprint for durable dedup.
- Decision entry/closure, timing configuration, deadlines and clock high-water changes.
- Outbox entries with stable committed identities.

A transactional repository fits the journal without rewriting an ever-growing history blob on
every `IPersistentState<T>` save. Separate ordinary state writes do not form a cross-record
transaction. No cross-campaign distributed transaction is required initially. Database checks
must reject stale writers even when grain serialization normally prevents overlapping commands.

Validate authorization on retries. Identical authorized retries return retained receipts;
conflicting payload/actor reuse rejects. Compute a candidate through Core, persist against the
expected head, then publish state and acknowledge. Never expose speculative state. A timeout can
leave commit outcome unknown: resolve through the durable request record/head before retrying.
On conflict discard the candidate, reload and revalidate. Delivery retries are deduplicated;
transport is not claimed exactly-once.

### Explicit remote-I/O policy gate

Repository guidance keeps remote I/O outside authoritative grain turns. Awaiting a database
commit inside a serialized grain method conflicts with that literal rule. H03 must resolve
this before implementation; this proposal grants no exception.

Default compliant candidate: prepare/persist/finalize. Grain prepares a version-bound candidate,
marks pending and ends its turn. Hosting infrastructure persists outside that turn. Grain
finalizes from durable evidence. Competing mutations stay rejected/queued while outcome is
unknown. Recovery reads committed storage, not pending memory. Specify activation loading,
crash windows, abandoned pending work and stale finalizers before implementing the protocol.

Alternatively, a reviewed policy change could permit bounded storage awaits. Model inference
and unrelated remote work remain outside authority turns under either design.

## Deadline and AI lifecycle

Persist deadlines with decision entry. Index pending work by deadline/partition; bounded workers
deliver campaign/decision/version-bound wakeups. Claims coordinate scheduling, never authority.
Campaign rechecks the current decision and trusted clock; Core applies fallback. Duplicate or
stale wakeups append nothing. Catch-up must not starve player commands.

Preserve clock-unavailable, regression and high-water semantics. Selecting only overdue rows
would change current periodic clock observations unless a replacement is specified and proved.
Define player-command versus expiry ordering and retain timing evidence for replay. Activation
timers must not be the sole deadline record. Reminders can support coarse wakeups; second-level
scheduling requires measured latency evidence.

AI workers have independent queues, quotas, cancellation, deadlines and cost budgets. Deduplicate
generation by decision ID; reject stale proposals; retain deterministic scripted fallback.
Always carry decision ID, state version and rules/configuration binding. Never send hidden
opposing state to AI. Bind player caches/projections to authorized audience and campaign/version;
spectators require an explicit visibility policy. Never broadcast authoritative state.

## Operations, cutover and rollback

Production requires membership, cluster/service identities, health checks, graceful drain,
secure storage connectivity and compatible rules/content routing. Rolling binaries must not
reinterpret existing history. Budget CPU, memory, commit I/O, scheduler work and fanout separately.
Stored campaigns, resident activations and actively issuing players are distinct quantities.

Quiesce each source campaign, stop its writes/deadline dispatch, capture a consistent journal,
verify/import a supported schema, compare recovered history/head/private cuts, then switch
routing/ownership and resume original deadlines. Reject old writers. No authoritative dual writes.

Retain source read-only. Before destination writes, rollback may return to verified source.
After destination accepts writes, the old SQLite copy is stale: require verified reverse migration
or destination recovery. Never reset deadlines or fabricate missing historical timing.
Schemas 5–13 need explicit read-only recovery/migration policy; no writable upgrade promise.

## Open decisions and evidence

Unknown: launch/growth concurrency, players/spectators, AI share, live/async cadence, region/provider,
data residency, operations expertise, budget, command percentiles, deadline lag and recovery targets.
Also unresolved: remote persistence protocol, checkpoint trust model and dedup retention.
Initial correctness target: no lost acknowledged commit, divergent accepted history or unauthorized
view. Performance limits remain unset until workload/SLO agreement.

External references consulted 2026-10-24; pin implementation choices to the repository's Orleans version:

- [Persistence](https://learn.microsoft.com/en-us/dotnet/orleans/grains/grain-persistence/):
  explicit writes, direct database access and concurrency tokens.
- [Relational storage](https://dotnet.github.io/orleans/docs/grains/grain-persistence/relational-storage/):
  one state-record transaction does not make multiple state writes atomic.
- [Placement](https://learn.microsoft.com/en-us/dotnet/orleans/grains/grain-placement) and
  [activation collection](https://learn.microsoft.com/en-us/dotnet/orleans/host/configuration-guide/activation-collection):
  distribute activations and reclaim residency; neither substitutes for persistence.
- [Timers and reminders](https://learn.microsoft.com/en-us/dotnet/orleans/grains/timers-and-reminders):
  activation timers differ from persistent reminder definitions and coarse periodic wakeups.
