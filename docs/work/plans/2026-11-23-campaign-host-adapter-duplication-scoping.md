# `CampaignHostAdapter.cs` vs `CampaignExercises.cs`: scoping the "duplication" finding (2026-11-23)

**Status:** scoping doc, read-only investigation, no code changed. Answers whether the
earlier audit's "`CampaignHostAdapter.cs` (4,611 lines) duplicates much of the same
session-routing surface as `CampaignExercises.cs`" finding needs its own fix, or is
already covered by the overhaul direction doc
([`2026-11-23-codebase-overhaul-direction.md`](2026-11-23-codebase-overhaul-direction.md)).

## 1. The actual relationship: one-directional adapter, not parallel duplication

Confirmed by direct reads and grep, not assumption:

- `CampaignHostAdapter.cs:13` carries its own class doc comment: *"Trusted, in-process
  boundary. None of these private records are browser DTOs."* — it is explicitly meant to
  be a boundary/adapter layer, not an independent implementation.
- `grep -c "CampaignHostAdapter\." src/Cna.Core/Exercises/CampaignExercises.cs` → **0**.
  `CampaignExercises` never calls `CampaignHostAdapter`. The dependency is strictly
  one-directional: `CampaignHostAdapter` → `CampaignExercises`.
- The bulk of the pregame/Graziani source-admission surface
  (`CampaignHostAdapter.cs:240-414`, the `AdmitSourceXxx`/`PromoteSourceXxx` family, ~30
  methods) is **pure one-line forwarding**, e.g.:
  ```csharp
  // CampaignHostAdapter.cs:240-241
  public static ExerciseStepResult AdmitSourceAxisSupplement(ExerciseSession session) =>
      CampaignExercises.AdmitSourceAxisSupplement(session);
  ```
  Same name, same signature, zero added logic. This is **not** duplication in the
  copy-paste/drift sense the codec-literal finding (Section 3 of the direction doc) or the
  god-file dispatch-chain finding (Section 1) describe — there is no second copy of any
  logic to drift out of sync. An edit to `CampaignExercises.AdmitSourceAxisSupplement`
  takes effect through this wrapper automatically, with no risk of the two diverging.
- Several other forwarders add exactly one guard check before delegating, e.g.
  `QueryPregameAxisSupplyOwnerDraft` (`CampaignHostAdapter.cs:30-36`) checks
  `CampaignExercises.IsPregameAxisSetup` before calling
  `CampaignExercises.QueryPregameAxisDraft`. Still a thin wrapper, not parallel logic.

**Verdict on this part: the original audit's "duplicates much of the same surface"
framing was imprecise.** It is large because it mirrors `CampaignExercises`' public
surface near-1:1 (hence the line count), not because it reimplements it. This needs no
de-duplication fix — there's nothing to de-duplicate.

## 2. The one genuinely distinct, non-trivial concern: `NativeXxxCommand` builders

`CampaignHostAdapter.cs:1455-3963` (confirmed by direct read, ~2,500 lines) contains 20
methods named `NativeXxxCommand` (e.g. `NativeRepeatCombatNoAttackCommand`,
`NativeCombatBarrageCommand`, `NativeCombatForceCompletionCommand`,
`NativeCombatForceCancellationCommand`, `NativeCombatRbaCancellationCommand`) with **no
equivalent anywhere in `CampaignExercises.cs`** (`grep` for a `Command`-suffixed builder
pattern there returns nothing). Each one:

1. Validates that an `ExerciseStepResult` actually represents the expected
   System-audience transition, by branching on **which of two mutually-exclusive session
   shapes applies** — e.g. `CampaignHostAdapter.cs:1480-1484`:
   ```csharp
   if (!accepted.IsAccepted
       || (predecessor.CurrentRepeatAssault is { Barrage: null }
           ? accepted.SuccessorSession?.CurrentRepeatAssault?.Barrage is null
           : predecessor.CurrentCombatDecision is null
               || accepted.SuccessorSession?.CurrentCombatBarrage is null))
       throw new ArgumentException("Accepted Barrage completion required.", nameof(accepted));
   ```
2. Calls `CampaignExercises.QueryCheckpoint(predecessor)`.
3. Builds and returns a `CampaignActionSubmission` — the next System-audience command to
   auto-drain (the #508 auto-drain mechanism from earlier this session).

These are called exclusively from the Host's per-mechanic timeout/auto-drain machinery
(`src/Cna.OrleansHost/Api/CampaignDurableAuthorityJournal.{Cycle,Retreat,Reserve,Rba,Custody,Force,Selection}Rounds.cs`
— confirmed by grep, 7 files, matching the "7 independent round state machines" the
earlier Host/API audit named) plus a handful of Core tests. This is genuinely distinct,
non-duplicate functionality — it's the "construct the next system step" concern, which
`CampaignExercises` itself never needs to do for itself.

**But it has the exact same root-cause disease Section 0 of the direction doc already
diagnosed**: branching on "which of several optional/nullable session shapes is this"
instead of reading an explicit, named state. The `is { Barrage: null } ? ... : ...`
ternary here is the same anti-pattern as `QueryCheckpoint`'s null-check cascade and the
`#7583` predecessor-walk — just written as a guard clause instead of an `if` chain.

## 3. Does Section 0's `PendingDecision` direction already cover this?

**Yes, as a side effect, once built — this does not need a separate fix.** Once
`ExerciseSession` exposes a single `PendingDecision` (or, for the already-generalized
Combat-cycle machinery specifically, an equivalent explicit "which variant is this"
marker) instead of parallel nullable fields, each `NativeXxxCommand` method's
`is { X: null } ? A : B` branch collapses to a single type/tag check the same way
`QueryCheckpoint` does. No new interface or registry is needed beyond what Section 0
already proposes — these 20 methods are additional *consumers* of `PendingDecision`, not
a new producer of the problem.

**Recommendation: do not open a separate workstream for this.** Fold awareness of these
20 methods into Section 0's migration sequence (step 3/4 in the direction doc's Section
0.6) as additional call sites to migrate when the Combat-cycle machinery's
`PendingDecision` equivalent is built — don't schedule or size it separately, since it's
not separate work, just more call sites for the same fix.

## 4. Safe-to-parallelize verdict

**Nothing here needs new code changed, so there is nothing to parallelize or sequence.**
This section exists only to correct the record: the original "duplication" framing
doesn't describe a real problem, and the one real finding (`NativeXxxCommand`'s
nullable-branching) is already inside Section 0's existing scope, not a new item for
Section 6's sequencing table.

One genuinely optional, zero-risk cleanup, **not recommended as a priority**: the ~30
pure one-line forwarders in `CampaignHostAdapter.cs:240-414` could be deleted and their
callers (seven `*Rounds.cs` files, two `CampaignDurableAuthorityJournal.*` files, per
grep) repointed straight at `CampaignExercises` — this would shrink
`CampaignHostAdapter.cs` by a few hundred lines with zero behavior change, since they're
pure delegation. It's safe to do whenever (touches neither the in-flight MTS branches'
files nor the Host-layer registry refactor's files), but it's cosmetic, not a
correctness or maintainability fix, so it doesn't warrant scheduling ahead of anything
in Section 6.

## 5. Correction owed to the original audit

The audit phrasing "`CampaignHostAdapter.cs` (4,611 lines) duplicates much of the same
session-routing surface as `CampaignExercises.cs`" should be read as **superseded by this
doc** — it's large because it mirrors the surface via delegation, not because it
duplicates logic. Future references to this finding should cite this doc, not the
original audit's framing.
