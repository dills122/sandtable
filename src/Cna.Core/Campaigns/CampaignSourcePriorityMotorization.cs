using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>
/// Partial first-line assignments to source-proven historically motorized
/// infantry. Component lift does not establish full unit mobility or complete
/// the global historical-priority obligation.
/// </summary>
internal sealed class CampaignSourcePriorityMotorization
{
    internal const string PolicyId = "graziani.cw-priority-motorization.infantry-toe.v1";
    internal const string InitialHash = "sha256:22ced0563dcefdee8dac1de13083a9cb8348d11e925477349649f8a42149f9aa";
    private static readonly int[] AssignmentSteps = [1, 5, 6];
    private static readonly CampaignSourcePriorityTarget[] ProvenTargets =
    [
        new("1-kings-royal-rifle-corps", "c3922", "cw-c3922-attached", 20),
        new("2-rifle-brigade", "c3320", "cw-c3320-attached", 10),
    ];
    private readonly ReadOnlyCollection<CampaignSourcePriorityAssignment> assigned;

    private CampaignSourcePriorityMotorization(string supplementHash, IEnumerable<CampaignSourcePriorityAssignment> assigned)
    {
        SupplementHash = supplementHash;
        this.assigned = Array.AsReadOnly(assigned.OrderBy(value => value.ElementId, StringComparer.Ordinal).ToArray());
    }

    internal string SupplementHash { get; }
    internal IReadOnlyList<string> AssignedTargets => Array.AsReadOnly(assigned.Select(value => value.ElementId).ToArray());
    internal int AssignedPoints(string elementId) => assigned.SingleOrDefault(value => value.ElementId == elementId)?.Points ?? 0;
    internal static IReadOnlyList<CampaignSourcePriorityTarget> Targets => Array.AsReadOnly(ProvenTargets.ToArray());

    internal static CampaignSourcePriorityMotorization Create(CampaignSourceCommonwealthSupplementalForces supplement,
        CampaignSourceMotorizationWorld motor)
    {
        ArgumentNullException.ThrowIfNull(supplement);
        ArgumentNullException.ThrowIfNull(motor);
        if (supplement.Hash() != CampaignSourceCommonwealthSupplementalForces.ContentHash)
            throw new InvalidOperationException("Priority motorization requires the pinned Commonwealth source supplement.");
        foreach (var target in ProvenTargets)
        {
            var force = supplement.Forces.SingleOrDefault(value => value.ElementId == target.ElementId);
            if (force is null || force.LocationId != target.LocationId || force.InitialToes is null
                || force.InitialToes.Count != 1 || force.InitialToes[0].Toe != 6
                || force.InitialToes[0].ComponentId != target.ElementId + ".infantry")
                throw new InvalidOperationException("Priority motorization target differs from the retained source force.");
        }
        ValidatePools(motor);
        var initial = new CampaignSourcePriorityMotorization(supplement.Hash(), []);
        if (initial.Hash() != InitialHash)
            throw new InvalidOperationException("Priority motorization profile differs from retained identity.");
        return initial;
    }

    internal IReadOnlyList<int> ValidDeltas(string targetId, CampaignSourceMotorizationWorld motor)
    {
        ValidatePools(motor);
        var target = ProvenTargets.SingleOrDefault(value => value.ElementId == targetId);
        if (target is null) return [];
        var current = AssignedPoints(targetId);
        var remaining = Remaining(target.PoolId, motor);
        var result = new List<int>();
        foreach (var amount in AssignmentSteps)
        {
            if (current >= amount) result.Add(-amount);
            if (current + amount <= 6 && remaining >= amount) result.Add(amount);
        }
        return Array.AsReadOnly(result.Order().ToArray());
    }

    internal int Remaining(string poolId, CampaignSourceMotorizationWorld motor)
    {
        ValidatePools(motor);
        var reserved = ProvenTargets.Where(value => value.PoolId == poolId).Sum(value => AssignedPoints(value.ElementId));
        var remaining = checked(motor.Remaining(poolId) - reserved);
        if (remaining < 0) throw new InvalidOperationException("Priority assignments exceed their first-line pool.");
        return remaining;
    }

