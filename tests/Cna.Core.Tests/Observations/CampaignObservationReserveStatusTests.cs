using System.Text;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Observations;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Observations;

public sealed class CampaignObservationReserveStatusTests
{
    [Fact]
    public void PublicReserveStatusVocabularyIsExactAndDistinctFromAuthority()
    {
        Assert.Equal(
            [
                CampaignObservationReserveStatus.None,
                CampaignObservationReserveStatus.ReserveI,
                CampaignObservationReserveStatus.ReserveII,
            ],
            Enum.GetValues<CampaignObservationReserveStatus>());
        Assert.Equal([0, 1, 2], Enum.GetValues<CampaignObservationReserveStatus>()
            .Select(value => (int)value));
        Assert.NotEqual(
            typeof(CampaignElementReserveStatus),
            typeof(CampaignObservationReserveStatus));
    }

    [Fact]
    public void OwnElementRejectsUndefinedStatusAndIncludesStatusInValueSemantics()
    {
        var none = CreateElement(CampaignObservationReserveStatus.None);
        var equivalent = CreateElement(CampaignObservationReserveStatus.None);
        var reserve = CreateElement(CampaignObservationReserveStatus.ReserveI);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreateElement((CampaignObservationReserveStatus)99));
        Assert.Equal(none, equivalent);
        Assert.Equal(none.GetHashCode(), equivalent.GetHashCode());
        Assert.NotEqual(none, reserve);
        Assert.NotEqual(none.GetHashCode(), reserve.GetHashCode());
    }

    [Theory]
    [InlineData(CampaignObservationReserveStatus.None, "none")]
    [InlineData(CampaignObservationReserveStatus.ReserveI, "reserve-i")]
    [InlineData(CampaignObservationReserveStatus.ReserveII, "reserve-ii")]
    public void CanonicalSerializerUsesExactReserveStatusStrings(
        CampaignObservationReserveStatus status,
        string canonical)
    {
        var bytes = CampaignObservationSerializer.SerializeCanonical(CreateObservation(status));
        var json = Encoding.UTF8.GetString(bytes);

        Assert.Contains(
            $"\"currentLocationId\":\"west\",\"reserveStatus\":\"{canonical}\"",
            json,
            StringComparison.Ordinal);
    }

    [Fact]
    public void OwnElementProjectionMapsOwnerStatusAndIgnoresOpponentStatus()
    {
        var artifact = Cna1979SyntheticContentCatalog.Artifact;
        var scenario = artifact.Definition.Scenarios.Single(candidate =>
            candidate.ScenarioId == "movement-contact-lab");
        var initial = CampaignWorldFactory.CreateInitial(artifact, scenario);
        var ownerReserve = ReplaceStatus(
            initial,
            "axis-element-a",
            CampaignElementReserveStatus.ReserveI);
        var opponentChanged = ReplaceStatus(
            ownerReserve,
            "commonwealth-element-a",
            CampaignElementReserveStatus.ReserveI);

        var baseline = CampaignObservationProjector.ProjectOwnElements(
            artifact.Definition,
            ownerReserve,
            LandSide.Axis);
        var changed = CampaignObservationProjector.ProjectOwnElements(
            artifact.Definition,
            opponentChanged,
            LandSide.Axis);

        Assert.Equal(
            CampaignObservationReserveStatus.ReserveI,
            baseline.Single(element => element.ElementId == "axis-element-a").ReserveStatus);
        Assert.True(baseline.SequenceEqual(changed));
    }

    [Fact]
    public void V1OwnElementProjectionRejectsReserveII()
    {
        var artifact = Cna1979SyntheticContentCatalog.Artifact;
        var scenario = artifact.Definition.Scenarios.Single(candidate =>
            candidate.ScenarioId == "movement-contact-lab");
        var world = ReplaceStatus(
            CampaignWorldFactory.CreateInitial(artifact, scenario),
            "axis-element-a",
            CampaignElementReserveStatus.ReserveII);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CampaignObservationProjector.ProjectOwnElements(
                artifact.Definition,
                world,
                LandSide.Axis));
    }

    private static ObservedOwnElement CreateElement(
        CampaignObservationReserveStatus status) => new(
            "axis-element-a",
            "axis-formation",
            "land.organization.battalion",
            20,
            "west",
            status);

    private static CampaignObservation CreateObservation(
        CampaignObservationReserveStatus status) => new(
            CampaignObservation.CurrentContractVersion,
            CampaignObservation.CurrentPolicyId,
            "campaign-1",
            1,
            Cna1979Ruleset.Manifest.Hash,
            "movement-contact-lab",
            LandSide.Axis,
            new CampaignObservationPosition(
                "land.position.initiative-determination",
                1,
                0,
                "land.stage.initiative-determination",
                "land.phase.initiative-determination",
                null,
                null,
                LandActorRole.None,
                null,
                null),
            null,
            [new CampaignObservationLocation("west", "land.terrain.clear")],
            [],
            [CreateElement(status)]);

    private static CampaignWorldSnapshot ReplaceStatus(
        CampaignWorldSnapshot world,
        string elementId,
        CampaignElementReserveStatus status) => new(
            CampaignWorldSnapshot.CurrentContractVersion,
            world.Elements.Select(element => element.ElementId == elementId
                ? new CampaignElementState(
                    element.ElementId,
                    element.CurrentLocationId,
                    status)
                : element).ToArray());
}
