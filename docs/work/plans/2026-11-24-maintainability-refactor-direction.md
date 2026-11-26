# Maintainability refactor direction: Findings A–C, scoped and sequenced (2026-11-24)

> **2026-11-26 status pointer:** The original scope/design/evidence below is unchanged. The
> real synthetic HTTP checkpoint now reaches open Turn 2 Reserve, not six-turn completion,
> and native Reaction/executor migration remain open. See the [current roadmap](../../roadmap/pre-alpha-roadmap.md#current-checkpoint-and-next-gates).

**Status:** design/direction doc only — no source code changed in this pass. Produced by
independently re-reading every file the 2026-11-24 audit cited (not trusting the audit
summary), cross-checking against the prior same-week overhaul doc
(`docs/work/plans/2026-11-23-codebase-overhaul-direction.md`) and its two already-landed
refactors (PR #525, PR #548), and checking `gh pr list` for in-flight collisions.

## 0. Corrections to the audit findings (read this before the rest)

1. **`CampaignHostAdapter.cs` (Finding A) does not need a registry.**
   `docs/work/plans/2026-11-23-campaign-host-adapter-duplication-scoping.md` (landed as
   PR #524, the same week as this audit) already investigated this exact file in depth
   and proved, by direct grep (`grep -c "CampaignHostAdapter\." CampaignExercises.cs` → 0),
   that the dependency is strictly one-directional (`CampaignHostAdapter` →
   `CampaignExercises`), so its ~30 `AdmitSourceXxx`/`PromoteSourceXxx` methods are pure
   1-line forwarders — "same name, same signature, zero added logic... there is no second
   copy of any logic to drift out of sync." That doc's own verdict: *"This needs no
   de-duplication fix — there's nothing to de-duplicate."* The file's size (4,688 lines,
   ~271 `public static` members, confirmed by direct count) comes from mirroring
   `CampaignExercises`'s public surface near-1:1, not from reimplementing it. The one real
   concern in that file — 20 `NativeXxxCommand` builder methods
   (`CampaignHostAdapter.cs:1455-3963`) that branch on "which of two mutually-exclusive
   session shapes applies" via nullable-field ternaries — is explicitly already scoped as
   additional consumers of the `PendingDecision` migration (overhaul doc Section 0.6, step
   3/4), not a separate workstream. **I have not designed a registry for this file; see
   Section 2.1.**

2. **`CampaignDurableAuthorityJournal.cs` (Finding A) is already substantially done.**
   PR #525 (merged, net -1,290 lines across 3 files) collapsed exactly the ~36
   `AdmitSourceXxx`/`PromoteSourceXxx` triads this audit flagged into one
   `SourceAdmissionHandler` class + one `AdmitCore` driver
   (`CampaignDurableAuthorityJournal.SourceAdmission.cs`, confirmed read in full), and
   collapsed the 41-branch owner-opening query dispatch into a
   `FrozenDictionary<CampaignOwnerOpeningKind, Func<...>>` registry
   (`CampaignDurableAuthorityJournal.OwnerOpeningQueries.cs`, confirmed read in full). The
   main file today has 28 one-line `internal bool AdmitSourceXxx(...)` wrappers that each
   call `AdmitCore(campaignId, operatorSecret, xxxHandler)` — confirmed by direct read at
   `CampaignDurableAuthorityJournal.cs:867+`. This is already about as compact as it can
   get without changing the public entry-point shape (each method is a distinct named
   HTTP-reachable operation); **no further registry work is recommended for this file.**

3. **Both corrections above mean Finding A's real remaining untouched surface is:**
   `CampaignSourceAuthority.cs` (~30 triads + ~80 parallel `ContractVersion`/`ProfileHash`
   pairs — confirmed by direct read/grep), `CampaignSourceBattlefield.cs` (the
   Propose/Confirm/Validate/Require quadruple × 7 groups — confirmed by direct read), and
   `CampaignDurableAuthorityJournalStore.cs` (~100 thin per-mechanic wrappers mixed with
   persistence plumbing — confirmed by grep count, `internal` member count 342, including
   7 `TickXRound` methods and ~40 `QueryXOpening` methods at `Store.cs:365-550+`).

4. **Findings B and C were confirmed accurate as described, no corrections needed** —
   see Section 1 below for the direct verification.

5. **Two PRs are open right now and directly collide with Findings B/C's files.** `gh pr
   list` (checked 2026-11-23) shows PR #551 ("fix(core): guard pre-existing unguarded
   CurrentSecondAssault reads against staleness," open) and PR #552 ("fix(core): guard all
   six unguarded ComputePending() second-side arms [CRITICAL]," open) — the latter is, by
   direct diff comparison, the exact "separate PR in flight" referenced in this task's own
   briefing for the `ComputePending()` staleness bug. Both touch `ExerciseSession.cs`,
   `CampaignExercises.cs`, and/or `CampaignHostAdapter.cs`. **No slice in this plan should
   start in those files until #551 and #552 are merged.** No other open PR
   (`gh pr list --repo dills122/sandtable`) touches `CampaignSourceAuthority.cs`,
   `CampaignSourceBattlefield.cs`, or `CampaignDurableAuthorityJournalStore.cs`, so those
   have no current collision.

## 1. Verification of each finding, against the actual files

**Finding A, `CampaignHostAdapter.cs`:** confirmed 4,688 lines, 271 `public static`
members (not ~202 — undercounted; still mostly 1-7 line delegators). **But see Correction
0.1: this is not the duplication problem it looks like.**

**Finding A, `CampaignDurableAuthorityJournal.cs`:** confirmed 1,825 lines today.
**But see Correction 0.2: the ~30 triads this describes were already collapsed by PR
#525** — what remains is 28 one-line wrappers, not "near-identical triads."

**Finding A, `CampaignDurableAuthorityJournalStore.cs`:** confirmed 1,400 lines, 342
`internal` members. Confirmed by direct read (`Store.cs:365-550`): 7 `TickXRound`/
`TickRoundAsync` methods, ~40 `QueryXOpening` one-liners (e.g.
`QueryPregameAxisSupplyOpening`, `QuerySourceAxisDumpOpening`, ... through
`QueryMovementSegmentCompletionOwnerOpening`), each forwarding to a credential/opening
helper. Confirmed accurate; this is the most genuinely untouched duplication of the three
Host-layer files.

**Finding A, `CampaignSourceAuthority.cs`:** confirmed 3,585 lines. Confirmed by direct
read: ~33 `internal const int ...ContractVersion` fields (lines 15-45, 523-538) and a
**sequential hash chain** of ~30 `...ProfileHash` properties (lines 632-736+), where each
hash is computed as `Digest(priorProfileHash + "\0" + policyId + "\0" + content.Hash())`
— e.g. `BattlefieldProfileHash` depends on `AxisGarrisonsProfileHash`, which depends on
`AxisPriorityMotorizationProfileHash`, and so on. Confirmed ~15+ `AdmitXxx`/
`CanAdmitXxx`/`SerializeXxxEvent` triads (e.g. `AdmitGiarabubMotorPlacementPolicy`,
`AdmitAxisSupplement`, `AdmitCommonwealthSupplement`, `AdmitPriorityMotorization`,
`AdmitAxisPriorityMotorization`, `AdmitAxisGarrisons`, `AdmitBattlefield`,
`AdmitCairoSetup`, `AdmitCairoWorld`, `AdmitCwFreeGuns`, `AdmitSourceTobrukBardiaGarrison`,
`AdmitSourceBardiaGarrisonRemainder`, `AdmitSourceTripoliTankRegiment`, ...). **Confirmed
accurate, with one important addition the audit didn't flag: the `ProfileHash` chain is
sequentially ordered, not independent** — this changes the right registry shape (Section
2.2).

**Finding A, `CampaignSourceBattlefield.cs`:** confirmed 1,558 lines (close to the audit's
1,552). Confirmed by direct read: `Propose/Confirm/Validate/Require` quadruples exist for
**FourthIndian** (lines 830-887), **Libyan** (888-970, parameterized by `groupId` already
— 2 sub-groups share one set of methods), **Catanzaro** (972-1024), **SeventhArmored**
(1026-1079), **SixteenthBrigade** (1081-1134), with **ItalianDivisions** partially shown
(1136-1178+) and **Matruh** (1358-1463) diverging the most — it uses
`AddMatruhFormation`/`ValidateGroupedMatruh`/`ConfirmMatruhFormation`/
`DetachMatruhUnit`/`PromoteMatruhDetached`, a 5-method shape with an extra "detach" step
that doesn't fit the clean quadruple. **Confirmed accurate overall, with the addition that
Libyan and Matruh are genuine structural outliers, not just naming variations** — this
matters for Section 2.2's design (don't force-fit them).

**Finding B, `CampaignNativeSecondBreakdownV46.cs`:** confirmed exactly as described.
`Create` (lines 18-90), `CreateForStageEntry` (106-172), `CreateForStageEntrySecondSide`
(185-251) are byte-for-byte identical in structure, differing only in: which 4 named
`ExerciseSession` fields are read (`CurrentSecondMovementCompletion`/
`SecondMovementCompletionPredecessor`/`SecondMovePredecessor`/`CurrentSecondMoveWorld` vs.
their `StageEntry`/`StageEntrySecond` equivalents), whether `orders` comes from
`initial.OperationStageOrders` directly (plain) or `CampaignExercises.StageEntryOrders(session)`
(both StageEntry variants), and an error-message prefix (`""`/`"Stage Entry "`/`"Stage
Entry second-side "`). The file's own comments (lines 92-105, 174-184) explicitly document
this as a known, deliberate duplication pattern, confirming the live-and-growing framing.
**Confirmed accurate, no corrections.**

**Finding C, `CampaignExercises.cs`:** confirmed the file is actually 10,845 lines (not
"~400+ collapsible lines" total — that described only the collapsible portion). Confirmed
by direct read (lines 6626-7430+) the Movement/Reserve plain/`StageEntry`/
`StageEntrySecond` triads exist in exactly the shape described (e.g.
`QuerySecondMovementSource`/`QueryStageEntryMovementSource`/
`QueryStageEntrySecondMovementSource`, each a near-identical predecessor-walk). Confirmed
49 instances file-wide of the repeated
`catch (Exception exception) when (exception is ArgumentException or ArithmeticException
or InvalidCampaignHistoryException or InvalidOperationException or JsonException)` block
(audit said "15x" for the subset inside these specific triads — plausible as a subset of
the 49, not independently re-verified at that granularity). Confirmed 8 test files share
an identical private `Start(CampaignCreationRequest)` / `CreateRequest()` fixture-helper
pair (`CampaignNativeStageEntryCombatSelectionTests.cs`,
`CampaignNativeStageEntryBreakdownTests.cs`, `CampaignNativeStageEntryCombatAssaultTests.cs`,
`CampaignNativeStageEntrySecondBreakdownTests.cs`,
`CampaignNativeStageEntrySecondCombatSelectionTests.cs`,
`CampaignNativeStageEntrySecondReserveTests.cs`,
`CampaignNativeStageEntrySecondCombatAssaultTests.cs`, `CampaignExercisesTests.cs`) —
exact match to the audit's "8x." **Confirmed accurate.**

**One finding not in the brief, surfaced during verification, worth flagging:** the
Reserve-Release/Cycle-Control `session.CurrentStageEntrySecondAssault is not null ? ... :
session.CurrentStageEntryAssault is not null ? ... : session.CurrentSecondAssault is not
null ? ... : ...` ternary cascade is itself duplicated across **9 call sites**
(`SubmitNativeReserveReleaseChoice`, `CompleteNativeReserveRelease`,
`QueryPendingNativeCycleControl`, `TimeoutNativeReserveRelease`,
`TimeoutNativeCycleControl`, `SubmitTruckConvoyPass`, `SubmitNativeCycleControl` in
`CampaignExercises.cs`, plus 2 sites in `CampaignHostAdapter.cs`). PR #551/#552 just
**hand-patched the identical guard condition into all 9 copies** rather than collapsing
them — i.e., the exact failure mode this task's brief warns against (fixing N copies by
hand instead of collapsing them) is what just happened, live, to unblock a CRITICAL bug.
This is deliberate and correct for PR #551/#552's narrow emergency-fix scope, and it is
explicitly the Combat/Reserve-Release chain the overhaul doc's Section 0.1 calls "original
pattern" / pre-`#394` — out of scope for *this* plan per that doc's own Section 0.6 step 5
("migrate opportunistically, if ever... don't schedule this"). **Flagging it here as a
known future candidate once `PendingDecision` extends to that chain; not adding it as a
slice in this plan.**

## 2. Target shapes for Finding A, per class

### 2.1 `CampaignHostAdapter.cs` — no registry; optional cosmetic cleanup only

Per Correction 0.1, do not build a dispatch registry here — there is no duplicated logic
to collapse. The one optional, zero-priority cleanup already identified by PR #524's doc:
delete the ~30 pure one-line `AdmitSourceXxx`/`PromoteSourceXxx` forwarders
(`CampaignHostAdapter.cs:240-414`) and repoint their callers (7 `*Rounds.cs` files, 2
`CampaignDurableAuthorityJournal.*.cs` files) directly at `CampaignExercises`. This is
"safe to do whenever," shrinks the file by a few hundred lines with zero behavior change,
but is not scheduled — it's cosmetic, not correctness/maintainability-bearing. The 20
`NativeXxxCommand` methods' nullable-branching should track as additional consumers of
whatever `PendingDecision`-equivalent eventually covers the Combat-cycle machinery — not
scoped into this plan (see Section 4's scope boundaries).

### 2.2 `CampaignSourceAuthority.cs` — ordered-chain registry, NOT a plain `FrozenDictionary`

PR #525's `SourceAdmissionHandler` pattern (independent per-kind delegates in a
dictionary) doesn't fit here directly, because this file's ~30 mechanics form a
**sequential hash chain** — each `ProfileHash` depends on the previous one by construction
(`BattlefieldProfileHash` reads `AxisGarrisonsProfileHash`, not an independent input).
Collapsing this into an unordered `Dictionary<Kind, ...>` would lose that ordering unless
each entry explicitly carries "my predecessor in the chain," which is exactly the kind of
structural guarantee this task's brief asks for (compiler/type-enforced, not
reviewer-enforced).

Recommended shape — one **ordered list** (not a `FrozenDictionary`, since chain position
*is* the meaning), each entry a small record:

```csharp
internal sealed record SourceAdmissionStep(
    string Kind,
    int ContractVersion,
    string PolicyId,
    Func<CampaignSourceAuthorityState, string> PriorProfileHash,   // the chain link
    Func<CampaignSourceAuthorityState, string> ContentHash,         // this step's own content
    Func<CampaignSourceAuthorityState, bool> CanAdmit,
    Func<CampaignSourceAuthorityState, CampaignSourceAuthorityState> Accept,
    string UnavailableMessage);
```

with `ProfileHash(step, state) => Digest(step.PriorProfileHash(state) + "\0" + step.PolicyId
+ "\0" + step.ContentHash(state))` as one shared method replacing all ~30 hand-written
`XxxProfileHash` properties, and `AdmitCore(state, step)` replacing all ~15+ `AdmitXxx`
bodies the same way PR #525's `AdmitCore` did. **Where this metadata lives:** its own file,
`CampaignSourceAuthority.AdmissionSteps.cs`, in the same partial-class-per-concern style
`CampaignDurableAuthorityJournal` already uses — not merged into PR #525's
`SourceAdmissionHandler` table, because the delegate signatures are fundamentally
different (`Func<ExerciseSession, ...>` there vs. `Func<CampaignSourceAuthorityState, ...>`
here — different layer, different aggregate type). Keep layers' registries separate, the
same discipline PR #525 already used (it built two *separate* tables —
`SourceAdmissionHandler` and `openingQueries` — rather than one god-table).
**Interaction with PR #525:** none required. The Journal's `SourceAdmissionHandler`
delegates call `CampaignHostAdapter.AdmitSourceAxisSupplement(session)`, which (per
Correction 0.1) is a pure forwarder into `CampaignExercises`/`CampaignSourceAuthority`;
that call shape is unaffected by reorganizing what's *inside*
`CampaignSourceAuthority.AdmitAxisSupplement`. The two registries stay decoupled, by
layer, exactly as they are today.

