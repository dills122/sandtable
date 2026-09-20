# Task014 dependency and scope proposal — research only

Written while012 full gate continues.013 planned, not implemented/accepted; neither013 nor014 authorized by this document. Read canonical task row, Result2 spec/schema-facing oracle and literal fixture, existing settlement constructors/spending helpers, planned013 factory/profile. Only this proposal edited. No .NET, oracle execution, production/tests, agents, commits or prior reviews. Fixture counts below use JSON inspection only; one initial inspection used incorrect events key then corrected to resultEventCanonicalUtf8, with no fixture edits.

## Recommendation

One bounded014 implementation slice is feasible after013 acceptance: extend existing Resolution engine/codec through retreat disposition, joint losses/capture allocation and actual retreat. Four material files normally suffice: CampaignCombatResolution.cs, CampaignCombatResolutionCodec.cs, CampaignCombatObligations.cs, new CombatLossRetreatTests.cs. If readability merits an internal focused settlement projection helper, new CampaignCombatLossRetreat.cs is fifth material path. No requirement to compress code into one file. Existing013 fixture link and Result2 grammar profile already supply needed transitive types/Route syntax. No new schema, fixture link, World7 constructor or RNG/rules changes expected.

This is a cohesive finite pipeline sharing one authenticated context, settlement receipt chain and World projection. Splitting is not required solely because it includes a timer: only retreat choice is implemented, with custody/relationships/closure explicitly outside014. Parent canonical dependency remains013→014→015→016. Do not turn research into a new broad spike.

Canonical scope: docs/design/combat-cycle-implementation-plan.md checkpointG Task014 (around907): retreat/refusal intent, simultaneous loss/capture allocation, subsequent actual retreat CP/DP/RP; conservation and victory RP only after evacuation. Active byte/clock authority: docs/specs/combat-result-settlement-v2.md sections Mandatory-window timing correction, Result/World7/closure and Canonical records; verify-combat-result-settlement-v2.py:173–248. World arithmetic: verify-combat-world-settlement-v1.py:160–208. Historical003B supplies arithmetic, not a replacement v1 event envelope.

## Exact literal inventory and stopping boundary

Read all32 traces in docs/specs/fixtures/combat-result-settlement-v2.json. Prefix through first retreat-settled contains:

| Retained effect | Count |
| --- | ---: |
| assault-resolved (013) |32|
| choice-opened with window.kind retreat |16|
| disposition-recorded |32|
| losses-settled |32|
| retreat-settled |32|

Cumulative014 prefix:144 literal events and176 state HASH cuts including each initial state. New014 contribution beyond01332events/64cuts:112events/112newcuts. No intermediate literal State JSON claim: fixture stores stateHashes and final downstream stateCanonicalUtf8. All8branches×2sides×2seal orders retained. Non-retreat prefixes length4; required-retreat prefixes length5. Required retreat branches: zero-retreat, refusal-loss-dp, attacker-capture-guard-cp-limit, attacker-capture-escape. Other four branches record not-required without window.

If root elects children:014A ends at disposition, cumulative80events/112hashcuts (new48/48 beyond013);014B adds64loss/retreat events/cuts, reaching144/176. ChildA depends013; childB dependsaccepted014A; parent014 closes only afterboth cumulative tests and complete acceptance. Each child replays actual separately trusted synthetic predecessor through012 and013 at every cut; no fabricated intermediate authority or new snapshot claim. A owns clock/disposition review, B owns joint allocation/World conservation/retreat review. This is fallback refinement for implementation size, not current recommendation or authorization.

Stop status retreat with pending custody where captured, no custody choice, guard/escape settlement, relationships or closure. Remaining128events/128newcuts of full272/304 remain015–016 (includes16custody openings,16custody settlements,32relationships,32round closures,32CA completions). A pending captive lot CREATED by simultaneous loss allocation is required014 behavior; choosing its custody is015. Do not omit lot creation to avoid later task scope.

