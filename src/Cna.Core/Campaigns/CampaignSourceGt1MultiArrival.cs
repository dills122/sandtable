using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Campaigns;

// Land 32.63: announce all used lanes, then handle each convoy separately.
// Lane 6 is delivered only when its complete convoy fits Tobruk; overflow
// remains pending until its treatment is established.
internal sealed class CampaignSourceGt1MultiArrival
{
    private readonly IReadOnlyDictionary<int, int> quantities;
    private CampaignSourceGt1MultiArrival(IReadOnlyDictionary<int, int> quantities, string planHash,
        string supplyHash, int deliveredMask, bool lane6Declined = false)
    {
        this.quantities = quantities;
        PlanHash = planHash; SupplyHash = supplyHash; DeliveredMask = deliveredMask; Lane6Declined = lane6Declined;
    }

    internal string PlanHash { get; }
    internal string SupplyHash { get; }
    internal int DeliveredMask { get; }
    internal bool Lane6Declined { get; }
    internal IReadOnlyList<int> Lanes => quantities.Keys.Order().ToArray();
    internal bool IsDue(int lane) => quantities.ContainsKey(lane);
    internal int Quantity(int lane) => quantities.GetValueOrDefault(lane);
    internal bool IsDelivered(int lane) => (DeliveredMask & (1 << lane)) != 0;
    // The Commonwealth cut never depends on hidden cargo quantity or its
    // capacity outcome. The owner sees only its own decision status.
    internal (bool CanDecline, bool AttackDeclined) Lane6CommonwealthCut() => (CanDecline(6), Lane6Declined);
    internal bool CanDecline(int lane) => lane is 2 or 3 or 6 && IsDue(lane) && !IsDelivered(lane)
        && (lane != 6 || !Lane6Declined);

    internal static CampaignSourceGt1MultiArrival Announce(CampaignSourceAuthorityState source, CampaignSourceGt1AxisArrival axis)
    {
        ArgumentNullException.ThrowIfNull(source); ArgumentNullException.ThrowIfNull(axis);
        if (source.Content.Identity.Hash != CampaignPregameSourceCreation.SupportedContentHash
            || !source.OpeningConvoyPlan.IsFinished || axis.Announced || axis.Delivered)
            throw new InvalidOperationException("Multi-lane arrival requires an unchanged source plan.");
        var due = source.OpeningConvoyPlan.Allocations().Where(value => value.OperationStage == 1).ToArray();
        if ((due.Length < 2 && !(due.Length == 1 && due[0].Lane == 6)) || due.Any(value => value.Lane is not (2 or 3 or 6) || value.SupplyUnits is < 1 or > 3
            || CampaignPregameSourceCreation.LaneRoute(value.Lane) != (value.Origin, value.Destination)))
            throw new InvalidOperationException("The announced lanes differ from the source plan.");
        return new(due.ToDictionary(value => value.Lane, value => value.SupplyUnits),
            source.OpeningConvoyPlan.Hash(), axis.Supply.Hash(), 0);
    }

    internal (CampaignSourceGt1MultiArrival Arrival, CampaignSourceGt1AxisArrival Axis) DeclineAndDeliver(
        CampaignSourceAuthorityState source, CampaignSourceGt1AxisArrival axis, int lane)
    {
        if (source.OpeningConvoyPlan.Hash() != PlanHash || axis.Supply.Hash() != SupplyHash || !CanDecline(lane))
            throw new InvalidOperationException("Multi-lane choice differs from the retained plan or supply.");
        // Only complete deliveries that fit the city stack limit are supported.
        // Overflow treatment is unresolved, so retain the entire unadjudicated
        // convoy pending after the Commonwealth's choice. The opposing side's
        // projection does not disclose the hidden capacity outcome.
        var canDeliver = lane != 6 || axis.Supply.CanReceiveGt1TobrukSupply(Quantity(lane));
        var stock = !canDeliver ? axis.Supply : lane switch
        {
            2 => axis.Supply.ReceiveGt1TripoliSupply(Quantity(lane)),
            3 => axis.Supply.ReceiveGt1BenghaziSupply(Quantity(lane)),
            6 => axis.Supply.ReceiveGt1TobrukSupply(Quantity(lane)),
            _ => throw new InvalidOperationException("Unsupported convoy lane."),
        };
        var nextAxis = axis.WithMultiSupply(stock);
        return (new(quantities, PlanHash, stock.Hash(), canDeliver ? DeliveredMask | (1 << lane) : DeliveredMask,
            Lane6Declined || lane == 6), nextAxis);
    }

    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 1);
            writer.WriteString("policyId", "graziani.gt1.op1.multi-convoy.v1");
            writer.WriteString("sources", "land:32.14-32.15,32.61-32.63;scenario:60.31,60.37;air:56.15,56.18");
            writer.WriteString("benghaziRuling", IsDue(3) ? "rul-gra-05-a4827-provisional-scenario-b4827" : null);
            writer.WriteString("planHash", PlanHash); writer.WriteString("supplyHash", SupplyHash);
            if (Lane6Declined) writer.WriteBoolean("lane6Declined", true);
            writer.WriteStartArray("lanes");
            foreach (var lane in Lanes)
            {
                writer.WriteStartObject(); writer.WriteNumber("lane", lane); writer.WriteNumber("supplyUnits", Quantity(lane));
                writer.WriteBoolean("delivered", IsDelivered(lane)); writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
}
