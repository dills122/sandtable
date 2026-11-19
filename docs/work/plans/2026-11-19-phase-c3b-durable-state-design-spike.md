# Durable non-event campaign state on Postgres + Marten: the Phase C3b design spike

**Status:** design spike, 2026-11-19. **No journal, sink, or grain code accompanies this
document.** It answers the question the Phase C3b fork doc
([`2026-11-19-phase-c3b-durable-non-event-state-fork.md`](2026-11-19-phase-c3b-durable-non-event-state-fork.md))
raised, re-scoped by the project owner: rather than choosing among that doc's A/B/C, find the
right storage shape for each kind of state the SQLite journal holds outside `accepted_steps`
before any of the swap is implemented. Its shape follows PR #435's snapshotting spike: re-verify
the premises against the code, answer the specific questions, give a concrete design, name what it
trades, list what stays open, and make a recommendation.

Every claim about Marten's behaviour below was checked empirically against Marten 9.62.0 on a real
Postgres 16 (Testcontainers, using the repo's own `PostgresContainerFixture` and
`CampaignEventStreamSchema.Configure`). The probe was throwaway and is not committed; its results
are quoted in §2. This follows the design doc §3.3 rule of verifying Marten's API rather than
trusting its documentation, a rule that caught two errors in C1 and C2.

**Invariants this design is held to** (`docs/roadmap/pre-alpha-roadmap.md`):
`DET-001` (seed + accepted command stream reproduces the same events/state), `DET-002`
(clock/identifiers/randomness injectable and deterministic in tests), `EVT-001` (authoritative
changes only through versioned commands and emitted events), `REL-001` (snapshots are checkpoints;
the event history is the authority). Plus the project-level rule that Core is the sole
adjudication authority.

## 0. Summary

- **These are four different kinds of state, not one, and they should not share one
  mechanism.** They split along two axes. The first is whether a value is a *fact about game
  history* or *host operational state*. The second is whether it is written once or mutated
  repeatedly (§1).
- **Genesis should be the campaign stream's first event**, a versioned `CampaignGenesisEvent`
  carrying Core's own creation bytes. A side document is the wrong home: genesis is immutable,
  already hashed into the chain's seed (`InitialHash`), and is exactly the input `DET-001` replays
  from. Putting it in the stream makes each campaign stream self-contained, which is what design
  doc §2.1's hot→cold archival assumes (§3.1).
- **Seats/credentials, owner openings/actions and the reference key should be plain Marten
  documents** with optimistic concurrency, written in the same session as any event append. They
  are authentication and binding state, some of it secret. They are not game history, and must not
  end up in an append-only, archived stream (§3.2).
- **Round timing should be split by mutability (§3.3):**
  - `opened_ms`, `deadline_ms` and `configuration_hash` are written exactly once, atomically with
    the step that opens the round. They become per-event headers on that step and are folded by the
    existing C2 round projections. This keeps "projections own derivable state" intact and needs no
    wall-clock read inside `Apply`.
  - `high_water_ms` is the only genuinely mutable timing value: it advances on owner opening polls
    with no accompanying event. It lives in one small per-campaign document.
- **Orleans' AdoNet reminder table is not viable as the store (§4.1).** Its rows have no payload
  column. It lives in a separate database with no shared transaction. It is in-memory in
  Development, the only environment the durable API runs in today. Rows are deleted when a round
  closes. Only the owning grain can write to it, and nothing that verifies history can read it. Any
  one of these is fatal.
- **Three findings change C3b's implementation plan whichever option is chosen (§2):**
  - (a) C1's `SetStepHeaders` is session-scoped, so every event in a multi-step unit of work gets
    the same headers. Real units of work do append several steps (timeout cascades). Per-event
    headers work and must be used instead.
  - (b) Owner opening queries mutate durable state and can append Core events outside
    `CampaignGrain`. That breaks the single-writer premise in design doc §5.
  - (c) Historical (closed-round) timing rows are redundant: the only timing that ever reaches
    adjudication is already inside the hash-chained timeout event.

## 1. The state, re-inventoried by access pattern

Re-verified against `CampaignDurableAuthorityJournal*.cs`. Table lines are from
`CampaignDurableAuthorityJournal.cs:191-338`.

