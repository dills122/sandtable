using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Observations;
using Cna.Core.Rules;
using Cna.Core.Setups;
using Cna.Core.Tests.Content;

namespace Cna.Core.Tests.Campaigns;

public sealed class BreakdownPublicActivationTests
{
    [Fact]
    public void CurrentActionPolicyIsBreakdownSuccessor()
    {
        Assert.Equal(2, CampaignLegalActionSet.CurrentContractVersion);
        Assert.Equal("sandtable.legal-actions.v3", CampaignLegalActionSet.CurrentPolicyId);
    }

    [Fact]
    public void PublicCreationAdmitsCertifiedContentSetupAndSnapshotTogether()
    {
        var setup = CampaignSetupSnapshotV6.FromDefinition(Cna1979BreakdownSetupCatalog.Definitions.Single(value => value.SetupId == Cna1979BreakdownSetupCatalog.TruckSetupId));
        var result = CampaignAuthority.Create(new CampaignCreationRequest(1, "breakdown-public",
            Cna1979BreakdownRuleset.Manifest.Hash, 12345, setup.SetupId, setup.SetupHash,
            setup.Content.Pack.PackId, setup.Content.Pack.Hash, setup.Content.ScenarioId));

        Assert.True(result.IsCreated, result.RejectionReason.ToString());
        Assert.Equal(11, result.Handle!.CurrentSnapshot!.ContractVersion);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void PublicCreationRequiresAllSuccessorIdentityFamiliesTogether(int successorMask)
    {
        var current = Cna1979BreakdownSetupCatalog.Definitions.Single(value =>
            value.SetupId == Cna1979BreakdownSetupCatalog.TruckSetupId);
        var historical = Cna1979SetupCatalog.Definitions[0];
        var useSetup = (successorMask & 2) != 0;
        var useContent = (successorMask & 4) != 0;
        var request = new CampaignCreationRequest(1, "breakdown-identity-matrix",
            (successorMask & 1) != 0 ? Cna1979Ruleset.Manifest.Hash : Cna1979Ruleset.HistoricalManifestV8.Hash,
            12345, useSetup ? current.SetupId : historical.SetupId,
            useSetup ? current.Hash : historical.Hash,
            useContent ? current.Content.Pack.PackId : historical.Content.Pack.PackId,
            useContent ? current.Content.Pack.Hash : historical.Content.Pack.Hash,
            useContent ? current.Content.ScenarioId : historical.Content.ScenarioId);
        var authority = CampaignAuthority.Create(request);
        var exercise = Cna.Core.Exercises.CampaignExercises.Begin(request);
        Assert.Equal(successorMask == 7, authority.IsCreated);
        Assert.Equal(authority.IsCreated, exercise.IsStarted);
        Assert.Equal(authority.RejectionReason, exercise.RejectionReason);
        if (successorMask == 7) return;
        Assert.Null(authority.Handle);
        Assert.Null(exercise.Session);
        Assert.Null(exercise.CreationEventBytes);
        Assert.Null(exercise.InitialSnapshotBytes);
    }

    [Fact]
    public void PublicCreationRejectsCrossBoundCertifiedSetupAndContent()
    {
        var truck = Cna1979BreakdownSetupCatalog.Definitions.Single(value =>
            value.SetupId == Cna1979BreakdownSetupCatalog.TruckSetupId);
        var contact = Cna1979SyntheticContentCatalog.ArtifactBreakdownReactionV6;
        var created = CampaignAuthority.Create(new CampaignCreationRequest(1, "breakdown-cross-bound",
            Cna1979Ruleset.Manifest.Hash, 12345, truck.SetupId, truck.Hash,
            contact.Identity.PackId, contact.Identity.Hash, "breakdown-reaction-lab"));
        Assert.False(created.IsCreated);
        Assert.Equal(CampaignCreationRejectionReason.SetupContentMismatch, created.RejectionReason);
        Assert.Null(created.Handle);
    }

    [Fact]
    public void PublicIdleMovementDrainsThroughBreakdownToUnsupportedCombat()
    {
        var handle = ReachMovement();
        var side = handle.CurrentSnapshot!.CurrentPosition.SequenceContext.ActiveSide!.Value;
        var actions = CampaignLegalActions.Query(handle, Audience(side)).ActionSet!;
        handle = Submit(handle, actions, Assert.Single(actions.Candidates.OfType<CompleteMovementSegmentAction>()));
        var before = handle.CurrentSnapshot!.RandomState;
        var system = CampaignLegalActions.Query(handle, CampaignActionAudience.System).ActionSet!;
        handle = Submit(handle, system, Assert.IsType<CompleteBreakdownSegmentAction>(Assert.Single(system.Candidates)));
        Assert.Equal(before, handle.CurrentSnapshot!.RandomState);
        Assert.Equal(LandSegmentIds.Combat, handle.CurrentSnapshot.CurrentPosition.SequenceContext.SegmentId);
        Assert.All(Enum.GetValues<CampaignActionAudience>(), audience =>
            Assert.Empty(CampaignLegalActions.Query(handle, audience).ActionSet!.Candidates));
    }

    [Fact]
    public void OpenRouteRequiresStopAndPendingStopHasOnlySystemCapability()
    {
        var handle = ReachMovement();
        var side = handle.CurrentSnapshot!.CurrentPosition.SequenceContext.ActiveSide!.Value;
        var actions = CampaignLegalActions.Query(handle, Audience(side)).ActionSet!;
        var move = actions.Candidates.OfType<MoveElementAction>().First(value => value.ElementId.EndsWith("truck", StringComparison.Ordinal));
        handle = Submit(handle, actions, move);
        var owner = CampaignObservations.Query(handle, side).Observation!;
        var route = Assert.IsType<CampaignObservationV7NormalDecisionState>(owner.DecisionState).ActiveMovement!;
        actions = CampaignLegalActions.Query(handle, Audience(side)).ActionSet!;
        Assert.DoesNotContain(actions.Candidates, value => value is CompleteMovementSegmentAction);
        Assert.All(actions.Candidates.OfType<MoveElementAction>(), value => Assert.Equal(route.ElementId, value.ElementId));
        var stop = Assert.Single(actions.Candidates.OfType<StopElementMovementAction>());
        Assert.Equal(CampaignBreakdownLifecycleFactory.CreateStopActionId(handle.CurrentSnapshot!), stop.ActionId);
        Assert.Equal(CampaignBreakdownLifecycleFactory.CreateRouteCapability(handle.CurrentSnapshot!), stop.RouteId);
        handle = Submit(handle, actions, stop);

        Assert.All(new[] { CampaignActionAudience.Axis, CampaignActionAudience.Commonwealth }, audience =>
            Assert.Empty(CampaignLegalActions.Query(handle, audience).ActionSet!.Candidates));
        var system = CampaignLegalActions.Query(handle, CampaignActionAudience.System).ActionSet!;
        Assert.Equal("land.position.breakdown-stop", system.PositionId);
        var resolve = Assert.IsType<ResolveBreakdownStopAction>(Assert.Single(system.Candidates));
        Assert.Equal(CampaignBreakdownLifecycleFactory.CreateResolveActionId(handle.CurrentSnapshot!), resolve.ActionId);
        Assert.Equal(system, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(system)));
        handle = Submit(handle, system, resolve);
        Assert.IsType<CampaignBreakdownFlow.Idle>(handle.CurrentSnapshot!.BreakdownFlow);
    }

