using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Campaigns;

namespace Cna.Core.Content;

// Independent (ungrouped) Tripoli Tank Regiment counters. Unlike Aresca
// (CampaignSourceArescaGroupedDefinition), this subset is never grouped
// anywhere in the codebase; it has carried a real, cited source group
// (Cna1979GrazianiSourceFacts.TripoliTankRegimentInitialSubset, already part
// of CampaignSourceAxisSupplementalForces' admitted groups, so these 3
// elements already have retained source-evidence Battlefield entries) with
// zero admission/activation wiring. Every element and TOE here already
// carries a citation in Cna1979GrazianiSourceFacts.cs
// (LibyanTankRegimentCombatCandidates / CreateLibyanTankRegimentToeCandidates,
// filtered to this pack's 3 elements, reused verbatim); none of it is newly
// asserted here.
internal sealed class ContentSourceTripoliTankRegimentPackArtifact
{
    internal const string FormatId = "sandtable.source-tripoli-tank-regiment.v1";
    internal const string ContentHash = "sha256:7aa0d1d4d68b2ef0afb9145fee70fe8f81cba819ebecb58c16fdaa0bc7655372"; // RUL-GRA-26: derived from CampaignSourceAxisSupplementalForces.ContentHash (via ContentSourceCwFreeGunsPackArtifact).
    private readonly byte[] bytes;

    private ContentSourceTripoliTankRegimentPackArtifact(ContentSourceCwFreeGunsPackArtifact basis, byte[] bytes)
    {
        Basis = basis; this.bytes = bytes.ToArray();
        Identity = new(FormatId, basis.Identity.PackId, basis.Identity.RulesetId,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)));
    }

    internal ContentSourceCwFreeGunsPackArtifact Basis { get; }
    internal ContentScheduledPackIdentity Identity { get; }
    internal byte[] CanonicalBytes => bytes.ToArray();

    internal static string LocationId { get; } = Cna1979GrazianiSourceFacts.TripoliTankRegimentInitialSubset.LocationId;

    internal static IReadOnlyList<string> AllIds { get; } =
        Array.AsReadOnly(Cna1979GrazianiSourceFacts.TripoliTankRegimentInitialSubset.ElementIds.ToArray());

    // Filters the already-published Libyan tank regiment TOE candidates
    // (shared with Aresca) down to this pack's 3 Tripoli elements. No new
    // TOE value is introduced anywhere here.
    internal static IReadOnlyList<ContentInitialPlacementCombatFacts> CreateToeSeeds(string scenarioId)
    {
        var seeds = Cna1979GrazianiSourceFacts.CreateLibyanTankRegimentToeCandidates(scenarioId)
            .Where(value => AllIds.Contains(value.ElementId, StringComparer.Ordinal))
            .ToArray();
        if (seeds.Length != AllIds.Count
            || seeds.Select(value => value.ElementId).Distinct(StringComparer.Ordinal).Count() != AllIds.Count
            || seeds.Any(value => !AllIds.Contains(value.ElementId, StringComparer.Ordinal)))
            throw new InvalidOperationException("Tripoli tank regiment TOE seeds differ from its retained element list.");
        return Array.AsReadOnly(seeds);
    }

    internal static ContentSourceTripoliTankRegimentPackArtifact Create(ContentSourceCwFreeGunsPackArtifact basis)
    {
        ArgumentNullException.ThrowIfNull(basis);
        if (basis.Hash() != ContentSourceCwFreeGunsPackArtifact.ContentHash)
            throw new InvalidOperationException("Tripoli tank regiment requires pinned native source content.");
        var seeds = CreateToeSeeds(Cna1979GrazianiCairoSourceExcerpt.ScenarioId)
            .ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("formatId", FormatId);
            writer.WriteString("baseHash", basis.Hash());
            writer.WriteBase64String("baseContent", basis.CanonicalBytes);
            writer.WriteString("sources", "scenario:60.31;axis-charts:oa-libyan-tank-command-p6,4.46b,4.48;land:4.46;land:4.45");
            writer.WriteStartArray("units");
            foreach (var id in AllIds.Order(StringComparer.Ordinal))
            {
                var toe = seeds[id];
                writer.WriteStartObject();
                writer.WriteString("elementId", id);
                writer.WriteString("sideId", "axis");
                writer.WriteString("locationId", LocationId);
                writer.WriteStartArray("components");
                foreach (var component in toe.InitialComponentToes)
                {
                    writer.WriteStartObject();
                    writer.WriteString("componentId", component.ComponentId);
                    writer.WriteNumber("toe", component.CurrentToe);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        var result = new ContentSourceTripoliTankRegimentPackArtifact(basis, stream.ToArray());
        if (result.Identity.Hash != ContentHash)
            throw new InvalidOperationException("Tripoli tank regiment content differs from its pinned identity.");
        return result;
    }

    internal static ContentSourceTripoliTankRegimentPackArtifact Read(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        using var document = JsonDocument.Parse(bytes);
        var basis = ContentSourceCwFreeGunsPackArtifact.Read(document.RootElement.GetProperty("baseContent").GetBytesFromBase64());
        var expected = Create(basis);
        if (!bytes.AsSpan().SequenceEqual(expected.bytes))
            throw new InvalidOperationException("Tripoli tank regiment content is not canonical.");
        return expected;
    }
}