| State | Written | Read | Nature | Secret? |
| --- | --- | --- | --- | --- |
| Genesis (`campaigns`: request fields, `creation_event`, `initial_snapshot`, `initial_contacts`, `timing_configuration`) | once, at `CreateCampaign*` | every `Load` (seed of replay + `InitialHash`, `.cs:2073`) | immutable fact; Core-produced bytes | no |
| Head (`head_sequence`/`head_hash`) | every step | every `Load` | derived | no |
| Seats (`epoch`, grant digest/expiry, credential digest/expiry) | a handful of times per campaign (`IssueSeatGrant`, `RedeemSeatGrant` — `Seats.cs:86,155`, single-use CAS) | every authenticated request (`AuthenticateSeat`) | host auth state | yes (digests) |
| Owner openings/actions | on owner opening queries (`Openings.cs:587` upsert, `:618` insert) | on submissions (`openingRevision` binding) | host binding state | no |
| Reference key | once, at creation (`.cs:350`, 32 random bytes) | each opening query (HMAC for public action refs, `Openings.cs:580-581,648-658`) | secret | **yes** |
| Round `opened_ms`/`deadline_ms`/`configuration_hash` | once, at `InitializeXRound` | `GetXRoundDeadlineMs` (every timeout-grain activation/reminder), every gate, `VerifyXRounds` | written once; fixed at a known stream position | no |
| Round `high_water_ms` | every `GateXRound` that observes a later time: owner opening polls (`Openings.cs:477-545`), submissions (`Submissions.cs:601-752`), ticks | every gate; timeout evidence | genuinely mutable | no |
| Round status / hash bindings | entry + closing steps | `VerifyXRounds`, opening/submission gates | event-derivable | no |

Status and bindings are already owned by C2's projections. Head maps onto Marten's stream version
plus the C1 headers. The design work is in the other rows.

## 2. Premises re-verified, including three that change the plan

### 2.1 Marten probe results (Marten 9.62.0, Postgres 16)

| # | Probe | Result |
| --- | --- | --- |
| P1 | `SetStepHeaders`-style session headers, then `StartStream` with **two** events in one `SaveChangesAsync` | both events stored `sequence=1`: **session headers are copied onto every event in the save** |
| P2 | Two pre-built `IEvent`s (`EventGraph.BuildEvent`) with per-event `SetHeader`, appended in one save, plus an extra `round_opened_ms` header on the second | each event kept its own headers; the extra header round-tripped; `CampaignStateProjection` folded the correct `sequence=2`/`record_hash` inline |
| P3 | `Store(doc)` plus `Append` with a wrong expected version, in one save | the save threw (`ApplyEventException`); the document was **not** persisted, so document and event writes are atomic |
| — | `Marten.dll` metadata | no synchronous `SaveChanges`; only `SaveChangesAsync` |

### 2.2 Finding (a): C1's header helper is unsafe for multi-step units of work

`CampaignEventStreamSchema.SetStepHeaders` sets headers on the session. P1 shows that every event
appended in that save receives the same `sequence`/`prior_hash`/`record_hash`. Real units of work
append several steps in one transaction. One example is the Selection timeout cascade:
`TimeoutSelectionRound` → `PersistSelectionTimeout` → `AppendSelectionNoAttack` →
`AppendEmptyReserveRelease` (`SelectionRounds.cs:93-100,223-239`). Another is the pregame admit
loop (`.cs:1940-1948`). A literal port would write a corrupt hash chain. That would be caught on
the next `Load` (`ReadStepHeaders` plus `StepHash` recomputation), but caught is not the same as
correct. **C3b must append pre-built `IEvent`s with per-event headers (P2).** This is a correction
to C1's helper; C1's schema design is unaffected. It is also the mechanism the timing design in
§3.3 relies on.

### 2.3 Finding (b): the single-writer premise does not hold yet

Design doc §5's "no write skew, because there is no second writer on a stream" relies on Orleans'
one-activation-per-key. C3a routed only submissions and ticks through `CampaignGrain`
(`ICampaignGrain.cs` remarks). Owner opening queries go straight to the store, and they **write**:

- the `owner_openings` upsert and `owner_actions` inserts;
- `high_water_ms` advances;
- complete Core timeout/cancellation steps when the gate finds the deadline passed. For example,
  `CancelRbaRound` followed by `Commit` inside `QueryOwnerOpening` (`Openings.cs:477-483`, same
  shape for all seven kinds).

