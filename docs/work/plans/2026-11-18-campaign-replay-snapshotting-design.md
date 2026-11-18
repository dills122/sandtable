# Campaign replay snapshotting: the `Cna.Core` design spike Appendix B asked for

**Status:** design proposal for human review. No code changes accompany this document.
This is the `Cna.Core`-owned spike that Appendix B of
[`2026-11-18-data-layer-design-1000-campaign-ceiling.md`](2026-11-18-data-layer-design-1000-campaign-ceiling.md)
(hereafter "the data-layer doc") said was needed before §5's snapshotting
mechanism could be implemented. It re-verifies Appendix B's three findings
against the engine code directly, then answers the two questions Appendix B
left open: (a) can the hash scope that proves correctness be made
window-since-snapshot instead of full-genesis without weakening anything, and
(b) can snapshot points be restricted to "clean" lane-boundary instants, or
does every mid-round field need a serialization format.

**Scope note on sources:** everything below was re-read directly from
`src/Cna.Core/Exercises/ExerciseSession.cs`, `src/Cna.Core/Exercises/CampaignExercises.cs`,
`src/Cna.Core/Hosting/CampaignHostAdapter.cs`,
`src/Cna.Core/Campaigns/CampaignCombatAmmunitionLedgerV1.cs`,
`src/Cna.Core/Campaigns/CampaignNativeCombatForceEligibility.cs`,
`src/Cna.Core/Campaigns/CampaignNativeRepeatAssaultRuntime.cs`, and
`src/Cna.OrleansHost/Api/CampaignDurableAuthorityJournal*.cs` in this worktree
(branch `docs/campaign-replay-snapshotting-design`, cut from `origin/main` at
`d9e47911`), not taken on faith from Appendix B's summary or from the data-layer
doc's prose. Line numbers below were confirmed to be identical between this
branch and `test/second-side-assault-selection-adjacent-enemy` (the branch the
original Appendix B investigation ran on) by diffing the cited files — no
intervening commit touched them. Governing requirements (`DET-001`, `DET-002`,
`EVT-001`, `REL-001`) are quoted from `docs/roadmap/pre-alpha-roadmap.md`'s
"Governing requirements" table, which is where `AGENTS.md`'s reference to them
actually resolves (`AGENTS.md` itself does not restate their text).

---

## 1. Re-verifying Appendix B's three findings

### 1.1 Finding 1 — no resume/skip-ahead entry point: **confirmed as stated**

Traced the exact call chain:

- `CampaignHostAdapter.Import`/`ImportWithContentResolver` (`CampaignHostAdapter.cs:2492-2513`)
  both call `ImportCore` (`:2515`), which — after step-kind validation — calls
  `ImportStarted(Create(request, resolver), ...)` at `:2574`. `Create` is a
  factory producing a **fresh** `ExerciseStartResult` from genesis; there is no
  overload that takes an existing `ExerciseSession`.
- `ImportStarted` (`:2695`) forwards to `ImportStartedStream` (`:2710`), which
  takes the `started.Session!` from that fresh `Create` call (`:2720`) and then
  `foreach`es over **every** item in `steps` (`:2724`), dispatching each one
  through a ~900-line `switch` (`:2974` onward) to the matching
  `CampaignExercises`/`CampaignHostAdapter` step-application function, folding
  forward with `session = accepted.SuccessorSession!;` (confirmed at the end of
  the loop body) before finally `return CampaignHostImportResult.Verified(session);`.
- `CampaignDurableAuthorityJournal.Load()` (`CampaignDurableAuthorityJournal.cs:1982-2157`)
  reads **every** row from `accepted_steps` with no `LIMIT` (`:2088-2096`),
  rebuilds the full `steps` list, and calls
  `CampaignHostAdapter.ImportWithContentResolver(request, creation, initial, steps, ...)`
  at `:2141` — i.e. the hot persistence path goes through exactly the
  fresh-`Create`-plus-full-replay chain above. There is no branch anywhere in
  `Load()`, `ImportCore`, `ImportStarted`, or `ImportStartedStream` that accepts
  a partial `steps` list plus a pre-built `ExerciseSession`.

Appendix B's finding 1 holds without qualification. §5.3 below names the actual
refactor needed to add a resume entry point.

### 1.2 Finding 2 — `HashFramed(CurrentHistory)` — **confirmed as written, but its relevance to `Load()` was overstated; a different full-history dependency is the real blocker**

`CampaignExercises.Reconstruct` (`:7149`) and the private `ReconstructOne`
(`:8179`) do exactly what Appendix B said: `ReconstructOne` computes
`HashFramed(completedSession.CurrentHistory.Select(value => ...))` as a
"stream hash" (six near-identical call sites, e.g. `:8206`, `:8238`, `:8265`,
`:8296`, `:8331`, `:8366`, `:8398`, `:8430`), and `Reconstruct` (`:7149-7189`)
walks a backward chain of up to six "native successor" predecessor links
(`SecondBreakdownPredecessor`, `SecondMovementCompletionPredecessor`,
`SecondMovePredecessor`, `SecondReservePredecessor`,
`PostCombatPassPredecessor`, `TruckConvoyPassPredecessor` — `:7164-7173`)
before computing that hash only once, at the head (`includeStreamHash &&
index == 0`, `:7184`). `HashFramed` itself is at `:9155`.

