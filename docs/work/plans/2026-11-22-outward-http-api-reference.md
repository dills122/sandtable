# The outward HTTP API (what the Maproom UI will actually consume)

This documents every route in
`src/Cna.OrleansHost/Api/CampaignDurableOutwardApi.cs` (the real
`/api/v2/campaigns/...` HTTP surface) and every
`CampaignOwnerOpeningKind` in
`src/Cna.OrleansHost/Api/CampaignDurableAuthorityJournal.Openings.cs`. This
is the actual production path a real player's client would use — distinct
from `Cna.Core.Exercises.CampaignExercises`/`Cna.ExerciseRunner`, the
test-automation lane documented separately in
[`2026-11-22-core-public-api-surface.md`](2026-11-22-core-public-api-surface.md).

Checked directly against source and tests in this worktree (branch
`worktree-agent-a5fbd11f77786b3ac`), same commit as `main` as of
2026-11-22: `06a2141a`. Every Stable/Experimental entry below cites a
passing test name or a file:line; nothing here is taken on a prior doc's
word. See
[`2026-11-21-initial-engine-release-scope.md`](2026-11-21-initial-engine-release-scope.md)
and
[`2026-11-21-initial-engine-release-readiness-check.md`](2026-11-21-initial-engine-release-readiness-check.md)
for the fuller release-scope narrative this reference draws on.

## Shape common to every gameplay route

All gameplay routes share one request/response envelope (not repeated per
entry below):

- `POST /api/v2/campaigns/{campaignId}/openings` — body
  `{contractVersion: 2, credential, stage, actingElementId}`. `credential`
  is a `CampaignSeatCredential` (`campaignId`, `audience`: `"axis"` or
  `"commonwealth"`, `epoch`, `secret`, `expiresUnixMs`), obtained via
  `POST /{campaignId}/seats` redeeming a grant issued by
  `POST /{campaignId}/grants/{audience}` (operator-only). Response:
  `{contractVersion, campaignId, audience, stage, actingElementId, revision,
  visibleCutBase64, publicActionRefs}` — `visibleCutBase64` is the
  mechanic's own canonical visible-cut bytes (JSON, base64-encoded);
  `publicActionRefs` are opaque HMAC references, one per legal choice, that
  must be echoed back unchanged on submission (`CampaignDurableOutwardApi.cs:717-730`).
- `POST /api/v2/campaigns/{campaignId}/actions` — body
  `{contractVersion: 2, credential, stage, actingElementId, openingRevision,
  publicActionRef}`. Response on success: `{contractVersion, accepted: true,
  openingRevision, publicActionRef}` (HTTP 200). On rejection: HTTP 409 with
  `{contractVersion, code}` — `code` is `"unavailable"` (generic — covers
  both "no such opening" and most rule violations) or
  `"rule-not-implemented"` (a source-legal choice Core has not built yet,
  surfaced explicitly rather than silently omitted — see Experimental
  section) (`CampaignDurableOutwardApi.cs:731-827,923-956`).
- Every route behind `/api/v2/campaigns` is gated to loopback + same-origin
  only, and only runs under `Development` hosting
  (`CampaignDurableOutwardApi.cs:23-48`) — this is not yet a publicly
  reachable API in any deployed sense; "real player" here means "real HTTP
  request through this code path," not "reachable over the public
  internet."
- `stage` is a fixed string key (e.g. `"movement"`, `"combat-selection"`)
  dispatched to a `CampaignOwnerOpeningKind` by
  `src/Cna.OrleansHost/Campaigns/CampaignGrain.cs:51-131`, which maps each
  stage to one `CampaignDurableAuthorityJournalStore`/`CampaignHostAdapter`
  method pair. The full `CampaignOwnerOpeningKind` enum lives at
  `CampaignDurableAuthorityJournal.Openings.cs:743-785`.

## Stable — verified reachable by a real HTTP player, cold-replay safe

