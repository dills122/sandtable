# Initial engine release — agreed scope (frozen 2026-11-21)

**Status: owner-agreed scope for the first engine-only pre-alpha release. Do not
reinterpret or narrow/broaden this without a new explicit owner ruling recorded
here.** This supersedes informal framing from this session's prior status
updates, which repeatedly conflated the Exercise Harness test-automation tool
with the real production path and caused real scope-tracking errors (see
"Known errors this doc corrects" below).

## The actual goal, in the owner's words

> Our main thing is being able to have both players conduct moves/actions and
> it be sent over properly and accurately to the game and other player to
> action on.

This is a statement about the **real production path** — the Orleans Host's
durable authority journal and its outward HTTP API
(`src/Cna.OrleansHost/Api/CampaignDurableAuthorityJournal.*`,
`/api/v2/campaigns/{id}/openings` and `/actions`) — not about the Exercise
Harness/Runner (`src/Cna.ExerciseRunner`, `CampaignExercises.QueryWithCombat`/
`SubmitWithCombat`), which is a separate, explicitly test-only automation lane
used for regression/determinism studies. Progress on the Exercise Harness does
**not** by itself mean real players can do the equivalent thing. Every status
claim in this doc and its follow-ups must say which of the two paths it is
about.

Explicitly out of scope for this release (content/UI, not engine):
real Graziani scenario content, Maproom/UI, model-backed/parser work,
packaging/distribution, multiplayer/remote hosting.

## Bar for "worth testing and stress testing"

