# Journal snapshotting follow-up: hash scope and clean-lane-boundary feasibility

**Status:** design-spike / research only. No code changes accompany this document.
It answers the two `Cna.Core`-owned questions that the prior journal-snapshotting
investigation (§5 of
[`2026-11-18-data-layer-design-1000-campaign-ceiling.md`](2026-11-18-data-layer-design-1000-campaign-ceiling.md))
correctly flagged as open rather than resolving itself.

**Note on the originating request's "Appendix B":** the request that prompted this
document said the prior finding was recorded in that document's "Appendix B." That
document, as it exists in this worktree today, has only an **Appendix A**
("corrections to the originating request") and ends there — there is no Appendix B,
and a repo-wide search (`grep -rl "Appendix B" docs/`) finds no such section anywhere.
The ~25-field/no-serialize-format/`ImportCore`-replays-fresh claims in the request are,
however, independently and precisely correct — confirmed directly against
`ExerciseSession.cs` and `CampaignHostAdapter.cs` below — so this is flagged as a
citation error in the request, not a reason to distrust the underlying finding. Per this
project's research-track ground rule, flagging a specific unverifiable claim beats
silently assuming it.

---

## Question 1: does the hash-chain genuinely require full-genesis scope on the live path?

### Short answer

**No — because `HashFramed(CurrentHistory)` is not on the live path at all.** It is
computed only inside `CampaignExercises.Reconstruct`/`ReconstructOne`
(`src/Cna.Core/Exercises/CampaignExercises.cs`), and every caller of
`CampaignExercises.Reconstruct` lives in `src/Cna.ExerciseRunner/` — a standalone CLI
executable ("Runs deterministic local Exercises and emits trusted verification
evidence," per its own `.csproj` `<Description>`) that nothing in `Cna.OrleansHost`
references, builds on, or calls into. `CampaignDurableAuthorityJournal.Load()` — the
function that runs on every write and read in the live host — never calls
`Reconstruct`, `ReconstructOne`, or `HashFramed`. Its own correctness check is a
completely different, cheaper mechanism: the row-by-row `StepHash` chain over
`accepted_steps`. §5.1's proposal to reserve a "slower, explicit verify-from-genesis
operation" for `HashFramed` doesn't need to be designed — that separation already
exists in the code today; it was just not yet traced.

### Evidence

**1. Every caller of `Reconstruct`/`ReconstructOne` is outside `Cna.OrleansHost`.**
`grep -rn "CampaignExercises.Reconstruct\|\.Reconstruct(" --include="*.cs" src/` finds
exactly three call sites, all in `src/Cna.ExerciseRunner/`:
- `Artifacts/ExerciseBundleSemanticValidator.cs:270`
- `Execution/ExerciseExecutionRuntime.cs:100` (`ReconstructionProof.From(CampaignExercises.Reconstruct(session))`)
- `Execution/ExerciseExecutor.cs:336` (`var reconstruction = runtime.Reconstruct(session);`)

`grep -rn "Reconstruct\|HashFramed" src/Cna.OrleansHost/` returns **zero matches**.
`src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj` only references `Cna.Core.csproj` —
nothing in `Cna.OrleansHost` references `Cna.ExerciseRunner`, and
`grep -rln "Cna.ExerciseRunner" --include="*.csproj" .` finds only its own test
project as a consumer. These are two genuinely disjoint call graphs.

**2. `Reconstruct` runs once, at the end of a harness run — not per step, not per
`Load()`.** `ExerciseExecutor.cs:310-348`: the executor loops submitting steps until
`checkpoint.PositionId == manifest.TerminalBoundary`, and only *then*, once, calls
`runtime.Reconstruct(session)` (line 336) to produce a final `ReconstructionProof` that
feeds into the exercise's pass/fail result. This is precisely the "rarer,
can-afford-to-be-slow, administrative/audit" shape hypothesized in the question — it's
a one-shot verification at the end of a deterministic exercise/fixture run (used by
`Cna.ExerciseRunner`'s CLI and its own tests), not a step-by-step or load-by-load check.

**3. `Load()`'s own verification is the separate `StepHash` chain, confirmed with no
`Reconstruct` call anywhere in it.** `CampaignDurableAuthorityJournal.cs:1634` is the
private `Load(SqliteConnection, SqliteTransaction, string campaignId)`. Its gameplay
branch (lines 1735-1808):
- Reads `accepted_steps` rows in sequence (lines 1737-1777), and for each row
  recomputes `hash = StepHash(hash, sequence, kind, ..., commandBytes, eventBytes,
  receiptBytes, checkpoint, contacts)` (line 1768) and checks it against the stored
  `record_hash` (line 1771) — a running SHA-256 chain over the **raw persisted bytes**
  of each step, independent of whether the engine can even replay them yet.
- After the loop, checks `sequence != headSequence || hash != headHash` (line 1778) —
  this is what actually proves "every row currently in the table, in this order, is
  exactly what was committed, unmodified, head pointer included."
- Only *then* calls `CampaignHostAdapter.ImportWithContentResolver(...)` (line 1793) to
  replay the already-hash-verified steps through the engine, and
  `VerifyForceRounds`/`VerifyRbaRounds`/`VerifyCycleRounds`/`VerifyReserveRounds`/
  `VerifyCustodyRounds`/`VerifyRetreatRounds`/`VerifySelectionRounds` (lines 1801-1807)
  to cross-check the seven round tables against the in-memory replay.
- `StepHash` itself is defined at line 1967 — a different function, over different
  inputs (per-row persisted fields), than `HashFramed`, which hashes the serialized
  *event* bytes of the in-memory `ExerciseSession.CurrentHistory` list
  (`CampaignExercises.cs:8924`, called at e.g. line 8034).

**4. These two hashes do a genuinely different job, not the same job at different
strictness.** The `StepHash` chain proves *storage-layer* integrity: the bytes in
`accepted_steps` have not been reordered, truncated, or tampered with since they were
written, and the head pointer matches. It says nothing about whether those bytes, when
replayed, produce an internally-consistent `ExerciseSession` — that's what
`ImportWithContentResolver` + the seven `VerifyXRounds` passes check instead, by
actually replaying and structurally validating, not by hashing. `HashFramed
(CurrentHistory)` is yet a third thing: a hash over the *canonically re-serialized*
event objects held by a *specific, already-replayed* `ExerciseSession`, used only to
give external tooling (the exercise harness, which never touches a durable journal at
all — its sessions are built in memory via `CampaignExercises.Begin`) a single
comparable value for "this harness run ended up at this exact history." None of the
three is redundant with another, but only the first two run on the live path, and the
live path's own replay+structural-verify (`ImportWithContentResolver` +
`VerifyXRounds`) already does strictly more engine-semantics checking than
`HashFramed` would add — `HashFramed` is not protecting against anything on the live
path that isn't already caught by the `StepHash` chain (storage tampering) or the
replay+verify passes (engine-state consistency).

**5. Live step-acceptance code never touches `Reconstruct` either, confirming the
separation holds end-to-end, not just at `Load()`.** `CampaignHostAdapter.cs` has zero
occurrences of `new ExerciseSession(` (0 matches) — it never constructs sessions
directly; it calls into `CampaignExercises`'s ~128 construction sites. The live
per-action methods actually used by the host
(`CampaignExercises.SubmitContactOwnerCombatSelection`, `SubmitOwnerForceSeal`, etc.,
confirmed by reading `SubmitContactOwnerCombatSelection` at lines 4311-4374) never call
`Reconstruct`. The generic `CampaignExercises.Submit` dispatcher that the exercise
*harness* uses instead (`CampaignExercises.cs:4237`,
`ExerciseExecutionRuntime.cs:94-97`) is not referenced anywhere in `src/Cna.OrleansHost/`
either (`grep -n "CampaignExercises.Submit\b" src/Cna.OrleansHost/ -r` → no matches).

### Conclusion

`HashFramed(CurrentHistory)` does not need to be — and today is not — scoped to the
live `Load()` path at all. §5.1's proposed design (snapshot the engine-replay state;
let a slower, explicit, snapshot-ignoring "verify from genesis" stay separate) already
matches reality rather than needing to be built: the one and only thing on that slower
path is `Cna.ExerciseRunner`'s one-shot `Reconstruct` call, already structurally
incapable of interacting with the durable journal or a live grain. A snapshot
mechanism on the `Cna.OrleansHost` side changes nothing about what
`Cna.ExerciseRunner` can later do with a *replayed-in-memory* `ExerciseSession`
(snapshots are a storage/replay-cost concern, not a thing `Reconstruct` even looks at).