    [Theory]
    [InlineData("audience")]
    [InlineData("state")]
    [InlineData("position")]
    [InlineData("campaign")]
    [InlineData("action")]
    public void SubmissionRejectsChangedBindingAtomically(string mutation)
    {
        var handle = ReachMovement();
        var side = handle.CurrentSnapshot!.CurrentPosition.SequenceContext.ActiveSide!.Value;
        var set = CampaignLegalActions.Query(handle, Audience(side)).ActionSet!;
        var candidate = Assert.Single(set.Candidates.OfType<CompleteMovementSegmentAction>());
        var submission = new CampaignActionSubmission(1, set.CampaignId, set.StateVersion, set.PositionId, set.Audience, candidate.ActionId);
        submission = mutation switch
        {
            "audience" => submission with { Audience = CampaignActionAudience.System },
            "state" => submission with { ExpectedStateVersion = set.StateVersion - 1 },
            "position" => submission with { ExpectedPositionId = "land.position.breakdown-stop" },
            "campaign" => submission with { CampaignId = "other-campaign" },
            "action" => submission with { ActionId = new CompleteBreakdownSegmentAction().ActionId },
            _ => throw new InvalidOperationException(mutation),
        };
        var before = handle.CurrentSnapshot;
        var result = CampaignLegalActions.Submit(handle, submission);
        Assert.False(result.IsAccepted);
        Assert.Null(result.SuccessorHandle);
        Assert.Null(result.Receipt);
        Assert.Same(before, handle.CurrentSnapshot);
    }

