using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Hosting;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignSourceIssuedActionBindingTests
{
    [Fact]
    public void SourceTurnSystemActionsAreDistinctReceiptCompatibleIdentities()
    {
        string[] identities =
        [
            CampaignSourceTurnAuthority.StartActionId,
            CampaignSourceTurnAuthority.InitiativeActionId,
            CampaignSourceTurnAuthority.CompleteGt1ScheduleActionId,
            CampaignSourceTurnAuthority.CompleteGt1OrganizationActionId,
            CampaignSourceTurnAuthority.CompleteGt1ArrivalsActionId,
            CampaignSourceTurnAuthority.CompleteGt1FleetActionId,
            CampaignSourceTurnAuthority.CompleteGt1FirstBreakdownActionId,
            CampaignSourceTurnAuthority.ResolveStageOneWeatherActionId,
            CampaignSourceTurnAuthority.AnnounceGt1Lane2ActionId,
            CampaignSourceTurnAuthority.AnnounceGt1Lane3ActionId,
            CampaignSourceTurnAuthority.AnnounceGt1MultiLanesActionId,
        ];
        Assert.Equal(identities.Length, identities.Distinct(StringComparer.Ordinal).Count());
        foreach (var identity in identities)
        {
            var receipt = new CampaignActionAcceptanceReceipt("graziani-real", 1, 2,
                "source-position", CampaignActionAudience.System, identity);
            Assert.Equal(identity, receipt.ActionId);
        }
    }

    [Fact]
    public void PolishArrivalOfferBindsVersionAndDestination()
    {
        const string visible = """{"choices":[{"family":"polish-arrival","choice":{"destination":"e1830"}},{"family":"polish-arrival","choice":{"destination":"e1930"}}]}""";
        var bytes = Encoding.UTF8.GetBytes(visible);
        var action = new CampaignSourceTurnAction(17, "e1830");
        Assert.True(CampaignHostAdapter.MatchesIssuedSourceAction(17, bytes, 0, action.ActionId));
        Assert.False(CampaignHostAdapter.MatchesIssuedSourceAction(18, bytes, 0, action.ActionId));
        Assert.False(CampaignHostAdapter.MatchesIssuedSourceAction(17, bytes, 1, action.ActionId));
        Assert.False(CampaignHostAdapter.MatchesIssuedSourceAction(17, bytes, -1, action.ActionId));

        const string supply = """{"choices":[{"family":"axis-gt3-supply","choice":{"kind":"roll","targetGameTurn":3}}]}""";
        var supplyBytes = Encoding.UTF8.GetBytes(supply);
        var roll = CampaignSourceTurnAction.RollGt3Supply(17);
        Assert.True(CampaignHostAdapter.MatchesIssuedSourceAction(17, supplyBytes, 0, roll.ActionId));
        Assert.False(CampaignHostAdapter.MatchesIssuedSourceAction(18, supplyBytes, 0, roll.ActionId));
        Assert.False(CampaignHostAdapter.MatchesIssuedSourceAction(17,
            Encoding.UTF8.GetBytes(supply.Replace("targetGameTurn\":3", "targetGameTurn\":4", StringComparison.Ordinal)),
            0, roll.ActionId));
        const string adjust = """{"choices":[{"family":"axis-gt3-supply","choice":{"kind":"add","targetGameTurn":3,"lane":2,"operationStage":1,"delta":1}}]}""";
        var adjustBytes = Encoding.UTF8.GetBytes(adjust);
        var add = CampaignSourceTurnAction.AdjustGt3Supply(17, 2, 1, 1);
        Assert.True(CampaignHostAdapter.MatchesIssuedSourceAction(17, adjustBytes, 0, add.ActionId));
        Assert.False(CampaignHostAdapter.MatchesIssuedSourceAction(17,
            Encoding.UTF8.GetBytes(adjust.Replace("\"kind\":\"add\"", "\"kind\":\"remove\"", StringComparison.Ordinal)),
            0, add.ActionId));
        const string finish = """{"choices":[{"family":"axis-gt3-supply","choice":{"kind":"finish","targetGameTurn":3}}]}""";
        Assert.True(CampaignHostAdapter.MatchesIssuedSourceAction(17, Encoding.UTF8.GetBytes(finish), 0,
            CampaignSourceTurnAction.FinishGt3Supply(17).ActionId));
        const string cwSupply = """{"choices":[{"family":"cw-gt5-supply","choice":{"kind":"roll","targetGameTurn":5}}]}""";
        var cwRoll = CampaignSourceTurnAction.RollGt5Supply(17);
        Assert.True(CampaignHostAdapter.MatchesIssuedSourceAction(17, Encoding.UTF8.GetBytes(cwSupply), 0, cwRoll.ActionId));
        Assert.False(CampaignHostAdapter.MatchesIssuedSourceAction(18, Encoding.UTF8.GetBytes(cwSupply), 0, cwRoll.ActionId));
        Assert.False(CampaignHostAdapter.MatchesIssuedSourceAction(17,
            Encoding.UTF8.GetBytes(cwSupply.Replace("targetGameTurn\":5", "targetGameTurn\":4", StringComparison.Ordinal)),
            0, cwRoll.ActionId));
        const string shipping = """{"choices":[{"family":"tactical-shipping","choice":{"kind":"decline"}}]}""";
        var decline = CampaignSourceTurnAction.DeclineTacticalShipping(17);
        Assert.True(CampaignHostAdapter.MatchesIssuedSourceAction(17, Encoding.UTF8.GetBytes(shipping), 0, decline.ActionId));
        Assert.False(CampaignHostAdapter.MatchesIssuedSourceAction(18, Encoding.UTF8.GetBytes(shipping), 0, decline.ActionId));
        const string initiative = """{"choices":[{"family":"initiative-declaration","choice":{"kind":"act-last","operationStage":1}}]}""";
        Assert.True(CampaignHostAdapter.MatchesIssuedSourceAction(17, Encoding.UTF8.GetBytes(initiative), 0,
            CampaignSourceTurnAction.ActLast(17).ActionId));
        Assert.False(CampaignHostAdapter.MatchesIssuedSourceAction(17,
            Encoding.UTF8.GetBytes(initiative.Replace("operationStage\":1", "operationStage\":2", StringComparison.Ordinal)),
            0, CampaignSourceTurnAction.ActLast(17).ActionId));
        const string organization = """{"choices":[{"family":"voluntary-organization","choice":{"kind":"decline"}}]}""";
        Assert.True(CampaignHostAdapter.MatchesIssuedSourceAction(17, Encoding.UTF8.GetBytes(organization), 0,
            CampaignSourceTurnAction.DeclineVoluntaryOrganization(17).ActionId));
        const string reserves = """{"choices":[{"family":"first-reserve-designation","choice":{"kind":"decline"}}]}""";
        Assert.True(CampaignHostAdapter.MatchesIssuedSourceAction(17, Encoding.UTF8.GetBytes(reserves), 0,
            CampaignSourceTurnAction.DeclineFirstReserves(17).ActionId));
        var reservePosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 1
            && value.ActorRole == LandActorRole.FirstActingSide && value.PhaseId == LandPhaseIds.ReserveDesignation);
        var reserveSet = new CampaignLegalActionSet("graziani-real", 17, Cna1979Ruleset.Manifest.Hash,
            reservePosition.PositionId, CampaignActionAudience.Commonwealth,
            [CampaignSourceTurnAction.DeclineFirstReserves(17)], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(reserveSet, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(reserveSet)));
        const string finishMovement = """{"choices":[{"family":"first-movement","choice":{"kind":"finish"}}]}""";
        Assert.True(CampaignHostAdapter.MatchesIssuedSourceAction(17, Encoding.UTF8.GetBytes(finishMovement), 0,
            CampaignSourceTurnAction.FinishFirstMovement(17).ActionId));
        var movementPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 1
            && value.ActorRole == LandActorRole.FirstActingSide && value.SegmentId == LandSegmentIds.Movement);
        var movementSet = new CampaignLegalActionSet("graziani-real", 17, Cna1979Ruleset.Manifest.Hash,
            movementPosition.PositionId, CampaignActionAudience.Commonwealth,
            [CampaignSourceTurnAction.FinishFirstMovement(17)], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(movementSet, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(movementSet)));
        var axisMovementSet = new CampaignLegalActionSet("graziani-real", 17, Cna1979Ruleset.Manifest.Hash,
            movementPosition.PositionId, CampaignActionAudience.Axis,
            [CampaignSourceTurnAction.FinishFirstMovement(17)], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(axisMovementSet, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(axisMovementSet)));
        const string attack = """{"choices":[{"family":"axis-gt1-convoy-attack","choice":{"kind":"decline","lane":2,"operationStage":1}}]}""";
        Assert.True(CampaignHostAdapter.MatchesIssuedSourceAction(17, Encoding.UTF8.GetBytes(attack), 0,
            CampaignSourceTurnAction.DeclineGt1Lane2Attack(17).ActionId));
        Assert.False(CampaignHostAdapter.MatchesIssuedSourceAction(17,
            Encoding.UTF8.GetBytes(attack.Replace("\"lane\":2", "\"lane\":3", StringComparison.Ordinal)), 0,
            CampaignSourceTurnAction.DeclineGt1Lane2Attack(17).ActionId));
        Assert.True(CampaignHostAdapter.MatchesIssuedSourceAction(17,
            Encoding.UTF8.GetBytes(attack.Replace("\"lane\":2", "\"lane\":3", StringComparison.Ordinal)), 0,
            CampaignSourceTurnAction.DeclineGt1Lane3Attack(17).ActionId));
        Assert.True(CampaignHostAdapter.MatchesIssuedSourceAction(17,
            Encoding.UTF8.GetBytes(attack.Replace("\"lane\":2", "\"lane\":6", StringComparison.Ordinal)), 0,
            CampaignSourceTurnAction.DeclineGt1Lane6Attack(17).ActionId));
        const string training = """{"choices":[{"family":"sixth-australian-training","choice":{"kind":"interrupt"}}]}""";
        Assert.True(CampaignHostAdapter.MatchesIssuedSourceAction(17, Encoding.UTF8.GetBytes(training), 0,
            CampaignSourceTurnAction.InterruptSixthAustralianTraining(17).ActionId));
        const string movement = """{"choices":[{"family":"fourth-indian-opening-move","choice":{"kind":"move","groupId":"4-indian-division","destination":"d3715","supplyId":"cw-matruh-1"}}]}""";
        var move = CampaignSourceTurnAction.MoveFourthIndianOpening(17, "cw-matruh-1");
        Assert.True(CampaignHostAdapter.MatchesIssuedSourceAction(17, Encoding.UTF8.GetBytes(movement), 0, move.ActionId));
        Assert.False(CampaignHostAdapter.MatchesIssuedSourceAction(17,
            Encoding.UTF8.GetBytes(movement.Replace("d3715", "d3716", StringComparison.Ordinal)), 0, move.ActionId));
        const string baseline = """{"choices":[{"family":"sixth-australian-baseline","choice":{"kind":"approve","rulingId":"graziani.6-australian.initial-untrained-zero.v1"}}]}""";
        Assert.True(CampaignHostAdapter.MatchesIssuedSourceAction(17, Encoding.UTF8.GetBytes(baseline), 0,
            CampaignSourceTurnAction.ApproveSixthAustralianBaseline(17).ActionId));
        Assert.False(CampaignHostAdapter.MatchesIssuedSourceAction(17,
            Encoding.UTF8.GetBytes(baseline.Replace("untrained-zero", "untrained-one", StringComparison.Ordinal)), 0,
            CampaignSourceTurnAction.ApproveSixthAustralianBaseline(17).ActionId));
    }

    [Fact]
    public void IssuedIdentityRequiresExactVersionFamilyChoiceAndOrdinal()
    {
        const string visible = """{"choices":[{"family":"axis-dump","choice":{"kind":"place","locationId":"c4120"}},{"family":"axis-dump","choice":{"kind":"place","locationId":"c3918"}}]}""";
        using var document = JsonDocument.Parse(visible);
        var first = document.RootElement.GetProperty("choices")[0];
        var action = new CampaignSourceSetupAction(7, first.GetProperty("family").GetString()!, "", first.GetProperty("choice"));
        var bytes = Encoding.UTF8.GetBytes(visible);
        Assert.True(CampaignHostAdapter.MatchesIssuedSourceAction(7, bytes, 0, action.ActionId));
        Assert.False(CampaignHostAdapter.MatchesIssuedSourceAction(8, bytes, 0, action.ActionId));
        Assert.False(CampaignHostAdapter.MatchesIssuedSourceAction(7, bytes, 1, action.ActionId));
        Assert.False(CampaignHostAdapter.MatchesIssuedSourceAction(7, bytes, -1, action.ActionId));
        Assert.False(CampaignHostAdapter.MatchesIssuedSourceAction(7, bytes, 2, action.ActionId));
        Assert.False(CampaignHostAdapter.MatchesIssuedSourceAction(7, bytes, 0, "sha256:" + new string('0', 64)));
        Assert.False(CampaignHostAdapter.MatchesIssuedSourceAction(7,
            Encoding.UTF8.GetBytes(visible.Replace("axis-dump", "axis-supply", StringComparison.Ordinal)), 0, action.ActionId));
        Assert.False(CampaignHostAdapter.MatchesIssuedSourceAction(7,
            Encoding.UTF8.GetBytes(visible.Replace("c4120", "c4121", StringComparison.Ordinal)), 0, action.ActionId));
        Assert.False(CampaignHostAdapter.MatchesIssuedSourceAction(7, "{"u8.ToArray(), 0, action.ActionId));
    }
}
