# 019A bounded first-cycle opening scope

Research after D2 source freeze; implementation waits D2 three-round acceptance.
Five primary files: new CampaignCombatReserveOpeningModels.cs, CampaignCombatReserveOpening.cs,
CampaignCombatReserveOpeningCodec.cs, CombatReserveOpeningTests.cs; existing completion codec only
exposes WriteCycle internally. Existing fixture already linked. Do not modify D1 precompletion state.

Full-history Replay admits Reserve suffix empty, designation, completion, designation+completion only.
Call D1/D2 full-history validators; private projection from validated D2 evidence, no public/internal
admission overload accepting evidence or fabricated cache. At most2records, unknown/repeated/reordered
or postcompletion record rejects. Full wrapper retains genuine D1 predecessor for World/member writer.

State order: contractVersion,campaignId,rulesetHash,configurationHash,creationBinding,creationEventHash,
stateVersion,prefix,sequencePosition,initiativeHolder,operationStageOrders,operationStageWeather,world,
randomState,receipts,firstActingSide,members,cycle,cycleId,openingBaseHash,completionReceiptId.
Before completion lastfour null. Aftercompletion version/position/cycle from SAME completion2 event,
cycleId,baseHash,completionReceiptId derived; EventPrefix(predecessorPrefix,exactevent) and one receipt
(inputHash,eventHash,id,actor,resultVersion). Empty terminal11/10receipts; I terminal12/11receipts.
Preserve World/members/history/Weather/advancedRNG/order/holder. No extra opening command/event.

Apply overloads for designation and completion: full replay, inputshape+trustedactor before retry;
search consumed priorversion. Exact wholeinput returns originalevent plus CURRENT fullstate duplicate;
changed occurrence rejects. No new input aftercompletion. Otherwise delegate existing D1/D2 generation
then replay appendedevent. Completionretry uses original base; designationretry aftercompletion returns
terminal12, never precompletion11. Retained arrays copied; ReadState exactreplay compare.

Acceptance all16traces40cuts152 artifacts (request/creation/predecessorlist/inputs/events/states/base),
allweathertypes/bothowners/empty-I. Assert cycle ordinal/version/precompletionprefix, Movement nullside,
first9receipts unchanged, onecompletionreceipt; independentprefix/receipt; retries at every latercut;
actors/changedoccurrence/foreign/partial/reorder/duplicate/raw/re-signedcycle/cache/buffer negatives.
Oldreaders/creationSnapshot reject. Movement handoff carries fullterminalprojection+acceptedhistory,
not standalone cache. No Movement adjudication, repeat/finish, later slots, public/durable restore.
Canonical Reserve oracle baseline already retained; no new prerequisite or spike found.
