using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Randomness;

namespace Cna.Core.Campaigns;

// Provisional pre-start supply planning for the remainder of September.
// This does not decide replacements, move convoys, resolve attacks, or deliver cargo.
internal sealed class CampaignSourceSeptemberConvoyPlans
{
    internal const string PolicyId = "graziani.pregame.axis-september-gt3-gt4-supply.v1";
    private static readonly int[] Lanes = [2, 3, 6];
    private static readonly int[] SupplyByDie = [0, 1, 1, 2, 2, 3];
    private readonly IReadOnlyList<CampaignSourceSeptemberConvoyEntry> entries;
    private CampaignSourceSeptemberConvoyPlans(RandomStreamState random, IEnumerable<CampaignSourceSeptemberConvoyEntry> entries)
    {
        RandomState = random;
        this.entries = Array.AsReadOnly(entries.OrderBy(value => value.TargetGameTurn).ToArray());
    }
    internal RandomStreamState RandomState { get; }
    internal IReadOnlyList<CampaignSourceSeptemberConvoyEntry> Entries => entries;
    internal int? NextTargetGameTurn => entries.FirstOrDefault(value => !value.Finished)?.TargetGameTurn;
    internal static CampaignSourceSeptemberConvoyPlans Create(CampaignSourceCommonwealthSupplyPlans plans)
    {
        ArgumentNullException.ThrowIfNull(plans);
        if (plans.NextTargetGameTurn is not null) throw new InvalidOperationException("Complete the retained Commonwealth prestart rolls first.");
        return new(plans.RandomState, [new(3, null, null, false, Array.AsReadOnly(new int[9])), new(4, null, null, false, Array.AsReadOnly(new int[9]))]);
    }
    internal CampaignSourceSeptemberConvoyPlans Roll(int turn)
    {
        var entry = Current(turn);
        if (entry.Die is not null) throw new InvalidOperationException("Supply availability has already been rolled.");
        var result = SandtableRandom.RollD6(RandomState);
        return Replace(entry with { Die = result.Value, Maximum = SupplyByDie[result.Value - 1] }, result.State);
    }
    internal IReadOnlyList<int> ValidDeltas(int turn, int lane, int stage)
    {
        var laneIndex = Array.IndexOf(Lanes, lane);
        if (NextTargetGameTurn != turn || laneIndex < 0 || stage is < 1 or > 3) return [];
        var entry = entries.Single(value => value.TargetGameTurn == turn);
        if (entry.Die is null) return [];
        var current = entry.Allocations[laneIndex * 3 + stage - 1];
        var result = new List<int>();
        if (current > 0) result.Add(-1);
        if (current < 3 && entry.Allocations.Sum() < entry.Maximum!.Value) result.Add(1);
        return result.AsReadOnly();
    }
    internal CampaignSourceSeptemberConvoyPlans Adjust(int turn, int lane, int stage, int delta)
    {
        if (!ValidDeltas(turn, lane, stage).Contains(delta)) throw new InvalidOperationException("Unavailable September supply allocation.");
        var entry = Current(turn); var values = entry.Allocations.ToArray();
        values[Array.IndexOf(Lanes, lane) * 3 + stage - 1] += delta;
        return Replace(entry with { Allocations = Array.AsReadOnly(values) }, RandomState);
    }
    internal bool CanFinish(int turn)
    {
        if (NextTargetGameTurn != turn) return false;
        var entry = entries.Single(value => value.TargetGameTurn == turn);
        return entry.Die is not null;
    }
    internal CampaignSourceSeptemberConvoyPlans Finish(int turn)
    {
        if (!CanFinish(turn)) throw new InvalidOperationException("The plan needs an availability roll and explicit finish.");
        return Replace(Current(turn) with { Finished = true }, RandomState);
    }
    private CampaignSourceSeptemberConvoyEntry Current(int turn) => NextTargetGameTurn == turn
        ? entries.Single(value => value.TargetGameTurn == turn) : throw new InvalidOperationException("September supply planning must proceed in target-turn order.");
    private CampaignSourceSeptemberConvoyPlans Replace(CampaignSourceSeptemberConvoyEntry changed, RandomStreamState random) =>
        new(random, entries.Select(value => value.TargetGameTurn == changed.TargetGameTurn ? changed : value));
    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 1); writer.WriteString("policyId", PolicyId);
            writer.WriteString("status", "provisional-prestart-planning-not-delivery");
            writer.WriteString("rngOrder", "axis-gt2-axis-gt1-cw-gt1-to-gt4-axis-gt3-axis-gt4");
            writer.WriteString("planningMonth", "1940-september-b-provisional-prestart");
            writer.WriteString("positiveAvailabilityPolicy", "provisional-owner-may-explicitly-finish-with-zero-supply");
            writer.WriteString("otherCargoStatus", "unresolved-including-gt4-light-aa-replacement-eligibility");
            writer.WriteString("axisChartsHash", "sha256:9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca");
            writer.WriteString("airRulesHash", "sha256:9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4");
            writer.WriteString("landRulesHash", "sha256:b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99");
            writer.WriteString("scenarioHash", "sha256:fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66");
            writer.WriteString("sources", "scenario:60.37;land:32.43-32.44,32.61-32.62;axis:32.46,20.66;air:56.18");
            writer.WriteString("randomAlgorithm", RandomState.AlgorithmId); writer.WriteNumber("randomSeed", RandomState.Seed); writer.WriteNumber("randomCursor", RandomState.NextByteCursor);
            writer.WriteStartArray("plans");
            foreach (var entry in entries)
            {
                writer.WriteStartObject(); writer.WriteNumber("targetGameTurn", entry.TargetGameTurn);
                if (entry.Die is int die) writer.WriteNumber("die", die); else writer.WriteNull("die");
                if (entry.Maximum is int maximum) writer.WriteNumber("maximumSupply", maximum); else writer.WriteNull("maximumSupply");
                writer.WriteBoolean("finished", entry.Finished); writer.WriteStartArray("allocations");
                for (var i = 0; i < Lanes.Length; i++) for (var stage = 1; stage <= 3; stage++)
                {
                    var quantity = entry.Allocations[i * 3 + stage - 1]; if (quantity == 0) continue;
                    var route = CampaignPregameSourceCreation.LaneRoute(Lanes[i]);
                    writer.WriteStartObject(); writer.WriteNumber("lane", Lanes[i]); writer.WriteNumber("operationStage", stage);
                    writer.WriteNumber("supplyUnits", quantity); writer.WriteString("origin", route.Origin); writer.WriteString("destination", route.Destination); writer.WriteEndObject();
                }
                writer.WriteEndArray(); writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
}

internal sealed record CampaignSourceSeptemberConvoyEntry(int TargetGameTurn, int? Die, int? Maximum, bool Finished, IReadOnlyList<int> Allocations);
