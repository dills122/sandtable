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
        return FoldForward(CreateInitial(context), events);
    }

    // Perf seeding path (Phase 1 of #436): callers that already hold a
    // ledger known to be current as of some point in the history -- e.g.
    // ExerciseSession.CurrentAmmunitionLedger, populated at a clean
    // ordinary-step boundary or carried forward across a single Combat
    // round -- fold forward only the events after that point instead of
    // re-deriving from CreateInitial over the full accepted-event history.
    // This does not weaken the per-event integrity checks below: every
    // event in `events` is still validated against the running hash chain
    // (ContentHash/SourceHash/PriorAmmunitionHash/AfterAmmunitionHash), so
    // a caller cannot pass a wrong or tampered `seed` and have it silently
    // accepted -- it only changes where the fold starts.
    internal static CampaignCombatAmmunitionLedgerV1 FromAcceptedEvents(
        CampaignCombatAmmunitionLedgerV1 seed, IEnumerable<object> events)
    {
        ArgumentNullException.ThrowIfNull(seed);
        ArgumentNullException.ThrowIfNull(events);
        return FoldForward(seed, events);
    }

    private static CampaignCombatAmmunitionLedgerV1 FoldForward(
        CampaignCombatAmmunitionLedgerV1 seed, IEnumerable<object> events)
    {
        var current = seed;
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

    internal static CampaignCombatAmmunitionLedgerV1 Deserialize(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        using var document = JsonDocument.Parse(bytes);
        var root = document.RootElement;
        if (!root.TryGetProperty("contractVersion", out var contractVersionElement)
            || contractVersionElement.GetInt32() != CurrentContractVersion)
            throw new InvalidOperationException("Combat ammunition contract version is unsupported.");
        var contentHash = root.GetProperty("contentHash").GetString()
            ?? throw new InvalidOperationException("Combat ammunition content hash is missing.");
        var sourceHash = root.GetProperty("sourceHash").GetString()
            ?? throw new InvalidOperationException("Combat ammunition source hash is missing.");
        var points = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var entry in root.GetProperty("ammunition").EnumerateArray())
        {
            var elementId = entry.GetProperty("elementId").GetString()
                ?? throw new InvalidOperationException("Combat ammunition element id is missing.");
            points[elementId] = entry.GetProperty("points").GetInt32();
        }
        return new CampaignCombatAmmunitionLedgerV1(contentHash, sourceHash, points);
    }
}