| Stage / opening kind | Audience(s) | Core method (`CampaignHostAdapter`) | Citation |
| --- | --- | --- | --- |
| `movement` / `Movement` | axis, commonwealth | `QueryContactOwnerDraft` / `SubmitContactOwnerMovement` | `tests/Cna.OrleansHost.Tests/CampaignDurableAuthorityJournalTests.cs:1695` `ContactMovementReferenceCommitsOnceAndReplaysAfterRestart` — accepts, retries, cold-replays |
| `initiative-order` / `InitiativeOrder` | whichever side won initiative | `QueryInitiativeOrderOwnerDraft` / `SubmitInitiativeOrder` | `tests/Cna.OrleansHost.Tests/CampaignDurableAuthorityJournalTests.InitiativeOrder.cs:66,111,166` — HTTP accept, stale/forged/wrong-audience rejection, cold-restart survival |
| `reserve-designation` / `ReserveDesignation` | first acting side | `QueryReserveDesignationOwnerDraft` / `SubmitReserveDesignation` | `tests/Cna.OrleansHost.Tests/CampaignDurableAuthorityJournalTests.ReserveDesignation.cs:60,110,146` — same pattern as `InitiativeOrder` |
| `combat-selection` / `CombatSelection` | the side currently in Combat Selection (first or second) | `QueryContactCombatOwnerDraft` / `SubmitOwnerCombatSelection` → `CampaignHostAdapter.SubmitContactOwnerMovement`-sibling dispatch in `Submissions.cs:650-700` | First side: `tests/Cna.OrleansHost.Tests/CampaignDurableAuthorityJournalTests.cs:166` `ContactCombatOpeningCommitsOnceAndColdReplaysSelectedAuthority`. Second side: `tests/Cna.OrleansHost.Tests/CampaignDurableAuthorityJournalTests.SecondCombatSelection.cs:287` `SecondSideRealAssaultSelectionIsLegalPersistedColdReplayableAndRetried` — persists and cold-replays, but **does not itself auto-continue to a result** for the second side (see sub-caveat below) |
| `rba-decline` / `RetreatBeforeAssault` | defender | `QueryContactRbaDeclineOwnerDraft` / `SubmitOwnerRbaDecline` | `tests/Cna.OrleansHost.Tests/CampaignDurableAuthorityJournalTests.cs:1142` `DefenderRbaOpeningIsDeclineOnlyAndExplicitStopColdReplaysThroughOwnerSeat` |
| `force-assignment` / `ForceAssignment` | both sides seal independently | `QueryContactForceOwnerDraft` / `SubmitOwnerForceSeal` | `tests/Cna.OrleansHost.Tests/CampaignDurableAuthorityJournalTests.cs:304,330` `CompletedInfantryForceRoundAdvancesAntiArmorAndColdReplays`, `SourceBackedTenToeForceRoundRetainsResultAndColdReplays`. Requires a pinned `CampaignDecisionTimingConfigurationV1` on the campaign or the opening query throws (`CampaignDurableAuthorityJournal.Openings.cs:220-221`) |
| `post-assault-retreat` / `PostAssaultRetreat` | defender | `QueryContactPostAssaultRetreatOwnerDraft` / `SubmitOwnerPostAssaultRetreat` | `tests/Cna.OrleansHost.Tests/CampaignDurableAuthorityJournalTests.cs:858` `PostResultDefenderRetreatUsesSeatOpeningAndColdReplay`; second side: `CampaignDurableAuthorityJournalTests.SecondSideCombat.cs:129` `SecondSideDefenderRetreatAfterAssaultUsesSeatOpeningAndColdReplay` |
| `custody` / `Custody` | captor | `QueryContactCustodyOwnerDraft` / `SubmitOwnerCustody` | `tests/Cna.OrleansHost.Tests/CampaignDurableAuthorityJournalTests.cs:900` `CaptorCustodyUsesSideSafeOpeningAndColdReceipt`; second side: `CampaignDurableAuthorityJournalTests.SecondSideCombat.cs:181` `SecondSideCaptorCustodyUsesSideSafeOpeningAndColdReceipt` |
| `reserve-release` / `ReserveRelease` | acting side | `QueryNativeReserveReleaseOwnerDraft` / `SubmitOwnerReserveRelease` | `tests/Cna.OrleansHost.Tests/CampaignDurableAuthorityJournalTests.cs:371` `SourceBackedReserveReleaseChoiceRetainsOwnerReceiptAfterRestart` |
| `cycle-control` / `CycleControl` — **Finish choice, both sides; Repeat choice, first side only** | axis, commonwealth | `QueryNativeCycleControlOwnerDraft` / `SubmitOwnerCycleControl` → `CampaignHostAdapter.SubmitNativeCycleControl` | First side (incl. Repeat): `tests/Cna.OrleansHost.Tests/CampaignDurableAuthorityJournalTests.cs:1217-1222` (within `VoluntaryCombatFinishHasNoPrivateTargetAndRetainsReceiptAcrossRestart`) and the Repeat chain in `CampaignDurableAuthorityJournalTests.IntegratedCampaign.cs:19-181` `IntegratedCampaignDurableSaveResumeMatchesUninterruptedAuthority`. **Sub-caveat, confirmed in code:** second side's Repeat choice stays visible in the legal set but is explicitly refused — `src/Cna.Core/Campaigns/CampaignNativeCycleControlV28.cs:50-55` ("Owner ruling 2026-11-21: the second acting side's Repeat edge is source-legal but not built this release. It stays offered and is refused explicitly, never silently dropped from the legal set."), proven by `tests/Cna.OrleansHost.Tests/CampaignDurableAuthorityJournalTests.SecondSideCombat.cs:309` `SecondSideRepeatIsOfferedButRefusedAsRuleNotImplemented` |
| `truck-convoy-pass` / `TruckConvoyPass` | acting side | `QueryTruckConvoyPassOwnerDraft` / `SubmitOwnerTruckConvoyPass` | `tests/Cna.OrleansHost.Tests/CampaignDurableAuthorityJournalTests.cs:1226-1238` (within `VoluntaryCombatFinishHasNoPrivateTargetAndRetainsReceiptAcrossRestart`) — side-safety (opponent query throws), idempotent retry, cold replay |
| `post-combat-pass` / `PostCombatPass` | acting side | `QueryPostCombatPassOwnerDraft` / `SubmitOwnerPostCombatPass` | `tests/Cna.OrleansHost.Tests/CampaignDurableAuthorityJournalTests.cs:1239-1259` (same test, continues through 4 repeated passes) |
| `second-reserve` / `SecondReserve` | second acting side | `QuerySecondReserveOwnerDraft` / `SubmitOwnerSecondReserve` | `tests/Cna.OrleansHost.Tests/CampaignDurableAuthorityJournalTests.cs:1260-1282` (same test) — side-safety, idempotent retry, cold replay, cross-process verification |
| `second-movement` / `SecondMovement` | second acting side | `QuerySecondMovementOwnerDraft` / `SubmitSecondMovement` | `tests/Cna.OrleansHost.Tests/CampaignDurableAuthorityJournalTests.cs:1405-1426` (same test: probes via `CampaignHostAdapter` directly to find a real non-Reaction-blocked adjacent-enemy move, then exercises the actual `QuerySecondMovementOwnerOpening`/`SubmitOwnerSecondMovement` journal methods — the same layer the HTTP route calls); HTTP-level second-side exercise also in `CampaignDurableAuthorityJournalTests.SecondCombatSelection.cs:55-150` (`AdvanceToSecondCombatSelectionWithAdjacentEnemy` helper) |
| `second-movement-completion` / `SecondMovementCompletion` | second acting side | `QuerySecondMovementCompletionOwnerDraft` / `SubmitOwnerSecondMovementCompletion` | `tests/Cna.OrleansHost.Tests/CampaignDurableAuthorityJournalTests.cs:1290-1360` (same test) — includes a real end-to-end HTTP round trip through `MapDurableCampaignApi()` with an actual `HttpClient`, not just the in-process journal call |
| `repeat-movement` / `RepeatMovement` — **first side only** | axis, commonwealth (first acting side only) | `QueryNativeRepeatMovementOwnerDraft` / `SubmitOwnerRepeatMovement` | `tests/Cna.OrleansHost.Tests/CampaignDurableAuthorityJournalTests.IntegratedCampaign.cs:19-181`. **Confirmed in code the second side has no Repeat edge here either:** `src/Cna.Core/Hosting/CampaignHostAdapter.cs:2536-2538` throws `InvalidOperationException` when `session.CurrentSecondAssault is not null` ("The second acting side has no Repeat edge this release") |
| `repeat-combat-selection` / `RepeatCombatSelection` — **first side only, by construction** | axis, commonwealth (first acting side) | `QueryNativeRepeatCombatOwnerDraft` / `SubmitOwnerRepeatCombat` → `CampaignHostAdapter.SubmitNativeRepeatCombat` | Same `IntegratedCampaign` test — reached only via `repeat-movement`, which the second side cannot reach (see above), so this stage is not independently second-side-gated but is unreachable by the second side this release |