### 2.3 `CampaignSourceBattlefield.cs` — generic quadruple for the 5 that fit, outliers stay hand-written

```csharp
internal sealed record GroupedWorldMechanic(
    string GroupId, string SideId, string RootId,
    Func<CampaignGroupedWorld, bool> IsProposed,
    Func<CampaignGroupedWorld, bool> IsConfirmed,
    IReadOnlyList<string> ExpectedMemberIds,
    string NotProposedMessage, string AlreadyProposedMessage, string DriftMessage);
```

with generic `ProposeGroup`/`ConfirmGroup`/`ValidateGroup`/`RequireGroup` methods
parameterized by one `GroupedWorldMechanic` instance, replacing **FourthIndian**,
**Catanzaro**, **SeventhArmored**, **SixteenthBrigade**, and (pending a full read of its
remaining lines) likely **ItalianDivisions** — the 5 that match the clean quadruple shape.
**Libyan** stays as-is (already parameterized by `groupId`, which is a different, already-
solved generalization, not more work) and **Matruh** stays hand-written (its extra
Detach step is a real structural difference, not a naming variant — forcing it into the
4-method interface would be exactly the "outlier-contorting" PR #525's own doc warned
against when explaining why it left the round-gating logic untouched). Metadata lives in
one file, `CampaignSourceBattlefieldGroupTable.cs`, same reasoning as 2.2.

