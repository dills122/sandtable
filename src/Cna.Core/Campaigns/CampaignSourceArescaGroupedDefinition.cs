using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Campaigns;

// Scenario 60.31; Axis OA p6; Land 9.26 and Common Charts 9.4.
// The other three assigned battalions are attached to other initial groups.
internal static class CampaignSourceArescaGroupedDefinition
{
    internal const string GroupId = "aresca-tank-regiment";
    internal const string RootId = "aresca-tank-regiment-hq";
    internal const string BattalionId = "xxi-libyan-light-tank";
    internal const string LocationId = "c3919";
    internal const string PoolId = "axis-c3919-attached";
    internal static CampaignGroupedFormation Create(CampaignSourceAuthorityState state)
    {
        var source = Cna1979GrazianiSourceFacts.ArescaTankRegimentInitialCandidate;
        var forces = state.AxisSupplement?.Forces.ToDictionary(value => value.ElementId, StringComparer.Ordinal)
            ?? throw new InvalidOperationException("Axis source supplement missing.");
        if (source.LocationId != LocationId || source.ElementIds.Count != 2 || state.AxisMotorization.Remaining(PoolId) != 10)
            throw new InvalidOperationException("Aresca source or attached motor pool differs.");
        var init = state.World.Initialization;
        var members = source.ElementIds.Select(id =>
        {
            var force = forces[id];
            var toe = id == RootId ? 1 : id == BattalionId ? 9 : throw new InvalidOperationException("Unknown Aresca member.");
            var component = force.InitialToes?.SingleOrDefault();
            if (force.LocationId != LocationId || component?.ComponentId != id + ".cv33" || component.Toe != toe)
                throw new InvalidOperationException("Aresca source tank strength differs.");
            return new CampaignGroupedMember(id, id == RootId ? null : RootId, null, 0, false,
                "cv33", toe, 30, init.CapabilityPointsExpended, init.CohesionLevel);
        });
        return new(GroupId, "axis", RootId, LocationId, 1, 10, members, true);
    }
    internal static void Validate(CampaignSourceAuthorityState state, CampaignGroupedWorld world)
    {
        var actual = world.Groups.Single(value => value.GroupId == GroupId);
        var expected = Create(state);
        if (actual.SideId != expected.SideId || actual.RootId != expected.RootId || actual.LocationId != LocationId
            || actual.StackValue != 1 || !actual.DispositionConfirmed || actual.MotorPool != 10
            || !actual.Members.SequenceEqual(expected.Members))
            throw new InvalidOperationException("Aresca group differs from source setup.");
    }
    internal static byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("policyId", "graziani.aresca-grouped.v1");
            writer.WriteString("sources", "scenario:60.31;axis:oa-libyan-tank-command-p6,4.46b;land:9.26;common:9.4");
            writer.WriteString("commonChartsHash", "sha256:51aa5a5bfdaca3d23794da71a45a830d98b798d060097cde6418126d6bd63bb0");
            writer.WriteString("supplementHash", CampaignSourceAxisSupplementalForces.ContentHash);
            writer.WriteString("groupId", GroupId); writer.WriteString("rootId", RootId); writer.WriteString("locationId", LocationId);
            writer.WriteString("detachedAssignedBattalions", "lxii-lxiii-i-medium");
            writer.WriteString("shellStatus", "one-of-four-assigned-battalions");
            writer.WriteNumber("stack", 1); writer.WriteString("poolId", PoolId); writer.WriteNumber("motorPoints", 10);
            writer.WriteStartArray("members");
            foreach (var (id, parent, toe) in new[] { (RootId, (string?)null, 1), (BattalionId, (string?)RootId, 9) })
            {
                writer.WriteStartObject(); writer.WriteString("id", id); writer.WriteString("parent", parent);
                writer.WriteString("component", "cv33"); writer.WriteNumber("toe", toe); writer.WriteNumber("cpa", 30); writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal static string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
}
