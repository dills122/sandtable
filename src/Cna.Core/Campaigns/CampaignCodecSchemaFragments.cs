namespace Cna.Core.Campaigns;

// Shared schema-string fragments for the codec Shapes catalogs that had byte-identical
// duplicate entries. Extracted 2026-11-23 in two passes: the first (CampaignCombatIdentityCodec,
// CampaignCombatPositiveEntryCodec, CampaignCombatResolutionCodec, CampaignCombatSealedRoundCodec,
// CampaignCombatSelectionStepsCodec, CampaignCombatSettledContinuationCodec,
// CampaignCombatSettledControlCodec) scoped to files with no participation in any Pins/Delivery
// provenance gate; the second (CampaignCombatActualMovedCycleControlCodec,
// CampaignCombatActualMovedRepeatBreakdownCompletionCodec, CampaignCombatActualMovedRepeatMovementCodec,
// CampaignCombatActualMovedRepeatMovementCompletionCodec, CampaignCombatActualRepeatedBreakdownCompletionCodec,
// CampaignCombatActualRepeatedMovementCodec, CampaignCombatActualRepeatedMovementCompletionCodec)
// after verifying per-file, codebase-wide, that none of these seven are themselves a pin target --
// CampaignCombatActualReactionCombatEntryCodec.cs was excluded from this second pass because it IS
// pinned by six other files. Each constant's value was byte-identical across 2+ Shapes-dict entries
// before extraction; some names were deliberately disambiguated (e.g. TrackMovingFlow vs MovingFlow)
// where a differently-shaped value already used the more obvious name elsewhere.
internal static class CampaignCodecSchemaFragments
{
    internal const string ActualProgressRef = "eventType:id receiptId:id eventHash:hash";
    internal const string Ammunition = "points:int initialAmmunitionOrigin:Origin";
    internal const string Assessment = "contractVersion:int mode:profile sourceOccurrence:hash sourceOrdinal:int nextOrdinal:int releaseCompletionReceiptId:id movementCompletionReceiptId:id worldHash:hash membersHash:hash progress:ActualProgressRef[] witnesses:ContinuationWitness[] unsupportedWitnesses:UnsupportedWitness[] combatAssessment:id witnessOutcome:id unsupportedReason:id? executionAvailable:bool supportCertificate:ProspectiveCertificate";
    internal const string AttackHistory = "commitmentId:id cycleId:hash segmentId:id attacker:UnitKey defender:UnitKey targetLocationId:id gameTurn:int operationStage:int";
    internal const string Authority = "contractVersion:int campaignId:id rulesetHash:rawHash setupId:id setupHash:hash contentPackId:id contentHash:hash scenarioId:id gameTurn:int operationStage:int playerPhaseSlot:id actingSide:id ordinal:int openedAuthorityVersion:long openingPrefix:hash admittedPolicyBundleDigest:hash";
    internal const string BaseControlProof = "contractVersion:int sourceId:id sourceHash:hash base:Base control:Control";
    internal const string Candidate = "attacker:Participant defender:Participant targetLocationId:id basis:id";
    internal const string Cause = "causeId:id ordinal:int receiptId:id elementId:id gameTurn:int operationStage:int kind:string points:int before:int after:int";
    internal const string Closure = "cycleId:hash ordinal:int outcome:id receiptId:id";
    internal const string CombatEntryState = "contractVersion:int campaignId:id rulesetHash:rawHash configurationHash:hash creationBinding:id creationEventHash:hash stateVersion:long prefix:hash sequencePosition:Position initiativeHolder:side operationStageOrders:PreambleOrder[] operationStageWeather:WeatherValue[] world:World randomState:Random receipts:PreambleReceipt[] firstActingSide:side members:ReserveMember[] cycle:Authority? cycleId:hash? openingBaseHash:hash? completionReceiptId:id? tracks:InheritedTrack[] actualProgressRefs:ActualProgressRef[] breakdownFlow:LifecycleFlow interruptContext:InterruptContext? movementEnd:MovementEndProof? breakdownCompletionReceiptId:id?";
    internal const string CompletionEffectWithProgress = "kind:id endLocations:EndLocation[] excludedUnits:UnitKey[] progress:ActualProgressRef[] successorPosition:Position";
    internal const string Component = "componentId:id currentToe:int initialToeOrigin:Origin";
    internal const string ComponentKey = "unit:UnitKey componentId:id";
    internal const string ContinuationWitness = "kind:id unit:UnitKey destinationLocationId:id terrainCost:int breakOffCost:int afterCp:int excessCpDp:int usesReleaseException:bool";
    internal const string Cp = "numerator:long denominator:int";
    internal const string Custody = "receiptId:id predecessorReceiptId:id lotId:id kind:string route:Route guardId:id? entitlementId:id? donorToeBefore:int donorToeAfter:int";
    internal const string Disposition = "receiptId:id predecessorReceiptId:id kind:string requiredDistance:int plannedDistance:int unfulfilledDistance:int route:Route";
    internal const string Element = "elementId:id currentLocationId:id reserveStatus:string operationalState:Operational components:Component[] sourceParentFormationId:id currentParentFormationId:id ammunition:Ammunition readiness:Readiness";
    internal const string EndLocation = "unit:UnitKey locationId:id";
    internal const string EndedMembership = "relationId:id creationReceiptId:id kind:id attacker:UnitKey defender:UnitKey endingCause:id";
    internal const string Entitlement = "entitlementId:id escapeReceiptId:id lotId:id originalComponent:ComponentKey quantity:int reunionLocationId:id earnedScope:Scope delayOperationStages:int eligibleScope:FutureScope status:string";
    internal const string FutureScope = "gameTurn:futureTurn operationStage:int";
    internal const string Guard = "guardId:id formationReceiptId:id lotId:id originComponent:ComponentKey currentLocationId:id toe:int baseCapabilityPointAllowance:int offensiveCloseAssaultRating:int defensiveCloseAssaultRating:int operationalState:Operational ammunition:Ammunition readiness:Readiness";
    internal const string IdleFlow = "kind:id";
    internal const string InheritedTrack = "unit:UnitKey route:OrderedLocations";
    internal const string InheritedTraversalEffect = "kind:id fromPositionId:id toPositionId:id previousStepReceiptId:id dispositionReceiptId:id proofKind:id";
    internal const string Input = "command:Command actor:actor";
    internal const string Intent = "contractVersion:int kind:id sourceOccurrence:hash sourceOrdinal:int expectedNextOrdinal:int parentSourceHash:hash worldHash:hash assessmentHash:hash configurationHash:hash decisionId:id witnessesHash:hash executionAvailable:bool";
    internal const string InterruptContext = "cycle:Authority cycleId:hash sequencePosition:Position";
    internal const string LifecycleStop = "stopId:hash recordedStateVersion:long route:MovingRoute reason:id weatherKind:id cohortInputs:empty";
    internal const string Losses = "receiptId:id predecessorReceiptId:id roles:RoleLoss[]";
    internal const string Lot = "lotId:id lossReceiptId:id originalComponent:ComponentKey captor:UnitKey quantity:int originLocationId:id currentLocationId:id? status:string guardId:id? escapeReceiptId:id?";
    internal const string MoveEffect = "kind:id unit:UnitKey representationId:id originLocationId:id destinationLocationId:id costs:MovementCosts endedMemberships:EndedMembership[]";
    internal const string MovementCosts = "terrainCost:int breakOffCost:int totalCost:int baseCpa:int voluntaryCeiling:int beforeCp:long afterCp:long excessCpDp:int beforeCohesion:signedInt afterCohesion:signedInt";
    internal const string MovementEndProof = "scope:ReleaseScope ordinal:int completionReceiptId:id endLocations:EndLocation[] excludedBefore:UnitKey[]";
    internal const string MovementException = "scope:ReleaseScope ordinal:int status:id completionReceiptId:id?";
    internal const string MovingFlow = "kind:id route:MovingRoute";
    internal const string MovingRoute = "routeId:hash firstMoveStateVersion:long elementId:id representationId:id owner:side originLocationId:id currentLocationId:id cohortIds:empty";
    internal const string Obligation = "obligationId:id receiptId:id kind:string subjectId:id earnedScope:Scope eligibleScope:FutureScope? activationGate:string status:string";
    internal const string Operational = "ledgerGameTurn:int ledgerOperationStage:int capabilityPointsExpended:Cp cohesionLevel:int vehicleBreakdownState:null movementEnded:null initialLedgerOrigin:Origin";
    internal const string Origin = "kind:string references:Reference[]";
    internal const string Participant = "unit:UnitKey representationId:id locationId:id componentIds:id[]";
    internal const string PendingFlow = "kind:id stop:Stop";
    internal const string PhasingStopFlow = "kind:id stop:LifecycleStop";
    internal const string Position = "contractVersion:int positionId:id gameTurn:int operationStage:int stageId:id phaseId:id segmentId:id? stepId:id? actorRole:id activeSide:id? sources:Reference[]";
    internal const string PreambleOrder = "contractVersion:int gameTurn:int operationStage:int firstSide:side secondSide:side";
    internal const string ProspectiveCertificate = "contractVersion:int profile:profile sourceOccurrence:hash sourceOrdinal:int nextOrdinal:int worldHash:hash configurationHash:hash selectedRulesHash:hash catalogueHash:hash moralePairs:int assaultPairs:int captureRefusalVariants:int supportHandlersHash:hash capacitiesHash:hash";
    internal const string PublishEffect = "kind:id intentReceiptId:id priorCycleId:hash nextCycleId:hash nextCycle:Authority successorPositionId:id worldHash:hash assessmentHash:hash witnessesHash:hash";
    internal const string Random = "contractVersion:int algorithmId:id seed:ulong nextByteCursor:ulong";
    internal const string Readiness = "gameTurn:int operationStage:int waterStatus:string storesStatus:string pinned:bool initialReadinessOrigin:Origin";
    internal const string Receipt = "commandHash:hash eventHash:hash receiptId:id actor:actor stateVersion:long";
    internal const string Reference = "sourceId:id locator:locator";
    internal const string RelationsReceipt = "receiptId:id predecessorReceiptId:id relationshipId:id? kind:string?";
    internal const string Relationship = "relationId:id creationReceiptId:id kind:string attacker:UnitKey defender:UnitKey gameTurn:int operationStage:int active:bool endedByReceiptId:id? endingCause:string?";
    internal const string ReleaseHistory = "scope:ReleaseScope designationReceiptId:id? conversionReceiptId:id? releasedType:id? releaseReceiptId:id? releaseCycle:int? cpaBasis:int? voluntaryCeiling:int? offensiveCommitmentId:id? nextMovement:MovementException?";
    internal const string ReleaseScope = "gameTurn:int operationStage:int playerPhaseSlot:id actingSide:side";
    internal const string Representation = "representationId:id currentLocationId:id bindingKind:string boundElementIds:id[]";
    internal const string ReserveMember = "unit:UnitKey status:id baseCpa:int spentCp:Cp history:ReleaseHistory";
    internal const string ReserveMember2 = "unit:UnitKey status:id baseCpa:int spentCp:CP history:ReserveHistory";
    internal const string ResolutionEffectWithSuccessorPosition = "kind:id stopId:id beforeRandomState:Random afterRandomState:Random checks:UnitKey[] createdLots:id[] sources:id[] successorPosition:Position";
    internal const string Result = "differential:int attackerCoordinate:int defenderCoordinate:int captureDie:int? attackerPercent:int defenderPercent:int rawEngaged:bool requiredRetreat:int capturedRole:string? captureShare:int";
    internal const string Retreat = "receiptId:id predecessorReceiptId:id kind:string route:Route completedDistance:int beforeCp:Cp afterCp:Cp excessCpDp:int attackerVictoryRp:int";
    internal const string RoleLoss = "role:string component:ComponentKey committedToe:int tablePercent:int refusalPercent:int lossToe:int capturedToe:int otherLossToe:int remainingToe:int lossDp:int";
    internal const string Scope = "gameTurn:int operationStage:int";
    internal const string Settlement = "settlementId:id commitmentId:id resultId:id gameTurn:int operationStage:int attacker:UnitKey defender:UnitKey preLossElements:Element[] result:Result disposition:Disposition? losses:Losses? retreat:Retreat? custody:Custody? relationships:RelationsReceipt?";
    internal const string StageActionCapabilityCommand = "contractVersion:int kind:id operationStage:int actionId:hash baseHash:hash cycleId:hash expectedPositionId:id expectedPriorVersion:long capabilityId:hash";
    internal const string Stop = "stopId:id recordedStateVersion:long track:Track lastMoveReceiptId:id reason:id weatherKind:id cohortInputs:UnitKey[]";
    internal const string StopEffectWithSuccessorPosition = "kind:id stop:Stop interruptContext:Context successorPosition:Position";
    internal const string SuspendedPositionContext = "cycle:Authority cycleId:hash suspendedPosition:Position";
    internal const string TargetUse = "commitmentId:id segmentId:id targetLocationId:id";
    internal const string Timing = "contractVersion:int configHash:hash kind:id decisionBudgetMilliseconds:int openedAtUnixMilliseconds:utc deadlineUnixMilliseconds:utc highWaterUnixMilliseconds:utc";
    internal const string Track = "unit:UnitKey route:id[]";
    internal const string TrackMovingFlow = "kind:id track:Track lastMoveReceiptId:id";
    internal const string UnitKey = "creationBinding:id originalSide:side elementId:id";
    internal const string UnsupportedWitness = "witness:ContinuationWitness reason:id";
    internal const string WeatherValue = "contractVersion:int gameTurn:int operationStage:int determiningSide:side season:id firstDie:int secondDie:int kind:id scope:id locationDie:int? affectedAreas:id[] fuelWaterReductionSubjectCount:int restoredWellCount:int damagedGroundedAircraftCount:int";
    internal const string World = "contractVersion:int creationBinding:id elements:Element[] representations:Representation[] brokenVehicleLots:LegacyBrokenVehicleLot[] cohesionCauses:Cause[] relationships:Relationship[] custodyLots:Lot[] guards:Guard[] replacementEntitlements:Entitlement[] futureObligations:Obligation[] settlements:Settlement[]";
}
