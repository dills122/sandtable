using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>
/// Source-backed Land-only motorization pools and bounded pregame placement.
/// This overlay does not implement movement, optional loads, or completion of
/// all first-line assignments.
/// </summary>
internal sealed class CampaignSourceMotorizationWorld
{
    internal const string PolicyId = "graziani.initial-motorization.curated-sites.v1";
    internal const string GiarabubPolicyId = "graziani.initial-motorization.curated-sites.v2";
    private static readonly int[] AmountSteps = [1, 5, 10, 25, 50];
    private static readonly string[] CairoSites = ["e1730", "e1829", "e1830", "e1930", "e1931"];
    private static readonly string[] AlexandriaSites = ["e3613", "e3714"];
    private static readonly string[] CommonwealthMapSites = ["c4131", "d3714"];
    private static readonly string[] AxisLibyaSites = ["c4507", "c4707"];
    private static readonly string[] AxisLibyaGiarabubSites = ["c1014", "c4507", "c4707"];
    private readonly ReadOnlyCollection<CampaignSourceMotorPool> pools;
    private readonly ReadOnlyCollection<CampaignSourceMotorAllocation> allocations;

    private CampaignSourceMotorizationWorld(LandSide side, IEnumerable<CampaignSourceMotorPool> pools,
        IEnumerable<CampaignSourceMotorAllocation> allocations, bool giarabubPlacementAdmitted = false)
    {
        Side = side;
        GiarabubPlacementAdmitted = giarabubPlacementAdmitted;
        this.pools = Array.AsReadOnly(pools.OrderBy(value => value.Id, StringComparer.Ordinal).ToArray());
        this.allocations = Array.AsReadOnly(allocations.OrderBy(value => value.PoolId, StringComparer.Ordinal)
            .ThenBy(value => value.LocationId, StringComparer.Ordinal).ToArray());
    }

    internal LandSide Side { get; }
    internal bool GiarabubPlacementAdmitted { get; }
    internal IReadOnlyList<CampaignSourceMotorPool> Pools => pools;
    internal IReadOnlyList<CampaignSourceMotorAllocation> Allocations => allocations;