---

## Question 2: can snapshot points be restricted to "clean" lane boundaries?

### Short answer

**The "all seven lanes idle" predicate is real and correctly derived, but — update
after a deeper trace, below — it is currently a one-time, not a recurring,
boundary.** `ExerciseSession.CurrentCombatDecision == null` is, by exhaustive trace of
the gameplay constructor's own argument-validation chain, a sufficient condition to
prove **every one of the ~34 other optional combat/reserve/cycle/repeat constructor
parameters is null or empty**, covering all seven round lanes (Selection, RBA, Force,
Retreat, Custody, Reserve, Cycle) plus the Repeat sub-chain, and it is already the
precondition the engine uses to decide whether a *new* Selection round may open
(`CampaignExercises.cs:4320`). That part holds exactly as first written. But I traced
every single place in the codebase that can change this field's value (below), and
found that **nothing currently resets `CurrentCombatDecision` back to null once it has
been set.** Concretely: before a campaign's first-ever Combat opens, the predicate is
(trivially) always true; the instant that first Selection is submitted, it goes false
and — as the engine is implemented today — stays false for the remainder of that
session chain's life, including through and after the post-combat sub-chain (Truck
Convoy Pass → Post-Combat Pass → Second Reserve → Second Move → Second Movement
Completion → Second Breakdown). This is not a trace I gave up on; I exhaustively
enumerated every call site that can possibly produce a new `ExerciseSession` and found
none that both (a) is reachable from a non-null `CurrentCombatDecision` and (b) omits
it from the result. See "Resolved: there is currently no post-combat reset, and the
roadmap confirms this is a known, not-yet-closed gap" below for the full trace, why
this is not a bug I'm the first to notice, and what the predicate's definition should
be once that gap closes.

