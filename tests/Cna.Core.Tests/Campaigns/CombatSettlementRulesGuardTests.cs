using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using static Cna.Core.Tests.Campaigns.CombatSealsTests;

namespace Cna.Core.Tests.Campaigns;

/// <summary>
/// Guards that the Result2 settlement and custody paths stay on the numbers and routes owned by
/// <see cref="CombatSettlementRules"/> and the native V24/V25 implementations (issues #576, #577).
/// </summary>
public sealed class CombatSettlementRulesGuardTests
{
    [Fact]
    public void Result2LossRetreatAndCustodyReceiptsAgreeWithSelectedRulesAndNativeArithmetic()
    {
        using var fixture = Fixture(); var rows = 0; var captures = 0; var refusals = 0;
        var rules = Cna1979CombatAdjudication.Definition;
        foreach (var row in fixture.RootElement.GetProperty("traces").EnumerateArray())
        {
            var test = CombatResolutionTests.Case(row); var inputs = Inputs(row); var events = Events(row);
            var end = Array.FindIndex(events, e => JsonNode.Parse(e)!["effect"]!["kind"]!.GetValue<string>() == "relationships-settled");
            var state = CampaignCombatResolution.ReplayTrustedBoundary(test.Request, test.Created, test.Boundary, test.PredecessorInputs,
                test.PredecessorEvents, test.Inputs, test.Events, inputs[..end], events[..end]);
            var settlement = state.World.Settlements.Single(); var assault = state.Result!;
            var disposition = settlement.Disposition!; var refused = disposition.Kind == "refuse-retreat";
            var native = Cna1979CombatAdjudication.Resolve(assault.AttackerMoraleCoordinate, assault.DefenderMoraleCoordinate,
                assault.Facts.AttackerCoordinate, assault.Facts.DefenderCoordinate, refused, assault.Facts.CaptureDie);
            Assert.Equal(rules.Settlement.RefusalPercentPerHex * disposition.UnfulfilledDistance, refused ? native.DefenderLossPercent - native.DefenderBaseLossPercent : 0);
            foreach (var loss in settlement.Losses!.Roles)
            {
                var expected = loss.Role == "attacker" ? native.Attacker : native.Defender;
                Assert.Equal(loss.Role == "attacker" ? 0 : rules.Settlement.RefusalPercentPerHex * disposition.UnfulfilledDistance, loss.RefusalPercent);
                Assert.Equal((expected.TotalLoss, expected.CapturedLoss, expected.DestroyedLoss, expected.RemainingToe, expected.LossDp),
                    (loss.LossToe, loss.CapturedToe, loss.OtherLossToe, loss.RemainingToe, loss.LossDp));
                Assert.Equal(rules.Costs.CommittedToePerRole, loss.CommittedToe);
            }
            var retreat = settlement.Retreat!;
            Assert.Equal(disposition.PlannedDistance == 1 ? rules.Settlement.VictoryRpPoints : 0, retreat.AttackerVictoryRp);
            Assert.Equal(retreat.CompletedDistance * rules.Settlement.ClearRetreatCapabilityPointsPerHex, retreat.AfterCp.Numerator - retreat.BeforeCp.Numerator);
            if (settlement.Custody is { } custody)
            {
                captures++;
                if (custody.Kind == "relocate-and-guard")
                {
                    Assert.True(custody.Route.Count - 1 <= rules.Settlement.GuardedPathMaximumHexes);
                    Assert.Equal(custody.DonorToeBefore - 1, custody.DonorToeAfter);
                    var guard = Assert.Single(state.World.Guards);
                    Assert.Equal((rules.Settlement.GuardToe, rules.Settlement.GuardCapabilityPointAllowance), (guard.Toe, guard.BaseCapabilityPointAllowance));
                }
                else
                {
                    var entitlement = Assert.Single(state.World.ReplacementEntitlements);
                    Assert.Equal(rules.Settlement.ReplacementDelayOperationStages, entitlement.DelayOperationStages);
                    var stages = rules.Settlement.OperationStagesPerGameTurn;
                    var ordinal = (entitlement.EarnedScope.GameTurn - 1) * stages + entitlement.EarnedScope.OperationStage - 1 + rules.Settlement.ReplacementDelayOperationStages;
                    Assert.Equal((ordinal / stages + 1, ordinal % stages + 1), (entitlement.EligibleScope.GameTurn, entitlement.EligibleScope.OperationStage));
                }
            }
            if (refused) refusals++;
            rows++;
        }
        Assert.True(rows > 0 && captures > 0 && refusals > 0);
    }

