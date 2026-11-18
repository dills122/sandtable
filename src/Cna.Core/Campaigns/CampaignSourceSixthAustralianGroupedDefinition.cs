using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Campaigns;

// Scenario 60.41; CW OA p.13 and unit chart 4.46a; Land 4.45 and 9.26.
// Bounded policy represents all ten initially present counters in one hex.
internal static class CampaignSourceSixthAustralianGroupedDefinition
{
    internal const string GroupId = "6th-australian-division";
    internal const string RootId = "6-australian-division-hq";
    internal static readonly string[] Locations = ["e1430", "e1730", "e1829", "e1830", "e1930", "e1931"];
    // Commonwealth OA PDF p.13 ID codes (chart 4.46a, PDF pp.22-23, gives the
    // matching TOE/CPA/close-assault ratings - see
    // CampaignSourceGroupedZoc.TryDefensiveCloseAssaultRating): the nine
    // named infantry battalions are code 'p' (TOE 6, CPA 10, close assault
    // 1/2), 16th Brigade HQ's organic antitank is 'f' (TOE 1, CPA 20, close
    // assault 0/1), 17th Brigade HQ's organic antitank is 'e' (same TOE/CPA/
    // close assault as 'f'), and the divisional cavalry's reconnaissance
    // component is 'll' (TOE 8, CPA 45, close assault 2/3). Only six of the
    // nine battalions (16th/17th Brigades') plus both of those HQs plus
    // cavalry are represented here - 19th Brigade's HQ is code 'a', not
    // 'e'/'f', and scenario 60.41 excludes 19th Brigade from the initial
    // training group, so it is never given an organic antitank component by
    // pattern-matching off 16th/17th.
    private static readonly (string Id, string? Parent, string? Type, int? Toe, int Cpa)[] Units =
    [
        (RootId, null, null, null, 30),
        ("16-australian-infantry-brigade-hq", RootId, "organic-antitank", 1, 20),
        ("2-1-australian-infantry", "16-australian-infantry-brigade-hq", "infantry", 6, 10),
        ("2-2-australian-infantry", "16-australian-infantry-brigade-hq", "infantry", 6, 10),
        ("2-3-australian-infantry", "16-australian-infantry-brigade-hq", "infantry", 6, 10),
        ("17-australian-infantry-brigade-hq", RootId, "organic-antitank", 1, 20),
        ("2-5-australian-infantry", "17-australian-infantry-brigade-hq", "infantry", 6, 10),
        ("2-6-australian-infantry", "17-australian-infantry-brigade-hq", "infantry", 6, 10),
        ("2-7-australian-infantry", "17-australian-infantry-brigade-hq", "infantry", 6, 10),
        ("6-australian-divisional-cavalry", RootId, "reconnaissance", 8, 45),
    ];
    internal static CampaignGroupedFormation Create(CampaignSourceAuthorityState state, string location)
    {
        if (!Locations.Contains(location, StringComparer.Ordinal)) throw new InvalidOperationException("Unknown Australian setup hex.");
        var init = state.World.Initialization;
        var members = Units.Select(unit => new CampaignGroupedMember(unit.Id, unit.Parent, unit.Type == "infantry" ? unit.Toe : null, 0, false,
            unit.Type, unit.Toe, unit.Cpa, init.CapabilityPointsExpended, init.CohesionLevel));
        return new(GroupId, "commonwealth", RootId, location, 5, 0, members, true, motorPoolKnown: false);
    }
    internal static void Validate(CampaignSourceAuthorityState state, CampaignGroupedWorld world)
    {
        var group = world.Groups.Single(value => value.GroupId == GroupId);
        var expected = Create(state, group.LocationId);
        if (world.ContentHash != (world.Groups.Any(value => value.GroupId == CampaignSourceCcnnBatch.FirstId) ? ContentSourceCcnnBatchPackArtifact.HashFor(world.Groups.Any(value => value.GroupId == CampaignSourceSixthAustralianGroupedDefinition.GroupId), world.Groups.Any(value => value.GroupId == CampaignSourceArescaGroupedDefinition.GroupId)) : ContentSourceSixthAustralianGroupedPackArtifact.ContentHash)
            || group.SideId != expected.SideId || group.RootId != expected.RootId || group.StackValue != 5
            || !group.DispositionConfirmed || group.MotorPoolKnown || group.MotorPool != 0
            || !group.Members.SequenceEqual(expected.Members))
            throw new InvalidOperationException("Australian group differs from its pinned source setup.");
    }
    internal static byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("policyId", "graziani.sixth-australian.grouped.v1");
            writer.WriteString("sources", "scenario:60.41;commonwealth-oa:p13;commonwealth:4.46a;land:4.45,9.26,17.31-17.36");
            writer.WriteString("placementPolicy", "owner-selected-single-hex-split-unsupported");
            writer.WriteString("moraleStatus", "untrained-minus-three-trained-plus-one-current-unknown");
            writer.WriteString("motorPoolStatus", "unknown"); writer.WriteNumber("stackValue", 5);
            writer.WriteStartArray("locations"); foreach (var location in Locations) writer.WriteStringValue(location); writer.WriteEndArray();
            writer.WriteStartArray("members");
            foreach (var unit in Units.OrderBy(value => value.Id, StringComparer.Ordinal))
            {
                writer.WriteStartObject(); writer.WriteString("id", unit.Id); writer.WriteString("parent", unit.Parent);
                writer.WriteString("type", unit.Type); if (unit.Toe is int toe) writer.WriteNumber("toe", toe); else writer.WriteNull("toe");
                writer.WriteNumber("cpa", unit.Cpa); writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal static string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
}
