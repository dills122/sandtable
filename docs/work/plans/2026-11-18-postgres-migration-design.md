# Moving the durable campaign journal off SQLite, onto PostgreSQL + Marten

**Status:** design converged through direct back-and-forth with the project
owner in a dedicated design session (not a one-shot proposal) — 2026-11-18.
**Not yet reviewed by the main orchestration session; no code changes
accompany this document.** This is the doc to review there before anything
gets implemented. Revised in place once during this same session after
real research corrected two of its own earlier working assumptions (§3.3,
§3.4) — corrections are kept visible below rather than silently folded in.

**Scope note on sources:** current-state claims about this repo are carried
forward, re-verified, from
[`2026-11-18-data-layer-design-1000-campaign-ceiling.md`](2026-11-18-data-layer-design-1000-campaign-ceiling.md)
(hereafter "the data-layer doc") and
[`2026-11-18-campaign-replay-snapshotting-design.md`](2026-11-18-campaign-replay-snapshotting-design.md)
(hereafter "the snapshotting doc"), both read in full before this discussion
started. Claims about Orleans and Marten are from live web research done
during this session (cited inline/at the end), not from training-data
memory — both the Orleans-provider ecosystem and Marten's actual feature set
turned out to matter in ways worth checking rather than assuming, and two
checks directly overturned this document's own first draft.

## 0. Why this reopens a question the data-layer doc already answered

The data-layer doc explicitly considered and rejected moving off SQLite
(its §3: "Do not move to Postgres"), for reasons sound *given its stated
assumption*: "RC ships SQLite-based, local-only, no server process to
administer." That assumption no longer describes the actual target.
**Local play is the engine-testing harness; the real product is a hosted,
multi-user web-app**, with a stated minimum supported load of ~100
concurrent active games. That fact invalidates the old doc's specific
argument, not the engineering work done under it.

This does not reopen anything about `Cna.Core`. The engine itself is already
storage-agnostic; only `Cna.OrleansHost`'s journal code (`CampaignDurableAuthorityJournal.cs`
and siblings) talks to SQLite directly. RC is not blocked by any of this.

## 1. Decision: migrate to PostgreSQL

Reasoning, each tied to a concrete gap in the old assumption:

- **Solves the clustering/grain-placement gap the data-layer doc's §3.2 left
  explicitly unresolved.** That section accepted "pin each campaign's grain
  permanently to the silo that created it, no failover" as a stopgap
  *because* a local SQLite file can't follow a grain to another silo and
  SQLite isn't safe over a network filesystem. For a real multi-user
  product, "if that machine dies, that user's campaign is unavailable until
  it comes back" is a real availability regression. A server-backed store
  removes the problem instead of working around it.
- **Matches the project owner's stated priorities** (crash-safety, "normal"
  responsiveness — see §8 on why raw write-latency was explicitly
  de-prioritized during this discussion) without inventing a new engine
  class: PostgreSQL's WAL-based crash recovery is materially more mature
  than SQLite's today.
- **Open/portable rather than cloud-locked**, matching "hosting target
  undecided" and the project owner's stated preference (SQL Server, blocked
  by licensing) — Postgres is the closest open equivalent in spirit.
- **Comfortably covers the stated scale.** ~100 concurrent active campaigns,
  each a few thousand rows at most (data-layer doc §3/§10) — a single
  reasonably-tuned Postgres instance handles this with large headroom.
- **Governing requirements unaffected:** `DET-001`/`DET-002`/`EVT-001`/`REL-001`
  (`docs/roadmap/pre-alpha-roadmap.md`) are properties of the event-sourced,
  hash-chained model itself, not of which engine stores it.

## 2. "N stores split by domain," made concrete

The project owner's original framing for this whole exercise was explicitly
**multiple stores** — "the hot one that takes a beating, cold storage/low-interaction,
and whatever else makes sense" — which is also exactly the data-layer doc's
own §2.2 principle. The first draft of this document under-delivered on that
by defaulting to "one shared schema." Corrected:

