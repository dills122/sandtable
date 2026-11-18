using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>
/// Source-backed Axis formation counters that supplement, rather than mutate,
/// the retained Tobruk/Bardia genesis. Incomplete characteristics and formation
/// relationships remain unknown; this is not yet a playable Axis world.
/// </summary>
internal sealed class CampaignSourceAxisSupplementalForces
{
    internal const string PolicyId = "graziani.axis-initial-forces.mainland-supplement.v1";
    // RUL-GRA-28 (2026-11-12-graziani-ruling-decisions.md) changed this
    // identity: 1-ccnn-hq and 4-ccnn-hq now carry one known artillery TOE
    // point with an explicit unidentified-weapon component type instead of
    // null InitialToes. This is an intentional, ruling-driven fact change,
    // not drift - any history pinned to the prior hash
    // (sha256:1a34826c4d5e1a3fe20891f9064b61e8e6b58f5ecee7935d5ac076839e083372)
    // predates the ruling and must not be silently reinterpreted under this one.
    internal const string ContentHash = "sha256:0646362c46b0354969caaf61a955fbde111664c29f4f072dd4ffb30d9bcc76fc";
    private readonly ReadOnlyCollection<CampaignSourceSupplementalGroup> groups;

    private CampaignSourceAxisSupplementalForces(string predecessorHash,
        IEnumerable<CampaignSourceSupplementalGroup> groups)
    {
        PredecessorHash = predecessorHash;
        this.groups = Array.AsReadOnly(groups.OrderBy(value => value.GroupId, StringComparer.Ordinal).ToArray());
    }

    internal string PredecessorHash { get; }
    internal IReadOnlyList<CampaignSourceSupplementalGroup> Groups => groups;
    internal IEnumerable<CampaignSourceInitialForce> Forces => groups.SelectMany(group => group.Forces);

