using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Actions;

public sealed class CampaignMovementActionCodecTests
{
    private const string MoveActionId =
        "sha256:d2b8b443b6bb9862e4f2974748540b077db73debb267faa2b93771023c590070";

    [Fact]
    public void MovementActionSetHasExactCanonicalBytesAndStrictRoundTrip()
    {
        var set = CreateMovementSet();
        var expected =
            $"{{\"contractVersion\":2,\"policyId\":\"sandtable.legal-actions.v2\"," +
            $"\"campaignId\":\"campaign-movement\",\"stateVersion\":10," +
            $"\"rulesetHash\":\"{Cna1979Ruleset.Manifest.Hash}\"," +
            "\"positionId\":\"land.position.operation-1.first-player.movement-and-combat.movement\"," +
            "\"audience\":\"axis\",\"candidates\":[" +
            "{\"contractVersion\":1," +
            "\"actionId\":\"sha256:054322426e5956a4340cca2ef3d0e9f3388848e998319ae8084a6e708a465bc9\"," +
            "\"kind\":\"complete-movement-segment\"}," +
            "{\"contractVersion\":1,\"actionId\":\"" + MoveActionId + "\"," +
            "\"kind\":\"move-element\",\"elementId\":\"axis-element-a\"," +
            "\"originLocationId\":\"west\",\"destinationLocationId\":\"center\"," +
            "\"costBreakdown\":{\"destinationTerrainId\":\"land.terrain.desert\"," +
            "\"destinationTerrainCost\":{\"numerator\":2,\"denominator\":1}," +
            "\"routeAdjustment\":{\"routeId\":\"land.route.track\"," +
            "\"costKind\":\"scale-underlying\",\"amount\":{\"numerator\":1,\"denominator\":2}}," +
            "\"crossedHexsideCosts\":[{\"hexsideId\":\"land.hexside.ridge\"," +
            "\"direction\":\"either\",\"addedCost\":{\"numerator\":1,\"denominator\":1}}," +
            "{\"hexsideId\":\"land.hexside.slope\",\"direction\":\"up\"," +
            "\"addedCost\":{\"numerator\":1,\"denominator\":2}}]," +
            "\"totalCost\":{\"numerator\":5,\"denominator\":2}}}]}";
        var canonical = CampaignLegalActionSerializer.Serialize(set);

        Assert.Equal(expected, Encoding.UTF8.GetString(canonical));

        var readback = CampaignLegalActionSerializer.DeserializeCanonical(canonical);

        Assert.Equal(set, readback);
        Assert.Equal(canonical, CampaignLegalActionSerializer.Serialize(readback));
        Assert.IsType<CompleteMovementSegmentAction>(readback.Candidates[0]);
        var move = Assert.IsType<MoveElementAction>(readback.Candidates[1]);
        Assert.Equal(new CapabilityPointAmount(5, 2), move.CostBreakdown.TotalCost);
    }

    [Fact]
    public void StrictActionSetReaderRoundTripsEveryClosedCandidateKind()
    {
        CampaignActionCandidate[] candidates =
        [
            new ResolveInitiativeAction(),
            new ResolveNoObligationNavalConvoyScheduleAction(),
            new ResolveNoObligationTacticalShippingAction(),
            new ResolveWeatherAction(),
            new ResolveNoObligationOrganizationAction(),
            new ResolveNoObligationNavalConvoyArrivalAction(),
            new ResolveNoObligationFleetAssignmentAction(),
            new ResolveNoObligationFleetRepairAction(),
            new ActFirstAction(1),
            new ActLastAction(1),
            new DesignateReserveAction("axis-element-a"),
            new CompleteReserveDesignationAction(),
            CreateMove(),
            new CompleteMovementSegmentAction(),
        ];
        var set = new CampaignLegalActionSet(
            "campaign-all-kinds",
            10,
            Cna1979Ruleset.Manifest.Hash,
            "land.position.operation-1.first-player.movement-and-combat.movement",
            CampaignActionAudience.Axis,
            candidates);
        var canonical = CampaignLegalActionSerializer.Serialize(set);

        var readback = CampaignLegalActionSerializer.DeserializeCanonical(canonical);

        Assert.Equal(set, readback);
        Assert.Equal(
            candidates.Select(value => value.GetType()).OrderBy(
                type => candidates.Single(candidate => candidate.GetType() == type).Kind,
                StringComparer.Ordinal),
            readback.Candidates.Select(value => value.GetType()));
        Assert.Equal(canonical, CampaignLegalActionSerializer.Serialize(readback));
    }