### 2.4 `CampaignDurableAuthorityJournalStore.cs` — reuse the proven `FrozenDictionary` pattern, split concerns first

The ~40 `QueryXOpening` one-liners are a good match for the exact `OwnerOpeningDraft`/
`openingQueries` `FrozenDictionary` pattern PR #525 already built and proved (same
`CampaignOwnerOpeningKind` enum, same shape — these Store-layer methods are one layer
below the Journal's already-registry-ized `QueryOwnerOpeningCore`). Before collapsing
them, split the file along its two stated concerns (per-mechanic dispatch wrappers vs.
"unrelated low-level persistence/concurrency plumbing," per the audit's own framing) into
two partial files — `CampaignDurableAuthorityJournalStore.cs` (persistence/concurrency
only) and `CampaignDurableAuthorityJournalStore.Openings.cs` (the dispatch wrappers,
collapsing into a registry) — mirroring how `CampaignDurableAuthorityJournal` itself is
already split into 14 partial files by concern. The 7 `TickXRound` methods are a smaller,
separate registry keyed by `CampaignRoundKind` (already an enum in this codebase per
`CampaignDurableAuthorityJournal.cs`'s own `RecordRoundOpened`/`RecordRoundClosed`
comments) — same pattern, different key type, its own small table.

## 3. Target shape for Finding B — smallest, most urgent, most mechanical

One private static `CreateCore`, three thin public entry points, each supplying a small
descriptor of "which session slots to read":

```csharp
private readonly record struct Slot(
    string Context,                                                          // "", "Stage Entry ", "Stage Entry second-side "
    Func<ExerciseSession, CampaignNativeSecondMovementCompletedV45?> Completed,
    Func<ExerciseSession, bool> AlreadyCompleted,
    Func<ExerciseSession, ExerciseSession?> Source,
    Func<ExerciseSession, IReadOnlyList<CampaignOperationStageOrder>> Orders,
    Func<ExerciseSession, (object Predecessor, CampaignWorldSnapshotV6 World)> MovementSource,
    Func<ExerciseSession, ExerciseSession?> MovePredecessor,
    Func<ExerciseSession, CampaignWorldSnapshotV6?> MoveWorld);

private static readonly Slot SecondSlot = new("",
    s => s.CurrentSecondMovementCompletion, s => s.CurrentSecondBreakdown is not null,
    s => s.SecondMovementCompletionPredecessor, s => s.CurrentSnapshot.OperationStageOrders,
    CampaignExercises.QuerySecondMovementSource, s => s.SecondMovePredecessor,
    s => s.CurrentSecondMoveWorld);
// StageEntrySlot / StageEntrySecondSlot follow the same shape, substituting the
// StageEntry*/StageEntrySecond* fields and CampaignExercises.StageEntryOrders(session).

internal static CampaignNativeSecondBreakdownV46 Create(ExerciseSession session) =>
    CreateCore(session, SecondSlot);
internal static CampaignNativeSecondBreakdownV46 CreateForStageEntry(ExerciseSession session) =>
    CreateCore(session, StageEntrySlot);
internal static CampaignNativeSecondBreakdownV46 CreateForStageEntrySecondSide(ExerciseSession session) =>
    CreateCore(session, StageEntrySecondSlot);
```

`CreateCore(session, slot)` is the body of today's `Create`, with every direct field read
replaced by `slot.Xxx(session)` and every error-message literal replaced by
`$"{slot.Context}Movement has not completed."`-style interpolation. This is a pure,
mechanical, behavior-preserving extraction — the resulting event's `SerializeCanonical()`
output is untouched (EVT-001 safe), only the internal control flow is parameterized.

## 4. Target shape for Finding C — narrow collapse now, defer the real 0.3 registry

**My call: a narrow, standalone collapse now, using the identical `Slot`-descriptor
technique from Section 3 — not an early build-out of the overhaul doc's Section 0.3
handler-registry.** Reasoning:

- The Movement/Reserve triads in `CampaignExercises.cs` share Finding B's *exact* shape
  ("differs only by which session field to read"), confirmed by direct comparison of
  `QuerySecondMovementSource`/`QueryStageEntryMovementSource`/
  `QueryStageEntrySecondMovementSource`. This is the same problem as Finding B, not a
  different one — reusing the same fix technique is both less work and more consistent
  than inventing a second pattern for the same shape.
- Section 0.3's full `IStageDecisionHandler<TPending>` registry is explicitly scoped by
  the overhaul doc as **step 3** of a 5-step sequence, gated on Steps 1-2 (which PR #548
  already shipped) and explicitly deferred until "the 6-turn push reaches a stable point"
  (overhaul doc Section 6). Building that full infrastructure now, just to fix Finding
  C's ~15 triads, would be pulling forward a bigger, differently-sequenced piece of work
  than this plan needs, and risks exactly the kind of mid-growth collision the overhaul
  doc's own sequencing section was written to avoid (MTS slices are still landing in
  `CampaignExercises.cs`).
