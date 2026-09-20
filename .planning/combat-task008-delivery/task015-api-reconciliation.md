# Task015 reconciliation against frozen014 — research only

014 frozen candidate under root gate/reviews, not accepted. Read existing task015-dispatch-proposal.md and frozen014 engine/codec/helper/Obligations/tests. This file only edited. No source/test changes, .NET, oracle/tests, agents, reviews or commits. No015 authorization. Root decides explicit path scope after014 acceptance.

## Recommended scope and layout

Retain one custody slice. Extend existing CampaignCombatLossRetreat.cs, currently139lines, with clearly named Custody receipt/projection methods and a private shared content-route helper. This adds an expected roughly80–140lines of cohesive typed projection to a small file; no new module or broad rename necessary. Resolution engine currently329lines already owns generic window/input/retry flow. Extend its existing window-kind branch to custody instead of duplicating an entire second timing engine.

Five material paths remain reasonable:

1. src/Cna.Core/Campaigns/CampaignCombatResolution.cs
2. src/Cna.Core/Campaigns/CampaignCombatResolutionCodec.cs
3. src/Cna.Core/Campaigns/CampaignCombatObligations.cs
4. src/Cna.Core/Campaigns/CampaignCombatLossRetreat.cs
5. New tests/Cna.Core.Tests/Campaigns/CombatCustodyTests.cs

Actual physical scope also needs narrow maintenance of TWO existing tests:

6. tests/Cna.Core.Tests/Campaigns/CombatLossRetreatTests.cs
7. tests/Cna.Core.Tests/Campaigns/CombatResolutionTests.cs

This is seven physical files, five material components plus two compatibility assertion edits. Do not claim five physical paths or weaken tests to avoid acknowledging scope. Root should explicitly dispatch allseven. New separate CampaignCombatCustody.cs is not recommended now: existing Project still needs integration edit, so it adds another material component/path without clear current size benefit. If implementation grows beyond bounded helpers, flag that to root rather than compressing code or silently expanding scope.

No schema/Identity/Round/World7/receipt-model/fixturelink changes expected. All exact014API names below remain subject to acceptance corrections.

## Existing authority and extension points

ReplayTrustedBoundary/ApplyTrustedBoundary signatures already carry separately trusted C3a/Round/Result input/event tuples. Keep unchanged; no caller retreat state or paid JSON authority. CombatResolutionContext now has get-only Committed, CommittedHash, Creation and owned paid WorldBytes. Creation supplies validated original Config1 and Content7 through Setup. No new context parameter or cache required.

CombatResolutionState already has immutable CombatResolutionWindow?, AcceptedHighWater, World/Result/RandomState/head and copied Receipts. CombatResolutionWindow(DecisionId,Kind,Owner,CombatStepsTiming) supports custody without a new model. Add only typed CombatResolutionEffect.Custody(Reason,Timing?,CampaignCombatCustodyReceipt Payload) beside current Resolve/Open/Disposition/Loss/Retreat arms; codec schema already recognizes CustodyEffect. No new wire version or local descriptor inventory.

Current Transition hardcodes retreat at three points: waiting-window Kind requirement/choice allowlist/fallback, actual opening predicate and window budget/owner, and RecordDisposition fold. Refine those narrowly around frozen two-kind vocabulary. Required custody opening occurs only status retreat with positive pending lot and null existing window; owner is lot.Captor.OriginalSide, not fixed defender. Valid independent time opens Config1 custody budget; local highwater=opening while audit max stays monotone. Missing/unavailable/overflow opens no window and directly leaves unguarded. Same generic choose/expire/unavailable ordering and original actor receipt must remain. Once custody is recorded, all advance/relationships/closure still reject016. Noncapture retreat must NOT open empty custody or synthesize custody receipt.

014's RecordDisposition is a local function; custody fold can be a separate small local function or bounded switch without changing either effect's semantics. Preserve context/actor/field checks before fallback, retry before staleclock/status, local exclusive deadline, System fallback author with separately retained original command actor, and stale/no-window NoOp.