**But this is not what `Load()` uses for correctness.** Grepping
`src/Cna.OrleansHost` and `src/Cna.Core/Hosting` for callers of
`CampaignExercises.Reconstruct` finds none. The only callers anywhere in the
repository are `src/Cna.ExerciseRunner/Artifacts/ExerciseBundleSemanticValidator.cs:270`
and `src/Cna.ExerciseRunner/Execution/ExerciseExecutionRuntime.cs:100` — the
exercise-bundle export/validation tool, not the live journal. `Load()`
(§1.1 above) never calls `Reconstruct`/`HashFramed`; its correctness guarantee
comes from (i) the host-layer SHA-256 chain over raw `accepted_steps` bytes
(`StepHash`, checked at `:2102-2120`), and (ii) the implicit per-step
"re-derived bytes match stored bytes" checks baked into each step-application
function inside the `ImportStartedStream` switch (e.g. the pregame-setup
pattern at `CampaignExercises.cs:8109-8110`: `if (!record.AsSpan().SequenceEqual(...)) throw ...`).

So Appendix B's literal claim is true (`HashFramed` does cover full genesis,
and discarding pre-snapshot history would change what it proves) but it is not
the obstacle to `Load()`'s performance goal that it was cited as — `Load()`
doesn't use `HashFramed` at all. **The actual full-history dependency that
blocks naive snapshotting on the hot path is a different one, not named in
Appendix B:**

`CampaignCombatAmmunitionLedgerV1.FromAcceptedEvents` (`CampaignCombatAmmunitionLedgerV1.cs:44-80`)
re-derives per-element ammunition by scanning **every** `CampaignNativeCombatAssaultResultV22`
in a passed-in event sequence, starting over from `CreateInitial(context)`
each time (`:48`). The type's own header comment is explicit about why:
*"Normal campaign Combat ammunition is derived from its exact Content source
and subsequently changed only by accepted Combat settlements. Replay applies
the same spends in event order rather than trusting a stored balance."*
(`:7-9`). This is called with the **full** history, not a tail slice, at every
one of its hot-path call sites:

- `ResolveNativeCombatAssault` (`CampaignExercises.cs:5399`), call at
  `:5418-5419`: `CampaignCombatAmmunitionLedgerV1.FromAcceptedEvents(session.Context, session.CurrentHistory)`.
- `ResolveRepeatedCombatAssault` (`:5460`), call at `:5469-5470`, same pattern.
- `SettleNativeCombatWorld` (`:5735`), call at `:5753-5756`: `priorHistory = session.CurrentHistory.Take(session.CurrentHistory.Count - 2)` — still effectively full history, minus the last two events.
- `CampaignNativeCombatForceEligibility.CanCompleteAssault` (`CampaignNativeCombatForceEligibility.cs:29`), called with an `acceptedEvents` parameter that traces back to the same full-history slices.
- `CampaignNativeRepeatAssaultRuntime`'s internal result step (`CampaignNativeRepeatAssaultRuntime.cs:226`), called with `PriorHistory` — itself constructed from `ExerciseSession`'s `prePostHistory`, which is `CurrentHistory` minus only the trailing repeat/assault-event tail (`ExerciseSession.cs:88-89`), i.e. still effectively full history from genesis.
- `ReconstructOne`'s own base-case re-derivation (`CampaignExercises.cs:8756`), confirming the ExerciseRunner path shares the same dependency.

Because `Load()`'s single call to `CampaignHostAdapter.ImportWithContentResolver`
replays **every** accepted step through these same functions, every combat
result encountered during a `Load()` replay re-triggers a full-history
ammunition scan. **This means the data-layer doc's "nine-plus-pass" framing of
`Load()`'s cost (§1.2 point 5 of that doc: one full replay plus seven
`VerifyXRounds` passes) undercounts the actual cost for any campaign with
multiple Combat engagements** — each engagement's result step is itself an
additional O(history) scan nested inside the O(history) replay, i.e. a
genuinely quadratic contribution from `Cna.Core` itself, not only from the
host-layer verify passes the original doc identified. This is new information
this document is adding, not a restatement of Appendix B.

A broader audit for other `FromAcceptedEvents`-shaped "derive by rescanning
full history" helpers in `Cna.Core` was not performed exhaustively — see §6.

### 1.3 Finding 3 — ~25 mid-round fields need a serialization format: **confirmed, and understated**

