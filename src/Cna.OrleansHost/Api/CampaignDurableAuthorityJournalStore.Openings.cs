using System.Collections.Frozen;
using Cna.Core.Hosting;
using Cna.OrleansHost.Timeouts;

namespace Cna.OrleansHost.Api;

/// <summary>
/// The per-mechanic dispatch surface split out of <see cref="CampaignDurableAuthorityJournalStore"/>
/// per <c>docs/work/plans/2026-11-24-maintainability-refactor-direction.md</c> Section 2.4 (Slice 5):
/// the ~40 <c>QueryXOpening</c> one-liners and the 7 <c>TickXRound</c> one-liners, collapsed into two
/// small <see cref="FrozenDictionary{TKey,TValue}"/> registries reusing the exact pattern
/// <c>CampaignDurableAuthorityJournal.OwnerOpeningQueries.cs</c> (PR #525) already proved one layer
/// up. Each named method below still exists with its original signature -- nothing calling this store
/// changes -- but its body now reads one shared table entry instead of hand-naming the
/// <see cref="CampaignDurableAuthorityJournal"/> method to forward to, so there is exactly one place
/// (per table) that lists "which kind maps to which journal method," not ~50 independent copies of
/// that mapping. The two tables are kept separate (not merged into one god-table) for the same reason
/// PR #525 kept its two tables separate: different key enum, different delegate shape, different
/// layer of the two round-kind/opening-kind concepts.
/// </summary>
internal sealed partial class CampaignDurableAuthorityJournalStore
{
    /// <summary>
    /// Kind-to-forwarder table for every owner-opening query this store exposes. Every delegate
    /// takes the acting element id even for kinds that ignore it, mirroring the Journal layer's own
    /// <c>openingQueries</c> convention, so one uniform signature serves all ~40 kinds.
    /// </summary>
    private static readonly FrozenDictionary<CampaignOwnerOpeningKind,
        Func<CampaignDurableAuthorityJournal, CampaignSeatCredential, string, CampaignOwnerOpening>> openingQueries =
        new Dictionary<CampaignOwnerOpeningKind,
            Func<CampaignDurableAuthorityJournal, CampaignSeatCredential, string, CampaignOwnerOpening>>
        {
            [CampaignOwnerOpeningKind.Movement] = (journal, credential, actingElementId) =>
                journal.QueryOwnerOpening(credential, actingElementId),
            [CampaignOwnerOpeningKind.PregameAxisSupply] = (journal, credential, actingElementId) =>
                journal.QueryPregameAxisSupplyOpening(credential),
            [CampaignOwnerOpeningKind.PregameAxisSupplyFinish] = (journal, credential, actingElementId) =>
                journal.QueryPregameAxisSupplyFinishOpening(credential),
            [CampaignOwnerOpeningKind.SourceAxisDump] = (journal, credential, actingElementId) =>
                journal.QuerySourceAxisDumpOpening(credential),
            [CampaignOwnerOpeningKind.SourceCommonwealthSupply] = (journal, credential, actingElementId) =>
                journal.QuerySourceCommonwealthSupplyOpening(credential),
            [CampaignOwnerOpeningKind.SourceDummy] = (journal, credential, actingElementId) =>
                journal.QuerySourceDummyOpening(credential),
            [CampaignOwnerOpeningKind.SourceMotorization] = (journal, credential, actingElementId) =>
                journal.QuerySourceMotorizationOpening(credential),
            [CampaignOwnerOpeningKind.SourceUnattachedCargo] = (journal, credential, actingElementId) =>
                journal.QuerySourceUnattachedCargoOpening(credential),
            [CampaignOwnerOpeningKind.SourceActions] = (journal, credential, actingElementId) =>
                journal.QuerySourceActionsOpening(credential),
            [CampaignOwnerOpeningKind.SourceAxisGt1Supply] = (journal, credential, actingElementId) =>
                journal.QuerySourceAxisGt1SupplyOpening(credential),
            [CampaignOwnerOpeningKind.SourceCommonwealthSupplyPlan] = (journal, credential, actingElementId) =>
                journal.QuerySourceCommonwealthSupplyPlanOpening(credential),
            [CampaignOwnerOpeningKind.SourceAutoblinda] = (journal, credential, actingElementId) =>
                journal.QuerySourceAutoblindaOpening(credential),
            [CampaignOwnerOpeningKind.SourcePriorityMotorization] = (journal, credential, actingElementId) =>
                journal.QuerySourcePriorityMotorizationOpening(credential),
            [CampaignOwnerOpeningKind.SourceAttachedInitialLoads] = (journal, credential, actingElementId) =>
                journal.QuerySourceAttachedInitialLoadsOpening(credential),
            [CampaignOwnerOpeningKind.SourceInitialWrecks] = (journal, credential, actingElementId) =>
                journal.QuerySourceInitialWrecksOpening(credential),
            [CampaignOwnerOpeningKind.SourceAxisFreeSetup] = (journal, credential, actingElementId) =>
                journal.QuerySourceAxisFreeSetupOpening(credential),
            [CampaignOwnerOpeningKind.SourceAxisCorpsSetup] = (journal, credential, actingElementId) =>
                journal.QuerySourceAxisCorpsSetupOpening(credential),
            [CampaignOwnerOpeningKind.SourceMalettiHierarchy] = (journal, credential, actingElementId) =>
                journal.QuerySourceMalettiHierarchyOpening(credential),
            [CampaignOwnerOpeningKind.SourceGt2Correction] = (journal, credential, actingElementId) =>
                journal.QuerySourceGt2CorrectionOpening(credential),
            [CampaignOwnerOpeningKind.SourceCwPriorityCatalog] = (journal, credential, actingElementId) =>
                journal.QuerySourceCwPriorityCatalogOpening(credential),
            [CampaignOwnerOpeningKind.SourceMatruhFormation] = (journal, credential, actingElementId) =>
                journal.QuerySourceMatruhFormationOpening(credential),
            [CampaignOwnerOpeningKind.SourceCwFreeGuns] = (journal, credential, actingElementId) =>
                journal.QuerySourceCwFreeGunsOpening(credential),
            [CampaignOwnerOpeningKind.SourceAxisPriority] = (journal, credential, actingElementId) =>
                journal.QuerySourceAxisPriorityOpening(credential),
            [CampaignOwnerOpeningKind.SourceCairoSetup] = (journal, credential, actingElementId) =>
                journal.QuerySourceCairoSetupOpening(credential),
            [CampaignOwnerOpeningKind.CombatSelection] = (journal, credential, actingElementId) =>
                journal.QueryCombatOwnerOpening(credential, actingElementId),
            [CampaignOwnerOpeningKind.RetreatBeforeAssault] = (journal, credential, actingElementId) =>
                journal.QueryRbaOwnerOpening(credential, actingElementId),
            [CampaignOwnerOpeningKind.ForceAssignment] = (journal, credential, actingElementId) =>
                journal.QueryForceOwnerOpening(credential, actingElementId),
            [CampaignOwnerOpeningKind.PostAssaultRetreat] = (journal, credential, actingElementId) =>
                journal.QueryPostAssaultRetreatOwnerOpening(credential, actingElementId),
            [CampaignOwnerOpeningKind.Custody] = (journal, credential, actingElementId) =>
                journal.QueryCustodyOwnerOpening(credential, actingElementId),
            [CampaignOwnerOpeningKind.ReserveRelease] = (journal, credential, actingElementId) =>
                journal.QueryReserveReleaseOwnerOpening(credential, actingElementId),
            [CampaignOwnerOpeningKind.CycleControl] = (journal, credential, actingElementId) =>
                journal.QueryCycleControlOwnerOpening(credential),
            [CampaignOwnerOpeningKind.InitiativeOrder] = (journal, credential, actingElementId) =>
                journal.QueryInitiativeOrderOwnerOpening(credential),
            [CampaignOwnerOpeningKind.ReserveDesignation] = (journal, credential, actingElementId) =>
                journal.QueryReserveDesignationOwnerOpening(credential),
            [CampaignOwnerOpeningKind.Reaction] = (journal, credential, actingElementId) =>
                journal.QueryReactionOwnerOpening(credential),
            [CampaignOwnerOpeningKind.MovementSegmentCompletion] = (journal, credential, actingElementId) =>
                journal.QueryMovementSegmentCompletionOwnerOpening(credential),
            [CampaignOwnerOpeningKind.TruckConvoyPass] = (journal, credential, actingElementId) =>
                journal.QueryTruckConvoyPassOwnerOpening(credential),
            [CampaignOwnerOpeningKind.PostCombatPass] = (journal, credential, actingElementId) =>
                journal.QueryPostCombatPassOwnerOpening(credential),
            [CampaignOwnerOpeningKind.SecondReserve] = (journal, credential, actingElementId) =>
                journal.QuerySecondReserveOwnerOpening(credential),
            [CampaignOwnerOpeningKind.StageEntryReserve] = (journal, credential, actingElementId) =>
                journal.QueryStageEntryReserveOwnerOpening(credential),
            [CampaignOwnerOpeningKind.StageEntryInitiativeDeclaration] = (journal, credential, actingElementId) =>
                journal.QueryStageEntryInitiativeDeclarationOwnerOpening(credential),
            [CampaignOwnerOpeningKind.SecondMovement] = (journal, credential, actingElementId) =>
                journal.QuerySecondMovementOwnerOpening(credential, actingElementId),
            [CampaignOwnerOpeningKind.StageEntryMovement] = (journal, credential, actingElementId) =>
                journal.QueryStageEntryMovementOwnerOpening(credential, actingElementId),
            [CampaignOwnerOpeningKind.SecondMovementCompletion] = (journal, credential, actingElementId) =>
                journal.QuerySecondMovementCompletionOwnerOpening(credential),
            [CampaignOwnerOpeningKind.StageEntryMovementCompletion] = (journal, credential, actingElementId) =>
                journal.QueryStageEntryMovementCompletionOwnerOpening(credential),
            [CampaignOwnerOpeningKind.RepeatMovement] = (journal, credential, actingElementId) =>
                journal.QueryRepeatMovementOwnerOpening(credential),
            [CampaignOwnerOpeningKind.RepeatCombatSelection] = (journal, credential, actingElementId) =>
                journal.QueryRepeatCombatOwnerOpening(credential, actingElementId),
        }.ToFrozenDictionary();