### Evidence

**1. The exact field count and shape, read directly from the constructor.**
`ExerciseSession.cs:39-74` is the gameplay constructor. Counting its optional
parameters after `contacts`: `combatSelection, noAttackCompletion, combatBarrage,
combatRetreatDecline, combatRetreatMove, combatRetreatWorld,
combatRetreatPriorContacts, combatRetreatMoves, combatForceSeals,
combatForceCompletion, combatForceCancellation, combatAntiArmor,
combatCancelledAssault, combatAssaultResult, combatPaidWorld,
combatPostResultRetreat, combatSettlement, combatSettledWorld,
combatCustodyDisposition, combatCustodyWorld, combatCustodyState, combatSuccessor,
reserveReleaseEvents, cycleControl, repeatMove, repeatMoveWorld, repeatBreakdown,
repeatCombatSelection, repeatNoAttack, repeatAssaultBaseContacts, repeatAssaultEvents,
postRepeatBaseContacts, postRepeatEvents, combatRbaCancellation` — 34 parameters (the
request's "~25" is in the right order of magnitude; it may be counting distinct
*stages* rather than every parameter, since several stages carry a paired
world/contacts snapshot alongside the decision object).

**2. Every one of those 34 is provably gated, transitively, on `combatSelection`
being non-null — read directly out of the constructor's own `throw new
ArgumentException` conditions, not inferred:**
- `noAttackCompletion`: requires `combatSelection` non-null (line 100-103).
- `combatBarrage`: requires `combatSelection` non-null (110-114).
- `combatRetreatDecline`, `combatRetreatMove`/`combatRetreatMoves`: require
  `combatBarrage` (hence `combatSelection`) non-null (115-143).
- `combatForceSeals`: require `combatRetreatDecline` (hence the chain above) non-null
  (144-154).
- `combatForceCompletion`/`combatForceCancellation`: require `combatForceSeals`
  populated or `combatRetreatDecline` non-null (155-167).
- `combatAntiArmor`: requires one of the two Force outcomes non-null (168-173).
- `combatCancelledAssault`/`combatAssaultResult`/`combatPaidWorld`/
  `combatPostResultRetreat`/`combatSettlement`/`combatSettledWorld`/
  `combatCustodyDisposition`/`combatCustodyWorld`/`combatCustodyState`/
  `combatSuccessor`: each requires its immediate predecessor in this list non-null,
  all the way back to `combatSelection` (174-245).
- `reserveReleaseEvents` (and hence `CurrentReserveRelease`, only ever assigned inside
  `if (reserveEvents.Length > 0)` at line 256): requires `combatSelection` non-null
  *and* one of `combatSuccessor`/`noAttackCompletion`/`combatCancelledAssault`/
  `combatRbaCancellation` non-null (256-266).
- `cycleControl`: requires `CurrentReserveRelease is {Completed:true}` (292-305), which
  requires the reserve-release chain above.
- `repeatMove`→`repeatBreakdown`→`repeatCombatSelection`→`repeatNoAttack`/
  `repeatAssaultBaseContacts`/`repeatAssaultEvents`→`postRepeatBaseContacts`/
  `postRepeatEvents`: each requires its predecessor non-null, all the way back
  (307-445).
- `combatRbaCancellation`: requires `combatSelection` and `combatBarrage` non-null
  (104-108).

Since every field's *own* validity check throws unless its precursor is already
non-null, a construction that succeeds with `combatSelection == null` can only have
every one of these 34 fields null/empty. This is not a convention or an assumption —
it is enforced by the same `ArgumentException`s that make the constructor safe to call
at all.

**3. This is exactly the live precondition the engine already uses to gate opening a
new Selection round**, not a new invariant invented for this document.
`CampaignExercises.SubmitContactOwnerCombatSelection` (lines 4311-4374):
```
if (session.CurrentCombatDecision is not null
    || session.Context.ArtifactV6 is not { } artifact
    ...)
    return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
