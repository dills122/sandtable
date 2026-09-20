# Task014 API reconciliation — research only

Frozen013 worker candidate under root fullgate/review;013 not yet accepted. This file alone edited. No source/test changes, .NET, oracle execution, agents, commits or review reads. Original task014-dispatch-proposal.md governs gameplay/counts; this reconciliation replaces its speculative API assumptions with inspected source.

## Decision and bounded paths

One014 slice remains feasible, with five material paths recommended for readable separation:

1. CampaignCombatResolution.cs: existing immutable state/context and private transition; retreat timing, disposition and structural settlement effects.
2. CampaignCombatResolutionCodec.cs: new typed effect/state writers and bounded World mapping, preserving existing full frozen grammar.
3. CampaignCombatObligations.cs: safe immutable hashed-identity settlement evolution through retreat.
4. New CampaignCombatLossRetreat.cs: internal typed disposition/loss/retreat World projection and certified geometry; no public authority API.
5. New CombatLossRetreatTests.cs:32 cumulative contexts/all cuts, clock/ownership/forgery and conservation integration proof.

All paths under existing src/Cna.Core/Campaigns or tests/Cna.Core.Tests/Campaigns. Four files possible by keeping projection in Resolution.cs; fifth helper is preferable to enlarging one transition module. No changes to Identity codec, fixture link, World7, Round2, RNG, rules or shared receipt models expected. No child split currently justified by an API obstacle. Exact accepted013 source must be rechecked after review corrections.

## Existing APIs and required state extension

CampaignCombatResolution.ReplayTrustedBoundary/ApplyTrustedBoundary already take request, retained Created, CombatStepsBoundary, separate predecessorInputs/events, roundInputs/events, separate CombatResolutionInput inputs and canonical result events; Apply appends fresh input. Keep signatures unchanged. No caller state/World authority or direct public loss/retreat transition. Private Transition currently rejects everything except resolve after retry/no-window callbacks; extend this finite switch in place.

CombatResolutionInput already carries Command, separately trusted Actor/AdmittedAt/ClockAvailable. Command already includes resolve/advance/choose/expire/unavailable vocabulary plus expected version, decisionId and choice; no new command contract needed. Grammar already includes all future effect arms, Timing, World receipt types. Recognition is syntax only;014 must still reject custody/relationship/close transitions.

CombatResolutionState currently stores get-only Context, init World/RandomState/Result/head/status and copied Receipts. It does NOT yet hold a window or mutable result audit time. Codec currently hardcodes window=null and acceptedHighWater=committed.Timing.OpeningFloorUnixMilliseconds.014 must add typed immutable nullable retreat choice window and init AcceptedHighWater seeded in state constructor, then serialize them. Reuse existing CombatStepsTiming (ContractVersion,ConfigHash,Kind,budget,openedAt,deadline,highWater) rather than Round ClockTiming. Existing CombatStepsWindow lacks explicit Kind; a small ResolutionWindow with Kind/DecisionId/owner/Timing is appropriate. Existing C3a private ClockGate/OpenWindow helpers have different policy and cannot be called or copied blindly: Result2 independent mandatory opening must ignore prior audit maximum.

CombatResolutionContext currently contains only get-only Committed, CommittedHash and private owned PAID WorldBytes. Preserve that coupling. Add immutable trusted configuration/content references needed by mandatory opening/route proof only after existing full Created/C3a/Round authentication. Request.Context.Configuration is available, already copied/validated by CampaignCombatCreationContext; request.Context.Setup.Artifact.Definition and Setup.Scenario provide certified geometry. RoundBase.Configuration is supplemental assignment-clock configuration, not Config1 retreat budgets. Use original Config1 Windows[kind=retreat] and config.Hash. Do not infer retreat budget from assignment budget or introduce caller config parameter. Context construction may take validated request.Context alongside committed; no new file or public API needed.

## Factory evolution: actual safety constraint

Current CreateResolvedResultV2 validates cmt.<64lowerhex>/res.<64lowerhex>, derives set.<64lowerhex> from canonical ordered commitmentId/resultId and calls private constructor with resolvedV2=true and all consequence receipts null. Public constructor forwards false and preserves old settlementId+'.commit'/'.result' rule. The private true branch currently skips only legacy ID comparison; it relies on the sole resolved factory for hash validation.

Do not reuse that private true branch from new append methods without strengthening its invariant. Recommended bounded refactor within Obligations.cs:

- Extract private Result2SettlementId(commitmentId,resultId) that validates exact formats and derives hash.
- For private Result2 mode, always require supplied settlementId equals that derivation. For legacy mode retain exact old comparison. No exposed skip-validation bool and no caller-supplied new occurrence IDs.
- Add internal immutable append methods named WithResultV2Disposition, WithResultV2Losses, WithResultV2Retreat (or equivalent typed factory) taking only next receipt. Each proves current stage: no prior corresponding receipt, required predecessors present, later receipts null, hashed identity valid. Retain existing scope/original participants/preloss elements/result and invoke complete common constructor checks. Legacy objects cannot acquire hashed append mode accidentally.
- Keep CreateResolvedResultV2 restricted to null consequence receipts. No custody/relationships extension yet. Do not rename wire fields or loosen constructor validations to satisfy literal bytes.

