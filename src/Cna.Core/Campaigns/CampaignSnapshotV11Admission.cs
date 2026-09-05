using Cna.Core.Content;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Campaigns;

/// <summary>Current checkpoint admission. Full history provenance is checked separately by replay.</summary>
internal static class CampaignSnapshotV11Admission
{
    public static bool IsValid(CampaignSnapshotV11 snapshot, ContentPackV6Artifact artifact, ContentScenario scenario)
    {
        try
        {
            if (!CampaignSnapshotV11Validator.IsValid(snapshot, artifact, scenario)
                || !Cna1979BreakdownSetupCatalog.TryGet(snapshot.Setup.SetupId, out var definition)
                || snapshot.Setup != CampaignSetupSnapshotV6.FromDefinition(definition)) return false;
            var positions = Cna1979LandSequenceV4.CreateTurn(snapshot.Setup.InitialGameTurn);
            var position = snapshot.CurrentPosition.SequenceContext;
            var initial = CampaignWorldV6Factory.CreateInitial(artifact, scenario);
            var reserveCount = snapshot.World.Elements.Count(x => x.ReserveStatus == CampaignElementReserveStatus.ReserveI);
            if (snapshot.World.Elements.Any(x => x.ReserveStatus == CampaignElementReserveStatus.ReserveII)) return false;
            var reservePosition = positions[9];
            var isBeforeMovement = snapshot.StateVersion < 10 || position == reservePosition;
            if (snapshot.StateVersion < 10)
            {
                if (position != positions[checked((int)snapshot.StateVersion - 1)] || snapshot.World != initial
                    || snapshot.BreakdownFlow is not CampaignBreakdownFlow.Idle) return false;
            }
            else if (position == reservePosition)
            {
                if (snapshot.StateVersion != 10 + reserveCount || snapshot.BreakdownFlow is not CampaignBreakdownFlow.Idle) return false;
            }
            else
            {
                if (snapshot.StateVersion < 11 + reserveCount
                    || position.PhaseId != LandPhaseIds.MovementAndCombat
                    || position.ActorRole != LandActorRole.FirstActingSide) return false;
                if (position.SegmentId == LandSegmentIds.BreakdownDetermination && snapshot.StateVersion < 12 + reserveCount) return false;
                if (position.SegmentId == LandSegmentIds.Combat && snapshot.StateVersion < 13 + reserveCount) return false;
            }
            if (snapshot.StateVersion == 1) return true;
            var initiative = InitiativeResolver.Resolve(snapshot.Setup.InitialGameTurn, snapshot.Setup.InitialInitiative,
                SandtableRandom.Create(snapshot.RandomState.Seed), snapshot.Setup.Sources);
            if (snapshot.InitiativeHolder != initiative.Outcome.Holder) return false;
            if (snapshot.StateVersion <= 4)
                return snapshot.OperationStageOrders.Count == 0 && snapshot.OperationStageWeather.Count == 0
                    && snapshot.RandomState == initiative.RandomState;
            if (snapshot.OperationStageOrders.Count != 1) return false;
            var order = snapshot.OperationStageOrders.Single();
            if (order.ContractVersion != CampaignOperationStageOrder.CurrentContractVersion || order.GameTurn != snapshot.Setup.InitialGameTurn || order.OperationStage != 1
                || order.FirstSide == order.SecondSide || !Enum.IsDefined(order.FirstSide) || !Enum.IsDefined(order.SecondSide)) return false;
            var elements = artifact.Definition.LegacyDefinition.Elements.ToDictionary(x => x.ElementId, StringComparer.Ordinal);
            foreach (var element in snapshot.World.Elements.Where(x => x.ReserveStatus != CampaignElementReserveStatus.None))
                if (elements[element.ElementId].SideId != CampaignSnapshotSerializer.FormatSide(order.FirstSide)
                    || elements[element.ElementId].PlacementMode != ContentPlacementMode.Independent) return false;
            if (snapshot.StateVersion == 5)
                return snapshot.OperationStageWeather.Count == 0 && snapshot.RandomState == initiative.RandomState;
            if (snapshot.OperationStageWeather.Count != 1) return false;
            var expected = Cna1979Weather.Resolve(snapshot.Setup.InitialGameTurn, initiative.RandomState);
            var weather = new CampaignOperationStageWeather(1, snapshot.Setup.InitialGameTurn, 1,
                initiative.Outcome.Holder, expected.Season, expected.FirstDie, expected.SecondDie, expected.Kind,
                expected.Scope, expected.LocationDie, expected.AffectedAreas, 0, 0, 0);
            if (snapshot.OperationStageWeather[0] != weather || snapshot.RandomState.NextByteCursor < expected.RandomState.NextByteCursor)
                return false;
            var atMovementEntry = snapshot.StateVersion == 11 + reserveCount;
            if (isBeforeMovement || atMovementEntry)
            {
                if (snapshot.RandomState != expected.RandomState || snapshot.World.BrokenVehicleLots.Count != 0
                    || !snapshot.World.Representations.SequenceEqual(initial.Representations)) return false;
                var expectedElements = initial.Elements.Select(x =>
                {
                    var actual = snapshot.World.Elements.Single(y => y.ElementId == x.ElementId);
                    return new CampaignElementStateV5(x.ElementId, x.CurrentLocationId, actual.ReserveStatus, x.OperationalState, x.Components);
                });
                if (!snapshot.World.Elements.SequenceEqual(expectedElements)) return false;
            }
            return true;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or ArithmeticException or KeyNotFoundException)
        { return false; }
    }
}