    [Fact]
    public void StrictActionSetReaderRejectsNoncanonicalAndMutatedEnvelopes()
    {
        var canonical = SerializeMovementSetText();
        var prefix =
            "{\"contractVersion\":2,\"policyId\":\"sandtable.legal-actions.v2\"," +
            "\"campaignId\":\"campaign-movement\",";
        var reorderedPrefix =
            "{\"policyId\":\"sandtable.legal-actions.v2\",\"contractVersion\":2," +
            "\"campaignId\":\"campaign-movement\",";
        var completion =
            "{\"contractVersion\":1," +
            "\"actionId\":\"sha256:054322426e5956a4340cca2ef3d0e9f3388848e998319ae8084a6e708a465bc9\"," +
            "\"kind\":\"complete-movement-segment\"}";

        AssertRejects(canonical.Replace(prefix, reorderedPrefix, StringComparison.Ordinal));
        AssertRejects(canonical.Replace(
            "\"contractVersion\":2,",
            "\"contractVersion\":2,\"contractVersion\":2,",
            StringComparison.Ordinal));
        AssertRejects(canonical.Replace(
            "\"policyId\":\"sandtable.legal-actions.v2\",",
            string.Empty,
            StringComparison.Ordinal));
        AssertRejects(canonical.Replace(
            "\"campaignId\":",
            "\"hiddenAuthority\":true,\"campaignId\":",
            StringComparison.Ordinal));
        AssertRejects(canonical.Replace(
            "\"contractVersion\":2",
            "\"contractVersion\":1",
            StringComparison.Ordinal));
        AssertRejects(canonical.Replace(
            "\"kind\":\"complete-movement-segment\"",
            "\"kind\":\"unknown-movement\"",
            StringComparison.Ordinal));
        AssertRejects(canonical.Replace(
            "\"destinationLocationId\":\"center\"",
            "\"destinationLocationId\":\"east\"",
            StringComparison.Ordinal));
        AssertRejects(canonical.Replace(
            "\"destinationTerrainCost\":{\"numerator\":2,\"denominator\":1}",
            "\"destinationTerrainCost\":{\"numerator\":4,\"denominator\":2}",
            StringComparison.Ordinal));
        AssertRejects(canonical.Replace(
            "\"candidates\":[" + completion + ",",
            "\"candidates\":[",
            StringComparison.Ordinal).Replace("]}", "," + completion + "]}",
                StringComparison.Ordinal));
        AssertRejects(canonical + "\n");
    }

    [Fact]
    public void StrictReaderRejectsCorrectlyRehashedButLocallyIncoherentMovementCost()
    {
        var canonical = SerializeMovementSetText();
        var incoherentId =
            "sha256:06e0e29764563596372d35dbd03ad5f54e8175d474edddf13a68ccdb8f947299";
        var rehashed = canonical
            .Replace(MoveActionId, incoherentId, StringComparison.Ordinal)
            .Replace(
                "\"totalCost\":{\"numerator\":5,\"denominator\":2}",
                "\"totalCost\":{\"numerator\":7,\"denominator\":2}",
                StringComparison.Ordinal);

        AssertRejects(rehashed);
    }

    [Fact]
    public void StrictReaderRejectsCorrectlyRehashedDuplicateHexsideFeatureDirections()
    {
        var canonical = SerializeMovementSetText();
        var duplicateSemantics =
            "{\"contractVersion\":1,\"kind\":\"move-element\"," +
            "\"elementId\":\"axis-element-a\",\"originLocationId\":\"west\"," +
            "\"destinationLocationId\":\"center\",\"costBreakdown\":" +
            "{\"destinationTerrainId\":\"land.terrain.desert\"," +
            "\"destinationTerrainCost\":{\"numerator\":2,\"denominator\":1}," +
            "\"routeAdjustment\":{\"routeId\":\"land.route.track\"," +
            "\"costKind\":\"scale-underlying\",\"amount\":{\"numerator\":1," +
            "\"denominator\":2}},\"crossedHexsideCosts\":[" +
            "{\"hexsideId\":\"land.hexside.slope\",\"direction\":\"either\"," +
            "\"addedCost\":{\"numerator\":1,\"denominator\":1}}," +
            "{\"hexsideId\":\"land.hexside.slope\",\"direction\":\"up\"," +
            "\"addedCost\":{\"numerator\":1,\"denominator\":2}}]," +
            "\"totalCost\":{\"numerator\":5,\"denominator\":2}}}";
        var duplicateId =
            $"sha256:{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(duplicateSemantics)))}";
        var rehashed = canonical
            .Replace(MoveActionId, duplicateId, StringComparison.Ordinal)
            .Replace(
                "\"hexsideId\":\"land.hexside.ridge\"",
                "\"hexsideId\":\"land.hexside.slope\"",
                StringComparison.Ordinal);

        AssertRejects(rehashed);
    }