## Authentication and transition matrix

Reuse accepted013 full trusted tuple and private replay transition: Created captured before caller-list access; separately trusted C3a/Round/Result inputs plus canonical events; immutable committed authority and original paid preloss snapshots. Do not introduce Apply taking arbitrary resolved/disposition state. Raw canonical parser precedes trusted context, then complete event regeneration/whole-state comparison. Result2 local history bound32 and1MiB/depth32/arrays512 remain unchanged.

- resolved + advance with requiredRetreat0: null time/available only, disposition not-required, planned0/unfulfilled0, route[origin], null timing, one event.
- resolved + advance with requiredRetreat1: open defender-owned retreat window at independently supplied valid instant, fixed Config1 budget and checked exclusive deadline. Local highwater=opening. Prior acceptedHighWater or private seal times cannot block opening. Update audit maximum separately. Missing/unavailable time or unrepresentable deadline immediately records refusal with null timing and opening-clock-unavailable reason.
- waiting-retreat + choose: validate owner, exact choice retreat/refuse-retreat, decision/policy/context first. Available time at/above local highwater and below deadline admits intent; equality/afterdeadline rejects. Missing/unavailable/belowlocaltime yields System-authored refusal. Explicit unavailability uses scripted refusal. Expire before deadline with valid clock no-ops; expired/lost clock records fallback.
- disposition + advance: structural null time/available, settle both role losses together from original preloss sides and retained intent; never compute defender from already damaged attacker or reroll.
- losses + advance: structural null time/available, apply actual paid retreat and DP/RP once. Refused/not-required route has no movement/CP/victoryRP.
- retreat + advance: reject pending015/016 regardless capture presence. Later fixture events cannot be accepted prematurely.

Command+actor exact retry precedes stale version/clock/status; primitive/context checks precede retry. Accepted intent cannot be overridden by later timer; no-window stale callbacks no-op. Deadline never renews. Window IDs settlementId+'.choice.retreat', owner original defender. Audited acceptedHighWater monotone but never gates opening. Foreign/malformed choice cannot trigger fallback. No attack cancellation, refund, extra RNG or changed result table column.

## Typed settlement and World compatibility

Planned013 CreateResolvedResultV2 derives hashed occurrence and accepts no consequence receipts.014 must EXTEND via named immutable methods/factory preserving that identity provenance, e.g. append disposition, append joint losses, append retreat. Do not reconstruct through legacy constructor, whose commitment/result IDs must remain settlementId+'.commit'/'.result'. Do not relax legacy publicly or add skip-validation boolean. Each extension retains exact commitment/result/settlement, original participants, scope, preloss elements and result. Allow only new expected receipt; custody/relationships remain null. Common validation already checks sequence, participant bindings, selected paid CP/ammo, result-based refusal/capture and before-retreat CP (CampaignCombatObligations.cs:228–334).

Receipt identities remain settlementId+'.disposition'/'.losses'/'.retreat'; disposition predecessor is actual hashed resultId, not settlementId+'.result'. Other receipt predecessors chain exact prior receipt. Existing constructors CampaignCombatRetreatDisposition, CampaignCombatRoleLoss, CampaignCombatLossReceipt, CampaignCombatRetreatReceipt in CampaignCombatSettlementReceipts.cs:65–218 enforce route, conservation, rounding, threshold, cost and role constraints. Retain them.

Disposition derives certified content route from defender origin toward side supply anchor: exactly one step for selected required retreat; verify geometry and original opponent distance. Reuse existing Content7/routing from certification, no fixture route lookup. Refusal leaves origin route, adds10 percent per unfulfilled hex to defender only.