- The `Slot`-descriptor helpers this plan produces for Findings B and C are not wasted
  work if/when Section 0.3 is eventually built: each descriptor's fields map naturally
  onto a future `PendingDecision`-typed handler's constructor arguments. This is strictly
  additive groundwork, not a competing design.

## 5. Sequencing plan

Mirrors the precedent `PendingDecision` itself used (overhaul doc Section 0.6's 5-step
incremental sequence: compute-derive first at zero risk, migrate the read side, then build
the registry, then require it for new code, then leave the oldest/riskiest code alone
unless forced) — each slice below is independently shippable, and destructive steps
(deleting old code) are split into their own PR from the additive step that proves
equivalence first, the same lesson Section 3's reverted codec-fragment attempt taught.

**Gate (external to this plan, not mine to schedule):** PR #551 and PR #552 must merge
before any slice below touches `ExerciseSession.cs`, `CampaignExercises.cs`, or
`CampaignHostAdapter.cs`.

| Slice | Scope | Files | Depends on |
|---|---|---|---|
| **1 — do first** | Finding B: `CreateCore` + 3 `Slot`s | `CampaignNativeSecondBreakdownV46.cs` only | Gate above |
| **2** | Finding C: same `Slot` technique for Movement/Reserve triads | `CampaignExercises.cs` | Slice 1 (same file family, reuse the pattern; land after 1 since 1 is smaller/more urgent) |
| **2b** | Finding C minor: 8x test-fixture `Start`/`CreateRequest` → shared test helper; 15x catch-block → shared private helper | 8 test files + `CampaignExercises.cs` | None — can go first of all, lowest risk |
| **3a** | Finding A: `CampaignSourceAuthority.cs` ordered-chain registry, built additively alongside existing methods | `CampaignSourceAuthority.cs`, new `.AdmissionSteps.cs` | None — independent file |
| **3b** | Finding A: delete old `CampaignSourceAuthority.cs` triads once 3a proven byte-identical | same | 3a, proven green first |
| **4a** | Finding A: `CampaignSourceBattlefield.cs` generic quadruple for the 5 clean-fit groups, built additively | `CampaignSourceBattlefield.cs`, new `GroupTable.cs` | None — independent file |
| **4b** | Finding A: delete old methods for the 5 migrated groups; Libyan/Matruh stay hand-written permanently | same | 4a, proven green first |
| **5** | Finding A: `CampaignDurableAuthorityJournalStore.cs` — split persistence/dispatch, then registry-ize dispatch | `CampaignDurableAuthorityJournalStore.cs` (split into 2 files) | None file-wise; sequence after 3a for pattern-reuse confidence, not a hard dependency |
| **6 — optional, unscheduled** | `CampaignHostAdapter.cs` cosmetic forwarder deletion | `CampaignHostAdapter.cs` + 9 callers | None — but zero priority, do only if idle |