    internal static CampaignSourceMotorizationWorld Create(CampaignPregameSourceState state, LandSide side)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Source.Session.Content.Identity.Hash != CampaignPregameSourceCreation.SupportedContentHash
            || side is not (LandSide.Axis or LandSide.Commonwealth))
            throw new InvalidOperationException("Motorization setup requires the pinned Graziani source and a player side.");
        if (side == LandSide.Axis)
            return new(side,
                [new("axis-tripoli-unattached", 180, true, "box.tripoli", false, 25, 140, 40),
                    new("axis-libya-unattached", 125, false, "region.libya", false, 30, 100, 25),
                    new("axis-c4218-attached", 35, false, "c4218", true, 10, 25, 10),
                    new("axis-c4120-attached", 50, false, "c4120", true, null, 45, 5),
                    new("axis-c4020-attached", 15, false, "c4020", true, 10, 15, null),
                    new("axis-c3920-attached", 15, false, "c3920", true, 15, 15, null),
                    new("axis-c3919-attached", 10, false, "c3919", true, null, 5, 5),
                    new("axis-c3918-attached", 45, false, "c3918", true, null, 40, 5),
                    new("axis-c3617-attached", 35, false, "c3617", true, 5, 30, 5),
                    new("axis-c4707-attached", 35, false, "c4707", true, null, 30, 5),
                    new("axis-c4507-attached", 35, false, "c4507", true, 10, 25, 10),
                    new("axis-bardia-attached", 20, false, "c4321", true, null, 20, null),
                    new("axis-tobruk-attached", 5, false, "c4807", true, null, 5, null),
                    new("axis-barce-attached", 5, false, "b5504", true, null, 5, null),
                    new("axis-saharan-attached", 0, false, "region.c0716-within-3", true, 5, null, null)],
                [new("axis-tripoli-unattached", "box.tripoli", 180)]);
        return new(side,
            [new("cw-cairo-unattached", 50, false, "area.cairo", false, null, 40, 10),
                new("cw-alexandria-unattached", 20, false, "area.alexandria", false, 10, 20, null),
                new("cw-any-map-unattached", 45, false, "region.any-map", false, 15, 40, 5),
                new("cw-c4131-attached", 10, false, "c4131", true, null, 10, null),
                new("cw-c3926-attached", 2, false, "c3926", true, null, 2, null),
                new("cw-c3922-attached", 20, false, "c3922", true, null, 15, 5),
                new("cw-c3721-attached", 5, false, "c3721", true, 5, 5, null),
                new("cw-c3520-attached", 5, false, "c3520", true, null, 5, null),
                new("cw-c3320-attached", 10, false, "c3320", true, null, 10, null),
                new("cw-c3020-attached", 3, false, "c3020", true, null, 3, null),
                new("cw-matruh-attached", 20, false, "d3714", true, 5, 15, 5),
                new("cw-7-armored-attached", 10, false, "d3612", true, 5, 5, 5),
                new("cw-4-indian-attached", 22, false, "d3615", true, 5, 20, 2),
                new("cw-16-brigade-attached", 20, false, "e1829", true, null, 20, null),
                new("cw-alexandria-attached", 20, false, "area.alexandria", true, 10, 15, 5)], []);
    }

    internal IReadOnlyList<string> ValidDestinations(string poolId)
    {
        if (pools.All(value => value.Id != poolId)) return [];
        // These are source-known friendly positions, real supply sites or major
        // cities under §59.44; other legal positions are intentionally omitted.
        return poolId switch
        {
            "axis-libya-unattached" when Side == LandSide.Axis => Array.AsReadOnly(
                GiarabubPlacementAdmitted ? AxisLibyaGiarabubSites : AxisLibyaSites),
            "cw-cairo-unattached" when Side == LandSide.Commonwealth => Array.AsReadOnly(CairoSites),
            "cw-alexandria-unattached" when Side == LandSide.Commonwealth => Array.AsReadOnly(AlexandriaSites),
            "cw-any-map-unattached" when Side == LandSide.Commonwealth => Array.AsReadOnly(CommonwealthMapSites),
            // Attached pool eligibility, including §32.54 priorities, is not
            // modeled. Never imply the raw pool is already allocated.
            _ => [],
        };
    }

    internal int Remaining(string poolId)
    {
        var pool = pools.Single(value => value.Id == poolId);
        return pool.Points - allocations.Where(value => value.PoolId == poolId).Sum(value => value.Points);
    }

    internal IReadOnlyList<int> ValidDeltas(string poolId, string locationId)
    {
        if (!ValidDestinations(poolId).Contains(locationId, StringComparer.Ordinal)) return [];
        var existing = allocations.SingleOrDefault(value => value.PoolId == poolId && value.LocationId == locationId)?.Points ?? 0;
        var remaining = Remaining(poolId);
        return Array.AsReadOnly(AmountSteps.Where(amount => amount <= existing).Select(amount => -amount)
            .Concat(AmountSteps.Where(amount => amount <= remaining)).Order().ToArray());
    }

    internal CampaignSourceMotorizationWorld Assign(string poolId, string locationId, int delta)
    {
        if (!ValidDeltas(poolId, locationId).Contains(delta))
            throw new InvalidOperationException("Unavailable motorization pool, destination or amount.");
        var next = allocations.Where(value => value.PoolId != poolId || value.LocationId != locationId).ToList();
        var previous = allocations.SingleOrDefault(value => value.PoolId == poolId && value.LocationId == locationId)?.Points ?? 0;
        var quantity = checked(previous + delta);
        if (quantity > 0) next.Add(new(poolId, locationId, quantity));
        return new CampaignSourceMotorizationWorld(Side, pools, next, GiarabubPlacementAdmitted);
    }

    internal CampaignSourceMotorizationWorld AdmitGiarabubPlacement()
    {
        if (Side != LandSide.Axis || GiarabubPlacementAdmitted)
            throw new InvalidOperationException("Giarabub placement policy is unavailable.");
        return new CampaignSourceMotorizationWorld(Side, pools, allocations, true);
    }

    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("policyId", GiarabubPlacementAdmitted ? GiarabubPolicyId : PolicyId);
            writer.WriteString("side", CampaignSnapshotSerializer.FormatSide(Side));
            writer.WriteString("scenarioSourceHash", "sha256:fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66");
            writer.WriteString("mapSourceHash", "sha256:8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030");
            writer.WriteString("airRulesSourceHash", "sha256:9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4");
            writer.WriteString("landRulesSourceHash", "sha256:b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99");
            writer.WriteString("cairoE1931Status", "neighbor-and-summary-inference");
            writer.WriteBoolean("targetCoverageComplete", false);
            writer.WriteString("attachedEligibilityStatus", "unresolved-land-32.53-32.54");
            writer.WriteString("firstLinePoolStatus", "printed-rows-retained-eligibility-unresolved");
            writer.WriteString("unlistedTruckQuantitiesStatus", "not-inferred");
            writer.WriteString("optionalInitialLoadsStatus", "not-selected");
            writer.WriteString("airFacilityTruckPolicy", "excluded-under-59.63B");
            writer.WriteStartArray("allocationSteps");
            foreach (var amount in AmountSteps) writer.WriteNumberValue(amount);
            writer.WriteEndArray();
            writer.WriteStartArray("sources");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.ScenariosSourceId + ":60.31");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.ScenariosSourceId + ":60.33");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.ScenariosSourceId + ":60.41");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.ScenariosSourceId + ":60.43");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.AirLogisticsSourceId + ":59.42-59.44");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.AirLogisticsSourceId + ":59.63-59.66");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.LandRulesSourceId + ":32.53-32.54");
            writer.WriteEndArray();
            writer.WriteStartArray("pools");
            foreach (var pool in pools)
            {
                writer.WriteStartObject();
                writer.WriteString("id", pool.Id);
                writer.WriteNumber("total", pool.Points);
                writer.WriteBoolean("fixedPlacement", pool.FixedPlacement);
                writer.WriteString("sourceLocation", pool.SourceLocation);
                writer.WriteBoolean("attached", pool.Attached);
                if (pool.LightTrucks is { } light) writer.WriteNumber("lightTrucks", light);
                else writer.WriteNull("lightTrucks");
                if (pool.MediumTrucks is { } medium) writer.WriteNumber("mediumTrucks", medium);
                else writer.WriteNull("mediumTrucks");
                if (pool.HeavyTrucks is { } heavy) writer.WriteNumber("heavyTrucks", heavy);
                else writer.WriteNull("heavyTrucks");
                writer.WriteNumber("remaining", Remaining(pool.Id));
                writer.WriteStartArray("curatedTargets");
                foreach (var site in ValidDestinations(pool.Id)) writer.WriteStringValue(site);
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("allocations");
            foreach (var allocation in allocations)
            {
                writer.WriteStartObject();
                writer.WriteString("poolId", allocation.PoolId);
                writer.WriteString("locationId", allocation.LocationId);
                writer.WriteNumber("points", allocation.Points);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
}

internal sealed record CampaignSourceMotorPool(string Id, int Points, bool FixedPlacement,
    string SourceLocation, bool Attached, int? LightTrucks, int? MediumTrucks, int? HeavyTrucks);
internal sealed record CampaignSourceMotorAllocation(string PoolId, string LocationId, int Points);
