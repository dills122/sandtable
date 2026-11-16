using System.Security.Cryptography;
using System.Text.Json;

namespace Cna.Core.Campaigns;

internal sealed record CampaignSourceFourthIndianMember(string Id, string? ParentId, string Kind,
    int? Toe, int CapabilityPoints, int Stacking, string TrainingStatus);

// Source-bound roster for the actual 4th Indian division at D3615.
internal sealed class CampaignSourceFourthIndianDefinition
{
    internal const string PolicyId = "graziani.fourth-indian-setup.v1";
    internal const string HorseRuling = "provisional-deployed-central-india-horse-untrained-oa-note-c";
    internal const string PoolId = "cw-4-indian-attached";
    private static readonly System.Collections.ObjectModel.ReadOnlyCollection<CampaignSourceFourthIndianMember> Data = Array.AsReadOnly(new[]
    {
        new CampaignSourceFourthIndianMember("4-indian-division-hq", null, "headquarters", null, 30, 5, "not-applicable"),
        new CampaignSourceFourthIndianMember("5-indian-brigade-hq", "4-indian-division-hq", "headquarters", null, 30, 2, "not-applicable"),
        new CampaignSourceFourthIndianMember("1-royal-fusiliers", "5-indian-brigade-hq", "infantry", 6, 10, 1, "source-default"),
        new CampaignSourceFourthIndianMember("3-1-punjab", "5-indian-brigade-hq", "infantry", 6, 10, 1, "source-default"),
        new CampaignSourceFourthIndianMember("4-6-rajputana", "5-indian-brigade-hq", "infantry", 6, 10, 1, "source-default"),
        new CampaignSourceFourthIndianMember("11-indian-brigade-hq", "4-indian-division-hq", "headquarters", null, 30, 2, "not-applicable"),
        new CampaignSourceFourthIndianMember("2-camerons", "11-indian-brigade-hq", "infantry", 6, 10, 1, "source-default"),
        new CampaignSourceFourthIndianMember("1-6-rajputana", "11-indian-brigade-hq", "infantry", 6, 10, 1, "source-default"),
        new CampaignSourceFourthIndianMember("4-7-rajput", "11-indian-brigade-hq", "infantry", 6, 10, 1, "source-default"),
        new CampaignSourceFourthIndianMember("central-india-horse", "4-indian-division-hq", "motor-infantry", 6, 10, 1, "provisional-untrained"),
        new CampaignSourceFourthIndianMember("25-field-artillery", "4-indian-division-hq", "3-7-inch-howitzer", 6, 20, 1, "source-default"),
    });
    private CampaignSourceFourthIndianDefinition() { }
    internal static IReadOnlyList<CampaignSourceFourthIndianMember> Members => Data;
    internal static CampaignSourceFourthIndianDefinition Create(CampaignSourceAuthorityState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Pregame.Source.Session.Content.Identity.Hash != CampaignPregameSourceCreation.SupportedContentHash)
            throw new InvalidOperationException("Fourth Indian setup requires pinned Graziani source.");
        var source = state.CommonwealthInitialForces.Forces;
        var pool = state.CommonwealthMotorization.Pools.SingleOrDefault(value => value.Id == PoolId);
        if (pool is null || pool.Points != 22 || !pool.Attached || pool.SourceLocation != "d3615"
            || Data.Any(member => source.All(value => value.ElementId != member.Id || value.LocationId != "d3615"))
            || Data.Any(member => state.World.World.Elements.Any(value => value.ElementId == member.Id)
                || state.World.PendingArrivals.Any(group => group.ElementIds.Contains(member.Id, StringComparer.Ordinal)))
            || !state.World.PendingArrivals.Any(group => group.GroupId == "graziani-7-indian-brigade"))
            throw new InvalidOperationException("Fourth Indian source roster, pool, or deferred group differs.");
        return new();
    }
    internal static void Validate(CampaignGroupedWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        var group = world.Groups.SingleOrDefault(value => value.GroupId == "4-indian-division");
        var hasMatruh = world.Groups.Any(value => value.GroupId == "matruh-garrison");
        var hasMaletti = world.Groups.Any(value => value.GroupId == "maletti-division");
        var hasLibyan = world.Groups.Any(value => value.GroupId == "1-libyan-division");
        var hasItalian = world.Groups.Any(value => value.GroupId == CampaignSourceCireneMarmaricaGroupedDefinition.CireneId);
        var hasSeventh = world.Groups.Any(value => value.GroupId == CampaignSourceSeventhArmoredGroupedDefinition.GroupId);
        var hasSixteenth = world.Groups.Any(value => value.GroupId == CampaignSourceSixteenthBrigadeGroupedDefinition.GroupId);
        var hasCatanzaro = world.Groups.Any(value => value.GroupId == CampaignSourceCatanzaroGroupedDefinition.GroupId);
        var hasAustralian = world.Groups.Any(value => value.GroupId == CampaignSourceSixthAustralianGroupedDefinition.GroupId);
        var hasFirstCcnn = world.Groups.Any(value => value.GroupId == CampaignSourceCcnnBatch.FirstId);
        var hasFourthCcnn = world.Groups.Any(value => value.GroupId == CampaignSourceCcnnBatch.FourthId);
        var hasAresca = world.Groups.Any(value => value.GroupId == CampaignSourceArescaGroupedDefinition.GroupId);
        if (hasFirstCcnn != hasFourthCcnn || (hasAresca && !hasFirstCcnn) || (hasFirstCcnn && !hasCatanzaro) || (hasAustralian && !hasCatanzaro) || (hasCatanzaro && !hasSixteenth) || (hasSixteenth && !hasSeventh) || (hasMaletti && !hasMatruh) || (hasLibyan && !hasMaletti) || (hasItalian && !hasLibyan) || (hasSeventh && !hasItalian) || world.Groups.Count != (hasFirstCcnn ? 12 + (hasAustralian ? 1 : 0) + (hasAresca ? 1 : 0) : hasAustralian ? 11 : hasCatanzaro ? 10 : hasSixteenth ? 9 : hasSeventh ? 8 : hasItalian ? 7 : hasLibyan ? 5 : hasMaletti ? 3 : hasMatruh ? 2 : 1) || group is null || group.SideId != "commonwealth"
            || group.RootId != "4-indian-division-hq" || group.LocationId != "d3615"
            || group.StackValue != (group.DispositionConfirmed ? (int?)5 : null) || group.MotorPool != 22
            || (!group.DispositionConfirmed && group.Members.Any(member => member.AssignedMotorPoints != 0))
            || group.Members.Count != Data.Count
            || Data.Any(source => !group.Members.Any(member => member.ElementId == source.Id
                && member.ParentId == source.ParentId
                && member.InfantryToe == (source.Kind is "infantry" or "motor-infantry" ? source.Toe : null)
                && member.HistoricalMotorPriority == (source.Kind == "motor-infantry")
                && member.ComponentType == (source.Toe is null ? null : source.Kind)
                && member.ComponentToe == source.Toe
                && member.AdditionalComponents is not { Count: > 0 }
                && member.CapabilityPointAllowance == source.CapabilityPoints
                && member.CapabilityPointsExpended == world.Prior.Initialization.CapabilityPointsExpended
                && member.CohesionLevel == world.Prior.Initialization.CohesionLevel))
            || world.ContentHash != (world.Groups.Any(value => value.GroupId == CampaignSourceCcnnBatch.FirstId) ? Cna.Core.Content.ContentSourceCcnnBatchPackArtifact.HashFor(world.Groups.Any(value => value.GroupId == CampaignSourceSixthAustralianGroupedDefinition.GroupId), world.Groups.Any(value => value.GroupId == CampaignSourceArescaGroupedDefinition.GroupId)) : world.Groups.Any(value => value.GroupId == CampaignSourceSixthAustralianGroupedDefinition.GroupId) ? Cna.Core.Content.ContentSourceSixthAustralianGroupedPackArtifact.ContentHash : hasCatanzaro ? Cna.Core.Content.ContentSourceCatanzaroGroupedPackArtifact.ContentHash : hasSixteenth ? Cna.Core.Content.ContentSourceSixteenthBrigadeGroupedPackArtifact.ContentHash : hasSeventh ? Cna.Core.Content.ContentSourceSeventhArmoredGroupedPackArtifact.ContentHash : hasItalian ? Cna.Core.Content.ContentSourceCireneMarmaricaGroupedPackArtifact.ContentHash : hasLibyan ? Cna.Core.Content.ContentSourceLibyanGroupedPackArtifact.ContentHash : hasMaletti ? Cna.Core.Content.ContentSourceMalettiGroupedPackArtifact.ContentHash : hasMatruh ? Cna.Core.Content.ContentSourceMatruhGroupedPackArtifact.ContentHash : Cna.Core.Content.ContentSourceFourthIndianPackArtifact.ContentHash))
            throw new InvalidOperationException("Fourth Indian grouped world differs from pinned source definition.");
    }
    internal static byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("policyId", PolicyId);
            writer.WriteString("scenarioHash", "sha256:fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66");
            writer.WriteString("cwChartsHash", "sha256:f75d074727071025f3145b273921ec03882b8b57cb4a429485995d4cf5255981");
            writer.WriteString("airRulesHash", "sha256:9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4");
            writer.WriteString("landRulesHash", "sha256:b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99");
            writer.WriteString("sources", "scenario:60.41;cw-charts:oa-indian-p15,4.46a,19.31;land:4.45,8.95-8.96,9.26-9.27,32.54;air:counter-section-8-p43");
            writer.WriteString("dispositionPolicy", "provisional-source-default-requires-owner-confirmation-alternatives-unavailable");
            writer.WriteString("confirmedStackPolicy", "two-of-three-full-brigades-non-shell-printed-five-no-detachment");
            writer.WriteString("horseTrainingRuling", HorseRuling); writer.WriteString("horseSourceAmbiguity", "oa-arrival-wording-applied-to-scenario-predeployment");
            writer.WriteString("locationId", "d3615"); writer.WriteString("poolId", PoolId); writer.WriteNumber("poolPoints", 22);
            writer.WriteBoolean("globalPriorityComplete", false); writer.WriteStartArray("members");
            foreach (var member in Data.OrderBy(value => value.Id, StringComparer.Ordinal))
            {
                writer.WriteStartObject(); writer.WriteString("id", member.Id);
                if (member.ParentId is null) writer.WriteNull("parentId"); else writer.WriteString("parentId", member.ParentId);
                writer.WriteString("kind", member.Kind); if (member.Toe is { } toe) writer.WriteNumber("toe", toe); else writer.WriteNull("toe");
                writer.WriteNumber("capabilityPoints", member.CapabilityPoints); writer.WriteNumber("printedStacking", member.Stacking);
                writer.WriteString("trainingStatus", member.TrainingStatus); writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal static string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
}