    internal static CampaignSourceAxisSupplementalForces Create(CampaignPregameSourceState state,
        CampaignSourceAxisInitialForces predecessor)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(predecessor);
        if (state.Source.Session.Content.Identity.Hash != CampaignPregameSourceCreation.SupportedContentHash)
            throw new InvalidOperationException("Axis supplementary forces require the pinned Graziani source.");
        if (predecessor.Hash() != CampaignSourceAxisInitialForces.Create(state).Hash())
            throw new InvalidOperationException("Axis supplementary forces require the retained initial-force predecessor.");
        var groups = new[]
        {
            Cna1979GrazianiSourceFacts.CireneDivisionChartCandidate,
            Cna1979GrazianiSourceFacts.MarmaricaDivisionChartCandidate,
            Cna1979GrazianiSourceFacts.FirstLibyanDivisionChartCandidate,
            Cna1979GrazianiSourceFacts.SecondLibyanDivisionChartCandidate,
            Cna1979GrazianiSourceFacts.MalettiInitialChartCandidate,
            Cna1979GrazianiSourceFacts.CatanzaroInitialChartCandidate,
            Cna1979GrazianiSourceFacts.FirstCcnnInitialChartCandidate,
            Cna1979GrazianiSourceFacts.FourthCcnnInitialChartCandidate,
            Cna1979GrazianiSourceFacts.ArescaTankRegimentInitialCandidate,
            Cna1979GrazianiSourceFacts.TripoliTankRegimentInitialSubset,
        };
        var members = groups.SelectMany(value => value.ElementIds).ToArray();
        if (groups.Any(value => value.SideId != "axis")
            || groups.Select(value => value.GroupId).Distinct(StringComparer.Ordinal).Count() != groups.Length
            || members.Distinct(StringComparer.Ordinal).Count() != members.Length
            || members.Intersect(predecessor.Forces.Select(value => value.ElementId), StringComparer.Ordinal).Any())
            throw new InvalidOperationException("Supplemental Axis source groups overlap each other or retained setup.");
        var scenarioId = state.Request.ScenarioId;
        var seeds = Cna1979GrazianiSourceFacts.CreateLibyanDivisionInfantryToeSeeds(scenarioId)
            .Concat(Cna1979GrazianiSourceFacts.CreateLibyanDivisionSupportToeSeeds(scenarioId))
            .Append(Cna1979GrazianiSourceFacts.CreateAttachedNinthLibyanTankToeSeed(scenarioId))
            .Concat(Cna1979GrazianiSourceFacts.CreateLibyanTankRegimentToeCandidates(scenarioId))
            .Concat(Cna1979GrazianiSourceFacts.CreateMalettiToeCandidates(scenarioId))
            .Concat(Cna1979GrazianiSourceFacts.CreateCatanzaroToeCandidates(scenarioId))
            .Concat(Cna1979GrazianiSourceFacts.CreateFirstCcnnToeCandidates(scenarioId))
            .Concat(Cna1979GrazianiSourceFacts.CreateFourthCcnnToeCandidates(scenarioId))
            .Concat(Cna1979GrazianiSourceFacts.CreateCireneMarmaricaInfantryToeSeeds(scenarioId))
            .Concat(Cna1979GrazianiSourceFacts.CreateCireneMarmaricaAntiTankToeSeeds(scenarioId))
            .Concat(Cna1979GrazianiSourceFacts.CreateCireneMarmaricaArtilleryToeSeeds(scenarioId))
            .ToArray();
        if (seeds.Select(value => value.ElementId).Distinct(StringComparer.Ordinal).Count() != seeds.Length
            || seeds.Any(value => !members.Contains(value.ElementId, StringComparer.Ordinal)))
            throw new InvalidOperationException("Supplemental Axis source TOE entries do not match these groups.");
        var byId = seeds.ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        var supplement = new CampaignSourceAxisSupplementalForces(predecessor.Hash(), groups.Select(group => new CampaignSourceSupplementalGroup(
            group.GroupId, group.LocationId,
            Array.AsReadOnly(group.Origin.References.OrderBy(value => value.SourceId, StringComparer.Ordinal)
                .ThenBy(value => value.Locator, StringComparer.Ordinal).ToArray()),
            Array.AsReadOnly(group.ElementIds.Select(id => new CampaignSourceInitialForce(id, group.LocationId,
                    byId.TryGetValue(id, out var seed)
                        ? Array.AsReadOnly(seed.InitialComponentToes.Select(component =>
                            new CampaignSourceInitialToe(component.ComponentId, component.CurrentToe))
                            .OrderBy(component => component.ComponentId, StringComparer.Ordinal).ToArray())
                        : null))
                .OrderBy(value => value.ElementId, StringComparer.Ordinal).ToArray()))));
        if (supplement.Hash() != ContentHash)
            throw new InvalidOperationException("Supplemental Axis source content differs from the retained identity.");
        return supplement;
    }

    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("policyId", PolicyId);
            writer.WriteString("predecessorHash", PredecessorHash);
            writer.WriteBoolean("worldCoverageComplete", false);
            writer.WriteString("scenarioSourceHash", "sha256:fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66");
            writer.WriteString("axisChartsSourceHash", "sha256:9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca");
            writer.WriteString("landRulesSourceHash", "sha256:b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99");
            writer.WriteString("errataSourceHash", "sha256:5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb");
            writer.WriteString("organizationStatus", "attachment-mobility-land-only-unresolved");
            writer.WriteString("sourceReviewStatus", "candidate-not-independently-reviewed");
            writer.WriteString("mapTopologyStatus", "not-verified");
            writer.WriteStartArray("groups");
            foreach (var group in groups)
            {
                writer.WriteStartObject();
                writer.WriteString("groupId", group.GroupId);
                writer.WriteString("locationId", group.LocationId);
                writer.WriteString("sourceStatus", GroupSourceStatus(group.GroupId));
                writer.WriteStartArray("sources");
                foreach (var source in group.Sources)
                {
                    writer.WriteStartObject();
                    writer.WriteString("sourceId", source.SourceId);
                    writer.WriteString("locator", source.Locator);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteStartArray("forces");
                foreach (var force in group.Forces)
                {
                    writer.WriteStartObject();
                    writer.WriteString("elementId", force.ElementId);
                    writer.WriteString("sideId", "axis");
                    writer.WriteString("identityStatus", "source-candidate");
                    writer.WriteString("landOnlyDisposition", "unresolved");
                    writer.WriteNull("mobilityId");
                    writer.WriteNull("attachmentId");
                    if (force.InitialToes is null) writer.WriteNull("initialToes");
                    else
                    {
                        writer.WriteStartArray("initialToes");
                        foreach (var component in force.InitialToes)
                        {
                            writer.WriteStartObject();
                            writer.WriteString("componentId", component.ComponentId);
                            writer.WriteNumber("toe", component.Toe);
                            writer.WriteEndObject();
                        }
                        writer.WriteEndArray();
                    }
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));

    private static string GroupSourceStatus(string id) => id switch
    {
        "maletti-source-candidate" => "separate-lx-tank-company-split-unmodeled",
        "tripoli-tank-regiment-source-subset" => "lx-tank-company-and-remainder-not-included",
        "catanzaro-source-candidate" or "1-ccnn-source-candidate" or "4-ccnn-source-candidate" =>
            "division-hq-and-engineer-facts-incomplete",
        "1-libyan-division-source-candidate" or "2-libyan-division-source-candidate" =>
            "division-hq-attachment-and-tank-relationships-unresolved",
        "cirene-division-source-candidate" or "marmarica-division-source-candidate" =>
            "errata-formation-hq-engineer-and-weapon-semantics-incomplete",
        _ => "group-membership-and-component-semantics-not-fully-reviewed",
    };
}

internal sealed record CampaignSourceSupplementalGroup(string GroupId, string LocationId,
    IReadOnlyList<RuleReference> Sources, IReadOnlyList<CampaignSourceInitialForce> Forces);