| Store | Backing | Scope | Written by |
| --- | --- | --- | --- |
| **Hot** | PostgreSQL + Marten (§3) | Active campaigns' event streams + projections | The owning campaign's grain, via Marten |
| **Cold** | A separate PostgreSQL database (own Marten `DocumentStore`, or a plain archival schema — §2.1 below) | Completed/cancelled campaigns, aggregated | The archival process, once per campaign |
| **Orleans infrastructure** | PostgreSQL via `Orleans.Clustering.AdoNet` / `Orleans.Reminders.AdoNet` (§4) | Silo membership, durable timer backstops | Orleans itself |
| **Session/auth** | Unchanged — `LocalSessionStore`, in-memory | Process-wide identity/grant state | Unchanged |

This is deliberately **more than one database**, not one schema with a
column discriminator — matching the original ask directly, and keeping
Orleans' own internal bookkeeping (clustering, reminders) out of the
game-domain stores entirely, since that traffic has nothing to do with
campaign data and shouldn't share blast radius with it.

### 2.1 Hot/cold as separate databases, not partitions

Marten supports exactly this shape natively: multiple independent
`DocumentStore` instances, each against its own database, with per-database
schema tracking (confirmed via Marten's own multi-tenancy docs, which
describe database-per-tenant as a first-class, built-in pattern — we do not
need Marten's multi-*tenancy* feature in the "many customers" sense, we
would just configure two ordinary separate stores, one per tier). Archival
moves a completed campaign's stream from the hot store to the cold store
once (write-once, read-rarely on the cold side) — same shape the data-layer
doc's original §4 proposed, just realized as two databases instead of two
SQLite files.

## 3. Use Marten, not hand-rolled ADO.NET — corrected from this document's own first draft

**First draft of this document recommended hand-written Npgsql/ADO.NET code
against our own schema, explicitly rejecting Orleans' generic persistence
abstractions.** The project owner pushed back — "we need real tools for our
data stuff, no hand rolling when great stuff is out there" — and that
pushback was right. Checked against real research rather than defended on
reflex:

### 3.1 What Marten is, and why it fits

[Marten](https://martendb.io) is a mature, actively-maintained (confirmed
releases through September 2026), production-proven (in production since
2016, per its own docs) .NET library providing an ACID-compliant event store
**directly on PostgreSQL**, with first-class projections. It is not a
general ORM suggestion — it is the specific, well-established answer in the
.NET ecosystem for "event-sourced system on Postgres," which is exactly our
shape.

### 3.2 Projections replace the hand-rolled snapshot *mechanism* — not the analytical work already done

