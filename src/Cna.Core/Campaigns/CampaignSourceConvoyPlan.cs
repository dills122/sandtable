using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;

namespace Cna.Core.Campaigns;

/// <summary>
/// Finalized Axis GT2 supply obligations captured from the real Graziani setup.
/// These are future convoy plans, not delivered supply or proof that other setup is complete.
/// </summary>
internal sealed class CampaignSourceConvoyPlan
{
    internal const int ContractVersion = 1;
    internal const int TargetGameTurn = 2;
    private readonly ReadOnlyCollection<CampaignSourceConvoyAllocation> allocations;

    private CampaignSourceConvoyPlan(int die, int maximumSupply,
        IEnumerable<CampaignSourceConvoyAllocation> allocations)
    {
        Die = die;
        MaximumSupply = maximumSupply;
        this.allocations = Array.AsReadOnly(allocations.ToArray());
    }

    internal int Die { get; }
    internal int MaximumSupply { get; }
    internal IReadOnlyList<CampaignSourceConvoyAllocation> Allocations => allocations;
    internal int AllocatedSupply => allocations.Sum(value => value.SupplyUnits);

    /// <summary>
    /// Capture only a replay-derived pregame state. Entering the successor is the
    /// Axis player's decision to finalize these allocations; unallocated supply is
    /// retained as unused availability, never silently created or delivered.
    /// </summary>
    internal static CampaignSourceConvoyPlan Create(CampaignPregameSourceState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Die is not { } die || state.MaximumSupply is not { } maximum)
            throw new InvalidOperationException("Axis supply availability must be rolled before finalizing the convoy plan.");
        var captured = state.Allocations().Select(value =>
        {
            var route = CampaignPregameSourceCreation.LaneRoute(value.Lane);
            return new CampaignSourceConvoyAllocation(value.Lane, value.Stage,
                value.Units, route.Origin, route.Destination);
        }).ToArray();
        if (captured.Any(value => value.SupplyUnits is < 1 or > 3)
            || captured.Sum(value => value.SupplyUnits) > maximum)
            throw new InvalidOperationException("Finalized supply allocations exceed available supply or lane capacity.");
        return new CampaignSourceConvoyPlan(die, maximum, captured);
    }

    /// <summary>Canonical private authority evidence; never a public player projection.</summary>
    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("policyId", CampaignPregameSourceCreation.PolicyId);
            writer.WriteNumber("targetGameTurn", TargetGameTurn);
            writer.WriteNumber("die", Die);
            writer.WriteNumber("maximumSupply", MaximumSupply);
            writer.WriteNumber("unusedAvailability", MaximumSupply - AllocatedSupply);
            writer.WriteStartArray("allocations");
            foreach (var allocation in allocations)
            {
                writer.WriteStartObject();
                writer.WriteNumber("lane", allocation.Lane);
                writer.WriteNumber("operationStage", allocation.OperationStage);
                writer.WriteNumber("supplyUnits", allocation.SupplyUnits);
                writer.WriteString("origin", allocation.Origin);
                writer.WriteString("destination", allocation.Destination);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
}

internal sealed record CampaignSourceConvoyAllocation(
    int Lane, int OperationStage, int SupplyUnits, string Origin, string Destination);