SQLite's `BEGIN IMMEDIATE` serialized these per file. Marten's stream-version check would reject a
concurrent event append, but nothing protects document writes unless optimistic concurrency is
enabled. A timeout taken from the query path also bypasses `CampaignGrain`'s round-timer drain,
which is harmless today because the timeout grain re-derives a null deadline when it fires, but it
is untidy.

**Recommendation:** C3b routes every *mutating* entry point through `CampaignGrain`, including
opening queries. C3a's remarks already named this as the follow-up. Optimistic concurrency on the
mutable documents (§3.2/§3.3) is defence in depth, not the primary guarantee.

### 2.4 Finding (c): closed-round timing rows have no consumer that needs them

Traced for Selection (`SelectionRounds.cs:241-344`); the other six kinds have the same shape. For
a **timed-out/cancelled** round, the timing that reached adjudication is inside the hash-chained
timeout event (`configurationHash`, `openedAtUnixMilliseconds`, `deadlineUnixMilliseconds`,
`highWaterUnixMilliseconds`). `VerifyXRounds` compares it against the retained row (`:337-339`).
Once the row is gone, that check becomes "the event's own evidence satisfies the timing
configuration's budget/ordering rules", which is the same predicate as `RequireXRoundBinding`
(`:145-155`) applied to chain-protected values. That is a stronger anchor than a mutable side row.
For a **completed** round, nothing reads the row's timing except `RequireXRoundBinding`'s
self-consistency check. So **only the currently open round per kind needs durable timing.**
History does not.

## 3. Design, per kind of state

### 3.1 Genesis → first event of the campaign stream

Options considered:

| Option | For | Against |
| --- | --- | --- |
| **G1: `CampaignGenesisEvent` as stream version 1** | Immutable fact goes in the append-only log. `StartStream` gives exactly-once creation natively (an id collision is rejected), replacing `campaigns.campaign_id PRIMARY KEY`. The stream is self-contained, so cold archival (design doc §2.1) moves one stream rather than a stream plus a document. `CampaignStateDocument` can fold a real sequence-0 state. | Accepted step *n* sits at stream version *n+1*. C1 already carries `sequence` as an explicit header and tells C3 to assert it rather than trust `Version`, so this is a documented offset, not a broken invariant. `Create` in C2's projections must accept the genesis type. |
| G2: `CampaignGenesisDocument` written alongside the first save | Keeps C1's version == sequence alignment. | The stream is not self-contained: replay needs the document plus the stream, and archival must move both. A mutable-table shape for an immutable fact. Campaign existence is split between a document and a stream that does not exist until step 1. |

**Recommended: G1.**
- *`EVT-001`:* genesis becomes a versioned event (`campaign-genesis-v1` envelope) whose payload is
  Core's own canonical creation bytes, which `CreateCampaign*` already produces.
- *`DET-001`:* the replay seed and the steps now come from one ordered source.
- *`REL-001`:* the stream is more completely the authority than before, because nothing replay
  needs lives outside it.
- *Hash chain:* the event carries `sequence=0` and `record_hash=InitialHash(...)` as headers.
  `InitialHash` and `StepHash` are unchanged.
- *Legacy:* the legacy-schema `timing_configuration` absence (`LegacySchemaVersion`) does not need
  porting (design doc §7: no real data).

### 3.2 Seats, owner openings/actions, reference key → plain Marten documents, optimistic concurrency

These are host authentication and binding state, not game history, so they are kept out of the
event stream on purpose:

- *They are not authoritative game changes.* `EVT-001` governs Core's state; issuing or redeeming
  a credential changes who may act, not what happened. Making them events would put credential
  digests into a stream that is archived and kept forever (design doc §2.1). That is the wrong
  retention for secrets and gains no replay benefit, since `Load` never needs them.
- *The access pattern suits documents.* They are written rarely and read by id on every
  authenticated request. `LoadAsync<T>(id)` is O(1).
- *A projection is not possible:* nothing in the stream derives them.

Shape:
- `CampaignSeatDocument` with id `{campaignId}/{side}`.
- `CampaignOwnerOpeningDocument` with id `{campaignId}/{side}/{actingElementId}`, embedding its
  actions list. This replaces two tables and their foreign key.
