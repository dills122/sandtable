# L8: Runner exit 11 at second-side Reserve Release (investigation)

Date: 2026-11-25. Base: `main` at `cc9c4d3d`. Investigation only; no production change. Labels: VERIFIED
(reproduced or read in this checkout), ASSUMED (not exercised).

## Reproduction (VERIFIED)

`rules-lab.reserve.breakdown.v1.json` with `terminalBoundary` set to an unreachable id, `maximumSteps` 400,
policy `first-by-action-id`, run through `dotnet Cna.ExerciseRunner.dll exercise run`:

- Exit code 11 (`ArtifactFailed`), stderr: "Artifact finalization failed; no completed bundle exists: The final
  checkpoint does not equal Core's reconstruction of the canonical events."
- A `failed/` bundle is still written. `run-result.json` carries `no-unique-legal-action`; the last accepted step is
  ordinal 29 (System, second-side `combat-no-attack-completed`), final position
  `land.position.operation-1.second-player.movement-and-combat.reserve-release`, 30 steps.
- The same happens for all 200 runs of the sweep dataset (see `tools/sweep/README.md`).

## Two distinct defects

The plan's single "exit 11 at second-side Reserve Release" is two separate defects that stack.

### Defect A: no legal action at second-side Reserve Release (the frontier)

`src/Cna.Core/Exercises/CampaignCombatExerciseActions.cs:219-229` (`SecondPlayer`): once
`CurrentSecondNoAttackCompletion` is set, the cut is `new(Stage.Frontier)`. The comment above it still says "the
second side's no-attack completion does not open Reserve Release". The Runner therefore offers zero actions and
stops with `no-unique-legal-action`.

This is stale relative to Core and the Host:

- Core initializes the second-side Release: `CampaignNativeRepeatAssaultRuntime.cs:193` calls
  `CampaignNativeReserveReleaseStateV27.InitializeAfterSecondNoAttack` (`CampaignNativeReserveReleaseV27.cs:266`).
- `CampaignExercises.QueryNativeReserveReleaseState` already returns `CurrentSecondAssault.Release` (`CampaignExercises.cs:6647`).
- The Host calls `AppendEmptyReserveRelease` after the second no-attack (`CampaignDurableAuthorityJournal.Submissions.cs:702`).

### Defect B: failed-bundle finalization mismatch (the exit 11 itself)

The step evidence checkpoint and Core's reconstruction disagree for the second-side no-attack completion:

- The accepted step's checkpoint bytes are `completion.SerializeStateCanonical(selected)`
  (`CampaignExercises.cs:4954-4960`, `CompleteSecondCombatNoAttack`): a document starting
  `{"contractVersion":13,"campaignId":...,"priorDecisionState":...,"completion":...}`.
- `WithSecondNoAttack` (`ExerciseSession.cs:946`) also advances `CurrentSecondAssault`, so
  `ExerciseSession.Pending` (`ExerciseSession.cs:2062`, `CurrentSecondAssault is { Events.Count: > 0 }` is checked before
  the SecondNoAttack arm) resolves to `SecondAssaultPending`. `Reconstruct` then returns the plain event bytes
  `CampaignCurrentEventSerializer.Serialize(secondAssault.Events[^1])` (`CampaignExercises.cs:9794-9800`), a document
  starting `{"contractVersion":13,"eventType":"combat-no-attack-completed",...}`.
- `ExerciseBundleSemanticValidator.DeserializeFinalCheckpoint` (`ExerciseBundleSemanticValidator.cs:243-262`) requires
  these bytes to be equal and throws the exit-11 message otherwise.

`Reconstruct(...).IsVerified` is true, which is why `CombatSelectionRunnerTests.UnsupportedNextCombatStepIsAnHonestNoLegalActionFrontier`
passes: it never compares the bytes or finalizes a bundle. `PendingDecisionReconstructCoverageTests` already notes
that `SecondNoAttackPending` is unreachable for the same ordering reason.

Reproduced as a test: `tests/Cna.ExerciseRunner.Tests/Execution/SecondSideReserveReleaseFinalizationTests.cs`
(VERIFIED, both tests green). It is a characterization test, not a red test: a failing row would block CI for every
PR, so it asserts the mismatch and says to invert it when fixed.

## Is it #431? (decision)

Partly, and not as written. #431 says second-side Reserve-Release initialization is unimplemented. Initialization is
implemented in Core and the Host. What remains is on the Exercise/Runner surface:

- Defect A is the Runner-side counterpart of #431's scope (second-side Reserve Release not offered). Keep #431 open
  and re-scope its title/body to "Exercise/Runner stage ladder for second-side Reserve Release".
- Defect B is not #431. It is a checkpoint-bytes inconsistency between `CompleteSecondCombatNoAttack` and
  `Reconstruct`. It will also hit any run that stops (for example at `maximumSteps`) right after the second no-attack.
  Recommend a new issue (not filed here).

## Proposed minimal fixes (for the Stage 3 chain owner)

| Defect | Location | Size | Hot file? |
|---|---|---|---|
| A | `CampaignCombatExerciseActions.cs` `SecondPlayer`: when `CurrentSecondNoAttackCompletion` is set, return an `AfterCombat`-style cut that reads the second-side Release (the query already does). | S for the cut. M if second-side `CompleteNativeReserveRelease`/Cycle Control successors need `ExerciseSession`/`CampaignExercises` changes (ASSUMED, not tried). | Cut: no. Successors: yes |
| B | In `Reconstruct`'s `SecondAssaultPending` arm (`CampaignExercises.cs:9794`), when `Events[^1]` is the V13 completion, expect `SerializeStateCanonical(selected)`; or have `Pending` resolve to `SecondNoAttackPending` until the Release event exists. | S (about 10 lines) | Yes (`CampaignExercises.cs`/`ExerciseSession.cs`) |

Fixing A likely hides B for default runs, because the run then ends after Reserve Release, whose checkpoint is the
plain event as on the first side (ASSUMED). B still bites capped runs.

## #448

Stale (VERIFIED): the Runner now drives Stage 1 for both sides through second-side Combat Barrage and the second
no-attack completion (30 steps). Its dead-end claim ("first-side Combat Position Determination, zero legal actions")
no longer holds. Recommend re-scope to "second-side Reserve Release frontier and post-assault Retreat owner stage",
or close in favor of #431. Not closed here.
