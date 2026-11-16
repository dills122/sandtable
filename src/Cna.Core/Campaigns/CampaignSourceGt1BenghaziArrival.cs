using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Campaigns;

// A sole lane-3 GT1 Op1 convoy. Mixed-lane arrivals remain pending; this
// does not claim support for convoy attack or general harbor operations.
internal sealed class CampaignSourceGt1BenghaziArrival
{
    private CampaignSourceGt1BenghaziArrival(int units, bool hasOtherStageOneLanes,
        string planHash, string supplyHash, bool announced, bool delivered)
    {
        Units = units; HasOtherStageOneLanes = hasOtherStageOneLanes; PlanHash = planHash;
        SupplyHash = supplyHash; Announced = announced; Delivered = delivered;
    }

    internal int Units { get; }
    internal bool HasOtherStageOneLanes { get; }
    internal string PlanHash { get; }
    internal string SupplyHash { get; }
    internal bool Announced { get; }
    internal bool Delivered { get; }
    internal bool CanAnnounce => Units > 0 && !HasOtherStageOneLanes && !Announced && !Delivered;

    internal static CampaignSourceGt1BenghaziArrival Create(CampaignSourceAuthorityState source,
        CampaignSourceGt1AxisArrival axisArrival)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(axisArrival);
        if (source.Content.Identity.Hash != CampaignPregameSourceCreation.SupportedContentHash
            || !source.OpeningConvoyPlan.IsFinished)
            throw new InvalidOperationException("Benghazi arrival requires the pinned source and completed convoy plan.");
        var due = source.OpeningConvoyPlan.Allocations().Where(value => value.OperationStage == 1).ToArray();
        var benghazi = due.SingleOrDefault(value => value.Lane == 3);
        if (benghazi is not null && (benghazi.Origin != "Italy" || benghazi.Destination != "Benghazi"
            || benghazi.SupplyUnits is < 1 or > 3))
            throw new InvalidOperationException("GT1 lane 3 differs from the source route or capacity.");
        return new(benghazi?.SupplyUnits ?? 0, due.Any(value => value.Lane != 3),
            source.OpeningConvoyPlan.Hash(), axisArrival.Supply.Hash(), false, false);
    }

    internal CampaignSourceGt1BenghaziArrival Announce(CampaignSourceAuthorityState source,
        CampaignSourceGt1AxisArrival axisArrival)
    {
        RequireAnchor(source, axisArrival);
        if (!CanAnnounce) throw new InvalidOperationException("Sole Benghazi lane is unavailable.");
        // Scenario 60.31 starts an Axis garrison at Benghazi; 60.7 gives
        // all ports except Tobruk their listed efficiency. No land move has
        // occurred before GT1 Op1 arrival. The provisional coordinate ruling
        // is disclosed in this successor and the delivery stock ledger.
        return new(Units, false, PlanHash, SupplyHash, true, false);
    }

    internal (CampaignSourceGt1BenghaziArrival Arrival, CampaignSourceGt1AxisArrival AxisArrival)
        DeclineAttackAndDeliver(CampaignSourceAuthorityState source, CampaignSourceGt1AxisArrival axisArrival)
    {
        RequireAnchor(source, axisArrival);
        if (!Announced || Delivered) throw new InvalidOperationException("Benghazi attack choice is unavailable.");
        var nextAxis = axisArrival.DeliverSoleBenghaziLane(source, Units);
        return (new(Units, false, PlanHash, nextAxis.Supply.Hash(), true, true), nextAxis);
    }

    private void RequireAnchor(CampaignSourceAuthorityState source, CampaignSourceGt1AxisArrival axisArrival)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(axisArrival);
        if (source.Content.Identity.Hash != CampaignPregameSourceCreation.SupportedContentHash
            || source.OpeningConvoyPlan.Hash() != PlanHash || axisArrival.Supply.Hash() != SupplyHash)
            throw new InvalidOperationException("Benghazi convoy anchor differs from its retained source.");
    }

    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 1);
            writer.WriteString("policyId", "graziani.gt1.op1.benghazi-convoy.rul-gra-05-provisional.v1");
            writer.WriteString("sources", "scenario:60.31,60.37,60.7,60.92b;land:32.14-32.15,32.61-32.63;air:37.42,important-locations-p26");
            writer.WriteString("coordinateRuling", "scenario-b4827-map-a4827-provisional");
            writer.WriteNumber("lane3SupplyUnits", Units); writer.WriteBoolean("otherStageOneLanesPending", HasOtherStageOneLanes);
            writer.WriteBoolean("announced", Announced); writer.WriteBoolean("delivered", Delivered);
            writer.WriteString("openingPlanHash", PlanHash); writer.WriteString("axisSupplyHash", SupplyHash); writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
}
