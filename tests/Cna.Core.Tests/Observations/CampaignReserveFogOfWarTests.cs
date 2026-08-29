using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Observations;
using Cna.Core.Rules;
using Cna.Core.Setups;
using Cna.Core.Tests.Campaigns;

namespace Cna.Core.Tests.Observations;

public sealed class CampaignReserveFogOfWarTests
{
    [Theory]
    [InlineData((int)InitiativeOrderChoice.ActFirst)]
    [InlineData((int)InitiativeOrderChoice.ActLast)]
    public void OpponentBytesHideZeroOneAndAllDesignationsAtReserveAndMovement(
        int choiceValue)
    {
        var initial = CampaignReserveActionTestData.ReachReserve(
            0,
            (InitiativeOrderChoice)choiceValue);
        var firstSide = FirstActingSideResolver.Resolve(initial.Snapshot);
        var actingAudience = CampaignReserveActionTestData.ToAudience(firstSide);
        var opponent = firstSide == LandSide.Axis
            ? LandSide.Commonwealth
            : LandSide.Axis;
        var opponentAudience = CampaignReserveActionTestData.ToAudience(opponent);
        var reserveHandles = new[]
        {
            initial,
            Designate(initial, actingAudience, 1),
            Designate(initial, actingAudience, int.MaxValue),
        };

        Assert.Equal([0, 1, 2], reserveHandles.Select(handle =>
            handle.Snapshot.World.Elements.Count(element =>
                element.ReserveStatus == CampaignElementReserveStatus.ReserveI)));
        Assert.All(reserveHandles, handle =>
        {
            var acting = CampaignReserveActionTestData.Query(handle, actingAudience);
            var opposing = CampaignReserveActionTestData.Query(handle, opponentAudience);
            var observation = Project(handle, opponent);

            Assert.Equal(handle.Snapshot.StateVersion, acting.StateVersion);
            Assert.Equal(10, opposing.StateVersion);
            Assert.Equal(10, observation.StateVersion);
            Assert.Equal(2, opposing.ContractVersion);
            Assert.Equal("sandtable.legal-actions.v2", opposing.PolicyId);
            Assert.Equal(5, observation.ContractVersion);
            Assert.Equal("sandtable.observation.movement-side-safe.v1", observation.PolicyId);
        });
        AssertByteInvariant(reserveHandles.Select(handle =>
            CampaignObservationSerializer.SerializeCanonical(Project(handle, opponent))));
        AssertByteInvariant(reserveHandles.Select(handle =>
            CampaignLegalActionSerializer.Serialize(
                CampaignReserveActionTestData.Query(handle, opponentAudience))));

        var staleSet = CampaignReserveActionTestData.Query(initial, actingAudience);
        var staleCandidate = staleSet.Candidates.OfType<DesignateReserveAction>()
            .OrderBy(candidate => candidate.ElementId, StringComparer.Ordinal)
            .First();
        var changed = reserveHandles[1];
        var stale = CampaignLegalActions.Submit(
            changed,
            CampaignReserveActionTestData.Bind(staleSet, staleCandidate));
        Assert.False(stale.IsAccepted);
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState,
            stale.RejectionReason);

        var movementHandles = reserveHandles.Select(handle =>
            Complete(handle, actingAudience)).ToArray();
        Assert.All(movementHandles, handle =>
        {
            var opposing = CampaignReserveActionTestData.Query(handle, opponentAudience);
            var observation = Project(handle, opponent);

            Assert.Equal(11, opposing.StateVersion);
            Assert.Equal(11, observation.StateVersion);
            Assert.Empty(opposing.Candidates);
            Assert.Equal(LandPhaseIds.MovementAndCombat, handle.Snapshot.PhaseId);
            Assert.Equal(LandSegmentIds.Movement, handle.Snapshot.SegmentId);
        });
        AssertByteInvariant(movementHandles.Select(handle =>
            CampaignObservationSerializer.SerializeCanonical(Project(handle, opponent))));
        AssertByteInvariant(movementHandles.Select(handle =>
            CampaignLegalActionSerializer.Serialize(
                CampaignReserveActionTestData.Query(handle, opponentAudience))));

        Assert.Equal(3, movementHandles.Select(handle =>
            CampaignObservationSerializer.SerializeCanonical(Project(handle, firstSide)))
            .Distinct(ByteArrayComparer.Instance).Count());
    }

    private static CampaignAuthorityHandle Designate(
        CampaignAuthorityHandle initial,
        CampaignActionAudience actingAudience,
        int count)
    {
        var handle = initial;
        for (var index = 0; index < count; index++)
        {
            var set = CampaignReserveActionTestData.Query(handle, actingAudience);
            var candidate = set.Candidates.OfType<DesignateReserveAction>()
                .OrderBy(value => value.ElementId, StringComparer.Ordinal)
                .FirstOrDefault();
            if (candidate is null) break;

            var result = CampaignLegalActions.Submit(
                handle,
                CampaignReserveActionTestData.Bind(set, candidate));
            handle = Assert.IsType<CampaignAuthorityHandle>(result.SuccessorHandle);
        }
        return handle;
    }

    private static CampaignAuthorityHandle Complete(
        CampaignAuthorityHandle handle,
        CampaignActionAudience actingAudience)
    {
        var set = CampaignReserveActionTestData.Query(handle, actingAudience);
        var candidate = Assert.Single(
            set.Candidates.OfType<CompleteReserveDesignationAction>());
        var result = CampaignLegalActions.Submit(
            handle,
            CampaignReserveActionTestData.Bind(set, candidate));
        return Assert.IsType<CampaignAuthorityHandle>(result.SuccessorHandle);
    }

    private static CampaignObservation Project(
        CampaignAuthorityHandle handle,
        LandSide observer) => Assert.IsType<CampaignObservation>(
            CampaignObservationProjector.Project(
                handle.Snapshot,
                handle.Context,
                observer).Observation);

    private static void AssertByteInvariant(IEnumerable<byte[]> values)
    {
        var copies = values.ToArray();
        Assert.Equal(3, copies.Length);
        Assert.All(copies.Skip(1), value => Assert.Equal(copies[0], value));
    }

    private sealed class ByteArrayComparer : IEqualityComparer<byte[]>
    {
        internal static ByteArrayComparer Instance { get; } = new();

        public bool Equals(byte[]? first, byte[]? second) =>
            first is not null && second is not null && first.SequenceEqual(second);

        public int GetHashCode(byte[] value)
        {
            var hash = new HashCode();
            foreach (var item in value) hash.Add(item);
            return hash.ToHashCode();
        }
    }
}