**Parallel (zero shared-file risk, can run simultaneously):** Slices 2b, 3a, 4a, 5, and 6
touch entirely disjoint files and can proceed at the same time, by different people/PRs,
with no merge-conflict surface.

**Sequential (same file or explicit dependency):** Gate → Slice 1 → Slice 2 (same
`CampaignExercises.cs`/`ExerciseSession.cs` family as the Gate PRs); 3a → 3b; 4a → 4b.

**Explicit priority call, per the task brief:** Slice 1 (Finding B) goes first, full
stop — it is the one still actively compounding (a 4th near-copy landed today per the
task's own briefing), and it is also the smallest, cheapest, and lowest-risk slice in the
whole plan, so there is no tension between "most urgent" and "do the easy one first."

## 6. Explicit scope boundaries — what this plan must NOT touch

- **EVT-001, absolute:** no slice changes any `CampaignNative*.SerializeCanonical()` /
  `Deserialize` byte output. Every slice is a reorganization of *which* existing,
  already-tested code path gets called, never a change to what gets serialized. Verified
  per-slice by the existing canonical-hash/replay test suite (Section 7).
- **The pre-`#394` Combat/Reserve-Release/Cycle-Control/Repeat chain** (overhaul doc
  Section 0.1's "original pattern": the 30-parameter `ExerciseSession` constructor and its
  dense cross-field invariant graph) — explicitly out of scope per that doc's own Section
  0.6 step 5 ("migrate opportunistically, if ever... don't schedule this"). This directly
  covers: `CampaignHostAdapter.cs`'s 20 `NativeXxxCommand` methods (Section 2.1, correctly
  deferred, not re-opened here) and the 9-site Reserve-Release/Cycle-Control ternary
  cascade PR #551/#552 just hand-patched (Section 1's flagged finding) — neither gets a
  slice in this plan.