This was investigated as its own question before writing this section (see
the conversation this doc summarizes). Conclusion: **Marten's `SingleStreamProjection<T>`
registered with `ProjectionLifecycle.Inline` is a better-built version of
exactly what the snapshotting doc (PR #435) designed by hand, not a
competing alternative:**

- Inline projections update a persisted current-state document **in the
  same transaction as the event append** — immediate, ACID-consistent, read
  afterward in O(1) with no replay. That is precisely the property the
  hand-rolled `snapshots` table (snapshotting doc §5.1) was built to
  deliver — Marten gives it as a supported feature instead of hand-written
  cadence/bookkeeping logic.
- The seven round tables (`selection_rounds`, `rba_rounds`, etc.) are,
  conceptually, derived/materialized views over the event stream — exactly
  what projections are for. They become projections too, replacing the
  hand-rolled `VerifyXRounds` full-table-scan functions the data-layer doc's
  §1.2 identified as a real cost contributor.
- **What this does *not* make moot, confirmed by Marten's own documentation
  rather than assumed:** rebuilding a projection from scratch (schema
  change, recovery) is itself an O(stream-length) operation, and Marten's
  own docs flag long streams as a real performance concern for exactly this
  case — the same shape of problem the snapshotting doc investigated. **The
  snapshotting doc's actual analytical findings remain the necessary
  prerequisite work, unaffected by which tool stores the result:**
  - The ammunition-ledger full-history-rescan fix (issue #436 Phase 1, `PR #437`)
    is `Cna.Core`-internal and has nothing to do with the storage engine —
    proceed with it regardless.
  - The "clean boundary" determination and the real measured ~1024-2048-step
    engagement ceiling (snapshotting doc §2/§4) are still exactly the
    numbers that should drive how often/where a projection is safe and cheap
    to maintain.
  - **Appendix B's core finding stands unchanged: `Cna.Core` has no
    resume-from-partial-history entry point today.** `CampaignHostAdapter.ImportStartedStream`
    always starts a fresh `ExerciseSession` from genesis. Marten gives us an
    excellent place to *store and automatically maintain* a snapshot
    payload — it does not, by itself, give `Cna.Core` the ability to *consume*
    one and resume from the tail. That engine-side work is still ours,
    exactly as scoped in the snapshotting doc's §5.3.

### 3.3 Hash-chain/tamper-evidence stays custom — smaller surface, not zero

Checked directly: Marten has no built-in cryptographic hash-chaining or
tamper-evidence feature. General research on tamper-evident event logs
confirms the standard approach (each record carries the hash of its
predecessor) is exactly what this project already does — nothing to
change there. What changes is the surface we have to hand-build: instead of
an entire persistence engine, it's one thin layer carrying a `prior_hash`/
`record_hash` pair alongside each Marten event. **The exact mechanism for
attaching that pair (Marten's own event metadata/headers, vs. a companion
table keyed by stream id + version) was not confirmed in this session's
research and needs verifying against Marten's current API before
implementation** — flagged honestly as unresolved rather than asserted.

### 3.4 Don't route through Orleans' own event-sourcing abstraction

Earlier in this discussion, before Marten was raised, this document's
predecessor correctly steered away from `Orleans.EventSourcing`/`JournaledGrain`
as too thin/unproven a corner of Orleans for correctness-critical code
(Microsoft's own docs say they "expect to extend or change the list of
log-consistency providers soon"). Checking again now that Marten is in the
picture doesn't change that call: there is a community-maintained Marten
adapter for Orleans' `JournaledGrain` abstraction, but it sits in a visibly
fragmented space (several small, competing community packages for the same
niche, no clear consolidated standard). **Use Marten directly from our own
grain code** — same shape as today's direct ADO.NET calls, just against a
real tool instead of raw SQL — rather than stacking an unproven adapter on
an already-niche Orleans subsystem.

### 3.5 Migrations: Marten's own tooling, not a hand-rolled script runner

Reversing an intermediate suggestion made earlier in this discussion (a
plain versioned-`.sql`-script runner): **Marten tracks and applies schema
changes per database natively**, and its own production guidance is to
generate and apply explicit migration scripts rather than rely on
auto-schema-sync in production — a real, supported workflow, not something
to hand-build from scratch. This also replaces issue #416's SQLite-specific
"apply a migration lazily to each of N separate files" mechanism, which
doesn't make sense once there's one shared store per tier (§2.1) rather than
N files.

## 4. Orleans integration for infrastructure concerns (unchanged from this document's prior revision)

Verified against current Orleans documentation and NuGet listings, not
assumed:

### 4.1 Clustering/membership — built-in provider

`Microsoft.Orleans.Clustering.AdoNet` against PostgreSQL via Npgsql.
Orleans' own internal "which silos are alive" bookkeeping — no domain logic,
official setup scripts exist. This is the concrete mechanism that makes
§1's "any silo can activate any campaign's grain" claim real.

### 4.2 Reminders — durability backstop only, not the primary timeout mechanism

Orleans' own docs are explicit that reminders are not precise and should be
used for periods measured in minutes/hours/days, not seconds — ruling out
"replace the sweep with reminders" as originally framed. **Corrected
design, Microsoft's own documented pattern:** a fast in-memory grain timer
(`RegisterGrainTimer`) handles the actual deadline precisely while the
campaign's grain is active; a single coarse reminder (e.g. every few
minutes, via `Microsoft.Orleans.Reminders.AdoNet`) exists purely as a
durability backstop to re-arm the timer after a deactivation or crash. This
eliminates `CampaignCombatTimeoutService`'s cross-campaign sweep entirely —
each campaign's own grain manages its own deadline locally.

**Consequence for the round tables:** with the sweep gone, they only ever
need per-`campaign_id` lookups (also now simplified further per §3.2 — they
are projections, not hand-indexed tables).

## 5. Concurrency/isolation model

**`READ COMMITTED`, no cross-campaign locking — and Marten already gives us
most of the correctness mechanism for free.** Research during this session
confirmed Marten enforces per-stream optimistic concurrency on the stream's
own version number as a built-in behavior, not something we need to design.
That is the same mechanism this document's earlier draft proposed hand-building
as a `(campaign_id, sequence)` unique constraint — Marten already does the
equivalent natively, scoped to exactly the single-writer-per-campaign shape
Orleans already guarantees. Write skew (the real risk at `READ COMMITTED`)
requires a second concurrent writer on the same stream; there isn't one, by
construction. No `SERIALIZABLE`, no lock scope wider than what Orleans
already provides for free.

## 6. Performance posture: explicitly not over-engineered

Raised directly with the project owner during this discussion and
deliberately walked back from this document's own earlier framing: **write
latency is not being treated as a top-tier design constraint.** This is a
turn-based simulation, not a trading system or a fast-twitch game — "normal"
response time is the actual bar, not something requiring dedicated
instrumentation designed in up front. Basic logging is enough; elaborate
write-latency tracing (OpenTelemetry spans around every write, etc.) is
explicitly out of scope for this design. This does not relax crash-safety or
correctness requirements — only the earlier, now-corrected emphasis on
latency as a primary design driver.

## 7. What doesn't change

The event-sourced, hash-chained append-only model carries over exactly as
designed — this is an engine/tool swap underneath an already-sound data
model, not a redesign of the model. `DET-001`/`DET-002`/`EVT-001`/`REL-001`
are satisfied the same way they are today. The data-layer doc's §0
assumption — pre-RC, no real production data exists, no migration path
needed — still holds: every campaign journal anywhere today is test/dev
data, so "build fresh on Postgres + Marten" is the right framing, not a
data-migration project.

## 8. Local dev/test story and secrets

- **Test against a real PostgreSQL instance** (e.g. Testcontainers, or a
  docker-compose service) in CI and local dev, not an in-memory or SQLite
  stand-in — confirmed as the right call: "we want to test it on the real
  setup of our future hosted app." Once a concrete hosting shape is chosen,
  the test setup should track it as closely as practical; for now, a
  realistic local/CI Postgres is the right interim target. This is heavier
  than today's zero-setup SQLite-file tests and changes `AGENTS.md`'s
  documented commands and CI workflow — real implementation-affecting work,
  not resolved further here.
- **Local secrets**: a `.env` file, same as any other local dev credential
  today — "locally we just use a env file like normal." Production
  credential handling (secrets manager, etc.) is explicitly deferred until
  a hosting target is chosen — not designed here.

## 9. Sequencing against in-flight work

- **Issue #436 Phase 1** (`PR #437`, ammunition-ledger seeding) is pure
  `Cna.Core`, storage-agnostic, and unaffected by any of this — proceed and
  merge it on its own merits.
- **Issue #436 Phase 2** (originally scoped as a hand-rolled `snapshots`
  table + `Load()` resume path against SQLite) should **not** proceed in
  that form. Hold it, then re-scope it as: (a) the still-necessary `Cna.Core`
  resume-capability work from the snapshotting doc's §5.3 (unchanged by any
  of this), landing as its own piece, plus (b) a Marten `SingleStreamProjection`
  configuration for the campaign-state and round-table projections (new,
  replacing the hand-rolled `snapshots` table design), once this document is
  accepted and the Postgres/Marten foundation exists to build it against.