The gameplay `ExerciseSession` constructor (`ExerciseSession.cs:39-74`) takes
**33** optional parameters, not ~25: `combatSelection`, `noAttackCompletion`,
`combatBarrage`, `combatRetreatDecline`, `combatRetreatMove`,
`combatRetreatWorld`, `combatRetreatPriorContacts`, `combatRetreatMoves`,
`combatForceSeals`, `combatForceCompletion`, `combatForceCancellation`,
`combatAntiArmor`, `combatCancelledAssault`, `combatAssaultResult`,
`combatPaidWorld`, `combatPostResultRetreat`, `combatSettlement`,
`combatSettledWorld`, `combatCustodyDisposition`, `combatCustodyWorld`,
`combatCustodyState`, `combatSuccessor`, `reserveReleaseEvents`,
`cycleControl`, `repeatMove`, `repeatMoveWorld`, `repeatBreakdown`,
`repeatCombatSelection`, `repeatNoAttack`, `repeatAssaultBaseContacts`,
`repeatAssaultEvents`, `postRepeatBaseContacts`, `postRepeatEvents`, plus
`combatRbaCancellation` — 33 counted by hand from the signature.

**Worse: eight more stateful fields exist that aren't constructor parameters
at all**, added for #394's second-side Combat continuation work
(`ExerciseSession.cs:745-972`): `CurrentTruckConvoyPass`/`TruckConvoyPassPredecessor`,
`CurrentPostCombatPass`/`PostCombatPassPredecessor`,
`CurrentSecondReserve`/`SecondReservePredecessor`,
`CurrentSecondMove`/`SecondMovePredecessor`,
`CurrentSecondMovementCompletion`/`SecondMovementCompletionPredecessor`,
`CurrentSecondBreakdown`/`SecondBreakdownPredecessor`,
`CurrentSecondCombatDecision`/`SecondCombatSelectionPredecessor`,
`CurrentSecondNoAttackCompletion`/`SecondNoAttackPredecessor`. These are set
only via `With*` methods (`:774-932`) that use `(ExerciseSession)MemberwiseClone()`
rather than the constructor.

The critical detail: each `*Predecessor` field is typed `ExerciseSession?` —
a pointer to a **whole prior `ExerciseSession`**, not a scalar. This is
confirmed by `CampaignExercises.Reconstruct`'s own chain-walk
(`:7157-7188`), which follows these predecessor links backward, one hop per
lane, verifying each with a recursive call to `ReconstructOne`/`Reconstruct`.
So the real in-memory shape at an arbitrary "mid-round" instant is not 33 flat
optional fields — it is 33 flat optional fields **plus a variable-length
linked list of up to six additional whole `ExerciseSession` objects**, each of
which independently carries its own 33-field shape (and, in principle, could
itself have been constructed at a point where some of *those* 33 fields were
populated, though in every call site traced here they are not — each `With*`
method is invoked on a session built via the 4-arg minimal constructor or
another `With*` clone, not on one with the big constructor's combat fields
populated).

Appendix B's "~25 fields need a real serialization format" undersells this:
it is 33 fields, not ~25, and the harder problem is the eight
recursive-predecessor fields, which are not expressible as "a field with a
type and a serialize/deserialize format" at all without first deciding how
deep the chain can get and whether to serialize it as a chain or flatten it.
§4 below argues this recursive-chain problem can be sidestepped entirely
rather than solved.

---

## 2. A "clean boundary" does exist, and the engine already refuses to checkpoint anywhere else

`CampaignExercises.Complete` (`:7084-7106`) is the step-completion path used
for ordinary (non-Combat-round) legal actions — movement, and other
single-step actions resolved via `CampaignCurrentActionExecutionResult`. It
builds the successor session with the **4-argument** `ExerciseSession`
constructor overload: `new ExerciseSession(execution.SuccessorSnapshot!,
session.Context, history, contacts)` (`:7100`). Every one of the 33
constructor parameters not listed defaults to `null`, and the eight
non-constructor fields (`CurrentTruckConvoyPass` etc.) are ordinary
auto-properties with no field initializer, so they default to `null` too —
they are only ever set by a `With*` clone applied to an *already-constructed*
session. So a session that was most recently produced by `Complete` (or by the
pregame-setup constructor overload) has **all 41 of the fields discussed in
§1.3 simultaneously null.** This is the "clean boundary" Appendix B's question
(b) asks about, and it demonstrably exists in the code today, not just in
concept.

