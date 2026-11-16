using System.Security.Cryptography;
using System.Text.Json;

namespace Cna.Core.Campaigns;

// Bounded first-stage Tripoli convoy. Unsupported lanes and attacks remain
// pending; this is not completion of the Naval Convoy Arrival Phase.
internal sealed class CampaignSourceGt1AxisArrival
{
    private CampaignSourceGt1AxisArrival(CampaignSourceAxisSupplyWorld supply, int lane2Units,
        bool hasOtherStageOneLanes, bool announced, bool delivered)
    {
        Supply = supply; Lane2Units = lane2Units; HasOtherStageOneLanes = hasOtherStageOneLanes;
        Announced = announced; Delivered = delivered;
    }

    internal CampaignSourceAxisSupplyWorld Supply { get; }
    internal int Lane2Units { get; }
    internal bool HasOtherStageOneLanes { get; }
    internal bool Announced { get; }
    internal bool Delivered { get; }
    internal bool CanAnnounce => Lane2Units > 0 && !HasOtherStageOneLanes && !Announced && !Delivered;

    internal static CampaignSourceGt1AxisArrival Create(CampaignSourceAuthorityState source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!source.OpeningConvoyPlan.IsFinished) throw new InvalidOperationException("GT1 convoy planning is unfinished.");
        var due = source.OpeningConvoyPlan.Allocations().Where(value => value.OperationStage == 1).ToArray();
        var tripoli = due.SingleOrDefault(value => value.Lane == 2);
        if (tripoli is not null && (tripoli.Origin != "West Sicily" || tripoli.Destination != "Tripoli" || tripoli.SupplyUnits is < 1 or > 3))
            throw new InvalidOperationException("GT1 lane 2 differs from its source-pinned route or capacity.");
        return new(source.AxisSupplyWorld, tripoli?.SupplyUnits ?? 0, due.Any(value => value.Lane != 2), false, false);
    }

    internal CampaignSourceGt1AxisArrival Announce()
    {
        if (!CanAnnounce) throw new InvalidOperationException("The bounded GT1 Tripoli convoy is unavailable.");
        // Scenario 60.31 starts Axis forces at Tripoli. Land 8.82 forbids
        // Commonwealth land/sea units from entering Tripoli/Tunisia boxes.
        // No land movement has occurred before GT1 Op1 Naval Arrival.
        return new(Supply, Lane2Units, false, true, false);
    }

    internal CampaignSourceGt1AxisArrival DeclineAttackAndDeliver()
    {
        if (!Announced || Delivered) throw new InvalidOperationException("The Tripoli attack choice is unavailable.");
        return new(Supply.ReceiveGt1TripoliSupply(Lane2Units), Lane2Units, false, true, true);
    }

    // Only the independently checked sole-lane-3 successor may call this.
    // Clearing the other-lanes flag cannot skip lane 2 or lane 6.
    internal CampaignSourceGt1AxisArrival DeliverSoleBenghaziLane(CampaignSourceAuthorityState source, int count)
    {
        ArgumentNullException.ThrowIfNull(source);
        var due = source.OpeningConvoyPlan.Allocations().Where(value => value.OperationStage == 1).ToArray();
        if (Lane2Units != 0 || !HasOtherStageOneLanes || Announced || Delivered)
            throw new InvalidOperationException("Benghazi delivery requires the sole pending stage-one lane.");
        if (!source.OpeningConvoyPlan.IsFinished || due.Length != 1 || due[0].Lane != 3
            || due[0].Origin != "Italy" || due[0].Destination != "Benghazi" || due[0].SupplyUnits != count)
            throw new InvalidOperationException("Benghazi delivery differs from the source convoy plan.");
        return new(Supply.ReceiveGt1BenghaziSupply(count), 0, false, false, false);
    }

    // The versioned multi-lane successor owns per-lane evidence and replaces
    // only the effective stock; the original sole-lane flags remain intact.
    internal CampaignSourceGt1AxisArrival WithMultiSupply(CampaignSourceAxisSupplyWorld supply)
    {
        ArgumentNullException.ThrowIfNull(supply);
        return new(supply, Lane2Units, HasOtherStageOneLanes, Announced, Delivered);
    }

    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 1);
            writer.WriteString("policyId", "graziani.gt1.op1.tripoli-convoy.v1");
            writer.WriteString("sources", "scenario:60.31,60.37;land:8.82,32.14-32.15,32.61-32.63;air:56.15,56.18");
            writer.WriteNumber("lane2SupplyUnits", Lane2Units); writer.WriteBoolean("otherStageOneLanesPending", HasOtherStageOneLanes);
            writer.WriteBoolean("announced", Announced); writer.WriteBoolean("delivered", Delivered);
            writer.WriteString("axisSupplyHash", Supply.Hash()); writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
}
