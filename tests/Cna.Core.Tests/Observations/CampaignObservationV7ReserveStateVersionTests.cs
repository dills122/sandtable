using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Observations;
using Cna.Core.Rules;
using Cna.Core.Setups;
using Cna.Core.Tests.Campaigns;
using Cna.Core.Tests.Content;

namespace Cna.Core.Tests.Observations;

/// <summary>
/// Characterization of issue #582. Pins CURRENT behavior, not a desired policy: the
/// Observation 7 projector publishes <c>snapshot.StateVersion</c> unmasked at the Reserve
/// position, while the V1 projector (see <see cref="CampaignReserveFogOfWarTests"/>) masks
/// it for the second-acting side. If a future change masks V7 (or documents a disclosure
/// window), these assertions are expected to change deliberately.
/// </summary>
public sealed class CampaignObservationV7ReserveStateVersionTests
{
    private const int ReserveBaseStateVersion = 10;

    [Fact]
    public void OpposingV7ObservationAndLegalActionsExposeRawStateVersionAtReserve()
    {
        var atReserve = ReachReserve();
        var firstSide = FirstSide(atReserve);
        var opponent = firstSide == LandSide.Axis ? LandSide.Commonwealth : LandSide.Axis;

        // Certified Breakdown content exposes exactly one designatable element per side
        // (the vehicle-cohort trucks cannot be Reserve), so 0 and 1 designations are reachable.
        var handles = new[] { atReserve, Designate(atReserve, firstSide), };

        for (var count = 0; count < handles.Length; count++)
        {
            var handle = handles[count];
            var snapshot = handle.CurrentSnapshot!;
            Assert.Equal(count, snapshot.World.Elements.Count(element =>
                element.ReserveStatus == CampaignElementReserveStatus.ReserveI));
            Assert.Equal(ReserveBaseStateVersion + count, snapshot.StateVersion);
            Assert.Equal(LandPhaseIds.ReserveDesignation,
                snapshot.CurrentPosition.SequenceContext.PhaseId);

            var acting = Observe(handle, firstSide);
            var opposing = Observe(handle, opponent);

            // Characterized leak: opponent-visible StateVersion tracks designation count.
            Assert.Equal(snapshot.StateVersion, acting.StateVersion);
            Assert.Equal(snapshot.StateVersion, opposing.StateVersion);

            // The public legal-action query for the opponent carries the same raw version.
            var opposingActions = CampaignLegalActions.Query(handle, Audience(opponent));
            Assert.True(opposingActions.IsSuccessful);
            Assert.Equal(snapshot.StateVersion, opposingActions.ActionSet!.StateVersion);
        }

        // Opponent-visible StateVersion differs across 0/1 designations (unlike V1, which
        // masks all three to the same value; see CampaignReserveFogOfWarTests).
        Assert.Equal(2, handles.Select(handle => Observe(handle, opponent).StateVersion)
            .Distinct().Count());
        Assert.Equal(2, handles.Select(handle =>
            CampaignObservationV7Serializer.SerializeCanonical(Observe(handle, opponent)))
            .Select(Convert.ToBase64String).Distinct().Count());
    }

    private static CampaignObservationV7 Observe(CampaignAuthorityHandle handle, LandSide side)
    {
        var result = CampaignObservations.Query(handle, side);
        Assert.True(result.IsProjected);
        return result.Observation!;
    }