Independent corroboration: a comment in `ExerciseSession.cs:783-789` (added
for #394) states that a direct `CampaignSnapshotV11` pointing at a mid-round
position "is rejected by `Cna1979LandSequenceV4.IsSupportedCheckpoint`
(confirmed by testing)" — i.e. the engine's own existing checkpoint validator
already refuses to recognize anything but these clean positions as a valid
`CampaignSnapshotV11`. This is direct evidence that "checkpoint-worthy
instants are clean-boundary instants" is not a new restriction this design
would invent; it is already how the one checkpoint type the engine has
(`CampaignSnapshotV11`) behaves.

---

## 3. Answering (a): can the hash scope be window-since-snapshot without weakening anything?

**Yes, with zero weakening of `DET-001`/`DET-002`/`EVT-001`, and `REL-001`
explicitly sanctions the split that makes this work — provided the snapshot
payload is extended to carry the ammunition ledger explicitly (§5).**

Reasoning:

1. **`HashFramed`/`Reconstruct` and `Load()` are already two independent
   consumers with no shared code path** (§1.2). Nothing requires them to use
   the same notion of "verified from." `Load()` can resume from a trusted
   snapshot for speed; `Cna.ExerciseRunner`'s export/validation tooling (and
   the data-layer doc's own §4.2 cold-tier archival-verification step, which
   uses the host-layer SHA-256 chain, not `HashFramed`) can keep replaying
   from true genesis for audit, because nothing is ever deleted from
   `accepted_steps` while a campaign is active (data-layer doc §1.5/§4.2).
   `REL-001` — *"Snapshots are replay checkpoints; Chronicle events remain the
   authoritative history"* — is precisely the governing requirement that
   licenses this split: it says a snapshot is allowed to be a *performance*
   shortcut for one consumer without being treated as a *replacement* for the
   authoritative record that a different consumer (an audit, a dispute
   resolution, a from-genesis `Reconstruct`) can still fall back on.