## Exact serializer-stage work required

WorldValue now explicitly checks:

- committed: no result/settlement;
- resolved/waiting-retreat: pending settlement without disposition/loss/retreat;
- disposition/losses/retreat: matching retained receipt stage;
- waiting-retreat iff Window nonnull.

This guard fixed real014 writer mismatch and must remain, with precise extension:

- retreat and waiting-custody require disposition+losses+retreat, Custody=null, Relationships=null;
- custody requires those plus Custody nonnull, Relationships=null;
- only waiting-retreat/waiting-custody carry windows, with matching window.Kind and correct lifecycle stage; terminal/intermediate statuses cannot borrow earlier Worlds.

Current stages should explicitly continue excluding later custody/relationship receipts. Full typedWorld equality against allowed projection remains mandatory; never remove it to make015 World serialize. WorldValue currently maps elementTOE/CP/Cohesion/location, representations, causes, lots and settlement preloss/disposition/loss/retreat. Extend settlement custody field and typed guards/replacementEntitlements/futureObligations only. Original paid root must still supply preLossElements BEFORE current element mutations. Context paidbytes/committedhash never rewritten.

Guard OperationalState/Readiness/Ammunition include ContentOrigin enums, so blindly JsonSerializer-ing typed guard objects may not match frozen Origin kind text. Use existing owned canonical donor element subtrees to construct guard operational/ammunition/readiness fields, after typed equality proves these match actual donor provenance, or explicit narrow typed writers for those existing fields. Existing WorldValue deliberately maps supported differences into authenticated canonical paid root; maintain that principle. No arbitrary caller JSON accepted as authority.

## Hashed settlement evolution

014 introduced ResultV2SettlementId(commitmentId,resultId) and now checks derivedsetID in EVERY private Result2 constructor path. Existing WithResultV2Disposition/Losses/Retreat enforce precise append stage; ResultV2Next currently forwards custody/relationships null.

Add WithResultV2Custody(nextReceipt): require Disposition/Losses/Retreat present, Custody/Relationships absent, positive capturedTOE, exact existing hashed occurrence. Preserve all original preloss/result/scope/participants. Extend private ResultV2Next or call same private constructor with new custody and nullrelationships; identity validation remains unconditional. No public skipflag, legacy widening, independently caller-chosen settlement ID or eager relationship receipt. Existing common constructor checks custody chain afterretreat and captured-presence consistency; reuse it. Legacy constructor must retain sample IDs and oldtests.

## Typed projection integration

Current CampaignCombatLossRetreat.Project rebuilds expected settlement throughretreat from authenticated committed paidWorld/result, compares expected==retained, then materializes losses/pendinglot/actualretreat. To support custody safely, do not compare incomplete expected to full retained before deriving custody and then bypass equality. Instead:

1. Derive same canonical expected prefix throughretreat and typed post-retreat World from original paid facts.
2. If retained Custody exists, derive canonical custody receipt from its closed kind plus actual post-retreat lot/captor/victim/content. Append through validated hashed method; derive guard/escape assets from that receipt.
3. Require complete expected settlement equals retained and final typedWorld matches serializer state.World. Unexpected early/missing/changed receipts fail. Temporary typed pre-custody World is local derived data, not another authority/cache.

No need rerun dice, payments or original admission against depleted World. Preserve existing helper as one projection entry point usable by engine and writer; independent literals/hashcuts remain test oracle.

## Geometry, resources and calendar fit

Reuse authenticated Creation.Setup.Artifact.Definition and Setup.Scenario. Current RetreatRoute has local BFS Path; extract private content-path routine inside same helper for retreat and custody, preserving unique selected-line behavior. Guard path origin→post-retreat captor <=3edges and excludes surviving victim in suffix. Escape origin→surviving victim tests actual NormalClear foot cost through Cna1979Movement.LookupTerrain; integer2CP peredge means<=8CP corresponds<=4edges. Existing CampaignCombatCustodyReceipt bound4 for escape is consistent, despite frozen world helper's loose<=8edge prefilter. No validator widening or new terrainprofile needed.