    /// <summary>
    /// Kind-to-forwarder table for the 7 round kinds' tick entry points -- a different key enum and
    /// a different (smaller) shape than <see cref="openingQueries"/>, so deliberately kept as its own
    /// table rather than merged into it, per the maintainability plan's Section 2.4.
    /// </summary>
    private static readonly FrozenDictionary<CampaignRoundKind, Func<CampaignDurableAuthorityJournal, string, bool>> roundTicks =
        new Dictionary<CampaignRoundKind, Func<CampaignDurableAuthorityJournal, string, bool>>
        {
            [CampaignRoundKind.Selection] = (journal, campaignId) => journal.TickSelectionRound(campaignId),
            [CampaignRoundKind.Rba] = (journal, campaignId) => journal.TickRbaRound(campaignId),
            [CampaignRoundKind.Force] = (journal, campaignId) => journal.TickForceRound(campaignId),
            [CampaignRoundKind.Retreat] = (journal, campaignId) => journal.TickRetreatRound(campaignId),
            [CampaignRoundKind.Custody] = (journal, campaignId) => journal.TickCustodyRound(campaignId),
            [CampaignRoundKind.Reserve] = (journal, campaignId) => journal.TickReserveRound(campaignId),
            [CampaignRoundKind.Cycle] = (journal, campaignId) => journal.TickCycleRound(campaignId),
        }.ToFrozenDictionary();