Common constructor already checks exact receipt chain, original role binding, paid preloss scope/ammo/CP/TOE, refusal distance, capture shares and defender before-retreat CP. Existing receipt models enforce selected rounding, conservation and 30%-of10 loss DP. World7 additionally checks current elements match settlement effects. Reuse these checks; engine remains sole proof of authorized causal transition.

## Current WorldValue restriction and safe extension

Codec WorldValue currently recreates a resolved-only settlement from state.Result and Context.Committed.World, constructs paidWorld+singleton pending settlement, requires full typed equality with state.World, then maps canonical paid root to pending settlement JSON. This intentionally rejects ANY new disposition/loss/retreat World. It must be extended deliberately; dropping full equality or merely serializing arbitrary state.World is not acceptable.

Recommended projection helper takes immutable authenticated committed context/result plus typed, validated settlement receipts and derives expected World through exact supported cut. Private transition uses it to create candidate; serializer uses same allowed typed projection to reject stale/mismatched World. This is normalization/projection, not an independent test oracle: literal event/state hashes provide independent expectations. Keep full equality and only map supported typed fields into owned canonical paid root: element component TOE/operational/location, representation location, causes, pending captive lots and settlement receipts. All unrelated fields preserved. No current-world JSON cache paired with record-with mutable state.

WorldBytes remains original PAID root for every later projection. Do not rewrite Context.Committed/CommittedHash/WorldBytes as settlement advances. PreLossElements remain original paid snapshots through retreat; current damaged elements cannot replace them. Result/RandomState remain unchanged; no recomputing dice or rerunning Resolve at disposition.

## Exact World and timing reuse

Existing CampaignCombatCertification.ProveSupport uses request.Context.Setup.Artifact.Definition graph and side supply anchors. Its local BFS Path is private;014 can implement small bounded pure route lookup inside new projection helper using same certified content, without modifying certification or copying fixture geometry. Route choice must derive from actual trusted content/participants and selected result; tests pin both sides and invalid/forged routes.

CampaignCombatSpending.ChargeMandatoryRetreat already supports unbounded mandatory spend with incremental excess-overCPA DP; call only for actual one-hex retreat, after joint loss causes. It receives explicit causeId and prior causes, preserving ordinals. Existing CampaignCombatCohesionCause validates lossDP and capped victoryRP arithmetic. Existing typed loss/retreat receipts validate role rounding/conservation/CP. No broader rules implementation required.

A pending custody lot is mandatory in losses when capturedTOE>0, preserving original component/captor/preloss victim location and pending status; this is014 allocation, not015 custody disposition. Defender retreat updates both element and representation position without moving that pending lot. Victory RP only after actual evacuation, no refund/cancel or RP for refusal/not-required.

## Cumulative proof and boundary

Reconfirmed prior JSON-only inventory:32 contexts;32 resolved events +16 retreat windows +32 dispositions +32 joint losses +32 actual-retreat receipts =144 literal events,176 initial/prefix state HASH cuts.013 contributes32/64;014 adds112/112. No intermediate literal JSON state claim. All32 contexts must replay actual C3a/Round/Result predecessor tuples at each cut. Prefix ends status retreat; every015/016 continuation still rejects. Zero-capture branches also stop before relationships.

Optional refinement only if implementation complexity warrants:014A clock+intent, cumulative80events/112hashcuts;014B adds64loss/retreat events/cuts, reaching144/176. Both must have independent causal replay/readback proof; parent014 remains open untilB completes full conservation/cost/timer ACs. No child claims Snapshot persistence or actual positive-history admission.

Tests map directly to extension boundaries: exact whole events/hashcuts; independent-opening and local-window clock matrix; owner/shape/context before fallback; accepted intent immune to stale timer; retries no second loss/capture/CP/RP; zero-loss retreat and refusal loss threshold; CP10→11/DP1; pending captive conservation both roles; cappedRP via existing verified model/rule proof where unreachable selected start prevents runtime extreme; unrelated World mutation rejected, hashed append identity/receipt chain invalid cases, original factory/legacy constructor regression. Keep body and event buffers owned and malformed syntax before trusted context. Shared013/012/World/Identity/Rules regressions followed by root gates/reviews.

No real architecture blocker found. Config1/context and serializer restrictions require explicit bounded work, not a new authority model. Full retreat→custody same-owner clock isolation belongs015;014 proves first mandatory opening independently. Root finalizes dispatch only after013 accepted. Actual positive campaign history, public actions, Snapshot successor and host publication remain out of scope.
