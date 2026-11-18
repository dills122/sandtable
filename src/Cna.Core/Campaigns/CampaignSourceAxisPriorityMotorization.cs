using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>
/// Real Maletti pregame detachment and priority infantry lift. Only three
/// source-verified units are represented; other force choices remain unresolved.
/// </summary>
internal sealed class CampaignSourceAxisPriorityMotorization
{
    internal const string PolicyId = "graziani.axis-priority-motorization.maletti-detached.v1";
    // Derived from (and so changes whenever) CampaignSourceAxisSupplementalForces.ContentHash
    // changes - see RUL-GRA-28's note on that constant for the most recent reason.
    internal const string InitialHash = "sha256:6697d603993a184628af8a7d6cf1f23dedfc0f268d513bc62f57c79a61e8a9b1";
    internal const string PoolId = "axis-c3617-attached";
    internal const string HistoricalParentId = "1-libyan-motor-regiment-hq";
    private static readonly string[] ProvenTargets =
        ["1-1-libyan-motor-infantry", "3-1-libyan-motor-infantry", "5-1-libyan-motor-infantry"];
    private static readonly int[] AssignmentSteps = [1, 5];
    private readonly ReadOnlyCollection<string> detached;
    private readonly ReadOnlyCollection<CampaignSourcePriorityAssignment> assigned;

    private CampaignSourceAxisPriorityMotorization(string supplementHash, IEnumerable<string> detached,
        IEnumerable<CampaignSourcePriorityAssignment> assigned)
    {
        SupplementHash = supplementHash;
        this.detached = Array.AsReadOnly(detached.Order(StringComparer.Ordinal).ToArray());
        this.assigned = Array.AsReadOnly(assigned.OrderBy(value => value.ElementId, StringComparer.Ordinal).ToArray());
    }

    internal string SupplementHash { get; }
    internal static IReadOnlyList<string> Targets => Array.AsReadOnly(ProvenTargets.ToArray());
    internal IReadOnlyList<string> DetachedTargets => detached;
    internal int AssignedPoints(string elementId) => assigned.SingleOrDefault(value => value.ElementId == elementId)?.Points ?? 0;

    internal static CampaignSourceAxisPriorityMotorization Create(CampaignSourceAxisSupplementalForces supplement,
        CampaignSourceMotorizationWorld motor)
    {
        ArgumentNullException.ThrowIfNull(supplement);
        ArgumentNullException.ThrowIfNull(motor);
        if (supplement.Hash() != CampaignSourceAxisSupplementalForces.ContentHash)
            throw new InvalidOperationException("Maletti motorization requires the pinned Axis source supplement.");
        foreach (var target in ProvenTargets)
        {
            var force = supplement.Forces.SingleOrDefault(value => value.ElementId == target);
            if (force is null || force.LocationId != "c3617" || force.InitialToes is null
                || force.InitialToes.Count != 1 || force.InitialToes[0].Toe != 5
                || force.InitialToes[0].ComponentId != target + ".infantry")
                throw new InvalidOperationException("Maletti target differs from its retained source force.");
        }
        ValidatePool(motor);
        var initial = new CampaignSourceAxisPriorityMotorization(supplement.Hash(), [], []);
        if (initial.Hash() != InitialHash)
            throw new InvalidOperationException("Maletti motorization profile differs from retained identity.");
        return initial;
    }

    // §59.2 permits freely detaching co-located units during scenario setup.
    // Historical OA assignment is preserved; detached refers only to current
    // organizational attachment, not a claim of changed historical assignment.
    internal CampaignSourceAxisPriorityMotorization Detach(string targetId)
    {
        if (!ProvenTargets.Contains(targetId, StringComparer.Ordinal) || detached.Contains(targetId, StringComparer.Ordinal))
            throw new InvalidOperationException("Maletti detachment choice is unavailable.");
        return new(SupplementHash, detached.Append(targetId), assigned);
    }

    internal CampaignSourceAxisPriorityMotorization UndoDetachment(string targetId)
    {
        if (!detached.Contains(targetId, StringComparer.Ordinal) || AssignedPoints(targetId) != 0)
            throw new InvalidOperationException("Remove the target's assigned motorization before undoing detachment.");
        return new(SupplementHash, detached.Where(value => value != targetId), assigned);
    }