    internal bool TickCustodyRound(string campaignId) =>
        roundTicks[CampaignRoundKind.Custody](JournalFor(campaignId, createIfMissing: false), campaignId);

    internal bool TickCycleRound(string campaignId) =>
        roundTicks[CampaignRoundKind.Cycle](JournalFor(campaignId, createIfMissing: false), campaignId);

    internal bool TickForceRound(string campaignId) =>
        roundTicks[CampaignRoundKind.Force](JournalFor(campaignId, createIfMissing: false), campaignId);

    internal bool TickRbaRound(string campaignId) =>
        roundTicks[CampaignRoundKind.Rba](JournalFor(campaignId, createIfMissing: false), campaignId);

    internal bool TickReserveRound(string campaignId) =>
        roundTicks[CampaignRoundKind.Reserve](JournalFor(campaignId, createIfMissing: false), campaignId);

    internal bool TickRetreatRound(string campaignId) =>
        roundTicks[CampaignRoundKind.Retreat](JournalFor(campaignId, createIfMissing: false), campaignId);

    internal bool TickSelectionRound(string campaignId) =>
        roundTicks[CampaignRoundKind.Selection](JournalFor(campaignId, createIfMissing: false), campaignId);

    internal CampaignOwnerOpening QueryOwnerOpening(CampaignSeatCredential credential, string actingElementId) =>
        openingQueries[CampaignOwnerOpeningKind.Movement](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, actingElementId);