2. **`DET-001`** ("a seed plus accepted command stream reproduces the same
   events and state") is about the command stream being sufficient to
   reproduce state deterministically — it does not require that every read of
   that state re-walk the stream from position zero. A verified snapshot
   (hash-bound to a specific `accepted_steps.record_hash` at a specific
   `sequence`, as the data-layer doc's §5.1 schema already proposes) is itself
   a deterministic function of exactly that same command stream; resuming from
   it does not introduce a second source of truth, provided the snapshot's
   own correctness was established by the same rules that would otherwise
   reproduce it (see the tradeoff named in §5.4 — this is where the real cost
   of this design shows up).
3. **`DET-002`** (clock/identifiers/randomness injectable and deterministic)
   is unaffected: `CampaignSnapshotV11.RandomState` already carries the RNG
   state forward today (used at, e.g., `CampaignExercises.cs:5425`'s
   `selected.PriorSnapshot.RandomState`), and a snapshot's `state_bytes` is
   exactly a serialized `CampaignSnapshotV11` (`CampaignCurrentSnapshotSerializer.Serialize`,
   precedent already used at `CampaignExercises.cs:7104`) — nothing new needs
   inventing for the RNG/clock/identifier state itself.
4. **`EVT-001`** (authoritative changes occur only through versioned commands
   and emitted events) is unaffected because the tail steps after a resume
   still go through the exact same `ImportStartedStream` switch
   (`CampaignHostAdapter.cs:2974` onward) as a from-genesis replay — resuming
   changes *where the fold starts*, not *how* each subsequent step is
   admitted.

**The actual correctness-relevant question (a) needed to resolve is therefore
not about `HashFramed`'s scope at all — it's about the ammunition ledger
(§1.2) and any other state `Cna.Core` derives by rescanning full history
instead of carrying it forward explicitly.** That is a real, scoped piece of
engine work (§5.3), not a free consequence of adding a `snapshots` table, and
not something Appendix B named — but it is tractable and small (one type,
~6 call sites), unlike a full engine redesign.

---

## 4. Answering (b): are clean boundaries workable, or does everything need serialization?

**Clean boundaries are workable as the snapshot policy. Recommend against
building a serialization format for the 33+8 mid-round fields.**

**Corrected 2026-11-18, post-benchmark — the claim below originally said
"20-30 steps... not something that grows with total campaign history." That
was wrong, and is kept struck through rather than deleted so the mistake is
visible:**

~~A clean boundary (§2) recurs at most once per fully-resolved Combat
engagement... This is bounded by the round-lane state machine's own one-shot
structure... so a single engagement cannot loop indefinitely; it is a
fixed-shape sequence of on the order of 20-30 accepted steps in the worst
case (full repeat chain plus full second-side chain), not something that
grows with total campaign history.~~

**What a dedicated measurement agent actually found**, driving the richest
existing continuation fixture (`AdvanceToSecondCombatSelection`,
`tests/Cna.OrleansHost.Tests/CampaignDurableAuthorityJournalTests.SecondCombatSelection.cs:20-64`)
and tracing the real repeat-cycle machinery:

- The one-shot throws cited above only prove each of the 33 scalar fields and
  8 predecessor-chain fields can be set **once per `ExerciseSession` object**.
  They say nothing about how many times a Combat engagement can *repeat*
  before returning to `Complete()`, because repeating is not implemented via
  those fields at all — `CampaignNativePostRepeatRuntime.Apply`
  (`CampaignNativePostRepeatRuntime.cs:106-231`) resets
  `Control`/`Movement`/`Breakdown`/`Selection` to `null` and loops back into
  another movement→breakdown→selection→resolution cycle on a `Repeat` choice
  (`:120-126`), appending to the same flat `CurrentPostRepeatEvents` list.
- **The real circuit breaker is an explicit retained-event-count cap, not a
  one-shot throw:** `CampaignNativeRepeatAssaultRuntime.cs:81-83` and
  `CampaignNativePostRepeatRuntime.cs:66-68,92` each throw
  `InvalidCampaignHistoryException` only once `retained.Length > 1024` — two
  independent lists (`CurrentRepeatAssaultEvents`, `CurrentPostRepeatEvents`),
  each capped at 1024. `CycleOrdinal` itself (`CampaignNativeCycleControlV28.cs:25-26`)
  has no cap beyond `int` overflow.
- **Measured lower bound: 14 accepted steps** for the cheapest possible path
  through the fixture above (voluntary finish, no barrage/retreat/seals/
  custody/repeat at all, plus the full second-side chain) — and that path
  *still hadn't reached a clean boundary* at step 14. No existing test in
  `tests/Cna.Core.Tests` or `tests/Cna.OrleansHost.Tests` chains more than one
  repeat cycle, or combines a full assault/custody chain with the second-side
  chain, so **14 is a real but weak lower bound; the repo has zero test
  coverage of the actual worst case.**

**Corrected conclusion: the true engine-enforced ceiling per engagement is
~1024-2048 accepted steps (two independent 1024 caps), roughly 30-70x the
original "20-30" estimate** — not because the original one-shot-throw
reasoning was fabricated, but because it answered a narrower question (can a
single field be set twice?) than the one §4 needed answered (can an
engagement repeat indefinitely?). Whether a real six-turn Graziani-scale
scenario's actual content ever drives a chain anywhere near 1024 is a
scenario/content question, not an engine guarantee — the design below treats
the engine-enforced ~1024-2048 ceiling as the only number safe to rely on for
correctness, and "typical play is much shorter" as an unverified assumption
about performance only, per §5.4.

Given that bound, and given the data-layer doc's own characterization of this
project's scale (six-turn scenarios, bounded Combat profiles per
`docs/roadmap/pre-alpha-roadmap.md`, "a few thousand rows at most" per
campaign, data-layer doc §3), a policy of **"take a snapshot at the next
clean boundary reached on or after the Nth accepted step since the last
snapshot"** is workable and strictly simpler than serializing the 33+8
mid-round fields, because:

- It needs **no new serialization format** for any of the 33 optional fields
  or the eight recursive predecessor-chain fields — at a clean boundary every
  one of them is already `null`, so there is nothing to capture beyond
  `CampaignSnapshotV11` (already serializable), `CampaignContactJournal`
  (already round-tripped at the host layer via `EncodeContactRecords`/
  `DecodeContactRecords`, confirmed in `Load()`'s own reads at
  `CampaignDurableAuthorityJournal.cs:2063`, `:2115`), and the ammunition
  ledger (§5).
- It sidesteps the recursive-predecessor-chain problem (§1.3) entirely —
  there is no case where a snapshot needs to represent "partway through a
  second-side continuation chain," because that state never persists across
  a clean boundary by construction.

**Tradeoff this policy accepts, named precisely, and corrected post-benchmark:**
replay cost is bounded by "steps since the last clean boundary reached at or
after N," not by a hard `LIMIT N` the way a pure step-count cap would be. The
bound does **not** degrade toward "a small constant in practice" as
originally believed — it degrades toward the engine's real ~1024-2048-step
per-engagement ceiling, confirmed above. This is still a real, useful bound
(it does not grow with *total campaign history*, which was §5's actual
goal), but it is a much weaker one than first claimed, and choosing a
snapshot cadence `N` without accounting for it would be a correctness-blind
performance choice, not a safe one. §7 updates the recommendation
accordingly.

This resolves (b) as: **clean boundaries occur often enough to avoid
serializing mid-round state; they do not occur as frequently as originally
estimated, and the real ~1024-2048-step ceiling, not "20-30," is what any
implementation must treat as the worst case.**

---

## 5. Concrete design

### 5.1 Updated `snapshots` table (builds on data-layer doc §5.1)

```sql
CREATE TABLE snapshots (
  campaign_id TEXT NOT NULL,
  sequence INTEGER NOT NULL,        -- accepted_steps.sequence this snapshot covers through; MUST be a clean-boundary sequence (see 5.2)
  chain_hash TEXT NOT NULL,         -- record_hash of accepted_steps row at `sequence`
  state_bytes BLOB NOT NULL,        -- CampaignCurrentSnapshotSerializer.Serialize(CampaignSnapshotV11) -- format already exists
  contacts_bytes BLOB NOT NULL,     -- EncodeContactRecords(CampaignContactJournal.Records) -- format already exists at the host layer
  ammunition_bytes BLOB NOT NULL,   -- NEW: CampaignCombatAmmunitionLedgerV1.SerializeCanonical() -- format exists; Deserialize does not yet (5.3)
  round_state_bytes BLOB NOT NULL,  -- status/high-water for the 7 lanes as of `sequence` -- not designed in Cna.Core terms here; see §6
  snapshot_hash TEXT NOT NULL,      -- sha256 over (campaign_id, sequence, chain_hash, state_bytes, contacts_bytes, ammunition_bytes, round_state_bytes)
  PRIMARY KEY(campaign_id, sequence)
)
```

The only change from the data-layer doc's original §5.1 shape is the new
`contacts_bytes` and `ammunition_bytes` columns. Everything else about that
section — snapshot-as-cache/chain-as-authoritative, pruning old snapshots,
why not reuse `accepted_steps.snapshot_checkpoint` — is unaffected and still
applies.

**New constraint this doc adds:** a `snapshots` row may only be written at a
`sequence` that corresponds to a clean boundary as defined in §2 (all 33+8
`ExerciseSession` fields null). The writer must check this explicitly — not
assume it — before persisting a row, because writing a snapshot at a
mid-round instant is exactly the case this design does not have an answer
for (§4).

### 5.2 Round-table schema gap this doc surfaces (not in the data-layer doc)

The seven round tables (`selection_rounds`, `rba_rounds`, `force_rounds`,
`cycle_rounds`, `reserve_rounds`, `custody_rounds`, `retreat_rounds`) are
keyed `PRIMARY KEY(campaign_id, <round-specific hash column>)` with no
sequence-ordered column (data-layer doc §1.1). The seven `VerifyXRounds`
functions (`CampaignDurableAuthorityJournal.ForceRounds.cs:378`,
`.RbaRounds.cs:207`, `.CycleRounds.cs:200`, `.ReserveRounds.cs:221`,
`.CustodyRounds.cs:200`, `.RetreatRounds.cs:200`,
`.SelectionRounds.cs:226`) each run an unbounded `SELECT ... FROM
<round_table> WHERE campaign_id=$id` (e.g. `SelectionRounds.cs:304-308`) and
cross-check every row against the **in-memory `steps` list** passed in.
Under this design, `steps` would become tail-only (post-resume), but these
`SELECT`s would still scan the **entire** round table for the campaign,
re-introducing an O(total-history) cost that snapshotting the engine side
alone does not fix. **This needs a `created_sequence INTEGER` column added to
all seven round tables, plus a `WHERE campaign_id=$id AND created_sequence >
$snapshotSequence` filter in each `VerifyXRounds` function, so the windowing
is applied consistently at both the engine-replay layer and the host
verification layer.** The data-layer doc's §5.2 already flagged that
`VerifyXRounds` would need "a starting index rather than always starting from
steps[0]" as real implementation work; this doc sharpens that into a specific
schema change (a new column) rather than leaving it as a vague parameter
threading exercise.

### 5.3 Changes needed, by file

- **`src/Cna.Core/Campaigns/CampaignCombatAmmunitionLedgerV1.cs`** — add a
  `Deserialize(byte[])` counterpart to the existing `SerializeCanonical`
  (`:95-116`; no `Deserialize` exists today — confirmed by reading the whole
  file, reproduced in full above). Add a way to seed `FromAcceptedEvents`-style
  logic from an already-known ledger instead of always starting from
  `CreateInitial(context)` (`:48`) — e.g. a new `FromAcceptedEvents` overload
  taking a starting `CampaignCombatAmmunitionLedgerV1` instead of `context`
  alone, used by the hot call sites below; the existing `context`-only overload
  stays unchanged for `Cna.ExerciseRunner`'s from-genesis callers.
- **`src/Cna.Core/Exercises/ExerciseSession.cs`** — new internal
  `CurrentAmmunitionLedger` property, populated by `Complete` (`:7084-7106`,
  the clean-boundary path) and by the resume-seeding constructor path (new),
  rather than by any mid-round field — consistent with §4's recommendation
  that only clean-boundary state needs a durable representation.
- **`src/Cna.Core/Exercises/CampaignExercises.cs`** — `ResolveNativeCombatAssault`
  (`:5399`, call at `:5418-5419`), `ResolveRepeatedCombatAssault` (`:5460`,
  call at `:5469-5470`), `SettleNativeCombatWorld` (`:5735`, call at
  `:5753-5756`), and the `ReconstructOne` base-case call (`:8756`) change to
  seed from `session.CurrentAmmunitionLedger` instead of rescanning
  `session.CurrentHistory`/`PriorHistory` from genesis on every call. This is
  also an independent performance improvement for **ordinary, non-replay**
  play — today every live Combat-result step already pays the full-history
  rescan once, not only during `Load()`.
- **`src/Cna.Core/Campaigns/CampaignNativeCombatForceEligibility.cs:29`** and
  **`src/Cna.Core/Campaigns/CampaignNativeRepeatAssaultRuntime.cs:226`** — same
  seeding change.
- **`src/Cna.Core/Hosting/CampaignHostAdapter.cs`** — refactor
  `ImportStartedStream` (`:2710`) to accept an already-constructed seed
  `ExerciseSession` as an alternative to always receiving `started.Session!`
  from a fresh `Create(request, resolver)` call; add a new
  `ResumeWithContentResolver`-shaped entry point (name TBD) that deserializes
  `state_bytes`/`contacts_bytes`/`ammunition_bytes` into a clean-boundary
  `ExerciseSession` and calls the refactored stream with only the tail
  `steps` (`sequence > snapshot.sequence`). `Import`/`ImportWithContentResolver`/
  `ImportPregameSource` keep calling the stream with a freshly-`Create`'d seed
  exactly as today — zero behavior change for every existing caller.
- **`src/Cna.OrleansHost/Api/CampaignDurableAuthorityJournal.cs`** — `Load()`
  (`:1982-2157`) changes to: look up the newest `snapshots` row for the
  campaign, verify `snapshot_hash` and that `chain_hash` matches the
  `accepted_steps` row at that `sequence` (one row read, not the full-table
  scan at `:2088-2096`), deserialize the four payload columns, call the new
  resume entry point with only the tail steps, then call the seven
  `VerifyXRounds` functions with both the tail `steps` and the new
  `created_sequence` cutoff (§5.2).
- **The seven `CampaignDurableAuthorityJournal.*Rounds.cs` files** — each
  `VerifyXRounds` function gains a starting-sequence parameter and its
  round-table `SELECT` gains the `created_sequence > $cutoff` filter (§5.2).
- **Writer side** — wherever a step is persisted (the `PersistAccepted`
  family), after a commit where the resulting session is a clean boundary
  (§2) and `current_sequence - last_snapshot_sequence >= N`, write a new
  `snapshots` row in the same transaction, including the new
  `ammunition_bytes`/`contacts_bytes` columns.

### 5.4 What is explicitly traded — named precisely, not hidden inside a "free" win

**Not traded (argued at the code level in §3, zero weakening):** `DET-001`,
`DET-002`, `EVT-001`. `REL-001` is not weakened either — it is the governing
requirement that licenses this design in the first place.

**Traded, named precisely:**

1. **A new trust boundary at the engine layer that does not exist today.**
   Right now, every single `Load()` call independently re-derives the entire
   `ExerciseSession` from `creationEvent`+`initialSnapshot`+`steps`, with
   nothing persisted and trusted at face value except the host-layer SHA-256
   chain over raw bytes. Every `Load()` is, in effect, already a from-genesis
   re-adjudication — a bug in `Cna.Core`'s own rules that produced a wrong
   state once would, in principle, still be caught by the next `Load()`
   replaying the same rules again (assuming the rules haven't changed; this is
   not a strong guarantee today either, but it is the guarantee that exists).
   After this change, `Load()` instead trusts a previously-written snapshot's
   bytes once `snapshot_hash`/`chain_hash` confirm they haven't been corrupted
   on disk — which proves the bytes are **unmodified**, not that they were
   **correctly derived** in the first place. A latent bug in the
   snapshot-writing path that produces an internally-consistent but subtly
   wrong snapshot would be invisible to `Load()` from that point on, for that
   campaign, until something runs a from-genesis audit against it. **This is
   the real tradeoff Appendix B was circling, stated precisely: routine
   correctness checking on the hot path moves from "every `Load()` re-proves
   correctness from genesis" to "every `Load()` trusts the last snapshot's
   self-consistency, with only an explicit, separately-scheduled from-genesis
   audit able to catch a wrong-but-consistent snapshot."** `REL-001` is what
   makes this an acceptable, conscious trade rather than a silent one — but it
   is a real change in where the proof lives, and it means the data-layer
   doc's own §4.2 archival-verification step (replay from the cold-store copy
   before ever deleting the hot-tier file) becomes load-bearing in a new way:
   it is the point every campaign is guaranteed to get at least one
   from-genesis audit, and should be scheduled (or an equivalent periodic
   audit added) rather than left as something that "only" happens once, at
   the end of a campaign's life.