    [Fact]
    public void PublicQueriesRejectLocallyValidFabricatedPreamble()
    {
        var snapshot = BreakdownSnapshotTests.Create(true);
        var context = CampaignContentContext.Create(BreakdownContentFixture.Artifact(), BreakdownSnapshotTests.Scenario().ScenarioId);
        Assert.True(CampaignSnapshotV11Validator.IsValid(snapshot, context.ArtifactV6!, context.Scenario));
        var handle = new CampaignAuthorityHandle(snapshot, context);
        Assert.False(CampaignLegalActions.Query(handle, CampaignActionAudience.Axis).IsSuccessful);
        Assert.False(CampaignObservations.Query(handle, LandSide.Axis).IsProjected);
    }

    internal static CampaignAuthorityHandle ReachMovement()
    {
        var setup = CampaignSetupSnapshotV6.FromDefinition(Cna1979BreakdownSetupCatalog.Definitions.Single(value => value.SetupId == Cna1979BreakdownSetupCatalog.TruckSetupId));
        var created = CampaignAuthority.Create(new CampaignCreationRequest(1, "breakdown-public-flow",
            Cna1979Ruleset.Manifest.Hash, 12345, setup.SetupId, setup.SetupHash,
            setup.Content.Pack.PackId, setup.Content.Pack.Hash, setup.Content.ScenarioId));
        Assert.True(created.IsCreated, created.RejectionReason.ToString());
        var handle = created.Handle!;
        for (var step = 0; step < 20; step++)
        {
            if (handle.CurrentSnapshot!.CurrentPosition.SequenceContext.SegmentId == LandSegmentIds.Movement)
                return handle;
            var set = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => CampaignLegalActions.Query(handle, audience).ActionSet!)
                .First(value => value.Candidates.Count > 0);
            var candidate = set.Candidates.FirstOrDefault(value => value is CompleteReserveDesignationAction)
                ?? set.Candidates.FirstOrDefault(value => value is ActFirstAction) ?? set.Candidates[0];
            handle = Submit(handle, set, candidate);
        }
        throw new InvalidOperationException("Preamble failed to reach Movement.");
    }

    private static CampaignAuthorityHandle Submit(CampaignAuthorityHandle handle, CampaignLegalActionSet set,
        CampaignActionCandidate candidate)
    {
        var result = CampaignLegalActions.Submit(handle, new CampaignActionSubmission(1, set.CampaignId,
            set.StateVersion, set.PositionId, set.Audience, candidate.ActionId));
        Assert.True(result.IsAccepted, $"{candidate.Kind}: {result.RejectionReason}");
        return result.SuccessorHandle!;
    }

    private static CampaignActionAudience Audience(LandSide side) => side == LandSide.Axis
        ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
}