    internal CampaignOwnerOpening QueryPregameAxisSupplyOpening(CampaignSeatCredential credential) =>
        openingQueries[CampaignOwnerOpeningKind.PregameAxisSupply](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, "");

    internal CampaignOwnerOpening QueryPregameAxisSupplyFinishOpening(CampaignSeatCredential credential) =>
        openingQueries[CampaignOwnerOpeningKind.PregameAxisSupplyFinish](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, "");

    internal CampaignOwnerOpening QuerySourceAxisDumpOpening(CampaignSeatCredential credential) =>
        openingQueries[CampaignOwnerOpeningKind.SourceAxisDump](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, "");

    internal CampaignOwnerOpening QuerySourceCommonwealthSupplyOpening(CampaignSeatCredential credential) =>
        openingQueries[CampaignOwnerOpeningKind.SourceCommonwealthSupply](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, "");

    internal CampaignOwnerOpening QuerySourceDummyOpening(CampaignSeatCredential credential) =>
        openingQueries[CampaignOwnerOpeningKind.SourceDummy](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, "");

    internal CampaignOwnerOpening QuerySourceMotorizationOpening(CampaignSeatCredential credential) =>
        openingQueries[CampaignOwnerOpeningKind.SourceMotorization](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, "");

    internal CampaignOwnerOpening QuerySourceUnattachedCargoOpening(CampaignSeatCredential credential) =>
        openingQueries[CampaignOwnerOpeningKind.SourceUnattachedCargo](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, "");

    internal CampaignOwnerOpening QuerySourceActionsOpening(CampaignSeatCredential credential) =>
        openingQueries[CampaignOwnerOpeningKind.SourceActions](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, "");

    internal CampaignOwnerOpening QuerySourceAxisGt1SupplyOpening(CampaignSeatCredential credential) =>
        openingQueries[CampaignOwnerOpeningKind.SourceAxisGt1Supply](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, "");

    internal CampaignOwnerOpening QuerySourceCommonwealthSupplyPlanOpening(CampaignSeatCredential credential) =>
        openingQueries[CampaignOwnerOpeningKind.SourceCommonwealthSupplyPlan](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, "");

    internal CampaignOwnerOpening QuerySourceAutoblindaOpening(CampaignSeatCredential credential) =>
        openingQueries[CampaignOwnerOpeningKind.SourceAutoblinda](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, "");

    internal CampaignOwnerOpening QuerySourcePriorityMotorizationOpening(CampaignSeatCredential credential) =>
        openingQueries[CampaignOwnerOpeningKind.SourcePriorityMotorization](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, "");

    internal CampaignOwnerOpening QuerySourceAttachedInitialLoadsOpening(CampaignSeatCredential credential) =>
        openingQueries[CampaignOwnerOpeningKind.SourceAttachedInitialLoads](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, "");

