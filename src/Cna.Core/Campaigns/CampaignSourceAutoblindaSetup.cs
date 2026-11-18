using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;

namespace Cna.Core.Campaigns;

/// <summary>
/// The scenario's two Autoblinda points: a versioned source setup choice,
/// not a complete TOE/vehicle lifecycle or a playable Axis world.
/// </summary>
internal sealed class CampaignSourceAutoblindaSetup
{
    internal const string PolicyId = "graziani.autoblinda.two-points.either-or.v1";
    // Derived from (and so changes whenever) CampaignSourceAxisSupplementalForces.ContentHash
    // changes - see RUL-GRA-28's note on that constant for the most recent reason.
    internal const string InitialHash = "sha256:54e9b24232c2342740cc0280624ff7ec1a42e5546bcda2e560f7563e0bbd2ea3";
    // §60.31 permits each point on any unit, ignoring ordinary composition
    // restrictions. These two source-proven infantry are a safe subset; AA
    // removal and eligible characteristics of remaining counters are not yet
    // adjudicated. HQ are not categorically excluded by the source rule.
    private static readonly string[] CuratedTargets = ["1-141-infantry", "2-141-infantry"];
    private readonly ReadOnlyCollection<string> targets;

    private CampaignSourceAutoblindaSetup(string supplementHash, bool replacements, IEnumerable<string> targets)
    {
        SupplementHash = supplementHash;
        ReplacementsSelected = replacements;
        this.targets = Array.AsReadOnly(targets.Order(StringComparer.Ordinal).ToArray());
    }

    internal string SupplementHash { get; }
    internal bool ReplacementsSelected { get; }
    internal IReadOnlyList<string> Targets => targets;
    internal bool IsComplete => ReplacementsSelected || targets.Count == 2;
    internal bool CanChooseReplacements => !ReplacementsSelected && targets.Count == 0;
    internal int ReplacementPoints => ReplacementsSelected ? 2 : 0;

    internal static CampaignSourceAutoblindaSetup Create(CampaignSourceAxisSupplementalForces supplement)
    {
        ArgumentNullException.ThrowIfNull(supplement);
        if (supplement.Hash() != CampaignSourceAxisSupplementalForces.ContentHash
            || CuratedTargets.Any(target => !supplement.Forces.Any(force => force.ElementId == target
                && force.LocationId == "c4707" && force.InitialToes is not null
                && force.InitialToes.Any(toe => toe.ComponentId == target + ".infantry" && toe.Toe > 0))))
            throw new InvalidOperationException("Autoblinda setup requires the pinned eligible Axis source counters.");
        var initial = new CampaignSourceAutoblindaSetup(supplement.Hash(), false, []);
        if (initial.Hash() != InitialHash)
            throw new InvalidOperationException("Autoblinda profile differs from retained identity.");
        return initial;
    }

    internal IReadOnlyList<string> ValidAttachmentTargets()
    {
        if (ReplacementsSelected || targets.Count == 2) return [];
        return Array.AsReadOnly(CuratedTargets.Where(target => !targets.Contains(target, StringComparer.Ordinal)).ToArray());
    }

    internal CampaignSourceAutoblindaSetup ChooseTobrukReplacements()
    {
        if (!CanChooseReplacements) throw new InvalidOperationException("Autoblinda replacement choice is unavailable.");
        return new(SupplementHash, true, []);
    }

    internal CampaignSourceAutoblindaSetup Attach(string elementId)
    {
        if (!ValidAttachmentTargets().Contains(elementId, StringComparer.Ordinal))
            throw new InvalidOperationException("Autoblinda attachment target is unavailable.");
        return new(SupplementHash, false, targets.Append(elementId));
    }

    // Setup remains editable until its eventual authoritative completion.
    internal CampaignSourceAutoblindaSetup Reset()
    {
        if (CanChooseReplacements) throw new InvalidOperationException("No Autoblinda choice exists to reset.");
        return new(SupplementHash, false, []);
    }

    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("policyId", PolicyId);
            writer.WriteString("policyStatus", "provisional-either-or-no-mixing");
            writer.WriteString("supplementHash", SupplementHash);
            writer.WriteString("scenarioSourceHash", "sha256:fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66");
            writer.WriteBoolean("targetCoverageComplete", false);
            writer.WriteBoolean("hqCategoricallyExcluded", false);
            writer.WriteStartArray("curatedTargets");
            foreach (var target in CuratedTargets) writer.WriteStringValue(target);
            writer.WriteEndArray();
            writer.WriteStartArray("sources");
            writer.WriteStringValue("spi-1979-scenarios:60.31");
            writer.WriteStringValue("spi-1979-land-rules:32.83");
            writer.WriteEndArray();
            writer.WriteNumber("availableToePoints", 2);
            writer.WriteNumber("replacementPoints", ReplacementPoints);
            if (ReplacementsSelected)
            {
                writer.WriteString("replacementLocationId", "c4807");
                writer.WriteString("replacementTraining", "trained");
                writer.WriteString("replacementFuel", "fueled");
            }
            else
            {
                writer.WriteNull("replacementLocationId");
                writer.WriteNull("replacementTraining");
                writer.WriteNull("replacementFuel");
            }
            writer.WriteStartArray("attachments");
            foreach (var target in targets)
            {
                writer.WriteStartObject();
                writer.WriteString("elementId", target);
                writer.WriteNumber("toePoints", 1);
                writer.WriteString("training", "unknown");
                writer.WriteString("fuel", "unknown");
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteBoolean("complete", IsComplete);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
}
