using System.Security.Cryptography;
using System.Text.Json;

namespace Cna.Core.Campaigns;

/// <summary>Optional source setup loads on explicitly attached idle first-line points.</summary>
internal sealed class CampaignSourceAttachedInitialLoads
{
    internal const string PolicyId = "graziani.cw-attached-initial-loads.provisional.v1";
    internal const string InitialHash = "sha256:e123f7be606c898b7c51ecbb874882e6e28c9008c75bd81587acf91853d991ab";
    private readonly IReadOnlyList<CampaignSourceInitialLoadTarget> targets;

    private CampaignSourceAttachedInitialLoads(IEnumerable<CampaignSourceInitialLoadTarget> targets) =>
        this.targets = Array.AsReadOnly(targets.OrderBy(value => value.ElementId, StringComparer.Ordinal).ToArray());

    internal static CampaignSourceAttachedInitialLoads Create()
    {
        var initial = new CampaignSourceAttachedInitialLoads([]);
        if (initial.Hash() != InitialHash) throw new InvalidOperationException("Initial load policy differs from pinned identity.");
        return initial;
    }
    internal IReadOnlyList<CampaignSourceInitialLoadTarget> Targets => targets;
    internal int Attached(string elementId) => targets.SingleOrDefault(value => value.ElementId == elementId)?.AttachedPoints ?? 0;
    internal int Reserved(string poolId) => targets.Where(value => value.PoolId == poolId).Sum(value => value.AttachedPoints);

    // Priority lift and existing load reservations always share the same source pool.
    internal bool CanApplyPriorityDelta(string elementId, int delta, CampaignSourcePriorityMotorization priority,
        CampaignSourceMotorizationWorld motor)
    {
        var target = CampaignSourcePriorityMotorization.Targets.SingleOrDefault(value => value.ElementId == elementId);
        return target is not null && priority.ValidDeltas(elementId, motor).Contains(delta)
            && (Attached(elementId) == 0 || priority.AssignedPoints(elementId) + delta == 6)
            && priority.Remaining(target.PoolId, motor) - delta >= Reserved(target.PoolId);
    }

    internal bool CanAttach(string elementId, CampaignSourcePriorityMotorization priority,
        CampaignSourceMotorizationWorld motor)
    {
        var target = CampaignSourcePriorityMotorization.Targets.SingleOrDefault(value => value.ElementId == elementId);
        return target is not null && priority.AssignedPoints(elementId) == 6
            && priority.Remaining(target.PoolId, motor) > Reserved(target.PoolId);
    }

    internal CampaignSourceAttachedInitialLoads Attach(string elementId, CampaignSourcePriorityMotorization priority,
        CampaignSourceMotorizationWorld motor)
    {
        if (!CanAttach(elementId, priority, motor)) throw new InvalidOperationException("Idle point attachment is unavailable.");
        var source = CampaignSourcePriorityMotorization.Targets.Single(value => value.ElementId == elementId);
        var previous = Target(elementId, source.PoolId);
        return Replace(previous with { AttachedPoints = checked(previous.AttachedPoints + 1) });
    }

    internal bool CanDetach(string elementId)
    {
        var value = targets.SingleOrDefault(item => item.ElementId == elementId);
        return value is not null && value.AttachedPoints > value.AmmoPoints + value.FuelPoints;
    }

    internal CampaignSourceAttachedInitialLoads Detach(string elementId)
    {
        if (!CanDetach(elementId)) throw new InvalidOperationException("Loaded points cannot be detached.");
        var previous = targets.Single(value => value.ElementId == elementId);
        return Replace(previous with { AttachedPoints = previous.AttachedPoints - 1 });
    }

    internal bool CanLoad(string elementId) => CanDetach(elementId);

    internal CampaignSourceAttachedInitialLoads Load(string elementId, string kind)
    {
        if (!CanLoad(elementId) || kind is not ("ammo" or "fuel"))
            throw new InvalidOperationException("Initial load is unavailable.");
        var previous = targets.Single(value => value.ElementId == elementId);
        return Replace(kind == "ammo" ? previous with { AmmoPoints = previous.AmmoPoints + 1 }
            : previous with { FuelPoints = previous.FuelPoints + 1 });
    }

    // Setup correction only; this does not move supplies or authorize gameplay transfer.
    internal bool CanClearLoad(string elementId, string kind)
    {
        var value = targets.SingleOrDefault(item => item.ElementId == elementId);
        return value is not null && (kind == "ammo" ? value.AmmoPoints > 0 : kind == "fuel" && value.FuelPoints > 0);
    }

    internal CampaignSourceAttachedInitialLoads ClearLoad(string elementId, string kind)
    {
        if (!CanClearLoad(elementId, kind)) throw new InvalidOperationException("Initial load correction is unavailable.");
        var previous = targets.Single(value => value.ElementId == elementId);
        return Replace(kind == "ammo" ? previous with { AmmoPoints = previous.AmmoPoints - 1 }
            : previous with { FuelPoints = previous.FuelPoints - 1 });
    }

    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("policyId", PolicyId);
            writer.WriteString("scenarioSourceHash", "sha256:fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66");
            writer.WriteString("landRulesSourceHash", "sha256:b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99");
            writer.WriteString("ruling", "provisional-idle-attachment-to-priority-unit-does-not-motorize-other-infantry");
            writer.WriteString("globalPriorityStatus", "incomplete");
            writer.WriteString("loadUse", "consume-first-no-transfer-to-supply-units");
            writer.WriteString("loadedDetachment", "only-to-subsidiary-combat-unit-during-gameplay-unimplemented");
            writer.WriteString("sourceLocators", "spi-1979-scenarios:59.66B;spi-1979-land-rules:32.54");
            writer.WriteString("setupCorrection", "provisional-clear-before-game-start-only");
            writer.WriteStartArray("targets");
            foreach (var target in targets)
            {
                writer.WriteStartObject();
                writer.WriteString("elementId", target.ElementId);
                writer.WriteString("poolId", target.PoolId);
                writer.WriteNumber("attachedPoints", target.AttachedPoints);
                writer.WriteNumber("ammoPoints", target.AmmoPoints);
                writer.WriteNumber("fuelPoints", target.FuelPoints);
                writer.WriteNumber("ammoSupplyPoints", target.AmmoPoints);
                writer.WriteNumber("fuelSupplyPoints", checked(target.FuelPoints * 3));
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
    private CampaignSourceInitialLoadTarget Target(string elementId, string poolId) =>
        targets.SingleOrDefault(value => value.ElementId == elementId) ?? new(elementId, poolId, 0, 0, 0);
    private CampaignSourceAttachedInitialLoads Replace(CampaignSourceInitialLoadTarget value) =>
        new(targets.Where(item => item.ElementId != value.ElementId).Concat(value.AttachedPoints == 0 ? [] : [value]));
}

internal sealed record CampaignSourceInitialLoadTarget(string ElementId, string PoolId,
    int AttachedPoints, int AmmoPoints, int FuelPoints);
