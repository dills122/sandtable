using System.Security.Cryptography;
using System.Text;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Content;

// Fixed synthetic fixture only. This is not campaign setup admission.
internal static class CampaignArrivalSyntheticProfile
{
    private const string Source = "sandtable-rules-lab";
    private const string Policy = "sandtable.synthetic-arrival-policy.v1:axis:1:1:south-west";
    public const string ProfileHash = "sha256:5da77965dd3976d8eb5c9805f6fa8be9d0ead54b70ff28a182f1a783255b3fbb";
    public static CampaignArrivalDraftIdentity Identity { get; } = CreateIdentity();

    public static CampaignArrivalDraftIdentity Resolve(ReadOnlySpan<byte> canonicalBytes, string expectedHash)
    {
        if (expectedHash != Identity.Hash || !canonicalBytes.SequenceEqual(Identity.CopyCanonicalBytes()))
            throw new ArgumentException("Unsupported synthetic arrival profile.", nameof(canonicalBytes));
        return CampaignArrivalDraftIdentityReader.Read(canonicalBytes, Identity.Hash);
    }

    private static CampaignArrivalDraftIdentity CreateIdentity()
    {
        var original = Cna1979SyntheticContentCatalog.Artifact.Definition;
        var scenario = original.Scenarios.Single(value => value.ScenarioId == "movement-contact-lab");
        var reference = new RuleReference(Source, "content.synthetic-arrival.v1");
        var origin = new ContentOrigin(ContentOriginKind.Synthetic, [reference]);
        var arriving = new ContentCombatElement("axis-arrival-lab", "axis", "axis-lab-formation",
            "land.organization.battalion", Cna1979Movement.NonMotorizedMobilityId, 10,
            ContentPlacementMode.Independent, origin);
        var pack = new ContentPackDefinition(original.SchemaVersion, original.FormatId,
            "rules-lab.content.arrival.v1", original.RulesetId, original.Capabilities, original.SourceIndex,
            original.Locations, original.WeatherAreaAssignments, original.Edges, original.Formations,
            [.. original.Elements, arriving], [scenario]);
        var group = new ScheduledArrivalGroup("axis-arrival-group", LandSide.Axis, 1, 1,
            [arriving.ElementId], [reference]);
        var rulesetHash = "sha256:" + Cna1979Ruleset.Manifest.Hash;
        var policyHash = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Policy)));
        var identity = CampaignArrivalDraftIdentity.Create(pack, scenario.ScenarioId, [group],
            new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal) { [group.GroupId] = ["south-west"] },
            rulesetHash, policyHash);
        if (identity.Hash != ProfileHash)
            throw new InvalidOperationException("Synthetic arrival profile inputs no longer match its pinned version.");
        return identity;
    }
}