Guard donor is CURRENT post-retreat captor element, not original preloss element. Transfer exactly1TOE, ensure positive donor remainder and1:5 custody capacity; inherit donor CP/Cohesion/breakdown/movement/ammo0/origin/readiness. Fixed guard1TOE/CPA10/CA0/1 already enforced by CampaignCombatGuardAsset. Lot keeps victim original component/origin, changes to guarded at donorlocation; no new donor CP. World7 checks exact donor before/after, guard provenance/resources and lot/obligation linkage (ValidateCustodyAssets around275–305).

Escape donorTOE unchanged; no guard; lot escaped/currentLocation null. Preserve captured quantity/originalcomponent. Derive eligible ordinal `(earned.GameTurn-1)*3+(earned.OperationStage-1)+12`, then turn=ordinal/3+1, stage=ordinal%3+1 with CampaignCombatScope(...,future:true). Existing entitlement validates exact12stage difference, quantity1..3 and awaiting-eligibility-and-training. Earned supported turn<=111 yields future<=115. No immediate TOE restoration or maturity/training/feeding execution.

Retain exact guard-priority-upkeep or replacement-training-gate obligation with frozen IDs/activation/status. World7 already validates scopes/subject/receipt/assets and open-settlement effects. Typed constructors suffice; no source receipt/World7 edits needed on current evidence. Serializer must retain these future obligations without calling them satisfied.

## Narrow existing-test maintenance

CombatLossRetreatTests.AllRetreatPrefixesMatch144LiteralEventsAnd176StateHashCuts keeps End at retreat and ALL144/176 assertions, including nullcustody at that cut. Its current loop at~28 rejects every input afterretreat, including newly legal custody opening: move rejection start to first relationships-settled index and preserve test authority prefix. New015test must reject016 from actual custody/noncapture-retreat terminal prefix, so rejection is not merely staleversion evidence.

CombatResolutionTests.DiscardRetryIndependentInputsAndLaterFamiliesCannotRerollOrSettle at~115 similarly starts later-family rejects afterretreat. Advance cutoff to first relationships-settled; keep alloriginal32/64/draw/overflow/provenance/ownership evidence. No need widen its original stage assertions or rewrite fixture helpers. These are routine assertion maintenance, but both paths must be owned explicitly before edits.

## Counts, tests and limits

Inventory unchanged from015proposal:32 contexts,176literal events/208stateHASHcuts cumulative.014144/176 +16custodywindows+16custodyevents;8guard/8escape across bothcaptorroles/sealorders. Remaining96events/96cuts (relationships/roundclose/CA) stay016. Literalguardroutes4oneedge+4twoedge;8escape routeszeroedge. No claim of maximal3edge/8CP coverage fromthose literals.

FirstRED positive pendinglot cannotopen custody. Newgoldens all176/208 with everycutstrictreadback and continuation. Newclockproof exact200same-owner paired comparisons from frozenv2 oracle: retreat accepted10001/11000, custodyopened10500 despiteaudit11000, choice10600 accepted;7openingpairs and9choice times×2confidence across2sides×2sealorders. Additionalwrongowner-beforefallback, earlyexpiry, deadlineequality, retry/discard andassetownership tests.

Verify donor+guardTOE conservation, no repeattransfer, currentledgerinheritance including CP11, lotorigin/quantity, escape noTOEreunion, calendar/obligationmatching. Rehashed forgedroute/resource reset/guard orentitlement linkage/omittedfutureobligation/receiptchain; stagewindow mismatches beforeprojection; malformedraw beforecontext. Existing009B/World/rulesproof covers broad support/caps without admitting unsupported geometry. No neworacle run needed for this research.

No real contract blocker found. Honest dispatch scope is five material plus TWO prior-test maintenance paths; recommended existinghelper extension avoids an unnecessary extra component.014acceptance androotexplicitpathdecision remain gates. No015implementation, actualpositivehistory, Snapshot/publicactivation/hosttransaction or016behavior authorized here.