Not full MVP-GATE-03 ("every mechanic and victory rule reachable... zero
silent approximation"). The bar is: the real Host path can loop both players
through all 6 turns repeatedly without dead-ending, and everything it
executes is real and deterministic — not every mechanic needs full rule
fidelity, but nothing may silently fake an answer or halt the loop.

## Required (must be real, through the real Host path, not the Exercise Harness)

| # | Item | Status as last verified in this doc |
| - | --- | --- |
| 1 | ~~Turn/stage/side sequencing, including looping Turn N → Turn N+1 through Turn 6~~ **RULED OUT OF SCOPE 2026-11-21** | **Confirmed structurally absent, not just unverified.** Core has no authority past Operation Stage 1 of Turn 1 for either side: no second-player rail/repair/patrol pass is accepted, no Operation Stage 2/3 exists, no Turn 2 initiative/convoy exists (citations: `Cna1979LandSequenceV4.IsSupportedCheckpoint`, `CampaignContactOwnerMovementCut.cs:159`, `CampaignSourceSetupBridge.cs:46-103`, `CampaignNativePostCombatPassV42.cs:31`). Building this is a large, previously-unscoped Core architecture rework (a re-entrant stage/side-block runtime), not a verification task. **Owner ruling 2026-11-21: out of scope for this release.** The required bar below is revised accordingly — see item 1' below. The 6-turn, multi-stage loop becomes a later release's goal. |
| 1' | **Revised requirement:** one full Operation Stage 1, both sides, start to finish | Replaces the struck item above. Both players must be able to fully play one complete side-block pair (Reserve, Movement, Breakdown, Combat including second-side resolution, Truck Convoy, Post-Combat Pass) through the real Host path. This is the actual release bar now — not a stepping stone to a 6-turn loop that isn't being built this release. |
| 2 | Preamble (weather/org/convoy/reserve designation) on synthetic/lab content | Done, per roadmap Sprint 3 closeout. Audit should spot-check, not re-derive from scratch. |
| 3 | Movement / ZOC / Reaction / Breakdown | Done, per roadmap Sprint 4 closeout, **and confirmed wired into the real Host API** (`CampaignOwnerOpeningKind.Movement`). |
| 4 | Combat, full cycle, first side resolving Selection → ... → Reserve Release | **Confirmed wired into the real Host API**: `CombatSelection`, `RetreatBeforeAssault`, `ForceAssignment`, `PostAssaultRetreat`, `Custody`, `ReserveRelease`, `CycleControl`, `RepeatCombatSelection`, `RepeatMovement` are all real `CampaignOwnerOpeningKind` values in `CampaignDurableAuthorityJournal.Openings.cs`, each backed by a real `CampaignHostAdapter` call and covered by Host tests including cold-replay. This predates this week's Exercise Harness work. |
| 5 | Combat, second side actually resolving an assault to a result (not just reaching Selection) | **Confirmed open** (`Submissions.cs:820-896`, in-code comments: "deliberately stops here... explicit, intentional follow-up ... for #394's next pass"). **In progress 2026-11-21.** Owner rulings: reuse existing V14-V26 event types, widen V27 Reserve Release/V28 Cycle Control to accept the second acting side (no new event types). Second side gets Cycle Control "Finish" only this release; "Repeat" is explicitly, visibly rejected (FID-002-compliant — not silently omitted from the legal set). |
| 6 | Truck Convoy Pass | **Confirmed done and wired into the real Host API** (`CampaignOwnerOpeningKind.TruckConvoyPass`, backed by `CampaignNativeTruckConvoyPassV41`, tested including cold replay in `CampaignDurableAuthorityJournalTests.cs`). This was incorrectly reported as "not started" earlier in this session by conflating it with the Exercise Harness's own lack of automated coverage for it — that was wrong and is corrected here. |
| 7 | Second-side Reserve / Movement / Movement-completion / Breakdown hand-off into second-side Combat | **Confirmed wired into the real Host API** (`SecondReserve`, `SecondMovement`, `SecondMovementCompletion`), and issue #394 (the specific gap about V46 Breakdown not handing off to second-side Combat selection) is **closed**. |
| 8 | Determinism/replay (DET-001/DET-002) holding across all of the above | Ongoing discipline, not new; must not regress. |

## Required, but a legal/explicit no-op path is acceptable (full rule fidelity not required)

| # | Item | Status |
| - | --- | --- |
| 9 | Rail | **Not started.** Confirmed absent from the real Host API's `CampaignOwnerOpeningKind` enum (checked to its end). |
| 10 | Repair (×2 per side-block) | **Not started.** Same confirmation as Rail. |
| 11 | Patrol | **Not started.** Same confirmation as Rail. |
| 12 | Supply/logistics | Partial — provisional policy tags already baked into canonical content (per 2026-11-17 capability audit); acceptable to stay minimal as long as unsupported paths reject explicitly (FID-002), never silently approximate. |

## Deferred (real gaps, acceptable to skip for this release)

- Automatic victory/winner declaration. Confirmed absent from the real Host API (no victory/termination opening kind exists). Need the scenario to **stop** cleanly at turn 6; do not need it to declare a winner.
- A meaningful (non-hash-driven) decision policy for automated testing — only relevant to the Exercise Harness, not the real player path, so doesn't block this release at all.
- Crash/power-loss recovery beyond process-termination kill-points (already deferred by the roadmap itself).
- Model/parser work, Maproom/UI, real scenario content, packaging/distribution.

## Known errors this doc corrects (keep for the record, don't delete)

- **Truck Convoy was reported "not started"** in this session's status updates. It isn't — it's done and wired into the real Host path. The error came from reading the Exercise Harness's own lack of automated test coverage for it as if it meant the engine mechanic itself didn't exist.
- Status updates throughout the preceding week answered "how are we doing" by reporting on the Exercise Harness/Runner thread in motion (Task 022–024, issue #448) without checking whether that thread was actually the gate for the owner's real goal (two real players moving through the real Host path). It often wasn't — large parts of Combat, Truck Convoy, and second-side Movement were already done in the real path before this week's Exercise Harness work started. This doc exists specifically so future status claims get checked against the real path, with citations, every time — not against whichever thread happens to be active.

## What the follow-up documentation/readiness audit must do

1. Audit every doc that claims API/engine functionality status (roadmap, RC checklist, specs, design docs, capability audits) against the actual code — cite file:line or test name for every claim, for both the real Host path and the Exercise Harness, labeling which is which. Correct anything wrong, including the Exercise-Harness-vs-real-path conflation pattern above.
2. Re-verify items 1 and 5 above specifically, since they're the two genuinely unconfirmed gaps blocking the required list.
3. Produce a release-readiness check against the required/deferred lists above, with every claim backed by a link to the actual code, test, or doc section that proves it — not a prose summary.