    private static CampaignAuthorityHandle ReachReserve()
    {
        var setup = CampaignSetupSnapshotV6.FromDefinition(Cna1979BreakdownSetupCatalog.Definitions
            .Single(value => value.SetupId == Cna1979BreakdownSetupCatalog.TruckSetupId));
        var created = CampaignAuthority.Create(new CampaignCreationRequest(1,
            "v7-reserve-state-version", Cna1979Ruleset.Manifest.Hash, 12345, setup.SetupId,
            setup.SetupHash, setup.Content.Pack.PackId, setup.Content.Pack.Hash,
            setup.Content.ScenarioId));
        Assert.True(created.IsCreated, created.RejectionReason.ToString());
        var handle = created.Handle!;
        for (var step = 0; step < 20; step++)
        {
            if (handle.CurrentSnapshot!.CurrentPosition.SequenceContext.PhaseId
                == LandPhaseIds.ReserveDesignation)
                return handle;
            var set = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => CampaignLegalActions.Query(handle, audience).ActionSet!)
                .First(value => value.Candidates.Count > 0);
            var candidate = set.Candidates.FirstOrDefault(value => value is ActFirstAction)
                ?? set.Candidates[0];
            handle = Submit(handle, set, candidate);
        }
        throw new InvalidOperationException("Preamble failed to reach Reserve designation.");
    }

    private static LandSide FirstSide(CampaignAuthorityHandle handle)
    {
        var position = handle.CurrentSnapshot!.CurrentPosition.SequenceContext;
        return handle.CurrentSnapshot.OperationStageOrders.Single(order =>
            order.GameTurn == position.GameTurn
            && order.OperationStage == position.OperationStage).FirstSide;
    }

    private static CampaignAuthorityHandle Designate(CampaignAuthorityHandle handle, LandSide side)
    {
        var set = CampaignLegalActions.Query(handle, Audience(side)).ActionSet!;
        var candidate = set.Candidates.OfType<DesignateReserveAction>()
            .OrderBy(value => value.ElementId, StringComparer.Ordinal).FirstOrDefault()
            ?? throw new InvalidOperationException("No designatable element.");
        return Submit(handle, set, candidate);
    }

    private static CampaignAuthorityHandle Submit(CampaignAuthorityHandle handle,
        CampaignLegalActionSet set, CampaignActionCandidate candidate)
    {
        var result = CampaignLegalActions.Submit(handle, new CampaignActionSubmission(1,
            set.CampaignId, set.StateVersion, set.PositionId, set.Audience, candidate.ActionId));
        Assert.True(result.IsAccepted, $"{candidate.Kind}: {result.RejectionReason}");
        return result.SuccessorHandle!;
    }

    private static CampaignActionAudience Audience(LandSide side) => side == LandSide.Axis
        ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
}

/// <summary>
/// Characterization of issue #582 for Observation 6 (Snapshot 10). Pins CURRENT behavior:
/// the V6 projector publishes the raw StateVersion at the Reserve position to both sides.
/// V6 is only reachable through historical replay factories, not the public query path.
/// </summary>
public sealed class CampaignObservationV6ReserveStateVersionTests
{
    [Fact]
    public void OpposingV6ObservationExposesRawStateVersionAtReserve()
    {
        var fixture = CampaignV10TestData.Create();
        var reserve = Cna1979LandSequence.CreateTurn(1)[9];
        var source = fixture.MovementSnapshot;
        var world = fixture.Created.InitialWorld;
        var designatedId = world.Elements[0].ElementId;
        var designatedWorld = new CampaignWorldSnapshotV5(5, world.Elements.Select(value =>
            value.ElementId == designatedId
                ? new CampaignElementStateV5(value.ElementId, value.CurrentLocationId,
                    CampaignElementReserveStatus.ReserveI, value.OperationalState, value.Components)
                : value), world.Representations);
        var snapshots = new[] { world, designatedWorld }.Select((candidate, count) =>
            new CampaignSnapshotV10(CampaignSnapshotV10.CurrentContractVersion, source.CampaignId,
                10 + count, source.RulesetHash, source.Setup, candidate, LandSide.Axis,
                source.OperationStageOrders, [], source.RandomState,
                CampaignPositionV10.FromSequence(reserve), null)).ToArray();

        var facts = new CampaignObservationV6AuthorityFacts([], []);
        var opposing = new List<long>();
        for (var count = 0; count < snapshots.Length; count++)
        {
            var snapshot = snapshots[count];
            Assert.True(CampaignSnapshotV10Validator.IsValid(snapshot, fixture.Artifact, fixture.Scenario));
            Assert.Equal(LandPhaseIds.ReserveDesignation, reserve.PhaseId);
            var firstSide = snapshot.OperationStageOrders.Single(order =>
                order.GameTurn == reserve.GameTurn && order.OperationStage == reserve.OperationStage).FirstSide;
            var other = firstSide == LandSide.Axis ? LandSide.Commonwealth : LandSide.Axis;
            var acting = CampaignObservationV6Projector.Project(snapshot, fixture.Artifact,
                fixture.Scenario, firstSide, facts);
            var observed = CampaignObservationV6Projector.Project(snapshot, fixture.Artifact,
                fixture.Scenario, other, facts);
            Assert.Equal(10 + count, acting.StateVersion);
            Assert.Equal(10 + count, observed.StateVersion);
            opposing.Add(observed.StateVersion);
        }

        // Unlike V1 (masked to a constant), the opposing view differs with designation count.
        Assert.Equal(2, opposing.Distinct().Count());
    }
}