- **Mid-flight PRs:** #551 and #552 (open, touching `ExerciseSession.cs`/
  `CampaignExercises.cs`/`CampaignHostAdapter.cs`) — this plan's Slices 1-2 are gated
  behind them, per Section 5. Checked and confirmed no other open PR
  (`gh pr list --repo dills122/sandtable --state open`) touches any file this plan
  modifies.
- **`CampaignHostAdapter.cs`'s "duplication" framing** is superseded by
  `docs/work/plans/2026-11-23-campaign-host-adapter-duplication-scoping.md` (PR #524) —
  treat that doc as authoritative; this plan does not re-litigate it (Correction 0.1).
- **Section 3's codec-schema-fragment extraction** (the `CombatEvidenceRevisions.cs`
  SHA256 source-file provenance-pin gate) is unrelated to Findings A-C, but its
  **lesson applies directly to Slice 3a**: a static "byte-identical" check passing is not
  proof a refactor is safe — the real `dotnet test` run is the only real proof. Slice 3a's
  plan explicitly requires a full targeted test run before 3b deletes anything, not just a
  hash comparison.
- **Don't widen `CampaignSnapshotV11`** — already protected by
  `tests/Cna.ArchitectureTests/CampaignSnapshotV11SurfaceTests.cs`; none of these slices
  should need to touch it, flagging only as a tripwire.
