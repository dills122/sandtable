using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignReserveCheckpointValidationTests
{
    [Fact]
    public void CanonicalDecoderRejectsReserveStatusesAtEveryPreDesignationCheckpoint()
    {
        var evidence = ExecuteToReserve();

        for (var stateVersion = 1; stateVersion <= 10; stateVersion++)
        {
            var snapshot = CampaignProjector.Replay(
                evidence.Events.Take(stateVersion),
                evidence.Context);
            var canonical = Encoding.UTF8.GetString(
                CampaignSnapshotSerializer.Serialize(snapshot));

            foreach (var status in new[] { "reserve-i", "reserve-ii" })
            {
                var forged = ReplaceFirst(
                    canonical,
                    "\"reserveStatus\":\"none\"",
                    $"\"reserveStatus\":\"{status}\"");

                Assert.Throws<JsonException>(() => CampaignSnapshotSerializer.Deserialize(
                    Encoding.UTF8.GetBytes(forged)));
            }
        }
    }

    [Fact]
    public void ReserveDesignationCheckpointsUseFiniteStatusCountArithmetic()
    {
        var evidence = ExecuteToReserve();
        var reserve = evidence.Snapshot;
        var firstSide = FirstActingSideResolver.Resolve(reserve);
        var ownElementIds = GetElementIds(evidence.Context, firstSide);

        Assert.Equal(2, ownElementIds.Length);

        for (var designationCount = 0; designationCount <= ownElementIds.Length;
            designationCount++)
        {
            var checkpoint = WithStatuses(
                reserve,
                ownElementIds.Take(designationCount),
                CampaignElementReserveStatus.ReserveI) with
            {
                StateVersion = reserve.StateVersion + designationCount,
            };

            Assert.True(CampaignSnapshotValidator.IsLocallyValid(checkpoint));
            Assert.True(CampaignSnapshotValidator.IsValid(checkpoint, evidence.Context));
            Assert.Equal(
                reserve.SequencePosition,
                CampaignSnapshotSerializer.Deserialize(
                    CampaignSnapshotSerializer.Serialize(checkpoint)).SequencePosition);
        }

        var countMismatch = WithStatuses(
            reserve,
            ownElementIds.Take(1),
            CampaignElementReserveStatus.ReserveI) with
        {
            StateVersion = reserve.StateVersion + 2,
        };
        var reserveII = WithStatuses(
            reserve,
            ownElementIds.Take(1),
            CampaignElementReserveStatus.ReserveII) with
        {
            StateVersion = reserve.StateVersion + 1,
        };

        Assert.False(CampaignSnapshotValidator.IsLocallyValid(countMismatch));
        Assert.False(CampaignSnapshotValidator.IsLocallyValid(reserveII));
    }

    [Fact]
    public void ContextValidationRejectsReserveStatusOwnedByTheOtherSide()
    {
        var evidence = ExecuteToReserve();
        var reserve = evidence.Snapshot;
        var firstSide = FirstActingSideResolver.Resolve(reserve);
        var otherSide = firstSide == LandSide.Axis
            ? LandSide.Commonwealth
            : LandSide.Axis;
        var otherElementId = Assert.Single(GetElementIds(evidence.Context, otherSide).Take(1));
        var forged = WithStatuses(
            reserve,
            [otherElementId],
            CampaignElementReserveStatus.ReserveI) with
        {
            StateVersion = reserve.StateVersion + 1,
        };

        Assert.True(CampaignSnapshotValidator.IsLocallyValid(forged));
        Assert.False(CampaignSnapshotValidator.IsValid(forged, evidence.Context));
        Assert.Equal(
            forged,
            CampaignSnapshotSerializer.Deserialize(
                CampaignSnapshotSerializer.Serialize(forged)));
    }

    private static StageEntryCampaignEvidence ExecuteToReserve() =>
        StageEntryCampaignTestData.Execute(
            Cna1979SetupCatalog.Definitions[0],
            12345,
            InitiativeOrderChoice.ActFirst);

    private static string[] GetElementIds(
        CampaignContentContext context,
        LandSide side)
    {
        var sideId = side switch
        {
            LandSide.Axis => "axis",
            LandSide.Commonwealth => "commonwealth",
            _ => throw new ArgumentOutOfRangeException(nameof(side)),
        };

        return context.Artifact.Definition.Elements
            .Where(element => element.PlacementMode == ContentPlacementMode.Independent
                && string.Equals(element.SideId, sideId, StringComparison.Ordinal))
            .Select(element => element.ElementId)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static string ReplaceFirst(string value, string oldValue, string newValue)
    {
        var index = value.IndexOf(oldValue, StringComparison.Ordinal);
        Assert.True(index >= 0);
        return string.Concat(
            value.AsSpan(0, index),
            newValue,
            value.AsSpan(index + oldValue.Length));
    }

    private static CampaignSnapshot WithStatuses(
        CampaignSnapshot snapshot,
        IEnumerable<string> elementIds,
        CampaignElementReserveStatus status)
    {
        var selected = elementIds.ToHashSet(StringComparer.Ordinal);
        return snapshot with
        {
            World = new CampaignWorldSnapshot(
                CampaignWorldSnapshot.CurrentContractVersion,
                snapshot.World.Elements.Select(element => new CampaignElementState(
                    element.ElementId,
                    element.CurrentLocationId,
                    selected.Contains(element.ElementId)
                        ? status
                        : CampaignElementReserveStatus.None)).ToArray()),
        };
    }
}
