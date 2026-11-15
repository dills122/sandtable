using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Campaigns;

// Source-derived initial counters, not a complete Axis world or playable map.
// Unknown headquarters, attachment and mobility facts remain explicitly unknown.
internal sealed class CampaignSourceAxisInitialForces
{
    internal const string PolicyId = "graziani.axis-initial-forces.tobruk-bardia.v1";
    private readonly ReadOnlyCollection<CampaignSourceInitialForce> forces;

    private CampaignSourceAxisInitialForces(IEnumerable<CampaignSourceInitialForce> forces) =>
        this.forces = Array.AsReadOnly(forces.OrderBy(value => value.ElementId, StringComparer.Ordinal).ToArray());

    internal IReadOnlyList<CampaignSourceInitialForce> Forces => forces;

    internal static CampaignSourceAxisInitialForces Create(CampaignPregameSourceState pregame)
    {
        ArgumentNullException.ThrowIfNull(pregame);
        if (pregame.Source.Session.Content.Identity.Hash != CampaignPregameSourceCreation.SupportedContentHash)
            throw new InvalidOperationException("Initial Axis forces require the pinned Graziani source.");
        var infantry = Cna1979GrazianiSourceFacts.TobrukInfantryChartSubset;
        var support = Cna1979GrazianiSourceFacts.TobrukArtilleryAndAaChartSubset;
        var bardia = Cna1979GrazianiSourceFacts.BardiaGarrisonChartCandidate;
        var tobrukMembers = infantry.ElementIds.Concat(support.ElementIds).ToArray();
        var members = tobrukMembers.Concat(bardia.ElementIds).ToArray();
        var seeds = Cna1979GrazianiSourceFacts.CreateTobrukInfantryToeSeeds(pregame.Request.ScenarioId)
            .Concat(Cna1979GrazianiSourceFacts.CreateTobrukSupportToeSeeds(pregame.Request.ScenarioId))
            .Concat(Cna1979GrazianiSourceFacts.CreateBardiaInfantryToeSeeds(pregame.Request.ScenarioId))
            .Concat(Cna1979GrazianiSourceFacts.CreateBardiaSupportToeSeeds(pregame.Request.ScenarioId))
            .ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        if (infantry.SideId != "axis" || infantry.LocationId != "c4807" || infantry.ElementIds.Count != 13
            || support.SideId != "axis" || support.LocationId != "c4807" || support.ElementIds.Count != 9
            || bardia.SideId != "axis" || bardia.LocationId != "c4321" || bardia.ElementIds.Count != 11
            || members.Distinct(StringComparer.Ordinal).Count() != members.Length
            || seeds.Count != 29 || seeds.Keys.Any(id => !members.Contains(id, StringComparer.Ordinal)))
            throw new InvalidOperationException("Axis source groups and strength entries do not agree.");
        return new CampaignSourceAxisInitialForces(members.Select(id =>
            new CampaignSourceInitialForce(id, bardia.ElementIds.Contains(id, StringComparer.Ordinal)
                    ? bardia.LocationId : infantry.LocationId,
                seeds.TryGetValue(id, out var seed)
                    ? Array.AsReadOnly(seed.InitialComponentToes.Select(component =>
                        new CampaignSourceInitialToe(component.ComponentId, component.CurrentToe))
                        .OrderBy(component => component.ComponentId, StringComparer.Ordinal).ToArray())
                    : null,
                bardia.ElementIds.Contains(id, StringComparer.Ordinal) || id.Contains("32-gaf", StringComparison.Ordinal)
                    ? "bardia-30-32-gaf-source-conflict" : "source-candidate")));
    }

    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("policyId", PolicyId);
            writer.WriteBoolean("worldCoverageComplete", false);
            writer.WriteStartArray("sources");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.ScenariosSourceId + ":60.31");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.AxisChartsSourceId + ":oa-tobruk-p12");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.AxisChartsSourceId + ":oa-bardia-p13");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.AxisCounterSheetSourceId + ":30-gaf-bardia-32-gaf-tobruk");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.LandRulesSourceId + ":4.45");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.LandRulesSourceId + ":32.83");
            writer.WriteEndArray();
            writer.WriteStartArray("forces");
            foreach (var force in forces)
            {
                writer.WriteStartObject();
                writer.WriteString("elementId", force.ElementId);
                writer.WriteString("sideId", "axis");
                writer.WriteString("locationId", force.LocationId);
                writer.WriteString("identityStatus", force.IdentityStatus);
                // The source OA contains AA units and components; their Land-only
                // removal/eligibility has not been adjudicated in this ledger.
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
        return stream.ToArray();
    }

    internal string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
}

internal sealed record CampaignSourceInitialForce(string ElementId, string LocationId,
    IReadOnlyList<CampaignSourceInitialToe>? InitialToes, string IdentityStatus = "source-candidate");

internal sealed record CampaignSourceInitialToe(string ComponentId, int Toe);