2. **Snapshot-eligible points are restricted to clean lane boundaries, not
   arbitrary step counts** (§4) — a softer, engagement-shaped bound on replay
   cost rather than a hard `LIMIT N`. Needs the benchmark named in §6 before
   anyone should rely on a specific worst-case number.
3. **Ammunition-ledger seeding duplicates a small piece of state outside the
   event log on the resume path specifically** — exactly the "trusting a
   stored balance" the type's own comment (`CampaignCombatAmmunitionLedgerV1.cs:8-9`)
   says the current design avoids. This is a narrow, explicit, and (per §5.3)
   independently-motivated exception scoped to post-snapshot resume, not a
   change to how ordinary gameplay computes ammunition — but it should be
   called out at the call site with a comment explaining why the exception is
   safe there, so a future reader doesn't mistake it for the general pattern.

---

## 6. What this document does not resolve — open items, not fabricated certainty

- ~~No exhaustive audit of other full-history-scan dependencies.~~
  **Resolved 2026-11-18, by a dedicated audit agent.** Exhaustive search
  across `src/Cna.Core/` for the `FromAcceptedEvents`/full-genesis-fold shape
  found exactly two other instances — `CampaignContactJournal.FromEvents`
  (`CampaignContactJournal.cs:116-137`) and `CampaignCurrentProjector.Replay`
  (`CampaignCurrentEventRuntime.cs:212-217`) — both with their **only** call
  site inside `ReconstructOne`, the same non-hot export/validation path
  already established for the ammunition ledger's `ReconstructOne` call
  (confirmed independently: `CampaignExercises.Reconstruct` has exactly 3
  callers repo-wide, all in `Cna.ExerciseRunner`, none in `Cna.OrleansHost`).
  `CampaignSourceCombatObligationLedger.cs` was traced completely: it does
  **not** match the rescan-from-genesis shape at all (it folds forward via
  ordinary `With*`-style clones, the pattern this design wants), and is in
  fact currently **unwired from any replay path whatsoever** — not just
  pregame-scoped as originally assumed, but dead/unused production code
  today. **Net result: no additional hot-path full-history dependency found.
  The ammunition ledger (§5.3) remains the only one that needs seeding.**
