using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Campaigns;

// Normal campaign Combat ammunition is derived from its exact Content source and
// subsequently changed only by accepted Combat settlements. Replay applies the
// same spends in event order rather than trusting a stored balance.
internal sealed class CampaignCombatAmmunitionLedgerV1
{
    internal const int CurrentContractVersion = 1;
    private readonly IReadOnlyDictionary<string, int> pointsByElement;

    private CampaignCombatAmmunitionLedgerV1(string contentHash, string sourceHash,
        IReadOnlyDictionary<string, int> pointsByElement)
    {
        ContentHash = contentHash;
        SourceHash = sourceHash;
        this.pointsByElement = pointsByElement;
    }

    internal string ContentHash { get; }
    internal string SourceHash { get; }
    internal string Hash => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));

    internal int Remaining(string elementId) => pointsByElement[elementId];

    internal static CampaignCombatAmmunitionLedgerV1 CreateInitial(CampaignContentContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var artifact = context.ArtifactV6
            ?? throw new InvalidOperationException("Combat ammunition requires normal Content 6.");
        var source = context.CombatSource
            ?? throw new InvalidOperationException("Combat ammunition requires a pinned source.");
        if (source.ContentHash != artifact.Identity.Hash)
            throw new InvalidOperationException("Combat ammunition source differs from Content.");
        var initial = artifact.Definition.LegacyDefinition.Elements.ToDictionary(
            element => element.ElementId,
            element => source.InitialAmmunition(element.ElementId), StringComparer.Ordinal);
        return new CampaignCombatAmmunitionLedgerV1(artifact.Identity.Hash, source.Hash, initial);
    }

    internal static CampaignCombatAmmunitionLedgerV1 FromAcceptedEvents(
        CampaignContentContext context, IEnumerable<object> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        var current = CreateInitial(context);
        var expectedSpend = Cna.Core.Rules.Cna1979CombatAdjudication.Definition.Costs
            .CommittedToePerRole * Cna.Core.Rules.Cna1979CombatAdjudication.Definition.Costs
            .AmmunitionPointsPerToe;
        foreach (var result in events.OfType<CampaignNativeCombatAssaultResultV22>())
        {
            if (result.ContentHash != current.ContentHash
                || result.SourceHash != current.SourceHash
                || result.PriorAmmunitionHash != current.Hash
                || result.AttackerElementId == result.DefenderElementId
                || result.AttackerAmmunitionSpent != expectedSpend
                || result.DefenderAmmunitionSpent != expectedSpend)
                throw new InvalidCampaignHistoryException(
                    "Combat ammunition differs from retained source and event order.");
            try
            {
                current = current.Spend(result.AttackerElementId,
                    result.AttackerAmmunitionSpent);
                current = current.Spend(result.DefenderElementId,
                    result.DefenderAmmunitionSpent);
            }
            catch (Exception exception) when (exception is ArgumentException
                or InvalidOperationException or KeyNotFoundException)
            {
                throw new InvalidCampaignHistoryException(
                    "Combat ammunition spend exceeds retained source.");
            }
            if (result.AfterAmmunitionHash != current.Hash)
                throw new InvalidCampaignHistoryException(
                    "Combat ammunition successor hash differs from accepted spend.");
        }
        return current;
    }

    internal CampaignCombatAmmunitionLedgerV1 Spend(string elementId, int points)
    {
        ContentContractGuards.RequireStableId(elementId, nameof(elementId));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(points);
        if (!pointsByElement.TryGetValue(elementId, out var available)
            || points > available)
            throw new InvalidOperationException("Combat ammunition is unavailable.");
        var next = pointsByElement.ToDictionary(pair => pair.Key, pair => pair.Value,
            StringComparer.Ordinal);
        next[elementId] = available - points;
        return new CampaignCombatAmmunitionLedgerV1(ContentHash, SourceHash, next);
    }

    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CurrentContractVersion);
            writer.WriteString("contentHash", ContentHash);
            writer.WriteString("sourceHash", SourceHash);
            writer.WriteStartArray("ammunition");
            foreach (var pair in pointsByElement.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                writer.WriteStartObject();
                writer.WriteString("elementId", pair.Key);
                writer.WriteNumber("points", pair.Value);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
}
