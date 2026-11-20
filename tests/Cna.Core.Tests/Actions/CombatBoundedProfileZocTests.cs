using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Exercises;
using Cna.Core.Rules;
using Cna.Core.Setups;
using Cna.Core.Tests.Content;
using Cna.Core.Visibility;

namespace Cna.Core.Tests.Actions;

/// <summary>
/// Task 020C: forced/surprise Combat (Land 10.3 mandatory ZOC attack) is out of the bounded profile
/// (T020-Q2, ruled 2026-11-20). These tests show it is rejected at activation and that no admitted
/// World can reach the Combat selection cut's positive-ZOC invariant.
/// </summary>
public sealed class CombatBoundedProfileZocTests
{
    [Fact]
    public void EveryAdmittedBreakdownSetupStartsWithoutAnyZocSource()
    {
        foreach (var definition in Cna1979BreakdownSetupCatalog.Definitions)
        {
            var session = Begin(definition.SetupId);
            foreach (var side in new[] { LandSide.Axis, LandSide.Commonwealth })
            {
                var zoc = CampaignElementMovedV3Factory.DeriveZocAuthority(session.CurrentSnapshot.World,
                    session.Context.ArtifactV6!, session.Context.Scenario, side);
                Assert.True(zoc.SourceRepresentationIds.Count == 0, $"{definition.SetupId} {side}");
                Assert.True(zoc.ControlledLocationIds.Count == 0, $"{definition.SetupId} {side}");
            }
        }
    }

    // Parameterless facts keep CI row identities stable (long InlineData strings are truncated in
    // display names).
    private static readonly (string SetupId, string First, string Second)[] SameSidePairs =
    [
        ("rules-lab.breakdown.reaction.combat-double-fresh.v1", "axis-infantry", "axis-infantry-second"),
        ("rules-lab.breakdown.reaction.combat-double.v1", "commonwealth-infantry", "commonwealth-infantry-west"),
        ("rules-lab.breakdown.battalion-matrix.v1", "axis-battalion-a", "axis-battalion-b"),
    ];

    [Fact]
    public void ContentAdmissionRejectsCoLocatedSameSideBattalions()
    {
        foreach (var (setupId, first, second) in SameSidePairs)
            AssertContentRejectsStack(setupId, first, second);
    }

    [Fact]
    public void NoAdmittedWorldCanStackTwoSameSideCombatElements()
    {
        foreach (var (setupId, first, second) in SameSidePairs)
            AssertWorldRejectsStack(setupId, first, second);
    }

    private static void AssertContentRejectsStack(string setupId, string first, string second)
    {
        var context = Begin(setupId).Context;
        var artifact = context.ArtifactV6!;
        Assert.True(ContentPackV6Validator.Validate(artifact.Definition).IsValid);
        var legacy = artifact.Definition.LegacyDefinition;
        var scenario = context.Scenario;
        var target = scenario.InitialPlacements.Single(value => value.ElementId == first).LocationId;
        var stacked = new ContentScenario(scenario.ScenarioId, scenario.Start, scenario.End,
            scenario.InitialPlacements.Select(value => value.ElementId != second ? value
                : new ContentInitialPlacement(value.ElementId, target, value.Origin)),
            scenario.ScheduledArrivals, scenario.EntryAreas, scenario.InitialGroups, scenario.Origin);
        var modified = new ContentPackV6Definition(ContentTestData.Copy(legacy, scenarios: legacy.Scenarios
                .Select(value => value.ScenarioId == scenario.ScenarioId ? stacked : value)),
            artifact.Definition.ElementCombatFacts, artifact.Definition.InitialPlacementCombatFacts,
            artifact.Definition.CapabilityProfileId);
        var result = ContentPackV6Validator.Validate(modified);
        Assert.False(result.IsValid, setupId);
        Assert.Contains(result.Issues, issue => issue.Code == BreakdownCapabilityDiagnostics.Organization);
        Assert.Throws<InvalidContentPackException>(() => ContentPackV6Serializer.SerializeCanonical(modified));
    }

    private static void AssertWorldRejectsStack(string setupId, string first, string second)
    {
        var session = Begin(setupId);
        var world = session.CurrentSnapshot.World;
        var target = world.Elements.Single(value => value.ElementId == first).CurrentLocationId;
        var stacked = new CampaignWorldSnapshotV6(6,
            world.Elements.Select(value => value.ElementId != second ? value
                : new CampaignElementStateV5(value.ElementId, target, value.ReserveStatus,
                    value.OperationalState, value.Components)),
            world.Representations.Select(value => !value.BoundElementIds.Contains(second) ? value
                : new CampaignMapRepresentationState(value.RepresentationId, target, value.BindingKind,
                    value.BoundElementIds)),
            world.BrokenVehicleLots);
        var artifact = session.Context.ArtifactV6!;
        Assert.True(CampaignWorldV6Validator.IsValid(world, artifact, session.Context.Scenario));
        Assert.False(CampaignWorldV6Validator.IsValid(stacked, artifact, session.Context.Scenario));
        // The only World shape that could carry a ZOC source is refused before any ZOC is derived.
        var side = first.StartsWith("axis", StringComparison.Ordinal) ? LandSide.Axis : LandSide.Commonwealth;
        Assert.ThrowsAny<Exception>(() => CampaignElementMovedV3Factory.DeriveZocAuthority(
            stacked, artifact, session.Context.Scenario, side));
    }

    [Fact]
    public void SelectionInvariantNamesTheBoundedProfile()
    {
        Assert.Equal("Unsupported: positive opposing ZOC exceeds the bounded Combat profile.",
            CampaignContactCombatSelectionCut.PositiveZocExceedsBoundedProfile);
    }

    private static ExerciseSession Begin(string setupId)
    {
        var setup = CampaignSetupSnapshotV6.FromDefinition(Cna1979BreakdownSetupCatalog.Definitions
            .Single(value => value.SetupId == setupId));
        var started = CampaignExercises.Begin(new CampaignCreationRequest(1, "campaign-bounded-zoc",
            Cna1979Ruleset.Manifest.Hash, 6, setup.SetupId, setup.SetupHash, setup.Content.Pack.PackId,
            setup.Content.Pack.Hash, setup.Content.ScenarioId));
        Assert.True(started.IsStarted, setupId);
        return started.Session!;
    }
}