- ~~The worst-case accepted-step count for a single Combat engagement (§4)
  was inferred... not measured.~~ **Resolved 2026-11-18, by a dedicated
  measurement agent — and the original estimate was wrong.** See the
  corrected §4 above: the real engine-enforced ceiling is ~1024-2048 steps
  per engagement (two independent 1024-event caps in
  `CampaignNativeRepeatAssaultRuntime.cs`/`CampaignNativePostRepeatRuntime.cs`),
  not 20-30. A measured lower bound of 14 steps was observed on the cheapest
  existing fixture path, confirming the original estimate's rough shape for
  a *non-repeating* engagement, but the repo has **zero test coverage of a
  chain that actually repeats**, so the true worst case remains unexercised
  by any existing test. §7 updates the recommendation to treat ~1024-2048 as
  the only number safe to design around.
- **`round_state_bytes`'s actual contents were not designed here.** The
  data-layer doc's §5.1 proposed it at the storage layer as "status/high-water
  for all 7 lanes as of `sequence`"; this document establishes that the seven
  lanes' verification lives entirely in `Cna.OrleansHost` (§5.2), not
  `Cna.Core` — so, unlike the ammunition ledger, there was no `Cna.Core`
  function to trace for this piece. The next incremental slice of this design
  should trace each `VerifyXRounds` function's actual per-row state
  (`selection_rounds`' `status`/`opened_ms`/`deadline_ms`/`high_water_ms`
  columns, confirmed at `SelectionRounds.cs:304-306`, and the equivalent for
  the other six tables) to define `round_state_bytes` precisely, the same way
  §5.3 did for the ammunition ledger.
