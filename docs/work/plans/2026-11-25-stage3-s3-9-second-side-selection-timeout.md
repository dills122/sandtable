# Stage 3 S3-9: second-side Combat and stage-aware selection rounds

Date: 2026-11-25. Follows S3-6..8 (#639), S3-5 (#635), and the Stage 2
second-side template (#540). This is the final Core occurrence slice before
the cursor-derived executor migration and End of Turn.

## Implementation

- Stage 3's second-side decision, assault runtime, and no-attack completion
  have distinct retained slots and predecessors. The second-side occurrence
  takes precedence over first-side and Stage 2 records in `Pending`,
  `QueryCheckpoint`, `ReconstructOne`, and assault dispatch. The existing V47
  selection and assault runtime are reused without changing rules or event codecs.
- `ActiveThirdStageAssault` selects the retained current side for the Tier 2
  release/control/truck/pass methods, including the finished runtime. The
  generic `CurrentRepeatAssault` excludes finished runtimes. Once Stage 3's
  second side begins, neither accessor nor its mutation path falls back to
  first-side authority. Before selection, the tail queries fail closed.
- Selection binding and timeout resolve the current V46 Breakdown by `Pending`
  for Stage 1 second side, both Stage 2 sides, and both Stage 3 sides. The
  legacy Stage 1 first-side initial/repeat V38 path remains unchanged. V48
  timeout creation and `ReconstructOne` share the same occurrence-bound world,
  position and Breakdown; a stale Breakdown cannot bind an intervening phase.
  Selected decisions use `LatestCursor`; selection and no-attack draw no dice.
- Stage 3 first- and second-side no-attack evidence now contains the retained
  event bytes as its checkpoint, matching `Reconstruct` of the observable
  Assault head (Defect B from S3-5). No existing event's `SerializeCanonical()`
  implementation or rules-artifact hash is changed.
- New pending names are in the reconstruct harness, occurrence/exception
  tables, oracle, spike, and open-seat catalog (`Unwired(Stage3)`). No CI timing
  manifest changes. The no-attack pending arm is unobservable: completing it
  installs a non-empty assault runtime, which takes precedence.

## Stale-field checklist

| Item | Disposition |
|---|---|
| Legacy `CampaignCombatExerciseActions.Classify` reading Stage 1 fields | Guard later-stage sessions; direct native APIs remain available. Later-stage Runner/Host classification is not implemented here. |
| Round binding and timeout keyed off retained Stage 1 Breakdown | Resolve by current pending occurrence and verify authority version. Reject before/after selection rather than bind stale fields. |
| First-side Combat queries/submissions and `HasAdvancedCombatSelection` | Stage 3 second-side selection uses second-side world/position and only opens at its Breakdown. Legacy movement projection does not answer for native cuts. |
| `SubmitPostCombatPass` stale first-side/Stage 2 runtime | Resolve current Stage 3 runtime, fail closed before it exists; retain Stage 1's initiative guard. Release/truck source queries similarly fail closed. |
| `InvalidNativeHistory` hashing a stale Stage 1 head | Hash the current history's retained last event. |

## Evidence and limits

The shared `pregame-no-attack` driver reaches Stage 3 second-side selection,
no-attack, release, cycle finish, truck pass, and all four post-combat passes.
The oracle's unvisited count shrinks from 28 to 20 (four actual-combat cuts in
each of the five non-legacy occurrences); the spike subtype count is 49.
Each observable new `PendingDecision` is fixture-driven, and a one-byte flip
of its retained event yields `InvalidHistory`, not an exception. The dedicated
second-side suite also verifies every driven tail cut, its serialized bytes,
its checkpoint, and the identity of the earlier retained runtimes.

The selection-timeout regression exercises five V46 occurrences, binds their
own Breakdown hashes, rejects early/duplicate/out-of-phase timeouts, and
re-admits serialized V48 events onto the retained predecessor before cold
Core `Reconstruct`. It does not simulate process restart or durable Host import:
Stage 3 accepted-step kinds do not exist. Host entry binding for the new Stage 2
accepted Breakdown kind in #640 must also be recognized by
`CampaignHostAdapter.QueryNativeCombatSelectionEntryBinding` for journal
`VerifySelectionRounds`; this dependency is coordinated with the Host owner.

Not proven: a real Stage 3 Move, a real attack or a repeat cycle in these
occurrences, or the unobservable `ThirdStageSecondNoAttackPending` arm. No
End of Turn action is introduced. Stage 2 ledger reset is deferred: the
StageEntry reserve source carries the previous stage's settled World, and
`ProjectSecondMovement` requires `LedgerOperationStage` to match the current
position. `CampaignNativeSecondMoveV44.Create` also calls that projection and
binds both prior and resulting world hashes. Altering only a projection or
resetting the authoritative ledger without a replay-compatible transition would
be incorrect. Close this in a separate ledger/world-transition slice.