```
(line 4320) — a new Selection can only be submitted when `CurrentCombatDecision` is
already null. And the successor session it builds (line 4360):
```
var successor = new ExerciseSession(session.CurrentSnapshot, session.Context,
    history, contacts, selected);
```
passes **only** `selected` (the new `combatSelection`) — none of the other 33 optional
parameters — so the brand-new session is, by construction, in the all-null "idle"
shape except for the one field that was just set. "Is this campaign currently between
rounds on every lane" is therefore already answerable today, with no new state and no
new field, as `session.CurrentCombatDecision is null` — it is just not currently
exposed as a reusable public predicate the way ~40 other `IsSourceXAdmitted`-style
checks are (`CampaignExercises.IsSourceTurnStarted` etc., `CampaignExercises.cs:134`
and onward, consumed by `CampaignDurableAuthorityJournal.cs` at dozens of call sites,
e.g. lines 659-1233).

**4. Round cadence: idle moments are the common case, not the rare one — combat
episodes are short and bounded relative to total campaign length.** A genuinely
complete six-turn scenario does not yet exist to measure directly — the roadmap is
explicit about this (`docs/roadmap/pre-alpha-roadmap.md:255`: "The campaign is not yet
playable: full Combat support, public Combat actions, the complete continual cycle...");
line 245 confirms "No complete six-turn scenario implementation" as of today. What
*is* measurable is how many accepted-step events one combat episode costs end-to-end.
`tests/Cna.OrleansHost.Tests/CampaignDurableAuthorityJournalTests.ThirdCycleReserve.cs`
exercises a single combat entry through **three** Reserve/Cycle repeat cycles (an
initial assault, a first repeat, and a second repeat) — counting its
`Submit*`/`Tick*` calls (lines 102-186): Force Seal ×2, Reserve Release (+ a rejected
duplicate retry), Tick, Repeat Combat selection, RBA decline, Force Seal ×2, Reserve
Release (+ retry), Repeat Combat selection, Cycle Control, Repeat Movement — on the
order of 15-20 accepted steps for what the roadmap (`pre-alpha-roadmap.md:45,254,268`)
describes as the *engine's current practical ceiling* ("initial plus two repeated
selected assaults"; the third Reserve Release in this fixture is Finish-only "because
its witness set is empty, not because of a fixed cycle limit" — so this is a
representative near-maximum, not a hard-coded cap). A campaign's total history, by
contrast, accumulates Movement, Reaction, Breakdown, Organization, Weather, Arrivals
and Supply steps continuously across six turns per side — the original design doc's
own §1.2 describes `Load()`'s cost as a function of "total campaign history since
creation," which is necessarily much larger than one ~15-20-step combat episode. Every
step that is not part of an open combat episode has `CurrentCombatDecision == null` by
definition (nothing else can set it). **This cadence reasoning describes how often the
predicate* would* recur once the engine supports returning to ordinary play after
combat — see the "Resolved" section below for a direct trace showing that, as the
engine is implemented today, no such return path exists yet, so in today's code this
predicate is actually true at most once per campaign (before its first combat) rather
than recurring between episodes as reasoned here.** The 15-20-steps-per-episode,
much-larger-total-history shape of the argument is still the right one to apply *once*
that gap closes; it just doesn't describe today's achievable frequency.

**5. What would need to be serialized at a clean boundary — far less than the full
25/34-field set, because at that exact instant they are all provably empty.** The
`ExerciseSession` gameplay constructor's only *other* parameters are `snapshot`
(`CampaignSnapshotV11`), `context` (`CampaignContentContext`), `history`
(`CurrentHistory`), and `contacts` (`CampaignContactJournal`):
- `CampaignSnapshotV11` (`src/Cna.Core/Campaigns/CampaignSnapshotV11.cs:42-90`) already
  has a byte-exact canonical serializer/deserializer:
  `CampaignSnapshotV11Serializer.Serialize`/`Deserialize`
  (`src/Cna.Core/Campaigns/CampaignSnapshotV11Serializer.cs`), which round-trips and
  self-checks canonical-JSON equality on read (line 110-113 of that file). This is not
  a new format to design.
- `CampaignContactJournal` (`src/Cna.Core/Visibility/CampaignContactJournal.cs:11-31`)
  is exactly `{CampaignId, AuthorityVersion, Records: IReadOnlyList<byte[]>}`, and the
  journal already has a working codec for precisely this shape —
  `EncodeContactRecords`/`DecodeContactRecords`
  (`CampaignDurableAuthorityJournal.Contacts.cs:11`, `:40`) — used today for
  `accepted_steps.canonical_contacts` and `campaigns.initial_contacts`. Also not a new
  format.
- `CampaignContentContext` (`src/Cna.Core/Campaigns/CampaignContentContext.cs:40-69`)
  is a resolved content-pack/scenario artifact bundle, not mutable per-campaign state —
  `Load()` already reconstructs it fresh on every call via `contentResolver` from the
  `campaigns` row's `content_pack_id`/`content_hash`/`scenario_id` columns
  (`CampaignDurableAuthorityJournal.cs:1662-1665`, feeding
  `ImportWithContentResolver`'s `contentResolver` argument at line 1793). It never
  needs to be part of a snapshot blob at all.
- `history` (`CurrentHistory`) is the one open question — see below.

So, restricted to clean boundaries, a `snapshots` row needs only `CampaignSnapshotV11`
bytes + `CampaignContactJournal` bytes (both already have tested codecs) plus the
bookkeeping §5.1 already specified (`sequence`, `chain_hash`, `snapshot_hash`). This is
a materially smaller scope than "design a serialize/deserialize format for the ~25-34
mid-round-resolution fields," because at a clean boundary all of those fields are
*absent*, not merely small.

**6. `CampaignSnapshotV11`/`CampaignContactJournal`/the constructor itself are
`internal` to `Cna.Core`, and `Cna.Core.csproj` grants `InternalsVisibleTo` only to
`Cna.Core.Tests`** (`grep -n "InternalsVisibleTo" src/Cna.Core/` → one line, naming
only the test assembly). `Cna.OrleansHost` cannot see any of this directly — which is
not a new constraint this design introduces, it's the pattern the codebase already
uses everywhere: `CampaignHostAdapter` and `CampaignExercises` are `public static`
facade classes (`CampaignHostAdapter.cs:13`: "Trusted, in-process boundary. None of
these private records are browser DTOs."; `CampaignExercises.cs:11`), and
`CampaignDurableAuthorityJournal.cs` only ever calls their public static methods (the
~40 `CampaignExercises.IsSourceXAdmitted(head.Session)` calls cited above are the
existing template). A clean-boundary check and a snapshot
serialize/restore helper both belong as new `public static` methods on one of these
two classes, in `Cna.Core` — e.g. `CampaignExercises.IsAllCombatLanesIdle(session)`
and a `CampaignHostAdapter`-level `SerializeCleanBoundarySnapshot`/
`RestoreFromCleanBoundarySnapshot` pair — not inline inside
`CampaignDurableAuthorityJournal.cs`.

### Resolved: there is currently no post-combat reset, and the roadmap confirms this is a known, not-yet-closed gap

*(Added after a follow-up pass that closed out the gap this section originally left
open — the prior draft stopped at "I did not locate the specific step that finally
resets `CurrentCombatDecision`." This section replaces that with the completed trace.)*

**Finding, stated plainly: no such call site exists anywhere in the current
turn-processing flow.** I enumerated every one of the 128 `new ExerciseSession(` call
sites in the entire codebase (`grep -rn "new ExerciseSession(" src/` — confirmed
earlier that all 128 live in `CampaignExercises.cs`; none exist anywhere else in
`src/`) and classified every non-pregame one by what it passes for `combatSelection`:

- **18 non-pregame gameplay call sites total:** lines 4360, 4431, 4465, 4496, 4557,
  4688, 4735, 4874, 4949, 5068, 5160, 5259, 5544, 5601, 5918, 5976, 6828, 6932.
- **16 of the 18** (4431 through 6828) explicitly pass `selected` or
  `session.CurrentCombatDecision` as the `combatSelection` argument — i.e. they
  *thread the existing non-null value forward*, while adding further combat-chain
  fields (barrage, retreat, force seals, custody, successor, repeat, …). Read directly,
  e.g. line 4496: `new ExerciseSession(session.CurrentSnapshot, session.Context,
  session.CurrentHistory.Add(barrage), contacts, selected, combatBarrage: barrage)` —
  `selected` here is `session.CurrentCombatDecision` captured earlier in the same
  method. None of these 16 omits it.
- **Exactly one** (line 4360, inside `SubmitContactOwnerCombatSelection`) sets
  `combatSelection` for the *first* time — and its own precondition
  (`CampaignExercises.cs:4320`) requires the input session's `CurrentCombatDecision`
  to already be null, so this site can never be the reset; it's the site that *first
  makes it non-null*.
- **Exactly one** (line 6932, inside the private `Complete(ExerciseSession,
  CampaignCurrentActionExecutionResult)` helper) constructs a session passing *no*
  combat-chain arguments at all — `new ExerciseSession(execution.SuccessorSnapshot!,
  session.Context, history, contacts)` — the one shape that would reset
  `CurrentCombatDecision` to null. But `Complete` has exactly two callers, both
  guarded immediately above the call by `if (session.CurrentCombatDecision is not
  null) return ExerciseStepResult.Rejected(...)`: the generic dispatcher `Submit`
  (`CampaignExercises.cs:4266-4268`) and `SubmitContactOwnerMovement`
  (`CampaignExercises.cs:4303-4304`, the ordinary-movement submission method). Neither
  can ever reach line 6932 while `CurrentCombatDecision` is non-null — so `Complete`
  cannot be the reset either; it only ever *confirms* an already-null value forward.
- The six `MemberwiseClone`-based `With*` methods for the post-combat sub-chain
  (`ExerciseSession.cs:760-861`: `WithTruckConvoyPass`, `WithPostCombatPass`,
  `WithSecondReserve`, `WithSecondMove`, `WithSecondMovementCompletion`,
  `WithSecondBreakdown`) are not constructor calls at all — `MemberwiseClone` copies
  every field, including `CurrentCombatDecision`, verbatim.

**By exhaustive case analysis over every call site that can produce an
`ExerciseSession`, there is no path from a non-null `CurrentCombatDecision` to a null
one.** Once a campaign's first Combat Selection is submitted, `CurrentCombatDecision`
is non-null for the remainder of that session chain's life — through Force, Retreat,
Custody, the successor projection, Reserve Release, Cycle Control, any Repeat cycles,
and the entire Truck-Convoy/Post-Combat/Second-phase sub-chain — and, as a direct
consequence, the generic `Submit` dispatcher and `SubmitContactOwnerMovement` (the
methods ordinary, non-combat gameplay actions go through) reject *every* subsequent
action from that point on, because both check `CurrentCombatDecision is not null`
before doing anything else.

**This is not a bug I'm the first to notice — it is explicitly named in the roadmap as
a not-yet-reached milestone.** `docs/roadmap/pre-alpha-roadmap.md`'s delivery-graph
status table (around line 230) lists a row for **"Working pre-alpha skeleton"** whose
"Executable today" column reads **"Not reached"** and whose "Next gate" column defines
the acceptance boundary as **"One authentic movement/contact/combat/release
repeat-or-finish loop with identical replay and Chronicle evidence."** The word *loop*
is doing exactly the work this trace independently found missing: going from Movement
→ Contact → Combat → Release and back around to Movement again. The prose right below
that table (line 255) is equally direct: "The campaign is not yet playable: full
Combat support, public Combat actions, **the complete continual cycle**, published
scenario content, remaining Land rules, victory, successor browser orders and accepted
user-facing campaign persistence remain future work." Corroborating this at the test
level: every integration test I found that drives a campaign through a full combat
episode (e.g. `CampaignDurableAuthorityJournalTests.cs`'s
`VoluntaryCombatFinishHasNoPrivateTargetAndRetainsReceiptAcrossRestart`, which tracks
`ContractVersion` 27 (Reserve Release) → 28 (Cycle Control) → 41 (Truck Convoy Pass) →
42 (Post-Combat Pass) → 43 (Second Reserve) → 45 (Second Movement Completion) → 46
(Second Breakdown), lines 1248-1482) stops at the end of that chain and never attempts
a second, independent combat or an ordinary movement afterward on the same campaign —
consistent with there being nothing to call that would succeed.

**So the predicate needs a different, explicitly scoped definition today versus once
this gap closes:**

- **Today:** `CurrentCombatDecision == null` is true only *before a campaign's first
  Combat has ever opened*. It is a one-time window, not a recurring boundary — it fires
  once per campaign (if the campaign has not yet had its first contact) and then never
  again for the rest of that campaign's life, under the engine as currently
  implemented. Restricting snapshots to this predicate *today* would snapshot, at most,
  once per campaign, before first contact — of no help for the replay-cost problem this
  whole investigation is about, which only matters for campaigns that are actively,
  repeatedly played (i.e., exactly the campaigns that *do* have combats).
- **Once the "Working pre-alpha skeleton" gate closes** (i.e., once Combat genuinely
  loops back to ordinary Movement — a prerequisite for the roadmap's own "Six-turn
  playable MVP," which in turn has to exist before "1000 concurrent campaigns" is a
  real scenario at all): the only shape this codebase uses anywhere for "a session with
  no combat-chain state" is the exact one `Complete` and
  `SubmitContactOwnerCombatSelection` both already use — the plain
  `(snapshot, context, history, contacts)` constructor call, passing none of the 34
  optional parameters. The strong prior, based on that being the only pattern this
  code has for the shape, is that whatever change closes the loop will do so by making
  *some* step after `CompleteSecondBreakdown` (or whichever step is actually last)
  construct a successor this same way — which would make `CurrentCombatDecision ==
  null` correct again, recurring once per combat episode, exactly as originally
  proposed in this document. I can't point at a line number for that change because it
  does not exist yet; a follow-up implementation task should re-run this same
  `new ExerciseSession(` enumeration once the loop-closing change lands, rather than
  assume the predicate still holds unchanged.
- **A looser alternative, if the eventual design does *not* go this route:** rather
  than "all fields null," define idle as "the tail of the combat/second-phase chain has
  reached a terminal step for that episode, and `CampaignExercises.Query` reports no
  further legal action pending for *any* audience at the current position." That
  definition is robust to whatever the eventual reset mechanism turns out to be, but it
  is weaker evidence-wise — I have not traced `Query`'s behavior once
  `CurrentSecondBreakdown` is set, so this is offered as a fallback hypothesis, not a
  confirmed finding like the bullet above.

One more open item, unaffected by the above: whether `CurrentHistory` (the `history`
parameter) needs to carry the *full* genesis-rooted event list on a snapshot-restored
session, or whether a windowed tail (steps since the snapshot) would suffice on the
live path. The only consumer of the full list I found is `HashFramed(CurrentHistory)`
inside `Reconstruct`/`ReconstructOne` — confirmed above to be exclusively an
offline/`Cna.ExerciseRunner` concern, never invoked from a snapshot-restored session in
`Cna.OrleansHost`. The constructor's own use of `CurrentHistory` (lines 81-94) only
checks the *tail* against `postRepeatEvents`/`repeatAssaultEvents`, which are empty at
a clean boundary anyway. I did not do an exhaustive audit of every consumer of
`.CurrentHistory` across the 8,934-line `CampaignExercises.cs` to be certain nothing
else depends on genesis-rooted history; a follow-up implementation task should
`grep -n "\.CurrentHistory"` across that file and classify each use before deciding
whether a windowed restore is safe.

---

## Recommendation

**The narrower, clean-boundary snapshotting mechanism is achievable in principle, and
is narrower than the original finding assumed it would need to be — but it is not
usable against today's engine, because the "idle" state it depends on does not yet
recur.** At a clean boundary, the ~34 combat/reserve/cycle/repeat fields (and the six
Second*/TruckConvoy fields identified above) are all null by construction, and the
remaining state (`CampaignSnapshotV11` + `CampaignContactJournal`) already has tested,
round-tripping codecs in the codebase today — so *if* a campaign could be relied on to
pass back through this state repeatedly, no new mid-round-resolution serialization
format would be needed, which is a materially smaller scope than the original
Appendix-A-adjacent finding worried about. The qualifier that matters: per the
"Resolved" section above, that boundary is, in the engine as implemented right now, a
one-time "before this campaign's first Combat" state, not a recurring one — so
building this mechanism *today* would snapshot each campaign at most once and then
never again, which does not solve the §1.2 replay-cost problem for any campaign that
has had a combat (the case that matters).

This is not, however, a reason to shelve the idea — it is a sequencing dependency, and
a legitimate one given where this project's own roadmap says it is. The "Working
pre-alpha skeleton" gate (closing the Movement→Contact→Combat→Release repeat-or-finish
loop) is a prerequisite for the roadmap's own "Six-turn playable MVP," which in turn
has to exist before "1000 concurrent campaigns" is a real hosting scenario at all —
and the original design doc this spike follows up on is explicit that it is a **post-RC
scaling design**, not something RC is blocked on. So the two gates are already ordered
correctly by the roadmap on their own terms; this finding just makes the dependency
explicit for whoever picks up snapshotting: **don't implement clean-boundary
snapshotting until the continual-cycle loop closes, because the predicate it relies on
doesn't hold until then.**

Concretely, a follow-up task could be scoped as two separable pieces:

1. **A verification task, cheap, to run once the continual-cycle loop lands:** re-run
   the `grep -rn "new ExerciseSession(" src/` enumeration this document used, confirm a
   new call site now resets `CurrentCombatDecision` (and the six Second*/TruckConvoy
   fields) to null/default after a combat episode concludes, and capture its exact
   line as the empirical basis for `CampaignExercises.IsAllCombatLanesIdle(session)`.
   If the loop-closing change instead takes the "looser" route sketched above (query
   legal actions rather than null-check fields), the predicate's implementation
   changes accordingly, but the serialization-is-nearly-free conclusion for
   `CampaignSnapshotV11`/`CampaignContactJournal` is unaffected either way.
2. **The snapshotting implementation itself**, once (1) is confirmed — unchanged from
   the original sketch: a new `public static` `IsAllCombatLanesIdle` predicate and a
   serialize/restore pair reusing `CampaignSnapshotV11Serializer` and the existing
   `EncodeContactRecords`/`DecodeContactRecords` codec, both in `Cna.Core`, following
   the `CampaignExercises.IsSourceXAdmitted` facade pattern already used ~40 times by
   `Cna.OrleansHost`; a new `snapshots` table in `Cna.OrleansHost`
   (`campaign_id, sequence, chain_hash, state_bytes, contacts_bytes, snapshot_hash`, no
   `round_state_bytes` needed) written only when the predicate holds; and a `Load()`
   fast-path that skips to the nearest prior snapshot before replaying only the steps
   since it. The `CurrentHistory`-windowing open item (below) should be resolved before
   finalizing this piece.

**Relative sizing:** tracing the gameplay constructor's validation chain and the
Combat/Repeat/Second-phase method surface in `CampaignExercises.cs` for this document
touched a few hundred lines out of an 8,934-line file covering dozens of distinct
mechanics (Source setup, Combat, Reserve Release, Cycle Control, Repeat, the
Second-phase chain, and more besides). Once the continual-cycle loop closes, the
snapshotting implementation sketched above — two small new public predicates/codec
glue in `Cna.Core`, one new SQLite table and a `Load()` fast-path in `Cna.OrleansHost` —
is comparable in scope to the `status`/`campaign_id` indexing fix the prior document
described as "already dispatched elsewhere" (PR #410, per this worktree's recent
history), i.e. a bounded, single-PR-sized change, **not** comparable in scope to
"serialize all ~25-34 mid-round-resolution fields," which would have been a much
larger, open-ended engine-wide effort touching every one of the
Combat/Reserve/Cycle/Repeat object types. Closing the continual-cycle loop itself is a
separate, almost certainly larger piece of `Cna.Core` engine work that this document
did not scope (it's game-logic work, not storage work, and is already tracked by the
roadmap's own "Working pre-alpha skeleton" gate rather than by anything in the
1000-campaign-ceiling design track).

If the `CurrentHistory`-windowing open item below turns out to resolve unfavorably —
e.g. if it genuinely must stay genesis-rooted even on the live path for some reason
this trace missed — the serialization-scope part of this recommendation still holds
for the round-lane state (the ~34 fields), since that finding (gated validation ⟹
all-null at the boundary) does not depend on it; only "how much of `CurrentHistory` has
to travel with a restored session" tuning would need revisiting.