Joint loss receipts use original committed10 each, attacker ceiling/defender floor on effective percentage; lossToe=captured+other, remaining+loss=10; captured ceil(loss*share/100) only captured role. Loss>=3 triggers3DP. Both element changes and causes appear atomically in role order. Create pending captivity lot preserving victim original component/source and preloss location even if later defender retreats; this is allocation, not custody disposition. Existing World7 validates retained preloss/effects/conservation (CampaignWorldV7.cs:126–140,177–254).

Actual retreat updates defender CP and both element/representation location; ChargeMandatoryRetreat from CampaignCombatSpending.cs:83 charges1 beyond ordinary ceilings and applies only incremental excess-over10 DP. CP10→11 yields1DP. Preserve paid ammo0 and ledgers. VictoryRP3 only after successful one-step evacuation, cap Cohesion10; no refusal/not-required RP. Cause order loss roles then defender retreat excess then attacker victory; receipt/cause IDs and ordinals frozen. CappedRP arithmetic should reuse existing verified rule/model proof for unreachable high-Cohesion synthetic cases, not weaken selected initialCohesion0 admission to manufacture an eligible campaign.

Projection writes current typed World, never patches untrusted JSON as authority. Full equality against expected prior World plus exact allowed settlement changes rejects unrelated mutation. Remaining rounds/history/target records, RNG and result facts stay frozen. Pending lot at victim preloss location and original participant identities must survive all cuts.

## Tests and acceptance

TDD first meaningful failing required-retreat/disposition or loss literal against accepted013. Then all32 authenticated prefixes:144literal events/176hash readbacks, every cut and continuation through retreat, original01332/64 retained. Expected values from fixture/hash or existing independent Rules proof, never production projection as oracle.

Required focused proof: zero-loss actual retreat CP/RP; refusal10percent and resulting lossDP; threshold below/at3TOE; simultaneous two-side losses; both capture roles/conservation/pending lots; CP-limit10→11/DP1; no ordinary-spend rejection for mandatory retreat; original paid ammunition and cursor unchanged; refusal/not-required no victoryRP. Reuse accepted005 full-table/settlement and World tests instead of duplicating all8840 cases. Supplement only missing integration evidence through authenticated contexts.

Clock tests cover earlier-than-audit opening, valid local endpoints, exactdeadline reject, beforedeadline expiryNoOp, null/unavailable/regression fallback, checkeddeadlineoverflow, originaldefender owner, stale timer afteracceptedintent, changed actor/choice, exactretry withchangedtimemetadata and disabledfreshpredecessor admission. Full same-owner retreat→custody10500 isolation matrix remains015 because014 cannot open custody;014 proves first mandatory-window independence only.

Rehashed event/World forgeries: dispositionroute/required/planned/unfulfilled/owner/timing; loss role/order/rounding/capture/origin/DP; retreatroute/location/representation/CP/excess/RP; missing/reorderedreceipt; cursor/result change. Hostile syntax/size/depth/array before sentryhistory; canonical Route syntax still causally rejects unsupported geography. Legacy sample constructor unchanged and named hashed append methods enforceexactchain. Owned collections/eventbuffers, failedcandidate/discard/replay, response-loss retry no doubleloss/capture/CP/RP. No fake hosttransaction test.

Scoped focused + shared Resolution/Commit/Seals/Identity/World/Rules regressions; root fullgate and independentreviews. Parent014 closure requires all above plus noadvance beyondretreat, no015/016 leakage. Actualcreation-rooted empty path remains unchanged; positive mechanism uses independently trusted syntheticBoundary. Snapshot/publicactivation/hostpublication remain open.

## Real blockers and decision boundary

No contract contradiction found.013 must expose immutable authenticated context/result and implement named identity factory/profile as planned; exact accepted APIs need reread before014 dispatch. If013 chooses a distinct pending overlay or exposes no safe immutable hashed-settlement evolution, revisit bounded scope with root rather than silently pivoting. Five paths feasible with existing typed constructors/spending/geometry, so no automatic child split or extra permission inferred. Root decides dispatch only after013 acceptance.