    internal CampaignOwnerOpening QuerySourceInitialWrecksOpening(CampaignSeatCredential credential) =>
        openingQueries[CampaignOwnerOpeningKind.SourceInitialWrecks](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, "");

    internal CampaignOwnerOpening QuerySourceAxisFreeSetupOpening(CampaignSeatCredential credential) =>
        openingQueries[CampaignOwnerOpeningKind.SourceAxisFreeSetup](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, "");

    internal CampaignOwnerOpening QuerySourceAxisCorpsSetupOpening(CampaignSeatCredential credential) =>
        openingQueries[CampaignOwnerOpeningKind.SourceAxisCorpsSetup](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, "");

    internal CampaignOwnerOpening QuerySourceMalettiHierarchyOpening(CampaignSeatCredential credential) =>
        openingQueries[CampaignOwnerOpeningKind.SourceMalettiHierarchy](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, "");

    internal CampaignOwnerOpening QuerySourceGt2CorrectionOpening(CampaignSeatCredential credential) =>
        openingQueries[CampaignOwnerOpeningKind.SourceGt2Correction](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, "");

    internal CampaignOwnerOpening QuerySourceCwPriorityCatalogOpening(CampaignSeatCredential credential) =>
        openingQueries[CampaignOwnerOpeningKind.SourceCwPriorityCatalog](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, "");

    internal CampaignOwnerOpening QuerySourceMatruhFormationOpening(CampaignSeatCredential credential) =>
        openingQueries[CampaignOwnerOpeningKind.SourceMatruhFormation](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, "");

    internal CampaignOwnerOpening QuerySourceCwFreeGunsOpening(CampaignSeatCredential credential) =>
        openingQueries[CampaignOwnerOpeningKind.SourceCwFreeGuns](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, "");

    internal CampaignOwnerOpening QuerySourceAxisPriorityOpening(CampaignSeatCredential credential) =>
        openingQueries[CampaignOwnerOpeningKind.SourceAxisPriority](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, "");

    internal CampaignOwnerOpening QuerySourceCairoSetupOpening(CampaignSeatCredential credential) =>
        openingQueries[CampaignOwnerOpeningKind.SourceCairoSetup](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, "");

    internal CampaignOwnerOpening QueryCombatOwnerOpening(CampaignSeatCredential credential, string actingElementId) =>
        openingQueries[CampaignOwnerOpeningKind.CombatSelection](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, actingElementId);

    internal CampaignOwnerOpening QueryRbaOwnerOpening(CampaignSeatCredential credential, string defenderElementId) =>
        openingQueries[CampaignOwnerOpeningKind.RetreatBeforeAssault](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, defenderElementId);

    internal CampaignOwnerOpening QueryForceOwnerOpening(CampaignSeatCredential credential, string elementId) =>
        openingQueries[CampaignOwnerOpeningKind.ForceAssignment](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, elementId);

    internal CampaignOwnerOpening QueryPostAssaultRetreatOwnerOpening(CampaignSeatCredential credential, string defenderElementId) =>
        openingQueries[CampaignOwnerOpeningKind.PostAssaultRetreat](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, defenderElementId);

    internal CampaignOwnerOpening QueryCustodyOwnerOpening(CampaignSeatCredential credential, string captorElementId) =>
        openingQueries[CampaignOwnerOpeningKind.Custody](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, captorElementId);

    internal CampaignOwnerOpening QueryReserveReleaseOwnerOpening(CampaignSeatCredential credential, string reserveElementId) =>
        openingQueries[CampaignOwnerOpeningKind.ReserveRelease](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, reserveElementId);

    internal CampaignOwnerOpening QueryCycleControlOwnerOpening(CampaignSeatCredential credential) =>
        openingQueries[CampaignOwnerOpeningKind.CycleControl](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, "");

    internal CampaignOwnerOpening QueryInitiativeOrderOwnerOpening(CampaignSeatCredential credential) =>
        openingQueries[CampaignOwnerOpeningKind.InitiativeOrder](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, "");

