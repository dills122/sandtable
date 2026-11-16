using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Randomness;

namespace Cna.Core.Campaigns;

// GT1 September III plans supply for GT3 October I. This records only
// availability, not lane assignment, other cargo, movement or delivery.
internal sealed class CampaignSourceGt1SupplyPlanning
{
    private static readonly int[] SupplyByDie = [0, 1, 1, 2, 2, 3];
    private static readonly int[] Lanes = [2, 3, 6];
    private readonly int[] allocations;

    private CampaignSourceGt1SupplyPlanning(RandomStreamState random, int? die, int? maximum,
        string? carriedPlanHash, bool finished, IEnumerable<int> allocations)
    {
        RandomState = random;
        Die = die;
        Maximum = maximum;
        CarriedPlanHash = carriedPlanHash;
        Finished = finished;
        this.allocations = allocations.ToArray();
        if (this.allocations.Length != 9 || this.allocations.Any(value => value is < 0 or > 3)
            || (maximum is null && this.allocations.Any(value => value != 0))
            || (maximum is int limit && this.allocations.Sum() > limit))
            throw new ArgumentException("Invalid GT3 supply allocations.", nameof(allocations));
    }

    internal RandomStreamState RandomState { get; }
    internal int? Die { get; }
    internal int? Maximum { get; }
    internal string? CarriedPlanHash { get; }
    internal bool Finished { get; }
    internal bool CarriedPlanFinished => CarriedPlanHash is not null && Finished;
    internal int Allocated => allocations.Sum();
    internal int QuantityAt(int lane, int stage)
    {
        var index = Array.IndexOf(Lanes, lane);
        if (index < 0 || stage is < 1 or > 3) throw new ArgumentOutOfRangeException(nameof(lane));
        return allocations[index * 3 + stage - 1];
    }

    internal static CampaignSourceGt1SupplyPlanning Create(CampaignSourceAuthorityState source, RandomStreamState random)
    {
        ArgumentNullException.ThrowIfNull(source); ArgumentNullException.ThrowIfNull(random);
        if (source.RandomState != random) throw new InvalidOperationException("GT1 planning must retain setup RNG.");
        if (source.CommonwealthSupplyPlans is null || source.CommonwealthSupplyPlans.NextTargetGameTurn is not null)
            throw new InvalidOperationException("GT1 planning requires completed Commonwealth prestart supply rolls.");
        var plans = source.SeptemberPlans;
        var plan = plans?.Entries.Single(value => value.TargetGameTurn == 3);
        return new(random, plan?.Die, plan?.Maximum, plans?.Hash(), plan?.Finished ?? false, plan?.Allocations ?? new int[9]);
    }

    internal CampaignSourceGt1SupplyPlanning Roll()
    {
        if (Die is not null || Finished) throw new InvalidOperationException("GT3 supply was already rolled.");
        var result = SandtableRandom.RollD6(RandomState);
        return new(result.State, result.Value, SupplyByDie[result.Value - 1], CarriedPlanHash, false, allocations);
    }

    internal CampaignSourceGt1SupplyPlanning WithRandomState(RandomStreamState random)
    {
        ArgumentNullException.ThrowIfNull(random);
        if (random.AlgorithmId != RandomState.AlgorithmId || random.Seed != RandomState.Seed
            || random.ContractVersion != RandomState.ContractVersion || random.NextByteCursor < RandomState.NextByteCursor)
            throw new InvalidOperationException("GT3 plan cannot replace or rewind its random stream.");
        return new(random, Die, Maximum, CarriedPlanHash, Finished, allocations);
    }

    internal IReadOnlyList<int> ValidDeltas(int lane, int stage)
    {
        var index = Array.IndexOf(Lanes, lane);
        if (index < 0 || stage is < 1 or > 3 || Die is null || Finished) return [];
        var value = allocations[index * 3 + stage - 1];
        var result = new List<int>(2);
        if (value > 0) result.Add(-1);
        if (value < 3 && allocations.Sum() < Maximum!.Value) result.Add(1);
        return result.AsReadOnly();
    }

    internal CampaignSourceGt1SupplyPlanning Adjust(int lane, int stage, int delta)
    {
        if (!ValidDeltas(lane, stage).Contains(delta)) throw new InvalidOperationException("GT3 supply allocation is unavailable.");
        var values = allocations.ToArray();
        values[Array.IndexOf(Lanes, lane) * 3 + stage - 1] += delta;
        return new(RandomState, Die, Maximum, CarriedPlanHash, false, values);
    }

    internal CampaignSourceGt1SupplyPlanning Finish()
    {
        if (Die is null || Finished) throw new InvalidOperationException("GT3 supply planning cannot be finished.");
        return new(RandomState, Die, Maximum, CarriedPlanHash, true, allocations);
    }

    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 1);
            writer.WriteString("policyId", "graziani.gt1.axis-gt3-supply-availability.v1");
            writer.WriteNumber("planningGameTurn", 1); writer.WriteNumber("targetGameTurn", 3);
            writer.WriteString("planningMonth", "1940-september");
            writer.WriteString("availabilityTable", "axis-supply-b");
            writer.WriteString("sources", "land:32.43-32.44;axis:32.46,56.4");
            writer.WriteString("status", "supply-only-not-complete-convoy-schedule");
            writer.WriteString("carriedPrestartPlanHash", CarriedPlanHash);
            writer.WriteBoolean("finished", Finished);
            if (Die is int die) writer.WriteNumber("die", die); else writer.WriteNull("die");
            if (Maximum is int maximum) writer.WriteNumber("maximumSupply", maximum); else writer.WriteNull("maximumSupply");
            writer.WriteString("unallocatedPolicy", "provisional-owner-may-explicitly-finish-with-unused-supply");
            writer.WriteStartArray("allocations");
            for (var i = 0; i < Lanes.Length; i++)
                for (var stage = 1; stage <= 3; stage++)
                {
                    var quantity = allocations[i * 3 + stage - 1];
                    if (quantity == 0) continue;
                    var route = CampaignPregameSourceCreation.LaneRoute(Lanes[i]);
                    writer.WriteStartObject(); writer.WriteNumber("lane", Lanes[i]); writer.WriteNumber("operationStage", stage);
                    writer.WriteNumber("supplyUnits", quantity); writer.WriteString("origin", route.Origin);
                    writer.WriteString("destination", route.Destination); writer.WriteEndObject();
                }
            writer.WriteEndArray();
            writer.WriteString("randomAlgorithm", RandomState.AlgorithmId);
            writer.WriteNumber("randomSeed", RandomState.Seed); writer.WriteNumber("randomCursor", RandomState.NextByteCursor);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
}
