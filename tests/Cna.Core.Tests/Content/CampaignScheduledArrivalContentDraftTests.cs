using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Content;

public sealed class CampaignScheduledArrivalContentDraftTests
{
    [Fact]
    public void RealContentReportsDeferredElementWithoutChangingLegacyValidation()
    {
        var pack = PackWithDeferred();
        var draft = CampaignScheduledArrivalContentDraft.Analyze(pack, "minimal-lab", [Group("one", "later")]);
        var path = "/scenarios/minimal-lab/initialPlacements/later";

        Assert.Contains(draft.LegacyValidation.Issues, issue => issue.Code == "placement.missing-element" && issue.Path == path);
        Assert.Empty(draft.DraftValidation.Issues);
        Assert.Empty(draft.UnexplainedLegacyValidation.Issues);
        Assert.Equal([path], draft.ExplainedLegacyMissingPlacementPaths);
        Assert.Contains(ContentPackValidator.Validate(pack).Issues, issue => issue.Code == "placement.missing-element" && issue.Path == path);
        Assert.Throws<InvalidContentPackException>(() => ContentPackArtifact.Create(pack));
        Assert.Throws<NotSupportedException>(() => ((IList<string>)draft.ExplainedLegacyMissingPlacementPaths).Clear());
    }

    [Fact]
    public void UnknownWrongSideBadSourceAndOutsideSlotCannotExplainMissingPlacement()
    {
        var pack = PackWithDeferred();
        ScheduledArrivalGroup[] groups =
        [
            Group("wrong-side", "later", side: LandSide.Commonwealth),
            Group("unknown", "not-in-pack"),
            Group("bad-source", "later", source: "not-in-index"),
            Group("outside", "later", turn: 2),
        ];

        foreach (var group in groups)
        {
            var result = CampaignScheduledArrivalContentDraft.Analyze(pack, "minimal-lab", [group]);
            Assert.NotEmpty(result.DraftValidation.Issues);
            Assert.Empty(result.ExplainedLegacyMissingPlacementPaths);
            Assert.Contains(result.LegacyValidation.Issues, issue => issue.Code == "placement.missing-element");
        }
    }

    [Fact]
    public void DuplicateGroupOrElementAndInitiallyPlacedUnitDoNotExplainAnArrival()
    {
        var pack = PackWithDeferred();
        ScheduledArrivalGroup[][] cases =
        [
            [Group("same", "later"), Group("same", "later")],
            [Group("first", "later"), Group("second", "later", stage: 3)],
            [Group("placed", "axis-element")],
        ];
        foreach (var groups in cases)
        {
            var result = CampaignScheduledArrivalContentDraft.Analyze(pack, "minimal-lab", groups);
            Assert.NotEmpty(result.DraftValidation.Issues);
            Assert.Empty(result.ExplainedLegacyMissingPlacementPaths);
        }
    }

    [Fact]
    public void ExplanationNeverAppliesToAnotherScenarioOrUnlistedElement()
    {
        var pack = PackWithDeferred();
        var original = pack.Scenarios.Single();
        var second = new ContentScenario("second-lab", original.Start, original.End,
            original.InitialPlacements, ContentTestData.Origin("scenario.second"));
        pack = ContentTestData.Copy(pack, scenarios: [original, second]);

        var result = CampaignScheduledArrivalContentDraft.Analyze(pack, "minimal-lab", [Group("one", "later")]);
        Assert.Equal(["/scenarios/minimal-lab/initialPlacements/later"], result.ExplainedLegacyMissingPlacementPaths);
        Assert.Contains(result.LegacyValidation.Issues, issue => issue.Path == "/scenarios/second-lab/initialPlacements/later");
        Assert.Contains(result.UnexplainedLegacyValidation.Issues, issue => issue.Path == "/scenarios/second-lab/initialPlacements/later");

        var empty = CampaignScheduledArrivalContentDraft.Analyze(pack, "minimal-lab", []);
        Assert.Empty(empty.ExplainedLegacyMissingPlacementPaths);
        Assert.Contains(empty.LegacyValidation.Issues, issue => issue.Path == "/scenarios/minimal-lab/initialPlacements/later");
        Assert.NotEmpty(CampaignScheduledArrivalContentDraft.Analyze(pack, "unknown", []).DraftValidation.Issues);
    }

    [Fact]
    public void ADefectiveGroupCannotMaskItsValidMemberOrOtherLegacyErrors()
    {
        var pack = PackWithDeferred();
        var mixed = new ScheduledArrivalGroup("mixed", LandSide.Axis, 1, 2,
            ["later", "not-in-pack"], [new RuleReference("sandtable-rules-lab", "arrival.test")]);
        var result = CampaignScheduledArrivalContentDraft.Analyze(pack, "minimal-lab", [mixed]);
        Assert.Contains(result.DraftValidation.Issues, issue => issue.Code == "arrival.element");
        Assert.Empty(result.ExplainedLegacyMissingPlacementPaths);
        Assert.Contains(result.UnexplainedLegacyValidation.Issues,
            issue => issue.Path == "/scenarios/minimal-lab/initialPlacements/later");

        var attached = new ContentCombatElement("attached", "axis", "axis-formation",
            "land.organization.battalion", Cna1979Movement.MotorizedMobilityId, 20,
            ContentPlacementMode.AttachmentOnly, ContentTestData.Origin("element.attached"));
        pack = ContentTestData.Copy(pack, elements: [.. pack.Elements, attached]);
        result = CampaignScheduledArrivalContentDraft.Analyze(pack, "minimal-lab", [Group("attached-group", "attached")]);
        Assert.Contains(result.DraftValidation.Issues, issue => issue.Code == "arrival.element-owner-or-placement");
        Assert.Empty(result.ExplainedLegacyMissingPlacementPaths);
    }

    private static ContentPackDefinition PackWithDeferred()
    {
        var pack = ContentTestData.CreateMinimalPack();
        var deferred = new ContentCombatElement("later", "axis", "axis-formation",
            "land.organization.battalion", Cna1979Movement.MotorizedMobilityId, 20,
            ContentPlacementMode.Independent, ContentTestData.Origin("element.later"));
        return ContentTestData.Copy(pack, elements: [.. pack.Elements, deferred]);
    }

    private static ScheduledArrivalGroup Group(string id, string unit, LandSide side = LandSide.Axis,
        int turn = 1, int stage = 2, string source = "sandtable-rules-lab") =>
        new(id, side, turn, stage, [unit], [new RuleReference(source, "arrival.test")]);
}