Determinism/replay (DET-001/DET-002) is not a separate opening kind — it is
a property every accepted step above is independently checked for in the
cited tests (`Assert.True(CampaignExercises.Reconstruct(...).IsVerified)`
appears throughout `CampaignDurableAuthorityJournalTests.cs`, e.g. lines
1210, 1238, 1258, 1282, 1310).

## Experimental / in-flux

| Stage / opening kind | Audience(s) | Core method | Caveat |
| --- | --- | --- | --- |
| `reaction` / `Reaction` | whichever side has the open window | `QueryReactionOwnerDraft` / `SubmitReaction` | **Real and HTTP-reachable, but resolves to an explicit rejection, not real resolution.** A Reaction window opened by a second-side Movement throws `CampaignHostAdapter.CampaignReactionWindowRuleNotImplementedException`, caught at `CampaignDurableAuthorityJournal.Openings.cs:527-534` / `.Submissions.cs:304-305` and surfaced by the outward API as HTTP 409 `rule-not-implemented` (`CampaignDurableOutwardApi.cs:934-935,948-949`). This is intentional, tracked design debt, not a bug: PR #502 (merged, commit `1cf5d7c5`) converted what used to be an unhandled crash into this explicit FID-002-style rejection. The real fix (#394 "Gap 3" — no existing Core method converts a second-side native result into a queryable `CampaignSnapshotV11`) is its own future design decision per `docs/work/plans/2026-11-18-combat-continuation-second-side-design-proposal.md`. Tests: `tests/Cna.OrleansHost.Tests/CampaignDurableAuthorityJournalTests.Reaction.cs:118` `ReactionOpenedBySecondSideMovementIsRejectedAsRuleNotImplementedNotUnavailable`, `:160` `ReactionOpenedBySecondSideMovementReturnsHttp409RuleNotImplementedNotACrash` (genuine HTTP round trip, not just in-process). |
| `combat-selection` (second side's own voluntary-finish / real-assault continuation past Selection) | commonwealth/axis, second acting side | `CampaignHostAdapter.SubmitNativeCombatCustody`/successor chain in `Submissions.cs:875-896` | The opening and submission themselves are Stable (see table above — a second-side selection persists and cold-replays). The gap is narrower: once a **second**-side selection is a real assault pick (not a voluntary finish), the submission is "accepted, persisted, and cold-replayable, but the campaign does not auto-advance into Barrage/Reserve Release past this point yet" (`CampaignDurableAuthorityJournal.Submissions.cs:884-896`, explicit "#394 extra-2" comment). Confirmed by `tests/Cna.OrleansHost.Tests/CampaignDurableAuthorityJournalTests.SecondCombatSelection.cs:287` `SecondSideRealAssaultSelectionIsLegalPersistedColdReplayableAndRetried`, which stops exactly at persisted selection — nothing in that test file, or in production code, drives it further. The second side's own voluntary-finish path (no real attack) *is* fully wired and Stable (`IsNativeSecondCombatVoluntaryFinish` branch, `Submissions.cs:922-943`), proven by `CampaignDurableAuthorityJournalTests.SecondSideCombat.cs:248` `SecondSideVoluntaryFinishCompletesOperationStageOne` and `:360` `BothSidesPlayACompleteOperationStageOne`. |

## `CampaignActionAudience.System` — not yet an HTTP route (known gap, in progress elsewhere)

`CampaignActionAudience.System` (`src/Cna.Core/Actions/CampaignActionAudience.cs`)
covers automatic dice-roll/resolution steps with no human decision — e.g.
`ResolveInitiativeAction`, the very first step after campaign creation.
**Confirmed in this pass: System still has no HTTP path.** The outward
API's `Audience(string)` helper
(`CampaignDurableOutwardApi.cs:892-897`) only maps `"axis"` and
`"commonwealth"` — there is no third case, so a System-audience seat or
submission cannot be represented at all over this HTTP surface as of this
commit (`06a2141a`, same as `main`). This matches the "New, foundational
finding 2026-11-22" item (#0) in
`2026-11-21-initial-engine-release-scope.md`: a separate agent, in a
different worktree, is actively building a general auto-drain mechanism so
the Host transparently resolves System-only steps whenever they would
otherwise block a real player, rather than exposing System as its own HTTP
audience. **As of this commit that fix has not merged to `main`** (`git log
origin/main` and `git log main` both show `06a2141a`/`8fcda223` as the tip,
matching this worktree exactly — no new commit has landed). This document
treats it as a known gap in progress, not as done, and does not guess at
its eventual HTTP shape.

## Omit — content-ingestion / setup plumbing, not gameplay actions

These routes exist on `CampaignDurableOutwardApi` but are operator-driven
setup/admission plumbing (require `X-Sandtable-Operator`, not a player seat
credential), not turn-by-turn gameplay actions a UI player submits. Listed
for completeness, not documented as part of the player-facing surface:

- `POST /source`, `POST /pregame-source`, `POST /pregame-source/graziani` —
  create a campaign from raw or pinned source content (operator-only,
  up to 24 MB body).
- `POST /{campaignId}/source-axis-supplement`,
  `/source-commonwealth-supplement`, `/source-priority-motorization`,
  `/source-attached-initial-loads`, `/source-initial-wrecks`,
  `/source-axis-free-setup`, `/source-axis-corps-setup`,
  `/source-maletti-hierarchy`, `/source-gt2-correction`,
  `/source-cw-priority-catalog`, `/source-compact-checkpoint`,
  `/source-fourth-indian-world`, `/source-matruh-grouped-world`,
  `/source-maletti-grouped-world`, `/source-libyan-grouped-world`,
  `/source-italian-divisions-grouped-world`,
  `/source-seventh-armored-grouped-world`,
  `/source-sixteenth-brigade-grouped-world`,
  `/source-catanzaro-grouped-world`, `/source-turn/*` (start, initiative,
  the `complete-gt1-*`/`gt1-*` family), `/source-seventh-shell-correction`,
  `/source-axis-minor-garrisons-activation`,
  `/source-axis-garrison-remainder-activation`,
  `/source-giarabub-motor-machine-gun-activation`,
  `/source-cw-frontline-activation`,
  `/source-giarabub-motor-placement-policy`,
  `/source-matruh-formation`, `/source-cw-free-guns`,
  `/source-axis-priority-motorization`, `/source-axis-minor-garrisons`,
  `/source-battlefield`, `/source-cairo-setup`, `/source-profiles`,
  `/source-cairo-world`, `/source-maletti-world`,
  `/source-independent-world`, `/source-expanded-independent-world`,
  `/source-axis-free-world`, `/source-axis-corps-world`,
  `/source-certified-maletti-world`, `/source-matruh-detached-world`,
  `/source-cw-priority-world`, `/source-cw-free-guns-world`,
  `/source-local-map` — each admits one named slice of the Graziani
  scenario's canonical source content into the campaign during setup
  (`CampaignDurableOutwardApi.cs:131-562`). These are scenario-content
  admission steps, not player decisions.
- `GET /{campaignId}/source-initialization` — read-only status of
  source-based initialization.
- `POST /{campaignId}/source-counter-contacts` — a player-credentialed
  observation query (not an action submission) used during the source-setup
  window; included here for completeness since it sits structurally between
  setup and gameplay, but it is a read, not a turn action.
- The `pregame-axis-supply`/`pregame-axis-supply-finish` openings and the
  `source-*` openings reachable via `/openings`/`/actions` with those
  `stage` values (`CampaignOwnerOpeningKind.PregameAxisSupply` through
  `SourceCairoSetup`, 22 enum values in
  `CampaignDurableAuthorityJournal.Openings.cs:762-784`) are the player-seat
  side of the same pregame/source-setup process (e.g. the Axis player
  confirming a supply draft during pregame). They use the same owner-opening
  mechanics as gameplay stages but belong to setup, not to the repeatable
  turn loop a UI would drive after the campaign is underway, so they are
  noted here rather than in the Stable/Experimental tables above.

## Summary

- **Stable (18 opening kinds / stages):** `Movement`, `InitiativeOrder`,
  `ReserveDesignation`, `CombatSelection`, `RetreatBeforeAssault`,
  `ForceAssignment`, `PostAssaultRetreat`, `Custody`, `ReserveRelease`,
  `CycleControl` (with the second-side-Repeat sub-caveat), `TruckConvoyPass`,
  `PostCombatPass`, `SecondReserve`, `SecondMovement`,
  `SecondMovementCompletion`, `RepeatMovement` (first side only),
  `RepeatCombatSelection` (first side only, unreachable by second side this
  release).
- **Experimental (1 opening kind, 2 caveats):** `Reaction` (explicit
  `rule-not-implemented` rejection, not real resolution — #394 Gap 3, future
  design decision) and second-side `CombatSelection`'s real-assault
  continuation past Selection (persists, but no auto-advance to
  Barrage/Reserve Release yet — #394 extra-2).
- **Not yet a route:** `CampaignActionAudience.System` — confirmed absent
  from the outward API as of commit `06a2141a`; a fix is reported in
  progress in a different worktree per the scope doc, not yet merged to
  `main` as of this check.
- **Omit (content-ingestion/setup plumbing):** ~50 `source-*`/`pregame-*`
  routes and their matching owner-opening kinds, all operator- or
  setup-driven, not part of the repeatable gameplay loop.

## What this doc does not cover

It does not re-verify item 1 (turn-to-turn looping across a `GameTurn`
boundary) or item 5 (second-side Combat resolving past Selection to a
result) from the frozen scope doc beyond what's captured above — those
remain Unverified/Open per
`2026-11-21-initial-engine-release-readiness-check.md`, and nothing found
in this pass changes that. It also does not cover the Exercise Harness's
own HTTP surface, because the Harness has none — see
[`2026-11-22-core-public-api-surface.md`](2026-11-22-core-public-api-surface.md).
