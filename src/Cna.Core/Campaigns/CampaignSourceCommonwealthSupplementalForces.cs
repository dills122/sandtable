using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Campaigns;

/// <summary>
/// Additional named Commonwealth starting counters on Map C, supplementing
/// the retained Indian/Armoured force ledger and Cairo source genesis.
/// </summary>
internal sealed class CampaignSourceCommonwealthSupplementalForces
{
    internal const string PolicyId = "graziani.commonwealth-initial-forces.map-c-supplement.v1";
    internal const string ContentHash = "sha256:a8fa59467db15c42e8f811caae738efc39e5ffb0976d50cf0e071406a03d0407";
    private readonly ReadOnlyCollection<CampaignSourceInitialForce> forces;

    private CampaignSourceCommonwealthSupplementalForces(string predecessorHash,
        IEnumerable<CampaignSourceInitialForce> forces)
    {
        PredecessorHash = predecessorHash;
        this.forces = Array.AsReadOnly(forces.OrderBy(value => value.ElementId, StringComparer.Ordinal).ToArray());
    }

    internal string PredecessorHash { get; }
    internal IReadOnlyList<CampaignSourceInitialForce> Forces => forces;

    internal static CampaignSourceCommonwealthSupplementalForces Create(CampaignPregameSourceState state,
        CampaignSourceCommonwealthInitialForces predecessor)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(predecessor);
        if (state.Source.Session.Content.Identity.Hash != CampaignPregameSourceCreation.SupportedContentHash
            || predecessor.Hash() != CampaignSourceCommonwealthInitialForces.Create(state).Hash())
            throw new InvalidOperationException("Commonwealth supplement requires the retained Graziani source and predecessor.");
        var placements = Cna1979GrazianiSourceFacts.CommonwealthInitialPlacementCandidates;
        var ids = placements.Select(value => value.ElementId).ToArray();
        var world = state.Source.Session.InitialWorld;
        var alreadyRepresented = predecessor.Forces.Select(value => value.ElementId)
            .Concat(world.World.Elements.Select(value => value.ElementId))
            .Concat(world.PendingArrivals.SelectMany(value => value.ElementIds));
        if (ids.Length != 12 || ids.Distinct(StringComparer.Ordinal).Count() != ids.Length
            || ids.Intersect(alreadyRepresented, StringComparer.Ordinal).Any())
            throw new InvalidOperationException("Commonwealth source placements duplicate retained or arriving counters.");
        var seeds = Cna1979GrazianiSourceFacts.CreateCommonwealthInitialToeSeeds(state.Request.ScenarioId)
            .ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        if (seeds.Count != ids.Length || seeds.Keys.Any(id => !ids.Contains(id, StringComparer.Ordinal)))
            throw new InvalidOperationException("Commonwealth source placement and strength candidates do not agree.");
        var supplement = new CampaignSourceCommonwealthSupplementalForces(predecessor.Hash(), placements.Select(placement => new CampaignSourceInitialForce(
            placement.ElementId, placement.LocationId,
            Array.AsReadOnly(seeds[placement.ElementId].InitialComponentToes.Select(component =>
                new CampaignSourceInitialToe(component.ComponentId, component.CurrentToe))
                .OrderBy(component => component.ComponentId, StringComparer.Ordinal).ToArray()),
            placement.ElementId == "1-french-motor-marines"
                ? "scenario-setup-vs-oa-arrival-errata-unresolved" : "source-candidate")));
        if (supplement.Hash() != ContentHash)
            throw new InvalidOperationException("Supplemental Commonwealth source content differs from retained identity.");
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
            writer.WriteString("commonwealthChartsSourceHash", "sha256:f75d074727071025f3145b273921ec03882b8b57cb4a429485995d4cf5255981");
            writer.WriteString("landRulesSourceHash", "sha256:b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99");
            writer.WriteString("errataSourceHash", "sha256:5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb");
            writer.WriteString("sourceReviewStatus", "candidate-not-independently-reviewed");
            writer.WriteString("mapTopologyStatus", "not-verified");
            writer.WriteStartArray("sources");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.ScenariosSourceId + ":60.41");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.CommonwealthChartsSourceId + ":oa-7-armored-p6");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.CommonwealthChartsSourceId + ":oa-british-p11");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.CommonwealthChartsSourceId + ":oa-indian-p15");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.ErrataSourceId + ":4.44b");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.LandRulesSourceId + ":4.45");
            writer.WriteEndArray();
            writer.WriteStartArray("forces");
            foreach (var force in forces)
            {
                writer.WriteStartObject();
                writer.WriteString("elementId", force.ElementId);
                writer.WriteString("sideId", "commonwealth");
                writer.WriteString("locationId", force.LocationId);
                writer.WriteString("identityStatus", force.IdentityStatus);
                writer.WriteNull("mobilityId");
                writer.WriteNull("attachmentId");
                writer.WriteString("trainingStatus", "unresolved");
                writer.WriteString("landOnlyDisposition", "unresolved");
                writer.WriteStartArray("initialToes");
                foreach (var component in force.InitialToes!)
                {
                    writer.WriteStartObject();
                    writer.WriteString("componentId", component.ComponentId);
                    writer.WriteNumber("toe", component.Toe);
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
}