- **`tests/Cna.ArchitectureTests` and `src/Cna.Analyzers` already exist** (confirmed by
  directory listing) — this plan doesn't need to build them (overhaul doc Section 6 item
  1, apparently already done), only use them where relevant for verification.

## 7. Verification plan per slice

- **Slice 1 (`CampaignNativeSecondBreakdownV46`):** `CreateForStageEntry` and
  `CreateForStageEntrySecondSide` each have a dedicated test file
  (`CampaignNativeStageEntrySecondBreakdownTests.cs` — confirmed exists) that must stay
  green. **Gap found:** plain `Create` has **no dedicated unit test file** — it's only
  reached indirectly via `CompleteSecondBreakdown` in `CampaignExercises.cs:6928` and
  broader integration/combat-exercise tests. **New regression test needed first,** per
  this task's own lesson (ComputePending's bug could have been one parameterized test
  over all arms instead of two hand-written ones): add a `[Theory]`-style test over the 3
  entry points (`Create`/`CreateForStageEntry`/`CreateForStageEntrySecondSide`) asserting
  each rejects its own missing-predecessor case with the right message — write this
  **before** extracting `CreateCore`, so it fails meaningfully pre-refactor and passes
  post-refactor for all 3 variants, not just the 2 that already had coverage.
- **Slice 2 (`CampaignExercises.cs` Movement/Reserve triads):** run
  `dotnet test --filter "FullyQualifiedName~Exercises|FullyQualifiedName~Movement|FullyQualifiedName~Reserve"`
  plus the full `EndToEndHttpRelease`/`EndToEndHttpMultiContact`/`MovementSegmentCompletion`
  integration suite (named in PR #525's own test plan as real coverage for this dispatch
  surface).
