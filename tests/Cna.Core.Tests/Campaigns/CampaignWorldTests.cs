using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Tests.Content;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignWorldTests
{
    [Fact]
    public void SnapshotDefensivelyCopiesSortsAndComparesElementsStructurally()
    {
        var input = new List<CampaignElementState>
        {
            new("commonwealth-element-a", "east"),
            new("axis-element-a", "west"),
        };
        var first = new CampaignWorldSnapshot(1, input);
        var equivalent = new CampaignWorldSnapshot(1, input.AsEnumerable().Reverse().ToArray());

        input.Clear();

        Assert.Equal(
            ["axis-element-a", "commonwealth-element-a"],
            first.Elements.Select(element => element.ElementId));
        Assert.Equal(first, equivalent);
        Assert.Equal(first.GetHashCode(), equivalent.GetHashCode());
    }

    [Fact]
    public void SnapshotRejectsInvalidContractValuesAndDuplicateElements()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CampaignWorldSnapshot(2, []));
        Assert.Throws<ArgumentException>(
            () => new CampaignElementState("Invalid ID", "west"));
        Assert.Throws<ArgumentException>(
            () => new CampaignWorldSnapshot(
                1,
                [
                    new CampaignElementState("axis-element-a", "west"),
                    new CampaignElementState("axis-element-a", "east"),
                ]));
    }

    [Fact]
    public void InitialProjectionExactlyMapsAndOrdersScenarioPlacements()
    {
        var artifact = Cna1979SyntheticContentCatalog.Artifact;
        var scenario = artifact.Definition.Scenarios.Single(
            candidate => candidate.ScenarioId == "movement-contact-lab");

        var world = CampaignWorldFactory.CreateInitial(artifact, scenario);

        Assert.Equal(
            [
                new CampaignElementState("axis-element-a", "west"),
                new CampaignElementState("axis-element-b", "north-west"),
                new CampaignElementState("commonwealth-element-a", "east"),
                new CampaignElementState("commonwealth-element-b", "south-east"),
            ],
            world.Elements);
        Assert.True(CampaignWorldValidator.IsValidInitial(world, artifact, scenario));
    }

    [Fact]
    public void InitialValidationRejectsMissingUnknownRelocatedAndInvalidLocationState()
    {
        var artifact = Cna1979SyntheticContentCatalog.Artifact;
        var scenario = artifact.Definition.Scenarios.Single(
            candidate => candidate.ScenarioId == "movement-contact-lab");
        var baseline = CampaignWorldFactory.CreateInitial(artifact, scenario);

        var missing = new CampaignWorldSnapshot(1, baseline.Elements.Skip(1).ToArray());
        var unknown = new CampaignWorldSnapshot(
            1,
            baseline.Elements.Append(new CampaignElementState("unknown-element", "west")).ToArray());
        var relocated = Replace(
            baseline,
            new CampaignElementState("axis-element-a", "east"));
        var invalidLocation = Replace(
            baseline,
            new CampaignElementState("axis-element-a", "unknown-location"));

        Assert.False(CampaignWorldValidator.IsValidInitial(missing, artifact, scenario));
        Assert.False(CampaignWorldValidator.IsValidInitial(unknown, artifact, scenario));
        Assert.False(CampaignWorldValidator.IsValidInitial(relocated, artifact, scenario));
        Assert.False(CampaignWorldValidator.IsValidInitial(invalidLocation, artifact, scenario));
    }

    [Fact]
    public void InitialValidationRejectsAttachmentOnlyElementsAndForeignScenarios()
    {
        var baseline = ContentTestData.CreateMinimalPack();
        var attachment = new ContentCombatElement(
            "axis-attachment",
            "axis",
            baseline.Formations[0].FormationId,
            "land.organization.battalion",
            10,
            ContentPlacementMode.AttachmentOnly,
            ContentTestData.Origin("content.element.attachment"));
        var artifact = ContentPackArtifact.Create(ContentTestData.Copy(
            baseline,
            elements: baseline.Elements.Append(attachment)));
        var scenario = artifact.Definition.Scenarios[0];
        var initial = CampaignWorldFactory.CreateInitial(artifact, scenario);
        var withAttachment = new CampaignWorldSnapshot(
            1,
            initial.Elements.Append(new CampaignElementState("axis-attachment", "west")).ToArray());
        var foreignScenario = Cna1979SyntheticContentCatalog.Artifact.Definition.Scenarios[0];

        Assert.False(CampaignWorldValidator.IsValidInitial(
            withAttachment,
            artifact,
            scenario));
        Assert.False(CampaignWorldValidator.IsValidInitial(
            initial,
            artifact,
            foreignScenario));
        Assert.Throws<ArgumentException>(
            () => CampaignWorldFactory.CreateInitial(artifact, foreignScenario));
    }

    private static CampaignWorldSnapshot Replace(
        CampaignWorldSnapshot world,
        CampaignElementState replacement) => new(
            1,
            world.Elements
                .Where(element => element.ElementId != replacement.ElementId)
                .Append(replacement)
                .ToArray());
}
