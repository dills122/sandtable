# D2 completion codec research

Five files: new CampaignCombatReserveCompletionModels.cs, CampaignCombatReserveCompletion.cs,
CampaignCombatReserveCompletionCodec.cs; modify CampaignCombatReserveCodec.cs to expose bounded
WriteWorld internally and extract WriteMembers; new CombatReserveCompletionTests.cs. Existingfixture
already linked, no csprojedit. Preserve D1bytes with its focusedtests. No019Adependency.

Trust: CreateCommand/Create/ReadEvent accept full trustedrequest+Created11+4prelude+1Weather+4stage+
0/1designation, call D1Replay themselves; privately derivebase/event. NO authority overload taking
callerconstructed D1state/OpeningBase alone. Return validated event/evidence+predecessor, notterminal
state.019A later consumes this and applies sameevent atomically.

OpeningBase order: contractVersion1,profile isolated-first-opening,creationRequest,firstActingSide,
position,priorVersion,priorPrefix,world,randomState,members. Request fromretainedcreation; RNG from
state.Stage.Weather.RandomState (advanced). BaseHash=plainSHA256 canonicalbase, no domain.
OpeningCommand2 kind complete-reserve-designation,baseHash,expectedPriorVersion,expectedPositionId.
Input command,actor; ownerfromfirstorder. OpeningEvent exactschema order includes sources beforeinput,
sequenceposition then cycle/cycleId/receipt. Movement sequence5 first-player,nullActiveSide.
Sources land18.11 then5.2.reserve-designation.

CycleAuthority order: contractVersion,campaignId,rulesetHash,setupId,setupHash,contentPackId,contentHash,
scenarioId,gameTurn,operationStage,playerPhaseSlot,actingSide,ordinal,openedAuthorityVersion,
openingPrefix,admittedPolicyBundleDigest. Fixed1/1/first-acting-side/ordinal1; resultprior+1;
openingPrefix actualprecompletionprefix; policyDigest configHash.

CycleID framing: ASCII sandtable.cycle.authority.v1 +NUL+tuple infieldorder. contractVersion U32BE;
gameTurn/operationStage/ordinal/openedAuthorityVersion U64BE; openingPrefix/policyDigest raw32bytes;
ALL OTHER FIELDS UTF8 lengthU32BE, INCLUDING rulesetHash/setupHash/contentHash. These three are NOTraw.
Receipt rc. + SHA256 ASCII sandtable.combat.reserve-completion-receipt.v2+NUL+eventomitreceipt.
EventHash includesreceipt.019A applies acceptedB1 EventPrefix(finalevent). No separateopeningevent.

Tests all16reservefixturecases actualchain: opening-base/input/event lengthhashes (empty input/event1,
I input/event2), literal11/12version+ordinal1+bothowners+nullMovementside+actualopeningPrefix;
independentbinarytuple (distinguish stringhash/rawdigest), allweather/advancedRNG, preservedpredecessor,
wrongactor/base/version/position, coherentcycle/receiptforgeries, rawcanonicalnegatives, fullhistory
rejection. Oldreaders andD1 rejectcompletion. Terminalstate11/12 and crosscompletionretry await019A.

Canonical docs/specs/verify-combat-cycle-sequence-v1.py identity atline35; Reserveoracle completion
atline128 derives solely frombase then _emit installsstate. Hence serializer→projector acyclic.
No edits/builds/tests inresearch. Proposal forlead validation afterD1acceptance.