- **Slice 2b (test fixtures / catch blocks):** no production assertions change; run the
  full `Cna.Core.Tests` suite once to confirm zero regressions from the mechanical
  extraction (pure refactor of test code and exception-handling boilerplate).
- **Slice 3a/3b (`CampaignSourceAuthority.cs`):** `CampaignSourceAuthorityStateInvariantTests.cs`
  (confirmed exists) plus `GrazianiPregameJournalTests` (PR #525's own cited admission-
  triad coverage) are the direct proof the hash chain still produces identical
  `ProfileHash`/event bytes. **Required before 3b:** a real `dotnet test` run (not just a
  static byte-comparison of the assembled chain — this is precisely the Section 3 lesson
  from the overhaul doc's reverted codec-fragment attempt) across the full
  `Cna.Core.Tests` + `Cna.OrleansHost.Tests` suites.
- **Slice 4a/4b (`CampaignSourceBattlefield.cs`):** **gap found** — no dedicated
  `*Battlefield*` test file exists under `tests/`. **New regression test needed first:**
  before building the generic quadruple, add a parameterized test iterating the 5
  migrating groups (FourthIndian/Catanzaro/SeventhArmored/SixteenthBrigade/
  ItalianDivisions) asserting `Propose`→`Confirm`→`Validate` produces identical
  `CampaignGroupedWorld` state and identical rejection messages as today's hand-written
  methods, run once pre-refactor (capturing today's actual behavior) and once post-refactor
  (must match). This is the single highest-value new test in this whole plan, since it's
  the only slice with zero existing direct coverage.
- **Slice 5 (`CampaignDurableAuthorityJournalStore.cs`):** run
  `CampaignDurableAuthorityJournalTests.*` (the `tests/Cna.OrleansHost.Tests/` suite,
  confirmed to contain per-mechanic files like `CampaignDurableAuthorityJournalTests.SecondCombatSelection.cs`)
  plus the full Orleans integration suite, since this is the persistence/concurrency
  layer directly.
- **Slice 6 (if ever done):** `dotnet build` clean + full `Cna.Core.Tests` run; pure
  deletion of dead forwarders plus repointing callers, no new test needed since no logic
  changes.

## Critical files for implementation

- src/Cna.Core/Campaigns/CampaignNativeSecondBreakdownV46.cs
- src/Cna.Core/Exercises/CampaignExercises.cs
- src/Cna.Core/Campaigns/CampaignSourceAuthority.cs
- src/Cna.Core/Campaigns/CampaignSourceBattlefield.cs
- src/Cna.OrleansHost/Api/CampaignDurableAuthorityJournalStore.cs
