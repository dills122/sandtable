# Initial engine release — readiness check (November 21, 2026)

> **2026-11-26 follow-up:** This is a dated baseline, not the current implementation ledger.
> #640, #645, #648–#650 and #658 cover bounded synthetic Stage 2/3 HTTP progression and real
> Combat rounds; #646, #651 and #652 prove Turn 1 through the next turn’s open Reserve.
> Turn 2 acceptance and later turns, native Reaction (#655) and executor migration remain open.
> See the [current roadmap](../../roadmap/pre-alpha-roadmap.md#current-checkpoint-and-next-gates).

This checks every item in
[the frozen scope doc](2026-11-21-initial-engine-release-scope.md) against
the actual code and tests, today. It does not take any other document's
word for a status claim — every line below was checked directly against
source or a passing test. "Real Host path" means the production API at
`src/Cna.OrleansHost/Api/CampaignDurableAuthorityJournal.*`, reachable over
HTTP through `CampaignDurableOutwardApi` at `/api/v2/campaigns/{id}/openings`
and `/actions` — the path a real player's move goes through. "Exercise
Harness" means `src/Cna.ExerciseRunner` and `Cna.Core.Exercises`
(`CampaignExercises.QueryWithCombat`/`SubmitWithCombat`) — a separate,
test-only automation lane. Progress on one never implies progress on the
other; every row below says explicitly which one it is about.

Status labels: **Done** (implemented and proven by a passing test on the
real Host path), **Partial** (real, but narrower than the requirement, or
missing an explicit guarantee), **Open** (not implemented, or implemented
but not reachable end-to-end), **Blocked** (needs a human decision),
**Unverified** (checked as far as this pass could go and still genuinely
unresolved — stated plainly rather than guessed).

## Phase 1 findings: the two previously-unconfirmed items

### Item 1 — does turn-to-turn looping work on the real Host path?

**Status: Unverified.** Not resolved either way by this pass, and here is
exactly why, so nobody has to re-derive this from scratch:

- The turn-wrap rule itself is correct. `Cna1979LandSequenceV4.GetNext`
  (`src/Cna.Core/Rules/Cna1979LandSequenceV4.cs:20-30`) rolls from a turn's
  last catalogue position into `CreateTurn(gameTurn + 1)[0]`, and this is
  proven by `tests/Cna.Core.Tests/Rules/GrazianiSequenceCatalogTests.cs:46`
  (`Assert.Equal(turn + 1, Cna1979LandSequence.GetNext(positions[^1]).GameTurn)`
  for all 6 turns). **But this is exactly the static-catalogue evidence the
  scope doc warned against** — the test file's own header comment says so:
  "Checks the static catalogue across the scenario horizon, not campaign
  execution." It proves the rule is right, not that the real Host path ever
  reaches and executes it.
- `GetNext` is called from many real Core classes that the Host path does
  reach at runtime — e.g. `CampaignNativeTruckConvoyPassV41.cs:31`,
  `CampaignNativeSecondReserveV43.cs:45`,
  `CampaignNativeSecondMovementCompletedV45.cs:30`,
  `CampaignNativeSecondBreakdownV46.cs:78`, `CampaignNativeCycleControlV28.cs:72`
  (the `CycleControl` "Finish" choice itself — but note this transition
  stays in the **same** `GameTurn`, moving to Truck Convoy Movement, not
  across a turn boundary: `CampaignNativeCycleControlV28.cs:73-76` asserts
  `next.GameTurn == release.Position.GameTurn`).
- The most turn-advancing Host integration test found,
  `tests/Cna.OrleansHost.Tests/CampaignDurableAuthorityJournalTests.IntegratedCampaign.cs:19-181`
  (`IntegratedCampaignDurableSaveResumeMatchesUninterruptedAuthority`), drives
  Force → Assault result → Reserve Release → Cycle Control **Repeat** →
  Repeat Movement → Repeat Combat Selection → RBA → Force → a second Assault
  result → Reserve Release → Cycle Control **Finish** (via timeout). That
  stays entirely inside Operation Stage 1's first-acting-side side-block,
  repeated. It never reaches Truck Convoy Pass, Second Reserve, Second
  Movement, a second-acting-side Combat resolution, Operation Stage 2, or a
  second `GameTurn`. No Host-level test was found that crosses a `GameTurn`
  boundary at runtime.
- Item 5 below (confirmed Open) is directly relevant: whenever a turn's
  second-acting-side block actually has a Combat contact that requires a
  decision, the real Host path currently has no automatic continuation past
  Combat Selection for that side (see below) — which would also block ever
  reaching that side's own Reserve Release, and therefore the end of that
  side-block, on the real Host path. Whether a second side with **no**
  contact can still complete its block and cross into the next Operation
  Stage/Turn was not traced in this pass — that would require its own
  investigation, not a guess from adjacent evidence.

**Next concrete step:** write one Host-level test that drives a full turn
(Reserve → Movement → Breakdown → first-side Combat-to-finish → Cycle
Control Finish → Truck Convoy Pass → Post-Combat Pass → Second Reserve →
Second Movement → Second Movement Completion, with a second side that has
*no* Combat contact so item 5's gap doesn't block it) all the way to
Operation Stage 2 (or Turn 2 if stage 3 is reached), and assert `GameTurn`
advanced. That test does not exist today.

### Item 2 — does a second-side Combat assault resolve to a result on the real Host path?

**Status: Open — confirmed, real gap.** This directly verifies the agent
claim the scope doc flagged as "not yet independently re-verified" — it was
right.

- `src/Cna.OrleansHost/Api/CampaignDurableAuthorityJournal.Submissions.cs:820-839`
  (the "`#394 extra-3`" comment and the code under it) and `:840-874` make
  this explicit in the code itself: when the **second** acting side's Combat
  Selection submission is a voluntary no-attack finish, it is "accepted,
  persisted, and cold-replayable, but the campaign does not auto-advance
  into Reserve Release/Cycle Control past this point yet." The comment names
  the reason: `CampaignExercises.QueryNativeReserveReleaseState`'s only lazy
  initializer (`InitializeAfterInitialNoAttack`) is keyed to the **first**
  side's decision; there is no second-side-keyed initializer.
- `Submissions.cs:875-896` (the "`#394 extra-2`" comment) says the same
  thing for a second-side **assault** pick: it "is likewise accepted and
  persisted by the submission itself, with no auto-continuation here —
  second-side Barrage/Retreat/Custody/Settlement resolution is out of ...
  scope." The branch that *does* continue into Barrage
  (`Submissions.cs:882-896`) is explicitly guarded with
  `!CampaignHostAdapter.IsSecondSideCombatPending(...)`, i.e. it only fires
  for the first side.
- Independently confirmed by a passing test, not just the comment:
  `tests/Cna.OrleansHost.Tests/CampaignDurableAuthorityJournalTests.SecondCombatSelection.cs:287-345`
  (`SecondSideRealAssaultSelectionIsLegalPersistedColdReplayableAndRetried`)
  proves a second side's **real** assault selection (against a genuine
  adjacent enemy, not a voluntary finish) is legal, persisted, and
  cold-replayable — and stops exactly there. Nothing in that test file
  drives the second side past selection into Barrage, a result, or Reserve
  Release, because nothing in the production code does either.
- The openings themselves are side-generic in principle: `CombatSelection`,
  `ForceAssignment`, `ReserveRelease`, etc. all key off `seat.Audience`
  (`CampaignDurableAuthorityJournal.Openings.cs:352-407`), so the gap is not
  "the Host can't do second-side Combat at all" — it is specifically that
  the automatic continuation after an accepted second-side selection
  (Barrage, then Reserve Release) was never written.

**Next concrete step:** add a second-side-keyed Barrage/Reserve-Release
continuation, mirroring the existing first-side logic at
`Submissions.cs:825-839` and `:882-896`, and a second-side-keyed
`CampaignExercises.QueryNativeReserveReleaseState` initializer alongside
the existing `InitializeAfterInitialNoAttack`. This is a scoped Core+Host
change, not a Runner/Exercise-Harness change.

## Phase 3: per-item status against the frozen scope

### Required (items 1-8)

| # | Item | Status | Path | Implementation citation | Test citation |
| - | --- | --- | --- | --- | --- |
| 1 | Turn/stage/side sequencing incl. Turn N→N+1 looping and scenario-end detection | **Unverified** (see Phase 1 above) | Real Host API | `Cna1979LandSequenceV4.GetNext` (`src/Cna.Core/Rules/Cna1979LandSequenceV4.cs:20-30`) | Rule-level only: `tests/Cna.Core.Tests/Rules/GrazianiSequenceCatalogTests.cs:46`. No Host-level test crosses a `GameTurn` boundary. |
| 2 | Preamble (weather/org/convoy/reserve designation) on synthetic/lab content | **Done** (spot-checked, not re-derived) | Real Host API | Weather/Organization/Reserve Designation implementation per `docs/roadmap/pre-alpha-roadmap.md` Tasks 3.3-3.5 ("Delivery status: Complete") | Reserve-gated `Movement` opening confirms the preamble boundary is enforced before Movement opens: `CampaignDurableAuthorityJournal.Openings.cs:44-46` |
| 3 | Movement / ZOC / Reaction / Breakdown | **Done** | Real Host API | `CampaignOwnerOpeningKind.Movement`, `CampaignDurableAuthorityJournal.Openings.cs:44-46` | `tests/Cna.OrleansHost.Tests/CampaignDurableAuthorityJournalTests.cs:1697-1743` (`ContactMovementReferenceCommitsOnceAndReplaysAfterRestart`) — accepts, retries, cold-replays |
| 4 | Combat, full cycle, first side Selection → ... → Reserve Release | **Done** | Real Host API | `CombatSelection`/`RetreatBeforeAssault`/`ForceAssignment`/`PostAssaultRetreat`/`Custody`/`ReserveRelease`/`CycleControl`/`RepeatCombatSelection`/`RepeatMovement` opening kinds, `CampaignDurableAuthorityJournal.Openings.cs:117-172`, `:352-457` | `tests/Cna.OrleansHost.Tests/CampaignDurableAuthorityJournalTests.IntegratedCampaign.cs:19-181` (`IntegratedCampaignDurableSaveResumeMatchesUninterruptedAuthority`) drives Force→result→Reserve Release→Cycle Control Repeat→Repeat Movement→Repeat Combat→RBA→Force→second result, cold-replay-verified throughout |
| 5 | Combat, second side resolving an assault to a result | **Open — confirmed gap** (see Phase 1 above) | Real Host API | `CampaignDurableAuthorityJournal.Submissions.cs:820-896` (explicit "#394 extra-2/extra-3" no-auto-continuation comments and guards) | `tests/Cna.OrleansHost.Tests/CampaignDurableAuthorityJournalTests.SecondCombatSelection.cs:287-345` proves selection persists; nothing proves further |
| 6 | Truck Convoy Pass | **Done** | Real Host API | `CampaignNativeTruckConvoyPassV41.cs`; `CampaignOwnerOpeningKind.TruckConvoyPass`, `Openings.cs:149-150,408-414` | `tests/Cna.OrleansHost.Tests/CampaignDurableAuthorityJournalTests.cs:1188-1239` (`VoluntaryCombatFinishHasNoPrivateTargetAndRetainsReceiptAcrossRestart`) reaches contract version 41 via query/submit/cold-replay |
| 7 | Second-side Reserve / Movement / Movement-completion / Breakdown hand-off into second-side Combat (issue #394) | **Done**, with item 5's gap as the one explicitly-deferred remainder | Real Host API | `SecondReserve`/`SecondMovement`/`SecondMovementCompletion` opening kinds, `Openings.cs:155-162` | `tests/Cna.OrleansHost.Tests/CampaignDurableAuthorityJournalTests.SecondCombatSelection.cs:55-59,111-150` (`AdvanceToSecondCombatSelectionWithAdjacentEnemy` helper) drives `SubmitOwnerSecondReserve`→`SubmitOwnerSecondMovementCompletion` to a real second-side Combat Selection opening |
| 8 | Determinism/replay (DET-001/DET-002) holding | **Done so far — ongoing, not a one-time gate** | Real Host API | n/a (a property of every accepted step, not one class) | `tests/Cna.OrleansHost.Tests/CampaignDurableAuthorityJournalTests.IntegratedCampaign.cs:202-221` (`IntegratedCampaignCompare`) asserts `EventStreamHash`/`ExpectedSnapshotHash`/`ReconstructedSnapshotHash`/`ReconstructedSnapshotBytes` equality after every one of the test's ~6 save/resume cycles |

### Required, no-op acceptable (items 9-12)

| # | Item | Status | Path | Citation |
| - | --- | --- | --- | --- |
| 9 | Rail | **Not started — confirmed** | Real Host API | Full `CampaignOwnerOpeningKind` enum read end to end, `Openings.cs:677-717` — no Rail entry exists |
| 10 | Repair (×2 per side-block) | **Not started — confirmed** | Real Host API | Same enum, same citation — no Repair entry |
| 11 | Patrol | **Not started — confirmed** | Real Host API | Same enum, same citation — no Patrol entry |
| 12 | Supply/logistics | **Partial** | Real Host API (content/source layer) | Provisional policy tags baked into canonical serialized content: `CampaignSourceAxisSupplyWorld.cs:116-128` and siblings, per `docs/work/plans/2026-11-17-engine-capability-deep-audit.md:64-79`. **Unverified sub-claim, not re-checked this pass:** whether every unsupported supply path explicitly rejects (FID-002) rather than silently approximating — the no-op-acceptable bar requires this and it was not independently confirmed here. |

## What this doc does not cover

It does not re-verify every claim in every doc corrected in this session's
companion pass (see the correction notes added to `docs/roadmap/pre-alpha-roadmap.md`,
`docs/work/plans/2026-10-24-rc-checklist.md`,
`docs/work/plans/2026-11-12-graziani-playable-rc-inventory.md`, and
`docs/work/plans/2026-11-17-rc-exhaustive-status.md`). It covers exactly the
12 items in the frozen scope doc plus the two items Phase 1 was asked to
resolve.

## Bottom line

Of the 8 required items: 5 are Done on the real Host path with a passing
test and a direct citation (2, 3, 4, 6, 7), 1 is Open with a confirmed,
well-understood gap and a scoped next step (5), 1 is "ongoing and holding
so far, not a one-time gate" (8), and 1 — the turn-to-turn loop itself — is
genuinely **Unverified**: the rule is correct, the wiring looks plausible,
but no test proves it happens at runtime on the real Host path, and item
5's gap is a plausible blocker for at least some reachable turns. Of the 4
no-op-acceptable items: 3 are confirmed not started (as expected and
accepted), and 1 (supply) is Partial with one sub-claim (explicit rejection)
still unverified.
