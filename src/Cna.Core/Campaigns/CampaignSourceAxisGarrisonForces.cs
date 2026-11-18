using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Campaigns;

/// <summary>
/// Source-candidate Giarabub and minor Axis garrison counters. Locations are
/// historical setup references, not a complete admitted map or playable world.
/// </summary>
internal sealed class CampaignSourceAxisGarrisonForces
{
    internal const string PolicyId = "graziani.axis-initial-forces.giarabub-minor.v1";
    // Derived from (and so changes whenever) CampaignSourceAxisSupplementalForces.ContentHash
    // changes - see RUL-GRA-26's note on that constant for the most recent reason.
    internal const string ContentHash = "sha256:84313115e7200cc53c732ff870945d7f48dbe7371bcea4f3d5e209f85ed6ad16";
    private readonly ReadOnlyCollection<CampaignSourceInitialForce> forces;

    private CampaignSourceAxisGarrisonForces(string baselineHash, string supplementHash,
        IEnumerable<CampaignSourceInitialForce> forces)
    {
        BaselineHash = baselineHash;
        SupplementHash = supplementHash;
        this.forces = Array.AsReadOnly(forces.OrderBy(value => value.ElementId, StringComparer.Ordinal).ToArray());
    }

    internal string BaselineHash { get; }
    internal string SupplementHash { get; }
    internal IReadOnlyList<CampaignSourceInitialForce> Forces => forces;

    internal static CampaignSourceAxisGarrisonForces Create(CampaignPregameSourceState state,
        CampaignSourceAxisInitialForces baseline, CampaignSourceAxisSupplementalForces supplement)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(supplement);
        if (state.Source.Session.Content.Identity.Hash != CampaignPregameSourceCreation.SupportedContentHash
            || baseline.Hash() != CampaignSourceAxisInitialForces.Create(state).Hash()
            || supplement.PredecessorHash != baseline.Hash()
            || supplement.Hash() != CampaignSourceAxisSupplementalForces.ContentHash)
            throw new InvalidOperationException("Axis garrisons require the retained Graziani force predecessors.");
        var group = Cna1979GrazianiSourceFacts.GiarabubGarrisonChartCandidate;
        var placements = Cna1979GrazianiSourceFacts.AxisMinorGarrisonPlacementCandidates;
        var groupMembers = group.ElementIds.ToArray();
        var ids = groupMembers.Concat(placements.Select(value => value.ElementId)).ToArray();
        var cwBaseline = CampaignSourceCommonwealthInitialForces.Create(state);
        var cwSupplement = CampaignSourceCommonwealthSupplementalForces.Create(state, cwBaseline);
        var world = state.Source.Session.InitialWorld;
        var represented = baseline.Forces.Select(value => value.ElementId)
            .Concat(supplement.Forces.Select(value => value.ElementId))
            .Concat(cwBaseline.Forces.Select(value => value.ElementId))
            .Concat(cwSupplement.Forces.Select(value => value.ElementId))
            .Concat(world.World.Elements.Select(value => value.ElementId))
            .Concat(world.PendingArrivals.SelectMany(value => value.ElementIds));
        if (group.SideId != "axis" || group.LocationId != "c1014" || groupMembers.Length != 6
            || placements.Count != 7 || ids.Distinct(StringComparer.Ordinal).Count() != ids.Length
            || ids.Intersect(represented, StringComparer.Ordinal).Any())
            throw new InvalidOperationException("Axis garrison sources overlap or differ from retained positions.");
        var seeds = Cna1979GrazianiSourceFacts.CreateGiarabubToeSeeds(state.Request.ScenarioId)
            .Concat(Cna1979GrazianiSourceFacts.CreateAxisMinorGarrisonToeSeeds(state.Request.ScenarioId))
            .ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        if (seeds.Count != ids.Length || seeds.Keys.Any(value => !ids.Contains(value, StringComparer.Ordinal)))
            throw new InvalidOperationException("Axis garrison source counters and TOE candidates disagree.");
        var locationById = groupMembers.Select(id => (Id: id, Location: group.LocationId))
            .Concat(placements.Select(value => (Id: value.ElementId, Location: value.LocationId)));
        var garrisons = new CampaignSourceAxisGarrisonForces(baseline.Hash(), supplement.Hash(), locationById.Select(value => new CampaignSourceInitialForce(
            value.Id, value.Location,
            Array.AsReadOnly(seeds[value.Id].InitialComponentToes.Select(component =>
                new CampaignSourceInitialToe(component.ComponentId, component.CurrentToe))
                .OrderBy(component => component.ComponentId, StringComparer.Ordinal).ToArray()),
            value.Id == "giarabub-motor-machine-gun"
                ? "historical-motorized-hh-8-plus-battalion-vs-company-eq-unresolved" : "source-candidate")));
        if (garrisons.Hash() != ContentHash)
            throw new InvalidOperationException("Axis garrison source content differs from retained identity.");
        return garrisons;
    }

    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("policyId", PolicyId);
            writer.WriteString("baselineHash", BaselineHash);
            writer.WriteString("supplementHash", SupplementHash);
            writer.WriteBoolean("worldCoverageComplete", false);
            writer.WriteString("scenarioSourceHash", "sha256:fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66");
            writer.WriteString("axisChartsSourceHash", "sha256:9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca");
            writer.WriteString("landRulesSourceHash", "sha256:b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99");
            writer.WriteString("sourceReviewStatus", "candidate-not-independently-reviewed");
            writer.WriteString("mapTopologyStatus", "not-verified");
            writer.WriteString("giarabubMotorizationStatus", "priority-needs-organization-and-transport");
            writer.WriteStartArray("sources");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.ScenariosSourceId + ":60.31");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.AxisChartsSourceId + ":oa-giarabub-p14");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.AxisChartsSourceId + ":oa-minor-garrisons-p14");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.AxisChartsSourceId + ":4.46b-hh");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.AxisCounterSheetSourceId + ":soluch-artillery-and-antitank");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.LandRulesSourceId + ":4.45");
            writer.WriteEndArray();
            writer.WriteStartArray("forces");
            foreach (var force in forces)
            {
                writer.WriteStartObject();
                writer.WriteString("elementId", force.ElementId);
                writer.WriteString("sideId", "axis");
                writer.WriteString("locationId", force.LocationId);
                writer.WriteString("identityStatus", force.IdentityStatus);
                writer.WriteString("landOnlyDisposition", "unresolved");
                writer.WriteNull("mobilityId");
                writer.WriteNull("attachmentId");
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