    internal int Remaining(CampaignSourceMotorizationWorld motor)
    {
        ValidatePool(motor);
        var remaining = checked(motor.Remaining(PoolId) - assigned.Sum(value => value.Points));
        if (remaining < 0) throw new InvalidOperationException("Maletti assignments exceed their shared first-line pool.");
        return remaining;
    }

    internal IReadOnlyList<int> ValidDeltas(string targetId, CampaignSourceMotorizationWorld motor)
    {
        ValidatePool(motor);
        if (!ProvenTargets.Contains(targetId, StringComparer.Ordinal)
            || !detached.Contains(targetId, StringComparer.Ordinal)) return [];
        var current = AssignedPoints(targetId);
        var remaining = Remaining(motor);
        var result = new List<int>();
        foreach (var amount in AssignmentSteps)
        {
            if (current >= amount) result.Add(-amount);
            if (current + amount <= 5 && remaining >= amount) result.Add(amount);
        }
        return Array.AsReadOnly(result.Order().ToArray());
    }

    internal CampaignSourceAxisPriorityMotorization Adjust(string targetId, int delta, CampaignSourceMotorizationWorld motor)
    {
        if (!ValidDeltas(targetId, motor).Contains(delta))
            throw new InvalidOperationException("Maletti priority motorization choice is unavailable.");
        var next = assigned.Where(value => value.ElementId != targetId).ToList();
        var points = checked(AssignedPoints(targetId) + delta);
        if (points > 0) next.Add(new(targetId, points));
        return new(SupplementHash, detached, next);
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
            writer.WriteString("axisChartsSourceHash", "sha256:9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca");
            writer.WriteString("commonChartsSourceHash", "sha256:51aa5a5bfdaca3d23794da71a45a830d98b798d060097cde6418126d6bd63bb0");
            writer.WriteString("landRulesSourceHash", "sha256:b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99");
            writer.WriteString("scenarioSourceHash", "sha256:fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66");
            writer.WriteString("airRulesSourceHash", "sha256:9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4");
            writer.WriteBoolean("targetCoverageComplete", false);
            writer.WriteString("globalPriorityStatus", "incomplete");
            writer.WriteString("fullUnitMobilityStatus", "unproven");
            writer.WriteString("infantryLiftPolicy", "provisional-one-motorization-per-infantry-toe");
            writer.WriteStartArray("sources");
            writer.WriteStringValue("spi-1979-scenarios:60.31");
            writer.WriteStringValue("spi-1979-axis-charts:oa-maletti-p7");
            writer.WriteStringValue("spi-1979-axis-charts:4.46b-w");
            writer.WriteStringValue("spi-1979-air-logistics:59.2-59.42");
            writer.WriteStringValue("spi-1979-land-rules:8.93-8.96");
            writer.WriteStringValue("spi-1979-land-rules:32.51-32.54");
            writer.WriteStringValue("spi-1979-common-charts:54.2");
            writer.WriteEndArray();
            writer.WriteStartArray("targets");
            foreach (var target in ProvenTargets)
            {
                writer.WriteStartObject();
                writer.WriteString("elementId", target);
                writer.WriteString("locationId", "c3617");
                writer.WriteString("historicalAssignedParentId", HistoricalParentId);
                writer.WriteString("currentAttachment", detached.Contains(target, StringComparer.Ordinal) ? "detached" : "unresolved");
                writer.WriteString("poolId", PoolId);
                writer.WriteNumber("sourcePoolPoints", 35);
                writer.WriteNumber("initialToe", 5);
                writer.WriteString("printedCharacteristics", "w:10+");
                writer.WriteNumber("assignedPoints", AssignedPoints(target));
                writer.WriteBoolean("infantryComponentLiftSatisfied", AssignedPoints(target) == 5);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));

    private static void ValidatePool(CampaignSourceMotorizationWorld motor)
    {
        ArgumentNullException.ThrowIfNull(motor);
        var pool = motor.Pools.SingleOrDefault(value => value.Id == PoolId);
        if (motor.Side != LandSide.Axis || pool is null || !pool.Attached || pool.SourceLocation != "c3617" || pool.Points != 35)
            throw new InvalidOperationException("Maletti first-line pool differs from scenario setup.");
    }
}