    internal CampaignSourcePriorityMotorization Adjust(string targetId, int delta, CampaignSourceMotorizationWorld motor)
    {
        if (!ValidDeltas(targetId, motor).Contains(delta))
            throw new InvalidOperationException("Priority motorization assignment is unavailable.");
        var next = assigned.Where(value => value.ElementId != targetId).ToList();
        var points = checked(AssignedPoints(targetId) + delta);
        if (points > 0) next.Add(new(targetId, points));
        return new CampaignSourcePriorityMotorization(SupplementHash, next);
    }

    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("policyId", PolicyId);
            writer.WriteString("supplementHash", SupplementHash);
            writer.WriteString("commonwealthChartsSourceHash", "sha256:f75d074727071025f3145b273921ec03882b8b57cb4a429485995d4cf5255981");
            writer.WriteString("commonChartsSourceHash", "sha256:51aa5a5bfdaca3d23794da71a45a830d98b798d060097cde6418126d6bd63bb0");
            writer.WriteString("landRulesSourceHash", "sha256:b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99");
            writer.WriteString("scenarioSourceHash", "sha256:fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66");
            writer.WriteBoolean("targetCoverageComplete", false);
            writer.WriteString("globalPriorityStatus", "incomplete");
            writer.WriteString("fullMotorizationStatus", "unproven");
            writer.WriteString("infantryLiftPolicy", "provisional-one-motorization-per-infantry-toe");
            writer.WriteStartArray("assignmentSteps");
            foreach (var amount in AssignmentSteps) writer.WriteNumberValue(amount);
            writer.WriteEndArray();
            writer.WriteStartArray("sources");
            writer.WriteStringValue("spi-1979-scenarios:60.41");
            writer.WriteStringValue("spi-1979-commonwealth-charts:oa-7-armored-p6");
            writer.WriteStringValue("spi-1979-commonwealth-charts:4.46a-n");
            writer.WriteStringValue("spi-1979-land-rules:8.93-8.95");
            writer.WriteStringValue("spi-1979-land-rules:32.53-32.54");
            writer.WriteStringValue("spi-1979-common-charts:54.2");
            writer.WriteEndArray();
            writer.WriteStartArray("targets");
            foreach (var target in ProvenTargets)
            {
                writer.WriteStartObject();
                writer.WriteString("elementId", target.ElementId);
                writer.WriteString("locationId", target.LocationId);
                writer.WriteString("poolId", target.PoolId);
                writer.WriteNumber("sourcePoolPoints", target.SourcePoolPoints);
                writer.WriteNumber("initialToe", 6);
                writer.WriteString("printedCharacteristics", "n:10+");
                writer.WriteNumber("assignedPoints", AssignedPoints(target.ElementId));
                writer.WriteBoolean("infantryComponentLiftSatisfied", AssignedPoints(target.ElementId) == 6);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));

    private static void ValidatePools(CampaignSourceMotorizationWorld motor)
    {
        ArgumentNullException.ThrowIfNull(motor);
        if (motor.Side != LandSide.Commonwealth)
            throw new InvalidOperationException("Priority assignments require Commonwealth motorization authority.");
        foreach (var target in ProvenTargets)
        {
            var pool = motor.Pools.SingleOrDefault(value => value.Id == target.PoolId);
            if (pool is null || !pool.Attached || pool.SourceLocation != target.LocationId
                || pool.Points != target.SourcePoolPoints)
                throw new InvalidOperationException("First-line motorization pool differs from scenario setup.");
        }
    }
}

internal sealed record CampaignSourcePriorityTarget(string ElementId, string LocationId, string PoolId, int SourcePoolPoints);
internal sealed record CampaignSourcePriorityAssignment(string ElementId, int Points);
