using Cna.OrleansHost.Api;
using Cna.OrleansHost.Timeouts;
using Orleans;

namespace Cna.OrleansHost.Campaigns;

/// <summary>
/// Implements <see cref="ICampaignGrain"/> -- see that interface's remarks for the design and
/// exactly which entry points route through here versus continuing to call
/// <see cref="CampaignDurableAuthorityJournalStore"/> directly. This grain wraps/delegates to the
/// store's existing per-campaign file routing (<see cref="CampaignDurableAuthorityJournalStore.JournalFor"/>);
/// it never touches a storage connection itself.
/// </summary>
internal sealed class CampaignGrain(CampaignDurableAuthorityJournalStore store, IGrainFactory grainFactory)
    : Grain, ICampaignGrain
{
    public async Task<CampaignGrainSubmissionResult> SubmitOwnerActionAsync(string stage,
        CampaignSeatCredential credential, string actingElementId, long openingRevision,
        string publicActionRef)
    {
        // Mirrors CampaignDurableOutwardApi's /{campaignId}/actions stage switch exactly (same
        // stage strings, same store methods) -- moved here so every stage that can open or close
        // a combat round runs behind this grain's single-writer boundary, with the round-timeout
        // arming/disarming below applied once, after the store call, regardless of which stage
        // triggered it.
        var result = await (stage switch
        {
            "pregame-axis-supply" => store.SubmitPregameAxisSupplyAsync(credential, openingRevision, publicActionRef),
            "pregame-axis-supply-finish" => store.SubmitPregameAxisSupplyFinishAsync(credential, openingRevision, publicActionRef),
            "source-axis-dump" => store.SubmitSourceAxisDumpAsync(credential, openingRevision, publicActionRef),
            "source-commonwealth-supply" => store.SubmitSourceCommonwealthSupplyAsync(credential, openingRevision, publicActionRef),
            "source-dummy" => store.SubmitSourceDummyAsync(credential, openingRevision, publicActionRef),
            "source-motorization" => store.SubmitSourceMotorizationAsync(credential, openingRevision, publicActionRef),
            "source-unattached-cargo" => store.SubmitSourceUnattachedCargoAsync(credential, openingRevision, publicActionRef),
            "source-actions" => store.SubmitSourceActionsAsync(credential, openingRevision, publicActionRef),
            "source-axis-gt1-supply" => store.SubmitSourceAxisGt1SupplyAsync(credential, openingRevision, publicActionRef),
            "source-commonwealth-supply-plan" => store.SubmitSourceCommonwealthSupplyPlanAsync(credential, openingRevision, publicActionRef),
            "source-autoblinda" => store.SubmitSourceAutoblindaAsync(credential, openingRevision, publicActionRef),
            "source-priority-motorization" => store.SubmitSourcePriorityMotorizationAsync(credential, openingRevision, publicActionRef),
            "source-attached-initial-loads" => store.SubmitSourceAttachedInitialLoadsAsync(credential, openingRevision, publicActionRef),
            "source-initial-wrecks" => store.PlaceSourceInitialWreckAsync(credential, openingRevision, publicActionRef),
            "source-axis-free-setup" => store.PlaceSourceAxisFreeUnitAsync(credential, openingRevision, publicActionRef),
            "source-axis-corps-setup" => store.PlaceSourceAxisCorpsUnitAsync(credential, openingRevision, publicActionRef),
            "source-maletti-hierarchy" => store.SubmitSourceMalettiHierarchyAsync(credential, openingRevision, publicActionRef),
            "source-gt2-correction" => store.SubmitSourceGt2CorrectionAsync(credential, openingRevision, publicActionRef),
            "source-cw-priority-catalog" => store.SubmitSourceCwPriorityCatalogAsync(credential, openingRevision, publicActionRef),
            "source-matruh-formation" => store.SubmitSourceMatruhFormationAsync(credential, openingRevision, publicActionRef),
            "source-cw-free-guns" => store.PlaceSourceCwFreeGunAsync(credential, openingRevision, publicActionRef),
            "source-axis-priority-motorization" => store.SubmitSourceAxisPriorityAsync(credential, openingRevision, publicActionRef),
            "source-cairo-setup" => store.SubmitSourceCairoUnitAsync(credential, openingRevision, publicActionRef),
            "movement" => store.SubmitOwnerMovementAsync(credential, actingElementId, openingRevision, publicActionRef),
            "initiative-order" => store.SubmitOwnerInitiativeOrderAsync(credential, openingRevision, publicActionRef),
            "reserve-designation" => store.SubmitOwnerReserveDesignationAsync(credential, openingRevision, publicActionRef),
            "reaction" => store.SubmitOwnerReactionAsync(credential, openingRevision, publicActionRef),
            "movement-segment-completion" => store.SubmitOwnerMovementSegmentCompletionAsync(credential, openingRevision, publicActionRef),
            "combat-selection" => store.SubmitOwnerCombatSelectionAsync(credential, actingElementId, openingRevision, publicActionRef),
            "rba-decline" => store.SubmitOwnerRbaDeclineAsync(credential, actingElementId, openingRevision, publicActionRef),
            "force-assignment" => store.SubmitOwnerForceSealAsync(credential, actingElementId, openingRevision, publicActionRef),
            "post-assault-retreat" => store.SubmitOwnerPostAssaultRetreatAsync(credential, actingElementId, openingRevision, publicActionRef),
            "custody" => store.SubmitOwnerCustodyAsync(credential, actingElementId, openingRevision, publicActionRef),
            "reserve-release" => store.SubmitOwnerReserveReleaseAsync(credential, actingElementId, openingRevision, publicActionRef),
            "cycle-control" => store.SubmitOwnerCycleControlAsync(credential, openingRevision, publicActionRef),
            "truck-convoy-pass" => store.SubmitOwnerTruckConvoyPassAsync(credential, openingRevision, publicActionRef),
            "post-combat-pass" => store.SubmitOwnerPostCombatPassAsync(credential, openingRevision, publicActionRef),
            "second-reserve" => store.SubmitOwnerSecondReserveAsync(credential, openingRevision, publicActionRef),
            "stage-entry-reserve" => store.SubmitOwnerStageEntryReserveAsync(credential, openingRevision, publicActionRef),
            "stage-entry-initiative-declaration" => store.SubmitOwnerStageEntryInitiativeDeclarationAsync(credential, openingRevision, publicActionRef),
            "second-movement" => store.SubmitOwnerSecondMovementAsync(credential, actingElementId, openingRevision, publicActionRef),
            "stage-entry-movement" => store.SubmitOwnerStageEntryMovementAsync(credential, actingElementId, openingRevision, publicActionRef),
            "second-movement-completion" => store.SubmitOwnerSecondMovementCompletionAsync(credential, openingRevision, publicActionRef),
            "stage-entry-movement-completion" => store.SubmitOwnerStageEntryMovementCompletionAsync(credential, openingRevision, publicActionRef),
            "repeat-movement" => store.SubmitOwnerRepeatMovementAsync(credential, openingRevision, publicActionRef),
            "repeat-combat-selection" => store.SubmitOwnerRepeatCombatAsync(credential, actingElementId, openingRevision, publicActionRef),
            _ => throw new ArgumentException($"Unknown owner stage '{stage}'.", nameof(stage)),
        });
        return await FinishSubmitAsync(result);
    }

    /// <summary>
    /// Phase C3b of issue #439: owner-opening queries mutate durable state -- they upsert the
    /// opening and its action references, advance the open round's high-water mark, and (when the
    /// gate finds the deadline passed or the clock regressed) commit a complete Core timeout or
    /// cancellation step (<c>CampaignDurableAuthorityJournal.Openings.cs</c>'s gate block). They now
    /// run here, behind this grain's single-writer-per-campaign boundary, instead of calling the
    /// store directly from <c>CampaignDurableOutwardApi</c> -- restoring design doc &#167;5's
    /// "no second writer on a campaign" premise -- and a round that query closes now disarms its
    /// timeout grain like any other close.
    /// </summary>
    public async Task<CampaignGrainOpeningResult> QueryOwnerOpeningAsync(string stage,
        CampaignSeatCredential credential, string actingElementId)
    {
        try
        {
            var opening = await (stage switch
            {
                "pregame-axis-supply" => store.QueryPregameAxisSupplyOpeningAsync(credential),
                "pregame-axis-supply-finish" => store.QueryPregameAxisSupplyFinishOpeningAsync(credential),
                "source-axis-dump" => store.QuerySourceAxisDumpOpeningAsync(credential),
                "source-commonwealth-supply" => store.QuerySourceCommonwealthSupplyOpeningAsync(credential),
                "source-dummy" => store.QuerySourceDummyOpeningAsync(credential),
                "source-motorization" => store.QuerySourceMotorizationOpeningAsync(credential),
                "source-unattached-cargo" => store.QuerySourceUnattachedCargoOpeningAsync(credential),
                "source-actions" => store.QuerySourceActionsOpeningAsync(credential),
                "source-axis-gt1-supply" => store.QuerySourceAxisGt1SupplyOpeningAsync(credential),
                "source-commonwealth-supply-plan" => store.QuerySourceCommonwealthSupplyPlanOpeningAsync(credential),
                "source-autoblinda" => store.QuerySourceAutoblindaOpeningAsync(credential),
                "source-priority-motorization" => store.QuerySourcePriorityMotorizationOpeningAsync(credential),
                "source-attached-initial-loads" => store.QuerySourceAttachedInitialLoadsOpeningAsync(credential),
                "source-initial-wrecks" => store.QuerySourceInitialWrecksOpeningAsync(credential),
                "source-axis-free-setup" => store.QuerySourceAxisFreeSetupOpeningAsync(credential),
                "source-axis-corps-setup" => store.QuerySourceAxisCorpsSetupOpeningAsync(credential),
                "source-maletti-hierarchy" => store.QuerySourceMalettiHierarchyOpeningAsync(credential),
                "source-gt2-correction" => store.QuerySourceGt2CorrectionOpeningAsync(credential),
                "source-cw-priority-catalog" => store.QuerySourceCwPriorityCatalogOpeningAsync(credential),
                "source-matruh-formation" => store.QuerySourceMatruhFormationOpeningAsync(credential),
                "source-cw-free-guns" => store.QuerySourceCwFreeGunsOpeningAsync(credential),
                "source-axis-priority-motorization" => store.QuerySourceAxisPriorityOpeningAsync(credential),
                "source-cairo-setup" => store.QuerySourceCairoSetupOpeningAsync(credential),
                "movement" => store.QueryOwnerOpeningAsync(credential, actingElementId),
                "initiative-order" => store.QueryInitiativeOrderOwnerOpeningAsync(credential),
                "reserve-designation" => store.QueryReserveDesignationOwnerOpeningAsync(credential),
                "reaction" => store.QueryReactionOwnerOpeningAsync(credential),
                "movement-segment-completion" => store.QueryMovementSegmentCompletionOwnerOpeningAsync(credential),
                "combat-selection" => store.QueryCombatOwnerOpeningAsync(credential, actingElementId),
                "retreat-before-assault" => store.QueryRbaOwnerOpeningAsync(credential, actingElementId),
                "force-assignment" => store.QueryForceOwnerOpeningAsync(credential, actingElementId),
                "post-assault-retreat" => store.QueryPostAssaultRetreatOwnerOpeningAsync(credential, actingElementId),
                "custody" => store.QueryCustodyOwnerOpeningAsync(credential, actingElementId),
                "reserve-release" => store.QueryReserveReleaseOwnerOpeningAsync(credential, actingElementId),
                "cycle-control" => store.QueryCycleControlOwnerOpeningAsync(credential),
                "truck-convoy-pass" => store.QueryTruckConvoyPassOwnerOpeningAsync(credential),
                "post-combat-pass" => store.QueryPostCombatPassOwnerOpeningAsync(credential),
                "second-reserve" => store.QuerySecondReserveOwnerOpeningAsync(credential),
                "stage-entry-reserve" => store.QueryStageEntryReserveOwnerOpeningAsync(credential),
                "stage-entry-initiative-declaration" => store.QueryStageEntryInitiativeDeclarationOwnerOpeningAsync(credential),
                "second-movement" => store.QuerySecondMovementOwnerOpeningAsync(credential, actingElementId),
                "stage-entry-movement" => store.QueryStageEntryMovementOwnerOpeningAsync(credential, actingElementId),
                "second-movement-completion" => store.QuerySecondMovementCompletionOwnerOpeningAsync(credential),
                "stage-entry-movement-completion" => store.QueryStageEntryMovementCompletionOwnerOpeningAsync(credential),
                "repeat-movement" => store.QueryRepeatMovementOwnerOpeningAsync(credential),
                "repeat-combat-selection" => store.QueryRepeatCombatOwnerOpeningAsync(credential, actingElementId),
                _ => throw new ArgumentException($"Unknown owner stage '{stage}'.", nameof(stage)),
            });
            return new CampaignGrainOpeningResult
            {
                CampaignId = opening.CampaignId,
                Audience = opening.Audience,
                ActingElementId = opening.ActingElementId,
                Revision = opening.Revision,
                VisibleBytes = opening.VisibleBytes,
                PublicActionRefs = [.. opening.PublicActionRefs],
            };
        }
        finally
        {
            // A query that closed a round (and then threw "round is closed") still committed that
            // close; disarm/arm accordingly either way. Round *opens* in a timeout cascade are
            // recorded by the journal and drained here; a gate-driven timeout *close* is not
            // recorded (C3a only instrumented normal completions), so re-derive the queried round
            // kind's durable deadline and disarm its timer if that round is no longer open.
            await ApplyPendingRoundTimerActionsAsync();
            if (RoundKindForOpeningStage(stage) is { } kind
                && await store.GetRoundDeadlineMsAsync(this.GetPrimaryKeyString(), kind) is null)
                await grainFactory.GetGrain<ICampaignRoundTimeoutGrain>(
                    CampaignRoundTimeoutGrainKey.For(this.GetPrimaryKeyString(), kind)).DisarmAsync();
        }
    }

    private static CampaignRoundKind? RoundKindForOpeningStage(string stage) => stage switch
    {
        "combat-selection" or "repeat-combat-selection" => CampaignRoundKind.Selection,
        "retreat-before-assault" => CampaignRoundKind.Rba,
        "force-assignment" => CampaignRoundKind.Force,
        "post-assault-retreat" => CampaignRoundKind.Retreat,
        "custody" => CampaignRoundKind.Custody,
        "reserve-release" => CampaignRoundKind.Reserve,
        "cycle-control" => CampaignRoundKind.Cycle,
        _ => null,
    };

    private async Task<CampaignGrainSubmissionResult> FinishSubmitAsync(CampaignOwnerSubmissionResult result)
    {
        await ApplyPendingRoundTimerActionsAsync();
        return new CampaignGrainSubmissionResult
        {
            IsAccepted = result.IsAccepted,
            CanonicalReceipt = result.CanonicalReceipt,
            RejectionCode = result.RejectionCode,
        };
    }

    public async Task<bool> TickRoundAsync(CampaignRoundKind roundKind)
    {
        var ticked = await store.TickRoundAsync(this.GetPrimaryKeyString(), roundKind);
        await ApplyPendingRoundTimerActionsAsync();
        return ticked;
    }

    /// <summary>
    /// Drains whatever <see cref="CampaignDurableAuthorityJournalStore.DrainPendingRoundTimerActions"/>
    /// recorded during the store call just made on this grain's turn, and applies each signal to
    /// the matching <c>CampaignRoundTimeoutGrain</c>. Always runs after the store call's own
    /// transaction has already committed (Phase C3b: the store's async call returns only after its
    /// Marten flush succeeded), so a round-timeout grain is only ever armed/disarmed for a
    /// transition that durably happened -- never for one that was later rolled back.
    /// </summary>
    private async Task ApplyPendingRoundTimerActionsAsync()
    {
        var campaignId = this.GetPrimaryKeyString();
        foreach (var (roundKind, deadlineMs) in store.DrainPendingRoundTimerActions(campaignId))
        {
            var timeoutGrain = grainFactory.GetGrain<ICampaignRoundTimeoutGrain>(
                CampaignRoundTimeoutGrainKey.For(campaignId, roundKind));
            if (deadlineMs is { } ms)
                await timeoutGrain.ArmAsync(DateTimeOffset.FromUnixTimeMilliseconds(ms));
            else
                await timeoutGrain.DisarmAsync();
        }
    }
}
