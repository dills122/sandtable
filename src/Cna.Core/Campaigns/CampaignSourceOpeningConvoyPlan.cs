using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Randomness;

namespace Cna.Core.Campaigns;

/// <summary>
/// Source-backed, provisional GT1 supply planning in the Axis setup. This is
/// a plan only: it neither resolves naval movement nor delivers supply.
/// </summary>
internal sealed class CampaignSourceOpeningConvoyPlan
{
    internal const string PolicyId = "graziani.pregame.gt1-supply.sep-i.maximum.after-gt2-draw.v1";
    private static readonly int[] Lanes = [2, 3, 6];
    private static readonly int[] SupplyByDie = [0, 1, 1, 2, 2, 3];
    private readonly int[] allocations;

    private CampaignSourceOpeningConvoyPlan(RandomStreamState random, int? die, int? maximum,
        bool finished, int[] allocations)
    {
        RandomState = random;
        Die = die;
        MaximumSupply = maximum;
        IsFinished = finished;
        this.allocations = allocations.ToArray();
    }

    internal RandomStreamState RandomState { get; }
    internal int? Die { get; }
    internal int? MaximumSupply { get; }
    internal bool IsFinished { get; }

    // Authority must replay the GT2 roll first. The original v3 identity says
    // that roll was the first recorded pregame draw; changing that order would
    // reinterpret an existing save, so this provisional policy follows it.
    internal static CampaignSourceOpeningConvoyPlan Create(CampaignPregameSourceState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Die is null || state.MaximumSupply is null)
            throw new InvalidOperationException("The retained GT2 availability roll must precede the GT1 plan.");
        return new(state.RandomState, null, null, false, new int[9]);
    }

    internal CampaignSourceOpeningConvoyPlan Roll()
    {
        if (Die is not null || IsFinished)
            throw new InvalidOperationException("GT1 supply availability has already been rolled.");
        var result = SandtableRandom.RollD6(RandomState);
        return new(result.State, result.Value, SupplyByDie[result.Value - 1], false, allocations);
    }

    internal IReadOnlyList<int> ValidDeltas(int lane, int stage)
    {
        var laneIndex = Array.IndexOf(Lanes, lane);
        if (laneIndex < 0 || stage is < 1 or > 3 || Die is null || IsFinished) return [];
        var current = allocations[laneIndex * 3 + stage - 1];
        var result = new List<int>(2);
        if (current > 0) result.Add(-1);
        if (current < 3 && allocations.Sum() < MaximumSupply!.Value) result.Add(1);
        return result.AsReadOnly();
    }

    internal CampaignSourceOpeningConvoyPlan Adjust(int lane, int stage, int delta)
    {
        if (!ValidDeltas(lane, stage).Contains(delta))
            throw new InvalidOperationException("Unavailable GT1 supply allocation.");
        var updated = allocations.ToArray();
        updated[Array.IndexOf(Lanes, lane) * 3 + stage - 1] += delta;
        return new(RandomState, Die, MaximumSupply, false, updated);
    }

    internal CampaignSourceOpeningConvoyPlan Finish()
    {
        if (Die is null || IsFinished)
            throw new InvalidOperationException("GT1 supply availability must be rolled before finishing the plan.");
        return new(RandomState, Die, MaximumSupply, true, allocations);
    }

    internal IEnumerable<CampaignSourceConvoyAllocation> Allocations()
    {
        for (var laneIndex = 0; laneIndex < Lanes.Length; laneIndex++)
            for (var stage = 1; stage <= 3; stage++)
            {
                var quantity = allocations[laneIndex * 3 + stage - 1];
                if (quantity == 0) continue;
                var lane = Lanes[laneIndex];
                var route = CampaignPregameSourceCreation.LaneRoute(lane);
                yield return new(lane, stage, quantity, route.Origin, route.Destination);
            }
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
            writer.WriteString("axisChartsSourceHash", "sha256:9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca");
            writer.WriteString("airRulesSourceHash", "sha256:9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4");
            writer.WriteString("landRulesSourceHash", "sha256:b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99");
            writer.WriteString("scenarioSourceHash", "sha256:fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66");
            writer.WriteString("planningPeriod", "1940-september-i");
            writer.WriteString("rngOrder", "gt2-then-gt1");
            writer.WriteString("startingTurnRule", "exception-allows-but-does-not-require-arrival");
            writer.WriteNumber("targetGameTurn", 1);
            writer.WriteString("randomAlgorithm", RandomState.AlgorithmId);
            writer.WriteNumber("randomSeed", RandomState.Seed);
            writer.WriteNumber("randomCursor", RandomState.NextByteCursor);
            if (Die is { } die) writer.WriteNumber("die", die);
            else writer.WriteNull("die");
            if (MaximumSupply is { } maximum) writer.WriteNumber("maximumSupply", maximum);
            else writer.WriteNull("maximumSupply");
            writer.WriteBoolean("finished", IsFinished);
            writer.WriteStartArray("sources");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.LandRulesSourceId + ":32.43-32.44");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.LandRulesSourceId + ":32.61-32.62");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.AxisChartsSourceId + ":32.46-56.4");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.ScenariosSourceId + ":60.37");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.AirLogisticsSourceId + ":56.18");
            writer.WriteEndArray();
            writer.WriteStartArray("allocations");
            foreach (var value in Allocations())
            {
                writer.WriteStartObject();
                writer.WriteNumber("lane", value.Lane);
                writer.WriteNumber("operationStage", value.OperationStage);
                writer.WriteNumber("supplyUnits", value.SupplyUnits);
                writer.WriteString("origin", value.Origin);
                writer.WriteString("destination", value.Destination);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
}
