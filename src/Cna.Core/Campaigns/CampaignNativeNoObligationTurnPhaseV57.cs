using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// D-T2-2: the caller must establish an explicit later-turn no-obligation
// policy from retained content. Opening-only policy is insufficient.
internal sealed record CampaignNativeNoObligationTurnPhaseV57(
    int ContractVersion, string CampaignId, long PriorStateVersion, string PriorEventHash,
    string PositionId, string NextPositionId, string WorldHash, int GameTurn)
{
    internal const int CurrentContractVersion = 57;
    internal long StateVersion => checked(PriorStateVersion + 1);

    internal static CampaignNativeNoObligationTurnPhaseV57 Create(
        object predecessor, bool noObligationAuthorized, long contactVersion)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        var (campaignId, version, positionId, worldHash, gameTurn) = predecessor switch
        {
            CampaignNativeTurnInitiativeV56 initiative => (initiative.CampaignId, initiative.StateVersion,
                initiative.NextPositionId, initiative.WorldHash, initiative.GameTurn),
            CampaignNativeNoObligationTurnPhaseV57 phase => (phase.CampaignId, phase.StateVersion,
                phase.NextPositionId, phase.WorldHash, phase.GameTurn),
            _ => throw new InvalidOperationException("Turn phase requires an accepted predecessor."),
        };
        if (contactVersion != version)
            throw new InvalidOperationException("Turn phase belongs to the current cut.");
        var position = Cna1979LandSequenceV4.CreateTurn(gameTurn).Single(value => value.PositionId == positionId);
        if (position.PhaseId is not (LandPhaseIds.NavalConvoySchedule or LandPhaseIds.TacticalShipping))
            throw new InvalidOperationException("This position does not admit a no-obligation turn phase.");
        if (!noObligationAuthorized)
            throw new CampaignRuleNotImplementedException("The later-turn phase requires an explicit no-obligation policy.");
        var priorBytes = predecessor switch
        {
            CampaignNativeTurnInitiativeV56 initiative => initiative.SerializeCanonical(),
            CampaignNativeNoObligationTurnPhaseV57 phase => phase.SerializeCanonical(),
            _ => throw new InvalidOperationException("Turn phase requires an accepted predecessor."),
        };
        var next = Cna1979LandSequenceV4.GetNext(position);
        return new(CurrentContractVersion, campaignId, version, TurnBoundaryCanonical.Hash(priorBytes),
            position.PositionId, next.PositionId, worldHash, gameTurn);
    }

    internal byte[] SerializeCanonical()
    {
        if (GameTurn is < 2 or > CampaignNativeEndOfTurnV55.LastRolloverTurn + 1
            || (PositionId, NextPositionId) is not
                (("land.position.naval-convoy.schedule", "land.position.naval-convoy.tactical-shipping")
                or ("land.position.naval-convoy.tactical-shipping", "land.position.operation-1.initiative-declaration")))
            throw new JsonException("Invalid no-obligation turn phase.");
        return TurnBoundaryCanonical.Write(ContractVersion, CurrentContractVersion, "native-no-obligation-turn-phase-resolved",
            CampaignId, PriorStateVersion, PriorEventHash, PositionId, NextPositionId, WorldHash, GameTurn, _ => { });
    }

    internal static CampaignNativeNoObligationTurnPhaseV57 Deserialize(ReadOnlyMemory<byte> bytes) =>
        TurnBoundaryCanonical.Read(bytes, "native-no-obligation-turn-phase-resolved", [], root => new CampaignNativeNoObligationTurnPhaseV57(
            root.GetProperty("contractVersion").GetInt32(), root.GetProperty("campaignId").GetString()!,
            root.GetProperty("priorStateVersion").GetInt64(), root.GetProperty("priorEventHash").GetString()!,
            root.GetProperty("positionId").GetString()!, root.GetProperty("nextPositionId").GetString()!,
            root.GetProperty("worldHash").GetString()!, root.GetProperty("gameTurn").GetInt32()),
            value => value.SerializeCanonical());
}