## 10. Open items — named, not resolved here

- **Concrete hosting target** (managed Postgres on some cloud, vs.
  self-hosted) — undecided, does not block this design.
- **Backup/point-in-time-recovery policy.** Postgres's own WAL makes this far
  more mature than SQLite's story today; someone still has to configure
  retention/archiving. Not designed here.
- ~~**The exact hash-chain attachment mechanism on Marten events** (§3.3) —
  needs verifying against Marten's current API, not assumed.~~ **Resolved in
  Phase A (issue #439):** Marten's own opt-in event headers
  (`StoreOptions.Events.MetadataConfig.HeadersEnabled`, set per-session via
  `IDocumentSession.SetHeader`), verified against Marten 9.62.0's current API
  and proven with a round-trip test
  (`tests/Cna.OrleansHost.Tests/Persistence/MartenEventHeaderHashChainTests.cs`).
  Not a companion table. This is still proof-of-concept scope — wiring
  `prior_hash`/`record_hash` into the real campaign event schema is Phase C.
- ~~**Marten version/channel to pin** (9.x vs. 8.x; both had 2026 releases
  found during research, no decision made between them here).~~ **Resolved in
  Phase A:** pinned to **Marten 9.62.0** — the latest stable release at
  decision time (2026-11-16), and the 9.x line explicitly lists `net10.0`
  compatibility where the 8.x line's NuGet listing does not. See
  `Directory.Packages.props`.
- **The benchmark owed since issue #330** ("Host scaling: benchmark sweep
  latency and transaction hold time...") — never done, carried forward doc
  to doc. This migration doesn't get to skip it either, just reframed for
  Postgres + Marten instead of SQLite.
- **Exact CI/Testcontainers wiring** for §8's real-Postgres test story —
  named as required, not implemented here. Phase A's local/test story uses
  Testcontainers directly in `Cna.OrleansHost.Tests` (Docker is already
  present on this repo's `ubuntu-24.04` GitHub-hosted CI runners, so the new
  tests should run there unmodified), but no CI workflow has been changed to
  exercise them yet — confirming and wiring that in remains Phase D's job.

## 11. Recommendation

Bring this to the main orchestration session for review before any
implementation starts. If accepted: stand up PostgreSQL (hot + cold
databases per §2) with Marten as the event-store library, configure the
three Orleans AdoNet packages for clustering and reminders only (§4, not
persistence), design the `Cna.Core` resume-capability work from the
snapshotting doc's §5.3 against Marten's `SingleStreamProjection` model
instead of a hand-rolled SQLite snapshots table, then resume issue #436
Phase 2 in that re-scoped form.

## Sources consulted this session

- [Marten — introduction](https://martendb.io/introduction)
- [Marten — single stream projections](https://martendb.io/events/projections/single-stream-projections)
- [Marten — read model projections tutorial](https://martendb.io/tutorials/read-model-projections)
- [Marten — multi-tenancy / database per tenant](https://martendb.io/configuration/multitenancy)
- [Marten — optimizing projection rebuilds](https://martendb.io/events/optimizing)
- [Marten GitHub repository](https://github.com/JasperFx/marten)
- [ADO.NET database configuration (Orleans)](https://learn.microsoft.com/en-us/dotnet/orleans/host/configuration-guide/adonet-configuration)
- [Relational storage (Orleans grain persistence)](https://learn.microsoft.com/en-us/dotnet/orleans/grains/grain-persistence/relational-storage)
- [Custom grain storage tutorial (Orleans)](https://learn.microsoft.com/en-us/dotnet/orleans/tutorials-and-samples/custom-grain-storage?pivots=orleans-7-0)
- [Event sourcing overview (Orleans)](https://learn.microsoft.com/en-us/dotnet/orleans/grains/event-sourcing/)
- [Orleans Timers and reminders](https://learn.microsoft.com/en-us/dotnet/orleans/grains/timers-and-reminders)
- [Microsoft.Orleans.Clustering.AdoNet (NuGet)](https://www.nuget.org/packages/microsoft.orleans.clustering.adonet/)
- [Microsoft.Orleans.Reminders.AdoNet (NuGet)](https://www.nuget.org/packages/microsoft.orleans.reminders.adonet/)
- [Serializable Snapshot Isolation in PostgreSQL (arXiv)](https://arxiv.org/pdf/1208.4179)
- [SSI – PostgreSQL wiki](https://wiki.postgresql.org/wiki/SSI)