    [Fact]
    public void SelectedSettlementRulesRetainTheValuesTheResult2PathsWereWrittenAgainst()
    {
        // Value pin: a revision of these numbers must be a deliberate, reviewed change that moves the
        // native V22-V26 and Result2 paths together.
        Assert.Equal(new CombatSettlementRules("ceiling", "floor", "ceiling", 10, 3, 3, 3, 10, 1, 1, 10, 0, 1, 0, 3, 8, 12, 3),
            Cna1979CombatAdjudication.Definition.Settlement);
        Assert.Equal(10, Cna1979CombatAdjudication.Definition.Costs.CommittedToePerRole);
        Assert.Equal(10, Cna1979CombatAdjudication.Definition.Costs.BaseCapabilityPointAllowance);
    }

    [Fact]
    public void EqualLengthRouteTieResolvesIdenticallyInNativeV25AndResult2()
    {
        // S has two equal-length routes to E: via "b" (first listed) and via "a" (ordinal-first).
        var edges = new[] { Edge("s", "b"), Edge("s", "a"), Edge("b", "e"), Edge("a", "e") };
        var native = CampaignNativeCombatCustodyCutV25.ShortestRoute(edges, "s", "e")!;
        var typed = CampaignCombatLossRetreat.ContentPath(edges, "s", "e");
        Assert.Equal(["s", "a", "e"], native);
        Assert.Equal(native, typed);
    }

    [Fact]
    public void RoutesAgreeForEveryLocationPairOfShippedCombatContent()
    {
        using var fixture = Fixture(); var pairs = 0;
        foreach (var row in fixture.RootElement.GetProperty("traces").EnumerateArray())
        {
            var test = CombatResolutionTests.Case(row);
            var definition = CampaignCombatResolution.ReplayTrustedBoundary(test.Request, test.Created, test.Boundary, test.PredecessorInputs,
                test.PredecessorEvents, test.Inputs, test.Events, [], []).Context.Creation.Setup.Artifact.Definition;
            var ids = definition.Locations.Select(l => l.LocationId).ToArray();
            foreach (var from in ids) foreach (var to in ids)
            {
                var native = CampaignNativeCombatCustodyCutV25.ShortestRoute(definition.Edges, from, to);
                if (native is null) { Assert.ThrowsAny<JsonException>(() => { _ = CampaignCombatLossRetreat.ContentPath(definition.Edges, from, to); }); continue; }
                Assert.Equal(native, CampaignCombatLossRetreat.ContentPath(definition.Edges, from, to)); pairs++;
            }
        }
        Assert.True(pairs > 0);
    }

    private static ContentHexEdge Edge(string first, string second) => new(first, second, [],
        new(ContentOriginKind.Synthetic, [new RuleReference("sandtable-rules-lab", "content.edge.tie-test")]));
    private static JsonDocument Fixture() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Campaigns", "Fixtures", "combat-result-settlement-v2.json")));
    private static CombatResolutionInput[] Inputs(JsonElement row) => row.GetProperty("resultInputs").EnumerateArray().Select(i => CampaignCombatResolutionCodec.ReadInput(JsonSerializer.SerializeToUtf8Bytes(i))).ToArray();
    private static byte[][] Events(JsonElement row) => row.GetProperty("resultEventCanonicalUtf8").EnumerateArray().Select(e => Encoding.UTF8.GetBytes(e.GetString()!)).ToArray();
}
