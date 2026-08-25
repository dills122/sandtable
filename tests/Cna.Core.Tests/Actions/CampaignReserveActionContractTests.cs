using System.Reflection;
using System.Text;
using Cna.Core.Actions;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Actions;

public sealed class CampaignReserveActionContractTests
{
    [Fact]
    public void EveryExistingCandidateIdRemainsFrozen()
    {
        (CampaignActionCandidate Candidate, string ActionId)[] cases =
        [
            (new ResolveInitiativeAction(),
                "sha256:550cc400544d848b77230f49747ee287eb9fe01b730dc9d5fd200e3e7591d12f"),
            (new ResolveNoObligationNavalConvoyScheduleAction(),
                "sha256:345996f7b8351499e74c62f5e1a62d07170431519e9f12483874302e3c86a78f"),
            (new ResolveNoObligationTacticalShippingAction(),
                "sha256:2e6e64614ddacd1da51af69c8da0bca0e7b7b859f47ac8e0057107131112d2d6"),
            (new ResolveWeatherAction(),
                "sha256:61bca28b7e06c2ec8b7919bce4c7c226198e7fecb0afcc2186b224311e7e1413"),
            (new ResolveNoObligationOrganizationAction(),
                "sha256:2200e6c4cef001d344d85de78fc7a10c13b32c12975d905c633ca430c3c4bd4c"),
            (new ResolveNoObligationNavalConvoyArrivalAction(),
                "sha256:a49ff99f7e52193fdee44b50751e64025121cb9a2a75a054fdf2ad045e013632"),
            (new ResolveNoObligationFleetAssignmentAction(),
                "sha256:c2d7dae34d20f826d2e7e682b8d3b437224e42b522857f63b3869d1a1bf3bcc5"),
            (new ResolveNoObligationFleetRepairAction(),
                "sha256:ea4fe4f27344a8659c81b05fd84df2e260bb22da1edf1115cd4d75dfd89d7d3e"),
            (new ActFirstAction(1),
                "sha256:6219b220b77d0d9a2c5909d83907910c0e30dca02e4fb5b3efba0b10954d78f8"),
            (new ActLastAction(1),
                "sha256:6c81de67130cc2fd57632326bfb654daabfb1d5d064768871b40a9a91b4ea84f"),
        ];

        Assert.All(cases, value => Assert.Equal(value.ActionId, value.Candidate.ActionId));
    }

    [Fact]
    public void ReserveCandidatesHaveExactSemanticsAndIds()
    {
        var designation = new DesignateReserveAction("axis-element-a");
        var completion = new CompleteReserveDesignationAction();
        var designationSemantics = Encoding.UTF8.GetBytes(
            "{\"contractVersion\":1,\"kind\":\"designate-reserve\"," +
            "\"elementId\":\"axis-element-a\"}");
        var completionSemantics = Encoding.UTF8.GetBytes(
            "{\"contractVersion\":1,\"kind\":\"complete-reserve-designation\"}");

        Assert.Equal("designate-reserve", designation.Kind);
        Assert.Equal("axis-element-a", designation.ElementId);
        Assert.Null(designation.OperationStage);
        Assert.Equal(
            "sha256:cc92582163def43d5ef16267cbc50e3d55e8db5e7bd949a12943303fada50c60",
            designation.ActionId);
        Assert.Equal(
            designationSemantics,
            CampaignActionCandidate.WriteSubjectSemantics(
                designation.Kind,
                designation.ElementId));

        Assert.Equal("complete-reserve-designation", completion.Kind);
        Assert.Null(completion.OperationStage);
        Assert.Equal(
            "sha256:eb5be22cb75b092e730000d3cc902f76cc73727f349b8fb8c2844f270130c774",
            completion.ActionId);
        Assert.Equal(
            completionSemantics,
            CampaignActionCandidate.WriteSemantics(completion.Kind, null));
    }

    [Fact]
    public void DesignationRejectsUnstableElementIdAndUsesStructuralEquality()
    {
        var first = new DesignateReserveAction("axis-element-a");
        var equivalent = new DesignateReserveAction("axis-element-a");
        var different = new DesignateReserveAction("axis-element-b");

        Assert.Throws<ArgumentException>(() => new DesignateReserveAction("Invalid ID"));
        Assert.Equal(first, equivalent);
        Assert.Equal(first.GetHashCode(), equivalent.GetHashCode());
        Assert.NotEqual(first, different);
    }

    [Fact]
    public void ReserveCandidateSetSerializationIsExactAndClosed()
    {
        var set = new CampaignLegalActionSet(
            "campaign-reserve",
            10,
            Cna1979Ruleset.Manifest.Hash,
            "land.position.operation-1.reserve-designation",
            CampaignActionAudience.Axis,
            [
                new DesignateReserveAction("axis-element-a"),
                new CompleteReserveDesignationAction(),
            ]);

        Assert.Equal(
            $"{{\"contractVersion\":2,\"policyId\":\"sandtable.legal-actions.v2\"," +
            $"\"campaignId\":\"campaign-reserve\",\"stateVersion\":10," +
            $"\"rulesetHash\":\"{Cna1979Ruleset.Manifest.Hash}\"," +
            "\"positionId\":\"land.position.operation-1.reserve-designation\"," +
            "\"audience\":\"axis\",\"candidates\":[" +
            "{\"contractVersion\":1," +
            "\"actionId\":\"sha256:eb5be22cb75b092e730000d3cc902f76cc73727f349b8fb8c2844f270130c774\"," +
            "\"kind\":\"complete-reserve-designation\"}," +
            "{\"contractVersion\":1," +
            "\"actionId\":\"sha256:cc92582163def43d5ef16267cbc50e3d55e8db5e7bd949a12943303fada50c60\"," +
            "\"kind\":\"designate-reserve\",\"elementId\":\"axis-element-a\"}]}",
            Encoding.UTF8.GetString(CampaignLegalActionSerializer.Serialize(set)));

        var unsupported = new CampaignLegalActionSet(
            "campaign-reserve",
            10,
            Cna1979Ruleset.Manifest.Hash,
            "land.position.operation-1.reserve-designation",
            CampaignActionAudience.Axis,
            [new UnsupportedPayloadAction()]);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CampaignLegalActionSerializer.Serialize(unsupported));
    }

    [Fact]
    public void ReserveCandidatesAreClosedOutputOnlyContracts()
    {
        Assert.All(
            new[]
            {
                typeof(DesignateReserveAction),
                typeof(CompleteReserveDesignationAction),
            },
            type =>
            {
                Assert.True(type.IsPublic);
                Assert.True(type.IsSealed);
                Assert.Empty(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
            });
        Assert.Equal(
            [nameof(DesignateReserveAction.ElementId)],
            typeof(DesignateReserveAction)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance
                    | BindingFlags.DeclaredOnly)
                .Select(property => property.Name));
        Assert.Empty(typeof(CompleteReserveDesignationAction)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance
                | BindingFlags.DeclaredOnly));
    }

    private sealed record UnsupportedPayloadAction : CampaignActionCandidate
    {
        public UnsupportedPayloadAction() : base("unsupported-payload") { }

        public string Payload => Kind;
    }
}