- `CampaignReferenceKeyDocument` with id `{campaignId}`.

All three use Marten's optimistic concurrency (`UseOptimisticConcurrency`/document version). This
is required, not optional, for `RedeemSeatGrant`. Grant redemption is single-use, which SQLite
enforced as a compare-and-swap (`UPDATE ... WHERE` plus a row-count check); the Marten equivalent
is a version-checked store that throws on conflict. Each document is written in the same session as
any event append in that unit of work (P3: atomic).

The reference key is created in the same session as the genesis `StartStream`, so a campaign never
exists without its key. Host identity (`_host-identity.db`, the operator capability digest) becomes
one host-scoped document of the same kind. That is a straightforward port and is not discussed
further.

### 3.3 Round timing → split by mutability

**Written once (`opened_ms`, `deadline_ms`, `configuration_hash`): per-event headers on the
round-opening step, folded by the C2 round projections.** Every `InitializeXRound` call site
inspected receives a head (`acceptedHead`, `breakdownHead`, `barrageHead`, `completedHead`,
`postResultHead`, `custodyHead`, `reserveHead`) that a `PersistAccepted` returned earlier in the
same transaction (`Submissions.cs:770-971`, `ForceRounds.cs:177-242`, `ReserveRounds.cs:17-118`,
`.cs:1033`). So there is a step in the same save to carry the values. One wrapper is idempotent:
`InitializeReserveRoundIfMissing` (`ReserveRounds.cs:17-33`) opens a round only when none exists
for that release hash. That branch was not traced to every caller's head. **C3b must assert, when
the session is saved, that every round opening has an appended event in the same save**, and fall
back to T1 below for any path where it does not. Headers `round_kind`, `round_opened_ms`, `round_deadline_ms`,
`round_configuration_hash` and `round_record_hash` go on that event (P2 shows per-event extra
headers round-trip and are visible to inline projections through `IEvent.Headers`).

`RoundEntry` gains nullable `OpenedMs`/`DeadlineMs`, and `ConfigurationHash` becomes populated on
*every* retained round, not only timed-out ones. This closes C2 fork doc §6's
"partially event-derivable" gap.

How this fits the invariants:
- *C2's purity rule holds:* `Apply` still reads no clock. The values are captured once at write
  time from the injected `TimeProvider` (`TryTrustedNow`, `ForceRounds.cs:333-345`) and stored
  with the event, so a projection rebuild reproduces them exactly.
- *`DET-002`:* the clock stays the injected one. **Do not** use Marten's own event `Timestamp`
  (database server time), which tests cannot inject.
- *`EVT-001`:* the canonical event bytes and `StepHash` are untouched. The headers are host
  metadata, protected by `round_record_hash` exactly as the SQLite row was by its own
  `record_hash` (an unkeyed self-hash, so equal strength, not weaker).
- *Read side:* `GetDeadlineAsync` becomes one `LoadAsync<XRoundsDocument>(campaignId)` and a
  lookup of the `collecting` entry, an O(1) read on every timeout-grain activation and reminder
  tick.
- *Relation to C2's decision:* this partly reverses C2's "drop the timing columns" narrowing. It
  does so in the form C2's fork doc §3 said was blocked only because the data had no event to ride
  on. It now does, without a new event type and without changing Core.

**Mutable (`high_water_ms`): one per-campaign `CampaignRoundHighWaterDocument`** (id
`{campaignId}`, a map from round kind to `{roundHash, highWaterMs}`), written in the same session
as whatever else the gate's unit of work writes, with optimistic concurrency. The entry is removed
when the round closes; under finding (c) nothing needs it after that. It is read only by the gates
and by the timeout path, which embeds it into Core's timeout event.

