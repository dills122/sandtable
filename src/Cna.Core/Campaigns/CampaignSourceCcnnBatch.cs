using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Campaigns;

// Scenario 60.31 and Axis OA pp17-18. Owner accepts source-default
// colocated hierarchies for both divisions; other detachments remain unsupported.
internal static class CampaignSourceCcnnBatch
{
    internal const string FirstId = "1-ccnn-division";
    internal const string FourthId = "4-ccnn-division";
    private static string? Parent(string root, string id) => id == root ? null : id switch
    {
        "129-ccnn-infantry" or "133-ccnn-infantry" or "146-ccnn-infantry" => "219-ccnn-legion-hq",
        "114-ccnn-infantry" or "116-ccnn-infantry" or "118-ccnn-infantry" => "233-ccnn-legion-hq",
        "70-ccnn-infantry" or "119-ccnn-infantry" or "176-ccnn-infantry" => "240-ccnn-legion-hq",
        "3-ccnn-infantry" or "17-ccnn-infantry" or "170-ccnn-infantry" => "270-ccnn-legion-hq",
        _ => root,
    };
    private static int Capability(string id) => id switch
    {
        "1-ccnn-hq" => 20,
        "4-ccnn-hq" or "i-medium-tank" => 30,
        "219-ccnn-legion-hq" or "233-ccnn-legion-hq" or "240-ccnn-legion-hq" or "270-ccnn-legion-hq" => 20,
        "201-ccnn-machine-gun" or "204-ccnn-machine-gun" => 8,
        "201-ccnn-artillery" or "204-ccnn-artillery" => 15,
        "201-ccnn-engineer" or "204-ccnn-engineer" or "201-ccnn-bersaglieri" => 25,
        _ when id.EndsWith("-ccnn-infantry", StringComparison.Ordinal) => 10,
        _ => throw new InvalidOperationException("Unknown CCNN capability source.")
    };
    internal static IReadOnlyList<CampaignGroupedFormation> Create(CampaignSourceAuthorityState state)
    {
        var forces = state.AxisSupplement?.Forces.ToDictionary(value => value.ElementId, StringComparer.Ordinal)
            ?? throw new InvalidOperationException("Axis source supplement missing.");
        CampaignGroupedFormation Build(ContentInitialGroup source, string groupId, string root, string pool)
        {
            if (state.AxisMotorization.Remaining(pool) != 35) throw new InvalidOperationException("CCNN motor pool was changed.");
            var init = state.World.Initialization;
            var members = source.ElementIds.Select(id =>
            {
                var force = forces[id];
                if (force.LocationId != source.LocationId) throw new InvalidOperationException("CCNN source location differs.");
                var components = force.InitialToes?.Select(value => new CampaignGroupedComponent(
                    value.ComponentId[(id.Length + 1)..], value.Toe)).OrderBy(value => value.ComponentType, StringComparer.Ordinal).ToArray();
                var first = components?.FirstOrDefault();
                return new CampaignGroupedMember(id, Parent(root, id), first?.ComponentType == "infantry" ? first.Toe : null,
                    0, false, first?.ComponentType, first?.Toe, Capability(id), init.CapabilityPointsExpended, init.CohesionLevel,
                    components is { Length: > 1 } ? Array.AsReadOnly(components.Skip(1).ToArray()) : null);
            });
            return new(groupId, "axis", root, source.LocationId, 5, 35, members, true);
        }
        return Array.AsReadOnly(new[]
        {
            Build(Cna1979GrazianiSourceFacts.FirstCcnnInitialChartCandidate, FirstId, "1-ccnn-hq", "axis-c4218-attached"),
            Build(Cna1979GrazianiSourceFacts.FourthCcnnInitialChartCandidate, FourthId, "4-ccnn-hq", "axis-c4507-attached"),
        });
    }
    // Mirrors CampaignSourcePlayableWorld.ReadinessBlockers()'s
    // "ccnn-characteristics-incomplete" predicate so it is independently
    // unit-testable against synthetic/filtered groups, without needing the
    // full source-authority chain. RUL-GRA-26 resolves this for 1-ccnn-hq
    // and 4-ccnn-hq (one artillery point, weapon identity explicitly
    // unknown - never null); 201-ccnn-engineer/204-ccnn-engineer remain out
    // of that ruling's scope and still trip this when their ComponentType
    // is null.
    internal static bool HasIncompleteCharacteristics(IEnumerable<CampaignGroupedFormation> groups) =>
        groups.Where(value => value.GroupId is FirstId or FourthId)
            .Any(value => value.Members.Any(member => member.CapabilityPointAllowance is null
                || (member.ElementId is "1-ccnn-hq" or "4-ccnn-hq" or "201-ccnn-engineer" or "204-ccnn-engineer"
                    && member.ComponentType is null)));

    internal static void Validate(CampaignSourceAuthorityState state, CampaignGroupedWorld world)
    {
        foreach (var expected in Create(state))
        {
            var actual = world.Groups.Single(value => value.GroupId == expected.GroupId);
            if (actual.SideId != expected.SideId || actual.RootId != expected.RootId || actual.LocationId != expected.LocationId
                || actual.StackValue != 5 || !actual.DispositionConfirmed || actual.MotorPool != 35
                || actual.Members.Count != expected.Members.Count
                || actual.Members.Zip(expected.Members).Any(pair => pair.First with { AdditionalComponents = null } != pair.Second with { AdditionalComponents = null }
                    || !(pair.First.AdditionalComponents ?? []).SequenceEqual(pair.Second.AdditionalComponents ?? [])))
                throw new InvalidOperationException("CCNN group differs from source batch.");
        }
    }
    internal static byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("policyId", "graziani.ccnn-batch.v1");
            writer.WriteString("sources", "scenario:60.31;axis:oa-ccnn-p17-p18,4.46b;land:4.25,4.45,9.26");
            writer.WriteString("axisChartsHash", "sha256:9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca");
            writer.WriteString("supplementHash", CampaignSourceAxisSupplementalForces.ContentHash);
            writer.WriteString("disposition", "owner-accepted-source-default-unresolved-attachments-not-offered");
            writer.WriteString("knownLimitations", "division-hq-and-engineer-component-details-incomplete");
            writer.WriteString("firstId", FirstId); writer.WriteString("fourthId", FourthId);
            writer.WriteString("firstLocation", "c4218"); writer.WriteString("fourthLocation", "c4507");
            writer.WriteNumber("eachStack", 5); writer.WriteNumber("eachMotorPool", 35);
            writer.WriteStartArray("formations");
            foreach (var (source, root) in new[]
            {
                (Cna1979GrazianiSourceFacts.FirstCcnnInitialChartCandidate, "1-ccnn-hq"),
                (Cna1979GrazianiSourceFacts.FourthCcnnInitialChartCandidate, "4-ccnn-hq"),
            })
            {
                writer.WriteStartObject(); writer.WriteString("root", root); writer.WriteStartArray("members");
                foreach (var id in source.ElementIds.Order(StringComparer.Ordinal))
                { writer.WriteStartObject(); writer.WriteString("id", id); writer.WriteString("parent", Parent(root, id)); writer.WriteNumber("cpa", Capability(id)); writer.WriteEndObject(); }
                writer.WriteEndArray(); writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal static string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
}
