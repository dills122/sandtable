using Cna.OrleansHost.Api;
using Cna.OrleansHost.Timeouts;
using Orleans;

namespace Cna.OrleansHost.Campaigns;

/// <summary>
/// Implements <see cref="ICampaignGrain"/> -- see that interface's remarks for the design and
/// exactly which entry points route through here versus continuing to call
/// <see cref="CampaignDurableAuthorityJournalStore"/> directly. This grain wraps/delegates to the
/// store's existing per-campaign file routing (<see cref="CampaignDurableAuthorityJournalStore.JournalFor"/>);
/// it never touches a SQLite connection itself.
/// </summary>
internal sealed class CampaignGrain(CampaignDurableAuthorityJournalStore store, IGrainFactory grainFactory)
    : Grain, ICampaignGrain
{
    public Task<CampaignGrainSubmissionResult> SubmitOwnerActionAsync(string stage,
        CampaignSeatCredential credential, string actingElementId, long openingRevision,
        string publicActionRef)
    {
        // Mirrors CampaignDurableOutwardApi's /{campaignId}/actions stage switch exactly (same
        // stage strings, same store methods) -- moved here so every stage that can open or close
        // a combat round runs behind this grain's single-writer boundary, with the round-timeout
        // arming/disarming below applied once, after the store call, regardless of which stage
        // triggered it.
        var result = stage switch
        {
            "pregame-axis-supply" => store.SubmitPregameAxisSupply(credential, openingRevision, publicActionRef),
            "pregame-axis-supply-finish" => store.SubmitPregameAxisSupplyFinish(credential, openingRevision, publicActionRef),
            "source-axis-dump" => store.SubmitSourceAxisDump(credential, openingRevision, publicActionRef),
            "source-commonwealth-supply" => store.SubmitSourceCommonwealthSupply(credential, openingRevision, publicActionRef),
            "source-dummy" => store.SubmitSourceDummy(credential, openingRevision, publicActionRef),
            "source-motorization" => store.SubmitSourceMotorization(credential, openingRevision, publicActionRef),
            "source-unattached-cargo" => store.SubmitSourceUnattachedCargo(credential, openingRevision, publicActionRef),
            "source-actions" => store.SubmitSourceActions(credential, openingRevision, publicActionRef),
            "source-axis-gt1-supply" => store.SubmitSourceAxisGt1Supply(credential, openingRevision, publicActionRef),
            "source-commonwealth-supply-plan" => store.SubmitSourceCommonwealthSupplyPlan(credential, openingRevision, publicActionRef),
            "source-autoblinda" => store.SubmitSourceAutoblinda(credential, openingRevision, publicActionRef),
            "source-priority-motorization" => store.SubmitSourcePriorityMotorization(credential, openingRevision, publicActionRef),
            "source-attached-initial-loads" => store.SubmitSourceAttachedInitialLoads(credential, openingRevision, publicActionRef),
            "source-initial-wrecks" => store.PlaceSourceInitialWreck(credential, openingRevision, publicActionRef),
            "source-axis-free-setup" => store.PlaceSourceAxisFreeUnit(credential, openingRevision, publicActionRef),
            "source-axis-corps-setup" => store.PlaceSourceAxisCorpsUnit(credential, openingRevision, publicActionRef),
            "source-maletti-hierarchy" => store.SubmitSourceMalettiHierarchy(credential, openingRevision, publicActionRef),
            "source-gt2-correction" => store.SubmitSourceGt2Correction(credential, openingRevision, publicActionRef),
            "source-cw-priority-catalog" => store.SubmitSourceCwPriorityCatalog(credential, openingRevision, publicActionRef),
            "source-matruh-formation" => store.SubmitSourceMatruhFormation(credential, openingRevision, publicActionRef),
            "source-cw-free-guns" => store.PlaceSourceCwFreeGun(credential, openingRevision, publicActionRef),
            "source-axis-priority-motorization" => store.SubmitSourceAxisPriority(credential, openingRevision, publicActionRef),
            "source-cairo-setup" => store.SubmitSourceCairoUnit(credential, openingRevision, publicActionRef),
            "movement" => store.SubmitOwnerMovement(credential, actingElementId, openingRevision, publicActionRef),
            "combat-selection" => store.SubmitOwnerCombatSelection(credential, actingElementId, openingRevision, publicActionRef),
            "rba-decline" => store.SubmitOwnerRbaDecline(credential, actingElementId, openingRevision, publicActionRef),
            "rba-move" => store.SubmitOwnerRbaMove(credential, actingElementId, openingRevision, publicActionRef),
            "force-assignment" => store.SubmitOwnerForceSeal(credential, actingElementId, openingRevision, publicActionRef),
            "post-assault-retreat" => store.SubmitOwnerPostAssaultRetreat(credential, actingElementId, openingRevision, publicActionRef),
            "custody" => store.SubmitOwnerCustody(credential, actingElementId, openingRevision, publicActionRef),
            "reserve-release" => store.SubmitOwnerReserveRelease(credential, actingElementId, openingRevision, publicActionRef),
            "cycle-control" => store.SubmitOwnerCycleControl(credential, openingRevision, publicActionRef),
            "truck-convoy-pass" => store.SubmitOwnerTruckConvoyPass(credential, openingRevision, publicActionRef),
            "post-combat-pass" => store.SubmitOwnerPostCombatPass(credential, openingRevision, publicActionRef),
            "second-reserve" => store.SubmitOwnerSecondReserve(credential, openingRevision, publicActionRef),
            "second-movement" => store.SubmitOwnerSecondMovement(credential, actingElementId, openingRevision, publicActionRef),
            "second-movement-completion" => store.SubmitOwnerSecondMovementCompletion(credential, openingRevision, publicActionRef),
            "repeat-movement" => store.SubmitOwnerRepeatMovement(credential, openingRevision, publicActionRef),
            "repeat-combat-selection" => store.SubmitOwnerRepeatCombat(credential, actingElementId, openingRevision, publicActionRef),
            _ => throw new ArgumentException($"Unknown owner stage '{stage}'.", nameof(stage)),
        };
        return FinishSubmitAsync(result);
    }

    private async Task<CampaignGrainSubmissionResult> FinishSubmitAsync(CampaignOwnerSubmissionResult result)
    {
        await ApplyPendingRoundTimerActionsAsync();
        return new CampaignGrainSubmissionResult
        {
            IsAccepted = result.IsAccepted,
            CanonicalReceipt = result.CanonicalReceipt,
        };
    }

    public async Task<bool> TickRoundAsync(CampaignRoundKind roundKind)
    {
        var ticked = store.TickRound(this.GetPrimaryKeyString(), roundKind);
        await ApplyPendingRoundTimerActionsAsync();
        return ticked;
    }

    /// <summary>
    /// Drains whatever <see cref="CampaignDurableAuthorityJournalStore.DrainPendingRoundTimerActions"/>
    /// recorded during the store call just made on this grain's turn, and applies each signal to
    /// the matching <c>CampaignRoundTimeoutGrain</c>. Always runs after the store call's own
    /// SQLite transaction has already committed (both call sites above are plain synchronous
    /// calls, not awaited), so a round-timeout grain is only ever armed/disarmed for a transition
    /// that durably happened -- never for one that was later rolled back.
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