    internal CampaignOwnerOpening QueryReserveDesignationOwnerOpening(CampaignSeatCredential credential) =>
        openingQueries[CampaignOwnerOpeningKind.ReserveDesignation](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, "");

    internal CampaignOwnerOpening QueryReactionOwnerOpening(CampaignSeatCredential credential) =>
        openingQueries[CampaignOwnerOpeningKind.Reaction](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, "");

    internal CampaignOwnerOpening QueryMovementSegmentCompletionOwnerOpening(CampaignSeatCredential credential) =>
        openingQueries[CampaignOwnerOpeningKind.MovementSegmentCompletion](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, "");

    internal CampaignOwnerOpening QueryTruckConvoyPassOwnerOpening(CampaignSeatCredential credential) =>
        openingQueries[CampaignOwnerOpeningKind.TruckConvoyPass](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, "");

    internal CampaignOwnerOpening QueryPostCombatPassOwnerOpening(CampaignSeatCredential credential) =>
        openingQueries[CampaignOwnerOpeningKind.PostCombatPass](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, "");

    internal CampaignOwnerOpening QuerySecondReserveOwnerOpening(CampaignSeatCredential credential) =>
        openingQueries[CampaignOwnerOpeningKind.SecondReserve](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, "");

    internal CampaignOwnerOpening QueryStageEntryReserveOwnerOpening(CampaignSeatCredential credential) =>
        openingQueries[CampaignOwnerOpeningKind.StageEntryReserve](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, "");

    internal CampaignOwnerOpening QueryStageEntryInitiativeDeclarationOwnerOpening(CampaignSeatCredential credential) =>
        openingQueries[CampaignOwnerOpeningKind.StageEntryInitiativeDeclaration](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, "");

    internal CampaignOwnerOpening QuerySecondMovementOwnerOpening(CampaignSeatCredential credential, string elementId) =>
        openingQueries[CampaignOwnerOpeningKind.SecondMovement](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, elementId);

    internal CampaignOwnerOpening QueryStageEntryMovementOwnerOpening(CampaignSeatCredential credential, string elementId) =>
        openingQueries[CampaignOwnerOpeningKind.StageEntryMovement](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, elementId);

    internal CampaignOwnerOpening QuerySecondMovementCompletionOwnerOpening(CampaignSeatCredential credential) =>
        openingQueries[CampaignOwnerOpeningKind.SecondMovementCompletion](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, "");

    internal CampaignOwnerOpening QueryStageEntryMovementCompletionOwnerOpening(CampaignSeatCredential credential) =>
        openingQueries[CampaignOwnerOpeningKind.StageEntryMovementCompletion](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, "");

    internal CampaignOwnerOpening QueryRepeatMovementOwnerOpening(CampaignSeatCredential credential) =>
        openingQueries[CampaignOwnerOpeningKind.RepeatMovement](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, "");

    internal CampaignOwnerOpening QueryRepeatCombatOwnerOpening(CampaignSeatCredential credential, string actingElementId) =>
        openingQueries[CampaignOwnerOpeningKind.RepeatCombatSelection](
            JournalFor(credential.CampaignId, createIfMissing: false), credential, actingElementId);

    // <auto-generated-by-hand: async>
    // Async twins, generated mechanically from the block above (same generator discipline as the
    // rest of this class -- see CampaignDurableAuthorityJournalStore.cs's own comment).
    internal Task<bool> TickCustodyRoundAsync(string campaignId) =>
        RunAsync(campaignId, false, journal => roundTicks[CampaignRoundKind.Custody](journal, campaignId));

    internal Task<bool> TickCycleRoundAsync(string campaignId) =>
        RunAsync(campaignId, false, journal => roundTicks[CampaignRoundKind.Cycle](journal, campaignId));

    internal Task<bool> TickForceRoundAsync(string campaignId) =>
        RunAsync(campaignId, false, journal => roundTicks[CampaignRoundKind.Force](journal, campaignId));

