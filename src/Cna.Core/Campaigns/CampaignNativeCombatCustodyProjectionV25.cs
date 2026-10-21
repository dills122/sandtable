using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignNativeCombatGuardV25(
    string GuardId, string DonorElementId, string DonorComponentId,
    string LocationId, int Toe, bool UpkeepPending);

internal sealed record CampaignNativeCombatReplacementV25(
    string EntitlementId, string VictimElementId, string VictimComponentId,
    string ReunionLocationId, int Quantity, int EligibleGameTurn,
    int EligibleOperationStage, bool TrainingPending);

internal sealed record CampaignNativeCombatCustodyStateV25(
    string LotId, string VictimElementId, string CaptorElementId,
    int Quantity, string OriginLocationId, string? CurrentLocationId,
    string Status, CampaignNativeCombatGuardV25? Guard,
    CampaignNativeCombatReplacementV25? Replacement);

// Custody is normal campaign authority after V24. World V6 retains element
// TOE; this typed companion retains captive, guard and delayed entitlement
// state until the next versioned campaign snapshot carries all three.
internal static class CampaignNativeCombatCustodyProjectionV25
{
    internal sealed record Projection(
        CampaignWorldSnapshotV6 World,
        CampaignNativeCombatCustodyStateV25? Custody);

    internal static byte[] SerializeStateCanonical(
        CampaignNativeCombatCustodyStateV25? custody)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            if (custody is null) writer.WriteNullValue();
            else
            {
                writer.WriteStartObject();
                writer.WriteString("lotId", custody.LotId);
                writer.WriteString("victimElementId", custody.VictimElementId);
                writer.WriteString("captorElementId", custody.CaptorElementId);
                writer.WriteNumber("quantity", custody.Quantity);
                writer.WriteString("originLocationId", custody.OriginLocationId);
                writer.WriteString("currentLocationId", custody.CurrentLocationId);
                writer.WriteString("status", custody.Status);
                writer.WritePropertyName("guard");
                if (custody.Guard is null) writer.WriteNullValue();
                else
                {
                    writer.WriteStartObject();
                    writer.WriteString("guardId", custody.Guard.GuardId);
                    writer.WriteString("donorElementId", custody.Guard.DonorElementId);
                    writer.WriteString("donorComponentId", custody.Guard.DonorComponentId);
                    writer.WriteString("locationId", custody.Guard.LocationId);
                    writer.WriteNumber("toe", custody.Guard.Toe);
                    writer.WriteBoolean("upkeepPending", custody.Guard.UpkeepPending);
                    writer.WriteEndObject();
                }
                writer.WritePropertyName("replacement");
                if (custody.Replacement is null) writer.WriteNullValue();
                else
                {
                    writer.WriteStartObject();
                    writer.WriteString("entitlementId", custody.Replacement.EntitlementId);
                    writer.WriteString("victimElementId", custody.Replacement.VictimElementId);
                    writer.WriteString("victimComponentId", custody.Replacement.VictimComponentId);
                    writer.WriteString("reunionLocationId", custody.Replacement.ReunionLocationId);
                    writer.WriteNumber("quantity", custody.Replacement.Quantity);
                    writer.WriteNumber("eligibleGameTurn", custody.Replacement.EligibleGameTurn);
                    writer.WriteNumber("eligibleOperationStage",
                        custody.Replacement.EligibleOperationStage);
                    writer.WriteBoolean("trainingPending", custody.Replacement.TrainingPending);
                    writer.WriteEndObject();
                }
                writer.WriteEndObject();
            }
        }
        return stream.ToArray();
    }

    internal static Projection Project(
        CampaignSnapshotV11 priorSnapshot,
        CampaignNativeCombatSettlementV24 settlement,
        CampaignWorldSnapshotV6 settledWorld,
        CampaignNativeCombatCustodyCutV25? cut,
        string choiceKind,
        CampaignContentContext context)
    {
        ArgumentNullException.ThrowIfNull(priorSnapshot);
        ArgumentNullException.ThrowIfNull(settlement);
        ArgumentNullException.ThrowIfNull(settledWorld);
        ArgumentNullException.ThrowIfNull(context);
        var artifact = context.ArtifactV6
            ?? throw new InvalidOperationException("Custody needs normal Content 6.");
        if (priorSnapshot.CampaignId != settlement.CampaignId
            || settlement.SettledWorldHash != CampaignNativeCombatPaidWorldV22.Hash(settledWorld)
            || !CampaignWorldV6Validator.IsValid(settledWorld, artifact, context.Scenario))
            throw new InvalidOperationException("Custody lacks accepted settlement authority.");
        if (settlement.PendingCaptives.Count == 0)
        {
            if (cut is not null || choiceKind != "not-required")
                throw new InvalidOperationException("No captive lot requires a System disposition.");
            return new(settledWorld, null);
        }
        if (settlement.PendingCaptives.Count != 1 || cut is null
            || cut.CampaignId != settlement.CampaignId
            || cut.AuthorityVersion != settlement.StateVersion
            || cut.SettlementHash != "sha256:" + Convert.ToHexStringLower(
                System.Security.Cryptography.SHA256.HashData(settlement.SerializeCanonical())))
            throw new InvalidOperationException("Custody choice lacks its exact owner cut.");
        var choice = cut.Choices.SingleOrDefault(value => value.Kind == choiceKind)
            ?? throw new InvalidOperationException("Custody choice was not offered.");
        var lot = settlement.PendingCaptives[0];
        if (lot.VictimElementId != cut.VictimElementId
            || lot.CaptorElementId != cut.CaptorElementId
            || lot.Quantity != cut.Quantity
            || choice.Route.Count == 0
            || choice.Route[0] != lot.OriginLocationId)
            throw new InvalidOperationException("Custody cut differs from the settled captive lot.");

        var captor = settledWorld.Elements.Single(value =>
            value.ElementId == lot.CaptorElementId);
        CampaignNativeCombatGuardV25? guard = null;
        CampaignNativeCombatReplacementV25? replacement = null;
        CampaignWorldSnapshotV6 nextWorld = settledWorld;
        string? currentLocation;
        string status;
        if (choiceKind == "relocate-and-guard")
        {
            var component = captor.Components.Single();
            if (component.CurrentToe <= 1
                || choice.Route[^1] != captor.CurrentLocationId)
                throw new InvalidOperationException("Guard lacks donor TOE or lawful route.");
            guard = new(cut.LotId + ".guard", captor.ElementId,
                component.ComponentId, captor.CurrentLocationId, 1, true);
            var donor = new CampaignElementStateV5(captor.ElementId,
                captor.CurrentLocationId, captor.ReserveStatus,
                captor.OperationalState,
                captor.Components.Select(value => value.ComponentId == component.ComponentId
                    ? new CampaignComponentToeState(value.ComponentId,
                        value.CurrentToe - 1, value.InitialToeOrigin)
                    : value));
            nextWorld = new CampaignWorldSnapshotV6(
                CampaignWorldSnapshotV6.CurrentContractVersion,
                settledWorld.Elements.Select(value => value.ElementId == donor.ElementId
                    ? donor : value), settledWorld.Representations,
                settledWorld.BrokenVehicleLots);
            currentLocation = captor.CurrentLocationId;
            status = "guarded";
        }
        else if (choiceKind == "leave-unguarded")
        {
            var ordinal = checked((priorSnapshot.CurrentPosition.SequenceContext.GameTurn - 1)
                * 3 + priorSnapshot.CurrentPosition.SequenceContext.OperationStage - 1 + 12);
            replacement = new(cut.LotId + ".replacement", lot.VictimElementId,
                lot.VictimComponentId, choice.Route[^1], lot.Quantity,
                ordinal / 3 + 1, ordinal % 3 + 1, true);
            currentLocation = null;
            status = "escaped";
        }
        else throw new InvalidOperationException("Unknown custody disposition.");

        if (!CampaignWorldV6Validator.IsValid(nextWorld, artifact, context.Scenario))
            throw new InvalidOperationException("Custody produced an invalid normal World.");
        return new(nextWorld, new(cut.LotId, lot.VictimElementId,
            lot.CaptorElementId, lot.Quantity, lot.OriginLocationId,
            currentLocation, status, guard, replacement));
    }
}
