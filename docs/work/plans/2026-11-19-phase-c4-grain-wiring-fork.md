# Phase C4 (#439) fork: no production campaign is grain-shaped yet, so the hybrid pattern can't be wired to a real deadline today

**Status: stop-and-report**, per issue #439's "Phase C breakdown" comment ("stop-and-report on
genuine architectural forks rather than guessing") and this phase's own task item 4 ("if it's not
cleanly removable yet ... say so explicitly rather than guessing"). Written during Phase C4
implementation. The Orleans AdoNet infrastructure wiring (item 1/2) and the timer+reminder hybrid
pattern itself (item 3) are implemented and tested in this PR; `CampaignCombatTimeoutService`'s
cross-campaign sweep (item 4) is **not** retired, for the reasons below.

## 1. What this phase found, re-verified directly rather than assumed

The task brief frames item 3 as "design and implement the timer+reminder hybrid pattern **on the
campaign grain**" and asks whether `TemporaryCampaignGrain.cs` is the right place to extend. Both
framings assume a production campaign is, or is about to become, an Orleans grain. Tracing the
actual code shows it is not:

- **`TemporaryCampaignGrain` is the only grain in this codebase**, and it is explicitly scoped as
  a non-persistent, in-memory-only "Rules9 lab" fixture ("One activation, one admitted Rules9 lab
  campaign, no persistence or browser ingress" -- its own class summary). It operates on an
  ephemeral `ExerciseSession` held in `TemporaryCampaignMemoryStore`, keyed to one fixed
  `FixtureCampaignId`, and has no concept of the seven combat round kinds
  (Selection/Rba/Force/Retreat/Custody/Reserve/Cycle) `CampaignCombatTimeoutService` sweeps at
  all. Extending it would conflate two unrelated systems and mislead future readers into thinking
  the lab-fixture grain now carries real timeout semantics -- confirmed not the right place, per
  the task's own instruction to use judgment here.
- **The real durable-campaign path is not grain-based.** `CampaignDurableOutwardApi.MapDurableCampaignApi`
  (`src/Cna.OrleansHost/Api/CampaignDurableOutwardApi.cs`) is a plain ASP.NET Core minimal-API
  surface that calls `CampaignDurableAuthorityJournalStore` directly -- no `IGrainFactory`, no
  grain activation, anywhere in that path. `CampaignCombatTimeoutService` sweeps that same store
  directly as a `BackgroundService`, not through any grain either.
- **Phase C3 (journal cutover to Marten) has not landed.** Per issue #439's Phase C breakdown,
  C3 is "Replace `CampaignDurableAuthorityJournal`'s actual SQLite read/write calls with Marten
  calls... Highest blast-radius piece." Today, `CampaignDurableAuthorityJournal`'s seven round
  tables (`SelectionRounds.cs` et al.) are still live SQLite tables with real `opened_ms`/
  `deadline_ms`/`high_water_ms` columns computed from wall-clock reads at round-open/tick time
  (`TryTrustedNow`) -- confirmed by reading `InitializeSelectionRound` and its six siblings
  directly, same finding the Phase C2 fork doc
  (`docs/work/plans/2026-11-19-phase-c2-round-table-projection-fork.md`) already made when it
  decided to drop these columns from the Marten projections, explicitly on the assumption that
  "once C4 lands, live deadline state lives in the owning grain's own memory." That assumption
  presupposes campaigns are grains by the time C4 lands; they are not yet, because nothing in
  C1-C3 made them grains and this phase was scoped as "largely independent of C1-C3."
- **Nothing else currently depends on `CampaignCombatTimeoutService`'s side effects beyond timeout
  enforcement itself.** Its only consumers are: `Program.cs` (registers it as a hosted service),
  its own direct unit tests (`CampaignDurableAuthorityJournalTests.*Timeout*.cs`,
  `*Crash.cs`), and `tests/Cna.OrleansHost.FoundationChild/Program.cs`'s cross-process harness,
  which calls `SweepOnce` directly to drive timeout behavior in its own fixture. All of these
  exercise it as *the* mechanism being tested, not an incidental side effect of something else.

## 2. Why this is a fork, not a shape-variation to resolve case by case

Wiring the hybrid pattern to a *real* deadline today would require one of:

1. **Make `CampaignDurableAuthorityJournal`'s seven round-open/tick call sites also arm a
   `CampaignRoundTimeoutGrain`**, so a live SQLite round's deadline is mirrored into a grain. This
   reaches directly into C1-era journal code (`CampaignDurableAuthorityJournal.*Rounds.cs`,
   `Submissions.cs`) that C3 is about to rewrite wholesale against Marten -- exactly the kind of
   cross-phase scope expansion the issue's phased-PR discipline asks to avoid deciding
   unilaterally mid-phase, and it risks being thrown away or substantially reshaped the moment C3
   lands.
2. **Make production campaigns grains now**, replacing `CampaignDurableOutwardApi`'s direct
   store calls with grain activation. This is a materially larger architectural change than
   "Orleans AdoNet wiring" (the issue's own one-line scope for C4) -- it is close to doing C3's
   (or a new phase's) job early, and the right grain boundary for *that* is itself a design
   question (one grain per campaign? per campaign+round-kind, as this PR's standalone grain
   assumes? how does it relate to Marten's per-stream model once C3 lands?) that was not asked
   for or reviewed in this phase's scope.
3. **Retire the sweep anyway, leaving nothing enforcing round deadlines in production** until
   C3 and the real grain wiring land. This would be a live-availability regression (timed-out
   combat rounds simply never resolve) disguised as a refactor, for every round type the sweep
   currently covers.

None of these is a minor implementation judgment call; each is a real decision about scope and
sequencing that the issue's own phase breakdown ("C4 ... Largely independent of C1-C3") did not
anticipate needing, because it assumed (reasonably, before this phase's own investigation) that
"the campaign grain" already existed or was trivial to stand up.

## 3. What this phase delivered instead

- **Items 1/2 (Orleans AdoNet wiring), done in full**: `Microsoft.Orleans.Clustering.AdoNet` and
  `Microsoft.Orleans.Reminders.AdoNet` against PostgreSQL via Npgsql, wired into `Program.cs` for
  every non-Development environment, replacing the previous "throw, no production clustering
  configured" guard. `OrleansAdoNetSchema.EnsureSchemaAsync`
  (`src/Cna.OrleansHost/Persistence/OrleansAdoNetSchema.cs`) applies the official, unmodified
  dotnet/orleans `v10.4.2` PostgreSQL schema scripts (membership + reminders) on startup, guarded
  by an existence check so repeat startups are a no-op.
- **Item 3 (the hybrid pattern itself), done as a standalone, reusable, well-tested mechanism**:
  `CampaignRoundTimeoutGrain`/`ICampaignRoundTimeoutGrain`
  (`src/Cna.OrleansHost/Timeouts/`), a clean new grain interface (not an extension of
  `TemporaryCampaignGrain` -- see §1) keyed per campaign+round-kind, implementing exactly the
  Microsoft-documented pattern: a precise one-shot `RegisterGrainTimer` while active, a coarse
  (3-minute) `IRemindable` reminder as a durability backstop that re-derives the current deadline
  from an injected `ICampaignRoundDeadlineSink` abstraction on every activation/reminder tick.
  "Core remains sole adjudication authority" is preserved exactly as `CampaignCombatTimeoutService`'s
  own doc comment states: the grain only calls `sink.TickAsync`, it never adjudicates.
  Proven in `tests/Cna.OrleansHost.Tests/Timeouts/CampaignRoundTimeoutGrainTests.cs` against a
  real in-process Orleans silo (localhost clustering + in-memory reminder service, the same
  "real silo, no mocks" precedent `LocalGameApiTests.cs` already established for grain-backed
  testing in this repo): one test proves the timer fires precisely and calls the sink; another
  forces a real `DeactivateOnIdle` and then simulates the backstop reminder firing (by invoking
  `ReceiveReminder` directly rather than waiting out Orleans' real minutes-scale reminder
  schedule, for speed/determinism) and proves it re-derives the still-open deadline from the sink
  and re-arms a timer that then fires.
- **No concrete `ICampaignRoundDeadlineSink` implementation against
  `CampaignDurableAuthorityJournalStore` ships in this PR**, and the grain is **not referenced by
  `TemporaryCampaignGrain`, `CampaignDurableOutwardApi`, or any other production code path** --
  it exists as proven, ready-to-wire infrastructure, not yet connected to a real deadline, for
  the reasons in §2.
- **`CampaignCombatTimeoutService` is unchanged.** It remains the sole production mechanism
  enforcing round deadlines against the still-live SQLite journal.

## 4. What unblocks the real wiring

Once C3 lands (campaigns' durable state moves to Marten) *and* a decision is made about whether/
how production campaigns become grains (which may be C3's own job, a new phase, or an explicit
amendment to this phase's scope), wiring this PR's `CampaignRoundTimeoutGrain` to a real
`ICampaignRoundDeadlineSink` and arming it from wherever a round actually opens/closes becomes a
comparatively small, well-contained step -- the hard part (the hybrid scheduling mechanism itself,
and proving it works) is what this PR delivers. Retiring `CampaignCombatTimeoutService`'s sweep
should happen in that later step, once something is actually arming a grain for every live round,
not before.