    internal Task<bool> TickRbaRoundAsync(string campaignId) =>
        RunAsync(campaignId, false, journal => roundTicks[CampaignRoundKind.Rba](journal, campaignId));

    internal Task<bool> TickReserveRoundAsync(string campaignId) =>
        RunAsync(campaignId, false, journal => roundTicks[CampaignRoundKind.Reserve](journal, campaignId));

    internal Task<bool> TickRetreatRoundAsync(string campaignId) =>
        RunAsync(campaignId, false, journal => roundTicks[CampaignRoundKind.Retreat](journal, campaignId));

    internal Task<bool> TickSelectionRoundAsync(string campaignId) =>
        RunAsync(campaignId, false, journal => roundTicks[CampaignRoundKind.Selection](journal, campaignId));

    internal Task<CampaignOwnerOpening> QueryOwnerOpeningAsync(CampaignSeatCredential credential, string actingElementId) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.Movement](journal, credential, actingElementId));

    internal Task<CampaignOwnerOpening> QueryPregameAxisSupplyOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.PregameAxisSupply](journal, credential, ""));

    internal Task<CampaignOwnerOpening> QueryPregameAxisSupplyFinishOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.PregameAxisSupplyFinish](journal, credential, ""));

    internal Task<CampaignOwnerOpening> QuerySourceAxisDumpOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.SourceAxisDump](journal, credential, ""));

    internal Task<CampaignOwnerOpening> QuerySourceCommonwealthSupplyOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.SourceCommonwealthSupply](journal, credential, ""));

    internal Task<CampaignOwnerOpening> QuerySourceDummyOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.SourceDummy](journal, credential, ""));

    internal Task<CampaignOwnerOpening> QuerySourceMotorizationOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.SourceMotorization](journal, credential, ""));

    internal Task<CampaignOwnerOpening> QuerySourceUnattachedCargoOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.SourceUnattachedCargo](journal, credential, ""));

    internal Task<CampaignOwnerOpening> QuerySourceActionsOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.SourceActions](journal, credential, ""));

    internal Task<CampaignOwnerOpening> QuerySourceAxisGt1SupplyOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.SourceAxisGt1Supply](journal, credential, ""));

    internal Task<CampaignOwnerOpening> QuerySourceCommonwealthSupplyPlanOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.SourceCommonwealthSupplyPlan](journal, credential, ""));

    internal Task<CampaignOwnerOpening> QuerySourceAutoblindaOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.SourceAutoblinda](journal, credential, ""));

    internal Task<CampaignOwnerOpening> QuerySourcePriorityMotorizationOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.SourcePriorityMotorization](journal, credential, ""));

    internal Task<CampaignOwnerOpening> QuerySourceAttachedInitialLoadsOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.SourceAttachedInitialLoads](journal, credential, ""));

    internal Task<CampaignOwnerOpening> QuerySourceInitialWrecksOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.SourceInitialWrecks](journal, credential, ""));

    internal Task<CampaignOwnerOpening> QuerySourceAxisFreeSetupOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.SourceAxisFreeSetup](journal, credential, ""));

    internal Task<CampaignOwnerOpening> QuerySourceAxisCorpsSetupOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.SourceAxisCorpsSetup](journal, credential, ""));

    internal Task<CampaignOwnerOpening> QuerySourceMalettiHierarchyOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.SourceMalettiHierarchy](journal, credential, ""));

    internal Task<CampaignOwnerOpening> QuerySourceGt2CorrectionOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.SourceGt2Correction](journal, credential, ""));

    internal Task<CampaignOwnerOpening> QuerySourceCwPriorityCatalogOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.SourceCwPriorityCatalog](journal, credential, ""));

    internal Task<CampaignOwnerOpening> QuerySourceMatruhFormationOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.SourceMatruhFormation](journal, credential, ""));

    internal Task<CampaignOwnerOpening> QuerySourceCwFreeGunsOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.SourceCwFreeGuns](journal, credential, ""));

    internal Task<CampaignOwnerOpening> QuerySourceAxisPriorityOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.SourceAxisPriority](journal, credential, ""));

    internal Task<CampaignOwnerOpening> QuerySourceCairoSetupOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.SourceCairoSetup](journal, credential, ""));

    internal Task<CampaignOwnerOpening> QueryCombatOwnerOpeningAsync(CampaignSeatCredential credential, string actingElementId) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.CombatSelection](journal, credential, actingElementId));

    internal Task<CampaignOwnerOpening> QueryRbaOwnerOpeningAsync(CampaignSeatCredential credential, string defenderElementId) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.RetreatBeforeAssault](journal, credential, defenderElementId));

    internal Task<CampaignOwnerOpening> QueryForceOwnerOpeningAsync(CampaignSeatCredential credential, string elementId) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.ForceAssignment](journal, credential, elementId));

    internal Task<CampaignOwnerOpening> QueryPostAssaultRetreatOwnerOpeningAsync(CampaignSeatCredential credential, string defenderElementId) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.PostAssaultRetreat](journal, credential, defenderElementId));

    internal Task<CampaignOwnerOpening> QueryCustodyOwnerOpeningAsync(CampaignSeatCredential credential, string captorElementId) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.Custody](journal, credential, captorElementId));

    internal Task<CampaignOwnerOpening> QueryReserveReleaseOwnerOpeningAsync(CampaignSeatCredential credential, string reserveElementId) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.ReserveRelease](journal, credential, reserveElementId));

    internal Task<CampaignOwnerOpening> QueryCycleControlOwnerOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.CycleControl](journal, credential, ""));

    internal Task<CampaignOwnerOpening> QueryInitiativeOrderOwnerOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.InitiativeOrder](journal, credential, ""));

    internal Task<CampaignOwnerOpening> QueryReserveDesignationOwnerOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.ReserveDesignation](journal, credential, ""));

    internal Task<CampaignOwnerOpening> QueryReactionOwnerOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.Reaction](journal, credential, ""));

    internal Task<CampaignOwnerOpening> QueryMovementSegmentCompletionOwnerOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.MovementSegmentCompletion](journal, credential, ""));

    internal Task<CampaignOwnerOpening> QueryTruckConvoyPassOwnerOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.TruckConvoyPass](journal, credential, ""));

    internal Task<CampaignOwnerOpening> QueryPostCombatPassOwnerOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.PostCombatPass](journal, credential, ""));

    internal Task<CampaignOwnerOpening> QuerySecondReserveOwnerOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.SecondReserve](journal, credential, ""));

    internal Task<CampaignOwnerOpening> QueryStageEntryReserveOwnerOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.StageEntryReserve](journal, credential, ""));

    internal Task<CampaignOwnerOpening> QueryStageEntryInitiativeDeclarationOwnerOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.StageEntryInitiativeDeclaration](journal, credential, ""));

    internal Task<CampaignOwnerOpening> QuerySecondMovementOwnerOpeningAsync(CampaignSeatCredential credential, string elementId) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.SecondMovement](journal, credential, elementId));

    internal Task<CampaignOwnerOpening> QueryStageEntryMovementOwnerOpeningAsync(CampaignSeatCredential credential, string elementId) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.StageEntryMovement](journal, credential, elementId));

    internal Task<CampaignOwnerOpening> QuerySecondMovementCompletionOwnerOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.SecondMovementCompletion](journal, credential, ""));

    internal Task<CampaignOwnerOpening> QueryStageEntryMovementCompletionOwnerOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.StageEntryMovementCompletion](journal, credential, ""));

    internal Task<CampaignOwnerOpening> QueryRepeatMovementOwnerOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.RepeatMovement](journal, credential, ""));

    internal Task<CampaignOwnerOpening> QueryRepeatCombatOwnerOpeningAsync(CampaignSeatCredential credential, string actingElementId) =>
        RunAsync(credential.CampaignId, false,
            journal => openingQueries[CampaignOwnerOpeningKind.RepeatCombatSelection](journal, credential, actingElementId));
}
