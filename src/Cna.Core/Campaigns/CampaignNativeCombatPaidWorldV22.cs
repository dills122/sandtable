using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// The assault result pays both roles in the normal World before losses or retreat.
// Replay derives this projection from the sealed Force inputs and prior World.
internal static class CampaignNativeCombatPaidWorldV22
{
    internal static CampaignWorldSnapshotV6 Project(CampaignWorldSnapshotV6 prior,
        CampaignNativeCombatAssaultRoleInputs attacker,
        CampaignNativeCombatAssaultRoleInputs defender)
    {
        ArgumentNullException.ThrowIfNull(prior);
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(defender);
        if (attacker.ElementId == defender.ElementId)
            throw new InvalidOperationException("Assault roles must be distinct.");
        var paid = new Dictionary<string, CapabilityPointAmount>(StringComparer.Ordinal)
        {
            [attacker.ElementId] = attacker.CapabilityPointsAfter,
            [defender.ElementId] = defender.CapabilityPointsAfter
        };
        if (prior.Elements.Count(element => paid.ContainsKey(element.ElementId)) != 2
            || prior.Elements.Any(element => paid.ContainsKey(element.ElementId)
                && element.OperationalState.CapabilityPointsExpended
                    != (element.ElementId == attacker.ElementId
                        ? attacker.CapabilityPointsBefore : defender.CapabilityPointsBefore)))
            throw new InvalidOperationException("Assault costs differ from retained World.");
        return new CampaignWorldSnapshotV6(CampaignWorldSnapshotV6.CurrentContractVersion,
            prior.Elements.Select(element => paid.TryGetValue(element.ElementId, out var after)
                ? new CampaignElementStateV5(element.ElementId, element.CurrentLocationId,
                    element.ReserveStatus,
                    new CampaignElementOperationalStateV5(
                        element.OperationalState.LedgerGameTurn,
                        element.OperationalState.LedgerOperationStage,
                        after, element.OperationalState.CohesionLevel,
                        element.OperationalState.VehicleBreakdownState,
                        element.OperationalState.MovementEnded),
                    element.Components)
                : element), prior.Representations, prior.BrokenVehicleLots);
    }

    internal static string Hash(CampaignWorldSnapshotV6 world)
    {
        ArgumentNullException.ThrowIfNull(world);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            CampaignV11CanonicalCodec.WriteWorld(writer, "world", world);
            writer.WriteEndObject();
        }
        return "sha256:" + Convert.ToHexStringLower(SHA256.HashData(stream.ToArray()));
    }
}
