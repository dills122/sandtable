using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Content;
using Cna.Core.Exercises;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Execution;

namespace Cna.ExerciseRunner.Tests.Execution;

public sealed class BreakdownTruckStudyTests
{
    private const string StudyPath = "scenarios/maneuvers/rules-lab.breakdown-truck.serial.v1.json";

    [Fact]
    public void TruckMoveBesideEnemyCombatDoesNotOpenReaction()
    {
        var (manifest, result) = Run("truck.no-reaction-trigger");
        Assert.Equal("rules-lab.content.breakdown-reaction.truck-mover.v1", manifest.ContentPackId);
        var content = Cna1979SyntheticContentCatalog.ResolveV6(manifest.ContentPackId, manifest.ContentHash).Artifact!;
        Assert.Contains(Parse(content.GetCanonicalBytes()).GetProperty("edges").EnumerateArray(),
            value => value.GetProperty("firstLocationId").GetString() == "center"
                && value.GetProperty("secondLocationId").GetString() == "east");
        var initial = Parse(result.InitialSnapshot);
        Assert.Equal("east", initial.GetProperty("world").GetProperty("elements").EnumerateArray()
            .Single(value => value.GetProperty("elementId").GetString() == "commonwealth-infantry")
            .GetProperty("currentLocationId").GetString());
        var move = Assert.Single(Events(result), value => Type(value) == "element-moved");
        Assert.Equal("axis-truck", move.GetProperty("elementId").GetString());
        Assert.Equal("center", move.GetProperty("destinationLocationId").GetString());
        Assert.Equal(JsonValueKind.Null, move.GetProperty("openedReactionWindow").ValueKind);
    }

    [Fact]
    public void CheapTruckStopConsumesNoRandomnessAndReachesCombat()
    {
        var (_, result) = Run("truck.no-roll");
        var resolution = Assert.Single(Events(result), value => Type(value) == "breakdown-stop-resolved");
        var check = Assert.Single(resolution.GetProperty("checks").EnumerateArray());
        Assert.Equal("raw-bp-not-above-three", check.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, check.GetProperty("roll").ValueKind);
        Assert.Equal(1, check.GetProperty("input").GetProperty("cumulativeBreakdownPoints").GetProperty("numerator").GetInt32());
        Assert.Equal(2, check.GetProperty("input").GetProperty("cumulativeBreakdownPoints").GetProperty("denominator").GetInt32());
        Assert.Equal(resolution.GetProperty("randomStateBefore").GetRawText(), resolution.GetProperty("randomStateAfter").GetRawText());
        Assert.Empty(resolution.GetProperty("createdLots").EnumerateArray());
    }

    [Theory]
    [InlineData("truck.eligible-zero-loss", new[] { 21, 66, 56 }, new[] { 0, 6, 2 }, new[] { 2, 4, 6, 8 })]
    [InlineData("truck.repeat-positive-loss", new[] { 32, 63, 43 }, new[] { 0, 3, 1 }, new[] { 3, 5, 7, 9 })]
    public void RepeatedStopsUseHigherBandsAndKeepBrokenLotsAtTheirCreationHex(
        string id, int[] dice, int[] losses, int[] cursors)
    {
        var (_, result) = Run(id);
        var events = Events(result);
        var resolutions = events.Where(value => Type(value) == "breakdown-stop-resolved").ToArray();
        Assert.Equal(3, resolutions.Length);
        var checks = resolutions.Select(value => Assert.Single(value.GetProperty("checks").EnumerateArray())).ToArray();
        Assert.Equal(dice, checks.Select(value => value.GetProperty("roll").GetProperty("coordinate").GetInt32()));
        Assert.Equal(losses, checks.Select(value => value.GetProperty("roll").GetProperty("lossCount").GetInt32()));
        Assert.Equal([26, 32, 58], checks.Select(value => value.GetProperty("input")
            .GetProperty("cumulativeBreakdownPoints").GetProperty("numerator").GetInt32()));
        Assert.Equal(3, checks.Select(value => value.GetProperty("effectiveBandId").GetString()).Distinct().Count());
        Assert.All(checks, value => Assert.Equal("rolled", value.GetProperty("status").GetString()));
        Assert.Equal(cursors.Take(3), checks.Select(value => value.GetProperty("roll").GetProperty("randomCursorBefore").GetInt32()));
        Assert.Equal(cursors.Skip(1), checks.Select(value => value.GetProperty("roll").GetProperty("randomCursorAfter").GetInt32()));
        Assert.Equal(JsonValueKind.Null, checks[0].GetProperty("input").GetProperty("highestEffectiveCheckedBandId").ValueKind);
        Assert.Equal(checks[0].GetProperty("effectiveBandId").GetString(), checks[0].GetProperty("highestEffectiveCheckedBandIdAfter").GetString());
        for (var index = 1; index < checks.Length; index++)
            Assert.Equal(checks[index - 1].GetProperty("highestEffectiveCheckedBandIdAfter").GetString(),
                checks[index].GetProperty("input").GetProperty("highestEffectiveCheckedBandId").GetString());
        Assert.Empty(resolutions[0].GetProperty("createdLots").EnumerateArray());
        var fixedLot = Assert.Single(resolutions[1].GetProperty("createdLots").EnumerateArray());
        Assert.Equal("west", fixedLot.GetProperty("locationId").GetString());
        var final = Parse(result.FinalSnapshot);
        Assert.Equal("center", AxisTruck(final).GetProperty("currentLocationId").GetString());
        Assert.Contains(final.GetProperty("world").GetProperty("brokenVehicleLots").EnumerateArray(),
            value => value.GetRawText() == fixedLot.GetRawText());
        Assert.Equal(12, AxisTruck(final).GetProperty("operationalState").GetProperty("vehicleBreakdownState")
            .GetProperty("workingPointCount").GetInt32() + final.GetProperty("world").GetProperty("brokenVehicleLots")
            .EnumerateArray().Where(value => value.GetProperty("owner").GetString() == "axis")
            .Sum(value => value.GetProperty("pointCount").GetInt32()));
        Assert.Equal("cp-exhausted", resolutions[2].GetProperty("stop").GetProperty("reason").GetString());
        Assert.Equal(["center", "west", "center"], events.Where(value => Type(value) == "element-moved")
            .Select(value => value.GetProperty("destinationLocationId").GetString()));
    }

