using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>Optional §59.66C regular supply units carried by initial unattached points.</summary>
internal sealed class CampaignSourceUnattachedCargo
{
    internal const string PolicyId = "graziani.initial-unattached-cargo.v1";
    private readonly SortedDictionary<string, int> loads;
    private CampaignSourceUnattachedCargo(LandSide side, IEnumerable<KeyValuePair<string, int>> loads, bool finished)
    { Side = side; this.loads = new SortedDictionary<string, int>(loads.ToDictionary(x => x.Key, x => x.Value), StringComparer.Ordinal); IsFinished = finished; }
    internal LandSide Side { get; }
    internal bool IsFinished { get; }
    internal IReadOnlyDictionary<string, int> Loads => loads;
    internal static CampaignSourceUnattachedCargo Create(CampaignSourceAuthorityState state, LandSide side)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (side is not (LandSide.Axis or LandSide.Commonwealth)) throw new InvalidOperationException("Cargo requires a player side.");
        var motor = side == LandSide.Axis ? state.AxisMotorization : state.CommonwealthMotorization;
        if (motor.Pools.Any(pool => !pool.Attached && motor.Remaining(pool.Id) != 0)
            || (side == LandSide.Axis && (state.AxisSupplyWorld.PendingDumpIds.Count != 0 || state.AxisSupplyWorld.PendingDummyIds.Count != 0))
            || (side == LandSide.Commonwealth && (state.CommonwealthSupplyWorld.PendingPlacementIds.Count != 0 || state.CommonwealthSupplyWorld.DummyDecision is null)))
            throw new InvalidOperationException("Finish the side's source supply and unattached motor placements first.");
        return new(side, [], false);
    }
    internal int Quantity(string location) => loads.TryGetValue(location, out var count) ? count : 0;
    internal IReadOnlyList<string> Locations(CampaignSourceAuthorityState state) => Array.AsReadOnly(Motor(state).Allocations
        .Where(value => !Motor(state).Pools.Single(pool => pool.Id == value.PoolId).Attached)
        .Select(value => value.LocationId).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray());
    internal int AvailablePoints(CampaignSourceAuthorityState state, string location) => Motor(state).Allocations
        .Where(value => value.LocationId == location && !Motor(state).Pools.Single(pool => pool.Id == value.PoolId).Attached)
        .Sum(value => value.Points);
    internal IReadOnlyList<int> ValidDeltas(CampaignSourceAuthorityState state, string location)
    {
        Validate(state);
        if (IsFinished || !Locations(state).Contains(location, StringComparer.Ordinal)) return [];
        var current = Quantity(location); var result = new List<int>();
        if (current > 0) result.Add(-1);
        if (current < Capacity(state, location)) result.Add(1);
        return result.AsReadOnly();
    }
    internal CampaignSourceUnattachedCargo Adjust(CampaignSourceAuthorityState state, string location, int delta)
    {
        if (!ValidDeltas(state, location).Contains(delta)) throw new InvalidOperationException("Initial cargo choice is unavailable.");
        var next = loads.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
        var value = checked(Quantity(location) + delta);
        if (value == 0) next.Remove(location); else next[location] = value;
        return new(Side, next, false);
    }
    internal CampaignSourceUnattachedCargo Finish(CampaignSourceAuthorityState state)
    {
        Validate(state);
        if (IsFinished) throw new InvalidOperationException("Initial cargo already finalized.");
        return new(Side, loads, true);
    }
    internal void Validate(CampaignSourceAuthorityState state)
    {
        var motor = Motor(state);
        if (motor.Pools.Any(pool => !pool.Attached && motor.Remaining(pool.Id) != 0)
            || loads.Any(item => item.Value < 1 || item.Value > Capacity(state, item.Key)))
            throw new InvalidOperationException("Initial cargo exceeds source points or supply stacking.");
    }
    private CampaignSourceMotorizationWorld Motor(CampaignSourceAuthorityState state) => Side == LandSide.Axis ? state.AxisMotorization : state.CommonwealthMotorization;
    private int Capacity(CampaignSourceAuthorityState state, string location)
    {
        var points = AvailablePoints(state, location) / 30;
        if (location is "box.tripoli" or "box.tunisia") return points;
        var existing = (Side == LandSide.Axis ? state.AxisSupplyWorld.Units : state.CommonwealthSupplyWorld.Units)
            .Count(unit => unit.LocationId == location);
        var majorCity = location is "e1730" or "e1829" or "e1830" or "e1930" or "e1931" or "e3613" or "e3714";
        return Math.Max(0, Math.Min(points, (majorCity ? 5 : 3) - existing));
    }
    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("policyId", PolicyId); writer.WriteString("side", CampaignSnapshotSerializer.FormatSide(Side));
            writer.WriteString("scenarioHash", "sha256:fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66");
            writer.WriteString("landHash", "sha256:b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99");
            writer.WriteString("rules", "59.63B-59.66C;32.12-32.14-32.32"); writer.WriteNumber("pointsPerUnit", 30);
            writer.WriteString("stackPolicy", "provisional-loaded-units-count-including-other-map-hex-cap3");
            writer.WriteString("boxPolicy", "tripoli-tunisia-count-as-hex-unlimited-supply-stack");
            writer.WriteBoolean("siteCoverageComplete", false); writer.WriteBoolean("scenarioSetupComplete", false);
            writer.WriteBoolean("finished", IsFinished); writer.WriteStartArray("loads");
            foreach (var (location, count) in loads)
            { writer.WriteStartObject(); writer.WriteString("locationId", location); writer.WriteNumber("regularSupplyUnits", count); writer.WriteNumber("ammoEach", 40); writer.WriteNumber("fuelEach", 60); writer.WriteEndObject(); }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
    internal static string InitialHash(LandSide side) => new CampaignSourceUnattachedCargo(side, [], false).Hash();
}