Why high-water does not get the same header treatment: most of its advances happen on owner opening
polls that append no event (finding (b)'s query path). Making each poll an event is option T3
below.

Alternatives for timing considered and rejected:

| Option | Verdict |
| --- | --- |
| T1: one mutable `RoundTimingDocument` holding all four values | Workable; this was the fork doc's option A. It loses nothing functionally, but it puts adjudication-input values that are fixed for the round's life into a mutable side document. The split keeps them bound to the stream position they belong to. If the split's extra moving part is judged not worth it, T1 is the acceptable fallback. |
| T2: new Core-stream events for round-open / high-water | Rejected. These would be steps Core never accepted, sitting inside the hash-chained Core stream (`EVT-001`, sole-adjudicator tension). They would also shift `sequence` and inflate the stream with one event per poll. |
| T3: a second per-campaign "host timing" Marten stream (`RoundOpened`/`HighWaterAdvanced`/`RoundClosed`) folded by an inline projection | Rejected. It is cleanly "projections own state", but every opening poll becomes an appended event. It also creates a second, non-hash-chained stream whose values feed Core adjudication: a stream that looks authoritative but is not, which muddies `REL-001`. Archival would have to decide its fate separately. That is a lot of machinery to avoid one small mutable value. |
| T4: Orleans reminder table | Rejected; see §4.1. |
| T5: stop persisting poll-time high-water advances | Rejected. After a restart a regressed clock would no longer be detected as `clock-regression`, so the timeout reason and Core's emitted timeout event would differ from the SQLite behaviour. That is an observable semantic change, not a storage swap. |

## 4. The two specific alternatives the owner asked about

### 4.1 Orleans AdoNet reminders as the timing store: not viable

The idea was that C4 already persists reminders in Postgres, so the reminder could carry what
`GetDeadlineAsync` needs. Checked against what C4 actually ships:

1. **There is no payload.** The `OrleansRemindersTable` columns are `ServiceId`, `GrainId`,
   `ReminderName varchar(150)`, `StartTime`, `Period`, `GrainHash` and `Version`
   (`OrleansAdoNetSchema.cs:459-470`). The deadline could be smuggled into `StartTime` (by
   registering the reminder to first fire at the deadline), but opened, high-water and config hash
   have nowhere to go except by encoding them into `ReminderName`. That is a hack Orleans gives no
   contract for, and it conflicts with C4's fixed name `"deadline-backstop"` and its 3-minute
   coarse period, which deliberately does not fire *at* the deadline.
2. **It is in a different database with no shared transaction.** Design doc §2 keeps Orleans
   infrastructure in its own database "never mixed with campaign data" (`Program.cs:50-56`). The
   timing write could not commit atomically with the step that opens the round, so a crash between
   them would leave an open round with no deadline, or a deadline for a round that never opened.
3. **It is not durable where the durable API runs today.** Development silos use
   `UseInMemoryReminderService()` (`TemporaryCampaignHostConfiguration.cs:28-29`), and
   `MapDurableCampaignApi` is Development-only (issue #439, C3 split comment). Timing would be lost
   on every dev restart.
4. **Rows are deleted at round close** (`CampaignRoundTimeoutGrain.ClearAsync` unregisters). That
   is fine for live deadlines, but nothing would be left for a later verifier. Finding (c) makes
   this survivable for history, but not for the open round's own `VerifyXRounds` binding check
   during `Load`.
5. **Only the owning grain can write it, and only Orleans can read it.** High-water advances
   happen in `CampaignGrain` and (today) on the query path, not in `CampaignRoundTimeoutGrain`.
   Each advance would become a cross-grain call plus a reminder upsert, which is churn on the
   infrastructure table the design keeps coarse on purpose. `Load`/`VerifyXRounds`, the
   equivalence tests and any offline audit cannot read `IReminderTable` outside a silo.
6. **The grain key is too coarse:** it is per (campaign, round kind), not per round instance.

Items 2, 3 and 5 are each fatal on their own. The reminder correctly stays what C4 designed it to
be: a *trigger* that re-reads the durable deadline, not the place the deadline lives.

### 4.2 A Marten-native per-campaign "root" document fed by an inline projection

This was evaluated as a single `CampaignRootDocument` folding genesis metadata, open-round timing
and status. The recommended design uses the same pattern where it fits:

- **The fold-derivable part of round timing *is* this pattern.** §3.3's header-fed C2 projections
  are inline projections owning that state.
- **The C2 round projections are not collapsed into one root.** Each campaign already has one
  document per round kind, and `GetDeadlineAsync` takes the kind as an argument. Merging them buys
  nothing and churns C2's tested code.
- **The pattern cannot reach two kinds of state.** No projection can own state with no event behind
  it: seats, opening rows, and high-water advances made on polls. A projection over a synthetic
  event created only to feed it is T3 again, with the same cost. The honest boundary is that a
  projection owns whatever a write-time event (plus headers) determines, and a plain document owns
  host operational state. The design keeps the plain-document category as small as the code
  allows: seats, openings, reference key, and one high-water value per open round.

## 5. Consequences for the C3b implementation (not decisions, recorded for planning)

- **Async propagation.** Marten 9.62.0 has no synchronous `SaveChanges`, so the journal, store,
  `CampaignGrain`, `CampaignDurableOutwardApi` and the sink become async. Behaviour is unchanged
  and signatures change. This follows directly from the library and is not a design choice.
- **Unit-of-work mapping.** Each SQLite `BEGIN IMMEDIATE` maps to one `LightweightSession` plus
  `SaveChangesAsync`. Appends use per-event headers (finding (a)). Under the single-writer routing
  in finding (b), the stream's expected version replaces the `campaigns` head compare-and-swap.
- **`Load`.** Load fetches the stream (genesis + steps), verifies the `sequence` header against
  `Version` − 1 and recomputes `StepHash` as today, then calls Core's `Import*` exactly as today.
  The resume-from-`CampaignStateDocument` path (Phase B) stays a separate, later optimisation and
  is out of scope.
- **`VerifyXRounds`.** It cross-checks the C2 projection documents (now carrying timing) against the
  fetched steps, verifies the open round's header timing and high-water document against the
  timing configuration, and checks each timeout event's evidence against that configuration
  (finding (c)).
- **Dropped rather than ported** (design doc §3.5/§7, no real data): legacy schema versions 5-13,
  the file crash-safety and migration machinery, and cross-file `CollectingXCampaigns`.
- **Equivalence gate before cutover**, in the `GrainEquivalence.cs`/`RoundTimerArming.cs` style:
  run the same realistic scenarios against both backends, with a fake `TimeProvider` (`DET-002`)
  driving rounds through normal completion, deadline timeout, clock regression and
  `-unavailable`. Compare:
  - canonical receipts;
  - the full accepted-step byte sequence and hash chain;
  - seat and opening outcomes;
  - each timeout event's evidence tuple;
  - `GetDeadlineAsync` results at every step.

## 6. What is explicitly traded

- The C2 decision is partly reversed: round projections carry `opened`/`deadline` again, sourced
  from headers. The C2 owner decision was made on a premise C4 did not keep, so this needs
  explicit acceptance.
- C1's version == sequence alignment becomes version == sequence + 1 because of the genesis
  event. C1's own remarks anticipated not trusting that alignment.
- Host state lives in mutable documents outside the hash chain. It was already outside the chain in
  SQLite, so this is no weaker.
- Closed-round timing is no longer retained beyond what timeout events embed. This is deliberate
  under finding (c); an operator-facing "how long did that round take" report for normally
  completed rounds would need it back. No such consumer exists today.

## 7. Open items, not resolved here

- **Version-offset contract.** Whether to formalise "sequence = Version − 1" with a tiny C1
  amendment PR, or keep accepted steps at version == sequence by giving genesis its own stream key
  (`{campaignId}:genesis`). The latter brings back the two-source problem G1 avoids; this spike
  recommends the amendment.
- **Cold-tier secrets.** At archival time, documents of the §3.2 kind should be discarded or
  retained only as hashes. Archival does not exist yet (design doc §2.1).
- **Optimistic-concurrency failures.** Whether a conflict should map to today's
  `InvalidOperationException` ("Campaign head changed before commit.") or to a retry inside
  `CampaignGrain`. Under single-writer routing it should not occur in practice.

## 8. Recommendation

Adopt the per-kind design:
- G1 genesis-as-first-event (§3.1);
- documents with optimistic concurrency for seats, openings and the reference key (§3.2);
- write-once round timing as per-event headers folded by the C2 projections, plus a per-campaign
  high-water document (§3.3).

Reject reminders-as-store (§4.1) and a single root projection (§4.2).

Before implementing the swap, fix C1's session-scoped header helper (§2.2) and route opening queries
through `CampaignGrain` (§2.3). Both are prerequisites under any option. Then implement C3b behind
the old-versus-new equivalence gate in §5.

If the owner prefers fewer moving parts over keeping write-once timing on the stream, T1 (one
mutable timing document) is the acceptable fallback. Everything else in this recommendation is
unchanged under T1.
