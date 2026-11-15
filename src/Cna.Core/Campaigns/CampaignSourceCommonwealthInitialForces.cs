using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Campaigns;

// Printed group placements; this is not a complete initial world, attachment
// selection, or a ruling on Central India Horse's starting training state.
internal sealed class CampaignSourceCommonwealthInitialForces
{
    internal const string PolicyId = "graziani.commonwealth-initial-forces.divisions.v1";
    private readonly ReadOnlyCollection<CampaignSourceInitialForce> forces;

    private CampaignSourceCommonwealthInitialForces(IEnumerable<CampaignSourceInitialForce> values) =>
        forces = Array.AsReadOnly(values.OrderBy(value => value.ElementId, StringComparer.Ordinal).ToArray());

    internal IReadOnlyList<CampaignSourceInitialForce> Forces => forces;

    internal static CampaignSourceCommonwealthInitialForces Create(CampaignPregameSourceState pregame)
    {
        ArgumentNullException.ThrowIfNull(pregame);
        if (pregame.Source.Session.Content.Identity.Hash != CampaignPregameSourceCreation.SupportedContentHash)
            throw new InvalidOperationException("Initial Commonwealth forces require the pinned Graziani source.");
        var groups = new[] { Cna1979GrazianiSourceFacts.FourthIndianDivisionInitialCandidate,
            Cna1979GrazianiSourceFacts.SeventhArmoredDivisionInitialCandidate };
        var seeds = Cna1979GrazianiSourceFacts.CreateFourthIndianDivisionToeCandidates(pregame.Request.ScenarioId)
            .Concat(Cna1979GrazianiSourceFacts.CreateSeventhArmoredDivisionToeCandidates(pregame.Request.ScenarioId))
            .ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        var ids = groups.SelectMany(group => group.ElementIds).ToArray();
        if (groups.Any(group => group.SideId != "commonwealth") || ids.Distinct(StringComparer.Ordinal).Count() != ids.Length
            || seeds.Keys.Any(id => !ids.Contains(id, StringComparer.Ordinal)))
            throw new InvalidOperationException("Commonwealth source groups and strengths do not agree.");
        return new CampaignSourceCommonwealthInitialForces(groups.SelectMany(group => group.ElementIds.Select(id =>
            new CampaignSourceInitialForce(id, group.LocationId,
                seeds.TryGetValue(id, out var seed) && seed.InitialComponentToes.Count > 0
                    ? Array.AsReadOnly(seed.InitialComponentToes.Select(component =>
                        new CampaignSourceInitialToe(component.ComponentId, component.CurrentToe))
                        .OrderBy(component => component.ComponentId, StringComparer.Ordinal).ToArray())
                    : null))));
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
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.ScenariosSourceId + ":60.41");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.CommonwealthChartsSourceId + ":oa-indian-p15");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.CommonwealthChartsSourceId + ":oa-7-armored-p6");
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