- **This design was not implemented or tested against this repository's
  pinned `GrazianiSourceAuthorityTests.AxisFinalizesRealSupplyPlanAndReplaysWithoutStartingGame`
  test or any other test.** Per this task's standing rule, that test file was
  not read or edited for this investigation.

---

## 7. Recommendation and sequencing

This unblocks the data-layer doc's §11 rollout step 5 (snapshotting), but the
dependency chain is now explicit and slightly longer than that doc's original
phrasing implied: step 5 can proceed, but it depends on the `Cna.Core` changes
in §5.3 of **this** document landing first (ammunition-ledger seeding, the
resume-capable `CampaignHostAdapter` entry point, and the `ExerciseSession`
clean-boundary ammunition property) — before any `Cna.OrleansHost`
`snapshots`-table or `Load()` change is implementable, and before the seven
round tables' `created_sequence` column (§5.2) is added. Recommend these as
their own PR(s), reviewed by whoever owns the native-combat round-lane state
machine, consistent with Appendix B's own ask that this not be decided
unilaterally by a storage-layer PR.

Nothing here blocks RC — same framing as the data-layer doc's own §11: RC is
local-only, single-player, low-scale, and not blocked on anything in this
document landing.

**Added 2026-11-18, post-benchmark:** choose the snapshot cadence `N` (§5.1)
with the real ~1024-2048-step engine ceiling in mind, not the original
"20-30" estimate — either pick `N` conservatively relative to that real
ceiling (e.g. low enough that even a worst-case repeat chain's cost is
acceptable), or add real test coverage of a long repeat chain (none exists
today) before relying on "typical play is much shorter" as anything more
than a performance assumption. This does not change the recommendation to
proceed — the design still bounds replay cost independent of *total
campaign history*, which was always the actual goal (§5) — but implementers
should not carry forward the original "small constant, 20-30 steps" framing
into a concrete `N` choice.
