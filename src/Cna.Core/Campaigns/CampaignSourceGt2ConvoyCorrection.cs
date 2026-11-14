using System.Security.Cryptography;
using System.Text.Json;

namespace Cna.Core.Campaigns;

/// <summary>Replay-safe owner amendment of the retained GT2 supply plan.</summary>
internal sealed class CampaignSourceGt2ConvoyCorrection
{
    internal const string PolicyId = "graziani.gt2-supply-minimum-arrival.provisional.v1";
    private static readonly int[] Lanes = [2, 3, 6];
    private readonly int[] allocations;

    private CampaignSourceGt2ConvoyCorrection(string predecessorHash, int maximumSupply, int[] allocations, bool finished)
    {
        PredecessorHash = predecessorHash;
        MaximumSupply = maximumSupply;
        this.allocations = allocations.ToArray();
        IsFinished = finished;
    }

    internal string PredecessorHash { get; }
    internal int MaximumSupply { get; }
    internal bool IsFinished { get; }
    internal int AllocatedSupply => allocations.Sum();
    internal bool CanFinish => !IsFinished && (MaximumSupply == 0 || AllocatedSupply > 0);

    internal static CampaignSourceGt2ConvoyCorrection Create(CampaignSourceConvoyPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var values = new int[9];
        foreach (var item in plan.Allocations)
        {
            var lane = Array.IndexOf(Lanes, item.Lane);
            if (lane < 0 || item.OperationStage is < 1 or > 3 || item.SupplyUnits is < 1 or > 3)
                throw new InvalidOperationException("Retained GT2 supply allocation is unsupported.");
            var index = lane * 3 + item.OperationStage - 1;
            if (values[index] != 0) throw new InvalidOperationException("Duplicate GT2 supply lane and stage.");
            values[index] = item.SupplyUnits;
        }
        if (values.Sum() > plan.MaximumSupply) throw new InvalidOperationException("Retained GT2 supply exceeds availability.");
        return new(plan.Hash(), plan.MaximumSupply, values, false);
    }

    internal int Quantity(int lane, int stage)
    {
        var index = Index(lane, stage);
        return index < 0 ? 0 : allocations[index];
    }

    internal IReadOnlyList<int> ValidDeltas(int lane, int stage)
    {
        var index = Index(lane, stage);
        if (index < 0 || IsFinished) return [];
        var result = new List<int>(2);
        if (allocations[index] > 0) result.Add(-1);
        if (allocations[index] < 3 && AllocatedSupply < MaximumSupply) result.Add(1);
        return result.AsReadOnly();
    }

    internal CampaignSourceGt2ConvoyCorrection Adjust(int lane, int stage, int delta)
    {
        if (!ValidDeltas(lane, stage).Contains(delta)) throw new InvalidOperationException("GT2 supply amendment is unavailable.");
        var next = allocations.ToArray();
        next[Index(lane, stage)] += delta;
        return new(PredecessorHash, MaximumSupply, next, false);
    }

    internal CampaignSourceGt2ConvoyCorrection Finish()
    {
        if (!CanFinish) throw new InvalidOperationException("A positive GT2 supply roll requires at least one planned arrival.");
        return new(PredecessorHash, MaximumSupply, allocations, true);
    }

    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("policyId", PolicyId);
            writer.WriteString("policyStatus", "provisional");
            writer.WriteString("landRulesSourceHash", "sha256:b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99");
            writer.WriteString("scenarioSourceHash", "sha256:fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66");
            writer.WriteString("sources", "spi-1979-land-rules:32.43-32.44;spi-1979-land-rules:32.61-32.62;spi-1979-scenarios:60.37");
            writer.WriteString("interpretation", "positive-non-start-roll-requires-at-least-one-planned-supply-unit");
            writer.WriteString("predecessorPlanHash", PredecessorHash);
            writer.WriteNumber("targetGameTurn", 2);
            writer.WriteNumber("maximumSupply", MaximumSupply);
            writer.WriteNumber("unusedAvailability", MaximumSupply - AllocatedSupply);
            writer.WriteBoolean("finished", IsFinished);
            writer.WriteString("cargoCompleteness", "supply-only-other-cargo-unresolved");
            writer.WriteStartArray("allocations");
            foreach (var lane in Lanes)
                for (var stage = 1; stage <= 3; stage++)
                {
                    var count = Quantity(lane, stage);
                    if (count == 0) continue;
                    var route = CampaignPregameSourceCreation.LaneRoute(lane);
                    writer.WriteStartObject();
                    writer.WriteNumber("lane", lane);
                    writer.WriteNumber("operationStage", stage);
                    writer.WriteNumber("supplyUnits", count);
                    writer.WriteString("origin", route.Origin);
                    writer.WriteString("destination", route.Destination);
                    writer.WriteEndObject();
                }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
    private static int Index(int lane, int stage)
    {
        var laneIndex = Array.IndexOf(Lanes, lane);
        return laneIndex < 0 || stage is < 1 or > 3 ? -1 : laneIndex * 3 + stage - 1;
    }
}