    [Fact]
    public void ZeroWorkingTruckHasNoMoveEvenWithTwelveCpRemaining()
    {
        var (manifest, result) = Run("truck.exhaustion");
        var events = Events(result);
        var resolution = Assert.Single(events, value => Type(value) == "breakdown-stop-resolved");
        var check = Assert.Single(resolution.GetProperty("checks").EnumerateArray());
        Assert.Equal(66, check.GetProperty("roll").GetProperty("coordinate").GetInt32());
        Assert.Equal(1, check.GetProperty("roll").GetProperty("lossCount").GetInt32());
        Assert.Equal(0, check.GetProperty("workingAfter").GetInt32());
        Assert.Equal(8, AxisTruck(Parse(result.FinalSnapshot)).GetProperty("operationalState")
            .GetProperty("capabilityPointsExpended").GetProperty("numerator").GetInt32());
        Assert.Single(events, value => Type(value) == "element-moved");

        var start = CampaignExercises.Begin(ExerciseExecutor.CreateRequest(manifest, result.SeedLedger.Identity));
        Assert.True(start.IsStarted);
        var session = start.Session!;
        var inspected = false;
        foreach (var step in result.Steps)
        {
            var set = CampaignExercises.Query(session, step.Audience).ActionSet!;
            var accepted = CampaignExercises.Submit(session, new(CampaignActionSubmission.CurrentContractVersion,
                set.CampaignId, set.StateVersion, set.PositionId, set.Audience, step.ActionId));
            Assert.True(accepted.IsAccepted);
            session = accepted.SuccessorSession!;
            if (!step.EventRecords.Any(bytes => Type(Parse(bytes)) == "breakdown-stop-resolved")) continue;
            var choices = CampaignExercises.Query(session, CampaignActionAudience.Axis).ActionSet!;
            Assert.EndsWith(".movement", choices.PositionId, StringComparison.Ordinal);
            Assert.Equal("complete-movement-segment", Assert.Single(choices.Candidates).Kind);
            inspected = true;
        }
        Assert.True(inspected);
    }

    private static (ExerciseManifest Manifest, ExerciseExecutionResult Result) Run(string id)
    {
        var value = Assert.Single(BreakdownFixtureMigrationTests.ReadCases(StudyPath), value => value.Manifest.ExerciseId == id);
        Assert.Equal(8UL, value.Identity.RootSeed);
        var result = ExerciseExecutor.Execute(value.Manifest, value.Identity, TestContext.Current.CancellationToken);
        Assert.True(result.IsSucceeded, System.Text.Encoding.UTF8.GetString(ExerciseRunResultCodec.Serialize(result.RunResult)));
        Assert.Equal("land.position.operation-1.first-player.movement-and-combat.combat.position-determination", result.BoundaryPositionId);
        Assert.True(result.Reconstruction!.IsVerified);
        Assert.True(ReadjudicationVerifier.Verify(value.Manifest, result).IsVerified);
        var events = Events(result);
        Assert.DoesNotContain(events, record => Type(record) is "reaction-window-opened" or "reacting-element-moved");
        Assert.Single(events, record => Type(record) == "breakdown-segment-completed");
        return (value.Manifest, result);
    }

    private static JsonElement[] Events(ExerciseExecutionResult result) => result.Steps.SelectMany(value => value.EventRecords).Select(Parse).ToArray();
    private static string Type(JsonElement value) => value.GetProperty("eventType").GetString()!;
    private static JsonElement AxisTruck(JsonElement snapshot) => snapshot.GetProperty("world").GetProperty("elements")
        .EnumerateArray().Single(value => value.GetProperty("elementId").GetString() == "axis-truck");
    private static JsonElement Parse(byte[] bytes)
    {
        using var document = JsonDocument.Parse(bytes);
        return document.RootElement.Clone();
    }
}