    [Fact]
    public void SubmissionCodecHasExactShapeAndStrictRoundTrip()
    {
        var submission = new CampaignActionSubmission(
            CampaignActionSubmission.CurrentContractVersion,
            "campaign-movement",
            10,
            "land.position.operation-1.first-player.movement-and-combat.movement",
            CampaignActionAudience.Axis,
            MoveActionId);
        var expected =
            "{\"contractVersion\":1,\"campaignId\":\"campaign-movement\"," +
            "\"expectedStateVersion\":10," +
            "\"expectedPositionId\":\"land.position.operation-1.first-player.movement-and-combat.movement\"," +
            "\"audience\":\"axis\",\"actionId\":\"" + MoveActionId + "\"}";

        var canonical = CampaignActionSubmissionSerializer.SerializeCanonical(submission);
        var readback = CampaignActionSubmissionSerializer.DeserializeCanonical(canonical);

        Assert.Equal(expected, Encoding.UTF8.GetString(canonical));
        Assert.Equal(submission, readback);
        Assert.Equal(canonical, CampaignActionSubmissionSerializer.SerializeCanonical(readback));
        AssertRejectsSubmission(expected.Replace(
            "\"campaignId\":",
            "\"elementId\":\"axis-element-a\",\"campaignId\":",
            StringComparison.Ordinal));
        AssertRejectsSubmission(expected.Replace(
            "\"audience\":\"axis\"",
            "\"audience\":\"unknown\"",
            StringComparison.Ordinal));
        AssertRejectsSubmission(expected.Replace(
            MoveActionId,
            "sha256:1234",
            StringComparison.Ordinal));
        AssertRejectsSubmission(expected.Replace(
            "\"expectedStateVersion\":10",
            "\"expectedStateVersion\":0",
            StringComparison.Ordinal));
        AssertRejectsSubmission(expected.Replace(
            "\"campaignId\":\"campaign-movement\",\"expectedStateVersion\":10",
            "\"expectedStateVersion\":10,\"campaignId\":\"campaign-movement\"",
            StringComparison.Ordinal));
        AssertRejectsSubmission(expected + "\n");
    }

    [Fact]
    public void AcceptanceReceiptCodecHasExactShapeAndStrictRoundTrip()
    {
        var receipt = new CampaignActionAcceptanceReceipt(
            "campaign-movement",
            10,
            11,
            "land.position.operation-1.first-player.movement-and-combat.movement",
            CampaignActionAudience.Axis,
            MoveActionId);
        var expected =
            "{\"contractVersion\":1,\"campaignId\":\"campaign-movement\"," +
            "\"priorStateVersion\":10,\"committedStateVersion\":11," +
            "\"resultingPositionId\":\"land.position.operation-1.first-player.movement-and-combat.movement\"," +
            "\"audience\":\"axis\",\"actionId\":\"" + MoveActionId + "\"}";

        var canonical = CampaignActionAcceptanceReceiptSerializer.Serialize(receipt);
        var readback = CampaignActionAcceptanceReceiptSerializer.DeserializeCanonical(canonical);

        Assert.Equal(expected, Encoding.UTF8.GetString(canonical));
        Assert.Equal(receipt, readback);
        Assert.Equal(canonical, CampaignActionAcceptanceReceiptSerializer.Serialize(readback));
        AssertRejectsReceipt(expected.Replace(
            "\"committedStateVersion\":11",
            "\"committedStateVersion\":12",
            StringComparison.Ordinal));
        AssertRejectsReceipt(expected.Replace(
            "\"resultingPositionId\":",
            "\"event\":\"element-moved\",\"resultingPositionId\":",
            StringComparison.Ordinal));
        AssertRejectsReceipt(expected.Replace(
            "\"campaignId\":\"campaign-movement\",\"priorStateVersion\":10",
            "\"priorStateVersion\":10,\"campaignId\":\"campaign-movement\"",
            StringComparison.Ordinal));
        AssertRejectsReceipt(expected.Replace(
            MoveActionId,
            "sha256:1234",
            StringComparison.Ordinal));
        AssertRejectsReceipt(expected + "\n");
    }

    private static CampaignLegalActionSet CreateMovementSet() => new(
        "campaign-movement",
        10,
        Cna1979Ruleset.Manifest.Hash,
        "land.position.operation-1.first-player.movement-and-combat.movement",
        CampaignActionAudience.Axis,
        [CreateMove(), new CompleteMovementSegmentAction()]);

    private static MoveElementAction CreateMove() => new(
        "axis-element-a",
        "west",
        "center",
        new MovementActionCostBreakdown(
            "land.terrain.desert",
            new CapabilityPointAmount(2, 1),
            new MovementActionRouteAdjustment(
                "land.route.track",
                MovementRouteCostKind.ScaleUnderlying,
                new CapabilityPointAmount(1, 2)),
            [
                new MovementActionHexsideCost(
                    "land.hexside.ridge",
                    MovementHexsideDirection.Either,
                    new CapabilityPointAmount(1, 1)),
                new MovementActionHexsideCost(
                    "land.hexside.slope",
                    MovementHexsideDirection.Up,
                    new CapabilityPointAmount(1, 2)),
            ],
            new CapabilityPointAmount(5, 2)));

    private static string SerializeMovementSetText() => Encoding.UTF8.GetString(
        CampaignLegalActionSerializer.Serialize(CreateMovementSet()));

    private static void AssertRejects(string value) => Assert.Throws<JsonException>(() =>
        CampaignLegalActionSerializer.DeserializeCanonical(Encoding.UTF8.GetBytes(value)));

    private static void AssertRejectsSubmission(string value) => Assert.Throws<JsonException>(() =>
        CampaignActionSubmissionSerializer.DeserializeCanonical(Encoding.UTF8.GetBytes(value)));

    private static void AssertRejectsReceipt(string value) => Assert.Throws<JsonException>(() =>
        CampaignActionAcceptanceReceiptSerializer.DeserializeCanonical(
            Encoding.UTF8.GetBytes(value)));
}
