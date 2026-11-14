using System.Security.Cryptography;
using System.Text.Json;

namespace Cna.Core.Campaigns;

internal sealed record CampaignSourceCwPriorityCatalogTarget(string ElementId, string LocationId,
    string PoolId, int InfantryToe, string SourceCode);

/// <summary>Versioned source catalog sharing retained first-line pools and load reservations.
/// Only the French assignment is stored here; prior target allocations remain in v7.</summary>
internal sealed class CampaignSourceCwPriorityCatalog
{
    internal const string PolicyId = "graziani.cw-priority-catalog.v1";
    internal const string PinnedInitialHash = "sha256:8d09aa81dba5e776e57d3c4784f7daec5d03172316260db5de72de0d156154cf";
    private static readonly IReadOnlyList<CampaignSourceCwPriorityCatalogTarget> TargetData = Array.AsReadOnly(new[]
    {
        new CampaignSourceCwPriorityCatalogTarget("1-french-motor-marines", "c3926", "cw-c3926-attached", 1, "v"),
        new CampaignSourceCwPriorityCatalogTarget("1-kings-royal-rifle-corps", "c3922", "cw-c3922-attached", 6, "n"),
        new CampaignSourceCwPriorityCatalogTarget("2-rifle-brigade", "c3320", "cw-c3320-attached", 6, "n"),
    });
    private CampaignSourceCwPriorityCatalog(int frenchPoints) => FrenchPoints = frenchPoints;
    internal static IReadOnlyList<CampaignSourceCwPriorityCatalogTarget> Targets => TargetData;
    internal int FrenchPoints { get; }

    internal static CampaignSourceCwPriorityCatalog Create(CampaignSourceAuthorityState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var initial = new CampaignSourceCwPriorityCatalog(0);
        initial.Validate(state);
        if (initial.Hash() != PinnedInitialHash)
            throw new InvalidOperationException("CW priority catalog differs from pinned source profile.");
        return initial;
    }
    internal int AssignedPoints(CampaignSourceAuthorityState state, string elementId)
    {
        var priority = state.PriorityMotorization ?? throw new InvalidOperationException("Retained priority state missing.");
        return elementId == "1-french-motor-marines" ? FrenchPoints : priority.AssignedPoints(elementId);
    }
    internal int Remaining(CampaignSourceAuthorityState state, string poolId)
    {
        var priority = state.PriorityMotorization ?? throw new InvalidOperationException("Retained priority state missing.");
        var loads = state.AttachedInitialLoads ?? throw new InvalidOperationException("Retained source loads missing.");
        var total = state.CommonwealthMotorization.Remaining(poolId);
        var legacy = CampaignSourcePriorityMotorization.Targets.Where(value => value.PoolId == poolId)
            .Sum(value => priority.AssignedPoints(value.ElementId));
        var newPoints = poolId == "cw-c3926-attached" ? FrenchPoints : 0;
        var result = checked(total - legacy - loads.Reserved(poolId) - newPoints);
        if (result < 0) throw new InvalidOperationException("Source pool is oversubscribed.");
        return result;
    }
    internal IReadOnlyList<int> ValidDeltas(CampaignSourceAuthorityState state, string elementId)
    {
        Validate(state);
        var target = TargetData.SingleOrDefault(value => value.ElementId == elementId);
        if (target is null || !state.World.World.Elements.Any(value => value.ElementId == elementId
            && value.CurrentLocationId == target.LocationId)) return [];
        if (elementId == "1-french-motor-marines"
            && state.Battlefield?.Entries.Any(value => value.ElementId == elementId && value.SideId == "commonwealth"
                && value.LocationId == target.LocationId && value.AuthorityStatus == "source-cw-priority-active") != true) return [];
        if (elementId != "1-french-motor-marines")
        {
            var priority = state.PriorityMotorization!; var loads = state.AttachedInitialLoads!;
            return Array.AsReadOnly(priority.ValidDeltas(elementId, state.CommonwealthMotorization)
                .Where(delta => loads.CanApplyPriorityDelta(elementId, delta, priority, state.CommonwealthMotorization)).ToArray());
        }
        return FrenchPoints == 0 && Remaining(state, target.PoolId) > 0 ? [1]
            : FrenchPoints == 1 ? [-1] : [];
    }
    internal (CampaignSourceCwPriorityCatalog Catalog, CampaignSourcePriorityMotorization Priority) Adjust(
        CampaignSourceAuthorityState state, string elementId, int delta)
    {
        if (!ValidDeltas(state, elementId).Contains(delta))
            throw new InvalidOperationException("Source priority assignment is unavailable.");
        var priority = state.PriorityMotorization!;
        return elementId == "1-french-motor-marines"
            ? (new CampaignSourceCwPriorityCatalog(checked(FrenchPoints + delta)), priority)
            : (this, priority.Adjust(elementId, delta, state.CommonwealthMotorization));
    }
    internal void Validate(CampaignSourceAuthorityState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var supplement = state.CommonwealthSupplement ?? throw new InvalidOperationException("Commonwealth source supplement missing.");
        var priority = state.PriorityMotorization ?? throw new InvalidOperationException("Retained priority state missing.");
        _ = state.AttachedInitialLoads ?? throw new InvalidOperationException("Retained source loads missing.");
        var french = supplement.Forces.SingleOrDefault(value => value.ElementId == "1-french-motor-marines");
        var pool = state.CommonwealthMotorization.Pools.SingleOrDefault(value => value.Id == "cw-c3926-attached");
        if (supplement.Hash() != CampaignSourceCommonwealthSupplementalForces.ContentHash
            || priority.SupplementHash != supplement.Hash() || french is null || french.LocationId != "c3926"
            || french.InitialToes is null || french.InitialToes.Count != 1
            || french.InitialToes[0].ComponentId != "1-french-motor-marines.infantry" || french.InitialToes[0].Toe != 1
            || pool is null || !pool.Attached || pool.Points != 2 || FrenchPoints is < 0 or > 1
            || FrenchPoints != 0 && !state.World.World.Elements.Any(value => value.ElementId == "1-french-motor-marines"
                && value.CurrentLocationId == "c3926"))
            throw new InvalidOperationException("Source priority catalog differs from retained source facts.");
        foreach (var target in TargetData) _ = Remaining(state, target.PoolId);
    }
    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("policyId", PolicyId);
            writer.WriteString("scenarioHash", "sha256:fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66");
            writer.WriteString("cwChartsHash", "sha256:f75d074727071025f3145b273921ec03882b8b57cb4a429485995d4cf5255981");
            writer.WriteString("commonChartsHash", "sha256:51aa5a5bfdaca3d23794da71a45a830d98b798d060097cde6418126d6bd63bb0");
            writer.WriteString("landHash", "sha256:b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99");
            writer.WriteString("errataHash", "sha256:5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb");
            writer.WriteString("frenchArrivalCorrection", "errata-4.44b-deployed-d");
            writer.WriteString("liftPolicy", "one-medium-equivalent-per-infantry-toe");
            writer.WriteBoolean("globalPriorityComplete", false);
            writer.WriteStartArray("targets");
            foreach (var target in TargetData)
            {
                writer.WriteStartObject(); writer.WriteString("elementId", target.ElementId); writer.WriteString("locationId", target.LocationId);
                writer.WriteString("poolId", target.PoolId); writer.WriteNumber("infantryToe", target.InfantryToe); writer.WriteString("sourceCode", target.SourceCode);
                writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteNumber("frenchAssignedPoints", FrenchPoints); writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
    internal static string InitialHash() => new CampaignSourceCwPriorityCatalog(0).Hash();
}
