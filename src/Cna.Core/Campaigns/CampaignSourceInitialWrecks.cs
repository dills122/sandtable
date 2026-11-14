using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;

namespace Cna.Core.Campaigns;

/// <summary>Scenario-initial damaged vehicles, not fabricated breakdown events.
/// Placement alone does not authorize repair or certify city capacity.</summary>
internal sealed class CampaignSourceInitialWrecks
{
    internal const string PolicyId = "graziani.initial-alexandria-wrecks.v1";
    internal const string InitialHash = "sha256:23517165282c3ca112e5c17eace4496f7ef881cc1f5ae13e113977f8b3efa964";
    private static readonly IReadOnlyList<string> Sites = Array.AsReadOnly<string>(["e3613", "e3714"]);
    private static readonly IReadOnlyList<CampaignSourceInitialWreck> Units = Array.AsReadOnly(new[]
    {
        new CampaignSourceInitialWreck("cw-alexandria-a9-1", "a9-cruiser"),
        new CampaignSourceInitialWreck("cw-alexandria-a9-2", "a9-cruiser"),
        new CampaignSourceInitialWreck("cw-alexandria-a10-1", "a10-cruiser"),
    });
    private readonly ReadOnlyCollection<CampaignSourceWreckPlacement> placements;
    private CampaignSourceInitialWrecks(IEnumerable<CampaignSourceWreckPlacement> placements)
    { this.placements = Array.AsReadOnly(placements.OrderBy(value => value.Id, StringComparer.Ordinal).ToArray()); }
    internal static IReadOnlyList<CampaignSourceInitialWreck> Stocks => Units;
    internal IReadOnlyList<CampaignSourceWreckPlacement> Placements => placements;
    internal IReadOnlyList<string> PendingIds => Array.AsReadOnly(Units.Select(value => value.Id)
        .Except(placements.Select(value => value.Id), StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray());

    internal static CampaignSourceInitialWrecks Create(CampaignPregameSourceState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Source.Session.Content.Identity.Hash != CampaignPregameSourceCreation.SupportedContentHash)
            throw new InvalidOperationException("Initial damaged vehicles require pinned Graziani source content.");
        var initial = new CampaignSourceInitialWrecks([]);
        if (initial.Hash() != InitialHash)
            throw new InvalidOperationException("Initial damaged vehicle stock differs from its pinned profile.");
        return initial;
    }

    internal IReadOnlyList<string> ValidDestinations(string id) => Units.Any(unit => unit.Id == id)
        && placements.All(placement => placement.Id != id) ? Sites : [];

    internal CampaignSourceInitialWrecks Place(string id, string locationId)
    {
        if (!ValidDestinations(id).Contains(locationId, StringComparer.Ordinal))
            throw new InvalidOperationException("Initial damaged vehicle placement is unavailable.");
        return new(placements.Append(new CampaignSourceWreckPlacement(id, locationId)));
    }

    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("policyId", PolicyId);
            writer.WriteString("source", "spi-1979-scenarios:60.41");
            writer.WriteString("scenarioSourceHash", "sha256:fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66");
            writer.WriteString("mapSourceHash", "sha256:8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030");
            writer.WriteString("cityHexPlacementPolicy", "provisional-player-choice-per-vehicle");
            writer.WriteString("repairStatus", "not-adjudicated");
            writer.WriteString("breakdownProvenance", "scenario-initial-not-breakdown-event");
            writer.WriteStartArray("sites"); foreach (var site in Sites) writer.WriteStringValue(site); writer.WriteEndArray();
            writer.WriteStartArray("stocks");
            foreach (var unit in Units.OrderBy(value => value.Id, StringComparer.Ordinal))
            {
                writer.WriteStartObject();
                writer.WriteString("id", unit.Id);
                writer.WriteString("vehicleTypeId", unit.VehicleTypeId);
                writer.WriteNumber("pointCount", 1);
                var placement = placements.SingleOrDefault(value => value.Id == unit.Id);
                if (placement is null) writer.WriteNull("locationId"); else writer.WriteString("locationId", placement.LocationId);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
}

internal sealed record CampaignSourceInitialWreck(string Id, string VehicleTypeId);
internal sealed record CampaignSourceWreckPlacement(string Id, string LocationId);
