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
| 2 | Preamble (weather/org/convoy/reserve designation) on synthetic/lab content | **Partially wrong — corrected 2026-11-21.** The three System-audience preamble steps (initiative resolution, no-obligation naval convoy scheduling/tactical shipping) auto-resolve and are real. But the very next step — the initiative holder's real owner choice between `ActFirstAction`/`ActLastAction` — has **no path anywhere in production code**: no `CampaignOwnerOpeningKind` entry, no Journal method, no HTTP stage. See item 2' below. This is release-blocking: it's the first real decision in any campaign, upstream of Movement/Combat/Reaction/Breakdown for both sides. |
| 2' | ActFirst/ActLast reachable through the real Host path | **Done, merged 2026-11-21 (PR #497, commit 31f2dde9).** New `CampaignOwnerOpeningKind.InitiativeOrder`, Journal query/submit methods, `"initiative-order"` HTTP stage. No new Core logic — wraps the existing `CampaignExercises.Query`/`Submit` decision. Full rigor delivered: HTTP end-to-end test, side-safety, stale/forged/wrong-audience rejection, idempotent retry, cold-replay. Two real DET-001/002 bugs found and fixed while wiring replay (`CampaignHostAdapter.AdmitAcceptedStepsStream` and the outward API's `/actions` route each had their own separate, incomplete step-kind allowlist copy — caught by `Reconstruct(...).IsVerified` throwing, not assumed fixed). |
| 2'' | Reserve Designation reachable through the real Host path | **Done, merged 2026-11-21 (PR #499, commit b8b301db).** Same pattern as 2'. |
| 3 | Movement / ZOC / Reaction / Breakdown | **Partially wrong — corrected 2026-11-21, triage complete (not assumed).** Ordinary Movement (once past the preamble gate) and the trivial/no-decision Breakdown and no-Reaction paths are genuinely done and wired into the real Host API (`CampaignOwnerOpeningKind.Movement`), verified by running `ContactMovementReferenceCommitsOnceAndReplaysAfterRestart` and others. **Real vehicle-Breakdown resolution is confirmed NOT reachable** — driving `commonwealth-truck` (a vehicle cohort already present in the standard fixture) through Second Movement throws "No owner opening is available at this cut" from an earlier, explicit, fail-closed eligibility check (`CampaignContactOwnerMovementCut.ProjectSecondMovement`) before the vehicle-cohort code path in `CampaignNativeSecondBreakdownV46` could ever be reached. **Ruled out of scope, same disposition as the ZOC topology-feature gap** — confirmed by driving the actual fixture, not inferred. **Reaction window resolution is confirmed reachable today** (same evidence as above) and **release-blocking, in progress — see item 3' below.** |
| 3' | **New finding 2026-11-21:** Reaction's natural implementation path (reusing `CampaignExercises.Query`/`Submit`, same as items 2'/2'') is blocked by an open, acknowledged design question, not a simple gap. The generic query/submit gate rejects once `session.CurrentCombatDecision is not null` — true for every real Reaction window, since Reaction only occurs after Combat has already resolved earlier in the round. This is documented pre-existing as **issue #394 "Gap 1"** in `docs/work/plans/2026-11-18-combat-continuation-second-side-design-proposal.md` (status: proposal, not a decision). PR #400 already looked at this exact field and **explicitly declined to fix it**, calling it "almost certainly load-bearing for the determinism/replay guarantees this project cares about" and scoping two options (give the field real reset semantics, or split into ~70-call-site side-scoped fields) that both need an explicit design decision first. The existing `HasAdvancedCombatSelection` helper (built for two other, narrower call sites) was tried and empirically confirmed **not sufficient** for Reaction's actual trigger point either — tested, not assumed. **Owner ruling 2026-11-21: build Reaction via a dedicated non-generic Core wrapper, sidestepping Gap 1 entirely — the same pattern Combat mechanics already use for their own special-cased paths.** Do not touch the shared `CurrentCombatDecision` gate or attempt #394 Gap 1 remediation as part of this release; that stays its own tracked future design decision. **Gap 1 sidestep built and confirmed working** (new `CampaignExercises.QueryReactionWindow`/`SubmitReactionWindow`, no gate, confirmed via probe that the position advances correctly). **Blocked on a second, deeper gap — see item 3'' below.** |
| 3'' | **New finding 2026-11-21:** Reaction also hits **#394 "Gap 3"** (same design doc), independent of Gap 1. `ExerciseSession.WithSecondMove` and every other V41-V46 second-side method never reassigns `CurrentSnapshot` — it stays frozen at the first-acting-side's old Combat position. The live reaction-window data exists only inside the native second-side result objects, not in any `CampaignSnapshotV11`-backed session `CampaignLegalActions.Query` can read. The doc itself calls this "the gap with the least existing precedent to build on and the highest remaining uncertainty," states no existing method converts a `CampaignWorldSnapshotV6` + V46 position into a new `CampaignSnapshotV11`, and explicitly declines to pick an approach. **Owner ruling 2026-11-21: do not attempt Gap 3 remediation this release.** Instead, convert the current unhandled `InvalidOperationException` ("Reaction is unavailable") into an explicit, FID-002-compliant rejection — same pattern as second-side Repeat's `rule-not-implemented` response — so a real player gets a clear "not supported yet" answer instead of a crash. The real Gap 3 fix (a new Core projection method, or a from-scratch native-result-based Reaction deriver) becomes its own tracked future design decision, same treatment as Gap 1. **Done, merged 2026-11-22 (PR #502, commit 1cf5d7c5).** New `CampaignHostAdapter.CampaignReactionWindowRuleNotImplementedException`, distinguished from "no window at all" by checking `session.CurrentSecondMove?.ReactionWindow is not null`. A second issue found and fixed along the way: a Reaction submission could never reach the rejection catch at all, because the opening-revision lookup it depends on is never persisted for a kind that can't be queried either — fixed with an early Reaction-specific check, same precedent as the existing `ForceAssignment` early-check. Both `/openings` and `/actions` HTTP routes return 409 `rule-not-implemented`, verified end-to-end over real HTTP, cold-replay safe. **Every Required item (1'-8) is now either Done or explicitly, honestly out of scope. The one thing not yet proven is a single end-to-end test driving the entire chain as a real HTTP player from campaign creation through Operation Stage 1's end for both sides — every piece so far has been verified individually, not as one continuous run.** |
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
