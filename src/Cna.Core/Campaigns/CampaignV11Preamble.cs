using Cna.Core.Actions;
using Cna.Core.Content;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Campaigns;

internal static class CampaignV11Preamble
{
    private static readonly RuleReference[] ConvoySources =
        [Cna1979LandSequence.SourceReference, Cna1979SetupCatalog.OpeningPreambleSourceReference];
    private static readonly RuleReference[] DeclarationSources =
        [Cna1979LandSequence.SourceReference, Cna1979LandSequence.InitiativeSideSourceReference,
         Cna1979LandSequence.StageChoiceSourceReference, new("spi-1979-land-rules", "7.16")];

    public static CampaignEvent Create(CampaignSnapshotV11 prior, ContentPackV6Artifact artifact,
        ContentScenario scenario, CampaignActionCandidate candidate)
    {
        if (!CampaignSnapshotV11Admission.IsValid(prior, artifact, scenario)
            || prior.BreakdownFlow is not CampaignBreakdownFlow.Idle
            || prior.CurrentPosition.Kind != CampaignPositionV11Kind.Sequence)
            throw new InvalidOperationException("invalid-breakdown-authority");
        var position = prior.CurrentPosition.SequenceContext;
        var next = Cna1979LandSequenceV4.GetNext(position);
        var version = checked(prior.StateVersion + 1);
        var turn = position.GameTurn;
        var from = position.PositionId;
        switch (candidate)
        {
            case ResolveInitiativeAction when prior.StateVersion == 1:
                var initiative = InitiativeResolver.Resolve(turn, prior.Setup.InitialInitiative,
                    prior.RandomState, prior.Setup.Sources);
                return new InitiativeDetermined(prior.CampaignId, version, from, initiative.Outcome,
                    prior.RandomState.AlgorithmId, prior.RandomState.NextByteCursor,
                    initiative.RandomState.NextByteCursor, next, initiative.Sources);
            case ResolveNoObligationNavalConvoyScheduleAction when prior.StateVersion == 2:
                return new NoObligationNavalConvoyScheduleResolved(prior.CampaignId, version, from, next, ConvoySources);
            case ResolveNoObligationTacticalShippingAction when prior.StateVersion == 3:
                return new NoObligationTacticalShippingResolved(prior.CampaignId, version, from, next, ConvoySources);
            case ActFirstAction or ActLastAction when prior.StateVersion == 4 && candidate.OperationStage == 1:
                var holder = prior.InitiativeHolder!.Value;
                var first = candidate is ActFirstAction ? holder : Opposite(holder);
                return new InitiativeOrderDeclared(prior.CampaignId, version, from, next, 1, holder,
                    first, Opposite(first), DeclarationSources);
            case ResolveWeatherAction when prior.StateVersion == 5:
                var weather = Cna1979Weather.Resolve(turn, prior.RandomState);
                return new WeatherDetermined(prior.CampaignId, version, from, turn, 1,
                    prior.InitiativeHolder!.Value, weather.Season, weather.FirstDie, weather.SecondDie,
                    weather.Kind, weather.Scope, weather.LocationDie, weather.AffectedAreas, 0, 0, 0,
                    weather.RandomState.NextByteCursor, next, WeatherEventFactory.GetSources(weather.Kind));
            case ResolveNoObligationOrganizationAction when prior.StateVersion == 6:
                return new NoObligationOrganizationResolved(prior.CampaignId, version, from, turn, 1,
                    next, NoObligationOrganizationResolved.RequiredSources);
            case ResolveNoObligationNavalConvoyArrivalAction when prior.StateVersion == 7:
                return new NoObligationNavalConvoyArrivalResolved(prior.CampaignId, version, from, turn, 1,
                    next, NoObligationNavalConvoyArrivalResolved.RequiredSources);
            case ResolveNoObligationFleetAssignmentAction when prior.StateVersion == 8:
                return new NoObligationFleetAssignmentResolved(prior.CampaignId, version, from, turn, 1,
                    next, NoObligationFleetAssignmentResolved.RequiredSources);
            case ResolveNoObligationFleetRepairAction when prior.StateVersion == 9:
                return new NoObligationFleetRepairResolved(prior.CampaignId, version, from, turn, 1,
                    next, NoObligationFleetRepairResolved.RequiredSources);
            case DesignateReserveAction reserve when position.PhaseId == LandPhaseIds.ReserveDesignation:
                var side = prior.OperationStageOrders.Single().FirstSide;
                var element = prior.World.Elements.SingleOrDefault(x => x.ElementId == reserve.ElementId);
                var content = artifact.Definition.LegacyDefinition.Elements.SingleOrDefault(x => x.ElementId == reserve.ElementId);
                if (element is null || content is null || content.PlacementMode != ContentPlacementMode.Independent
                    || content.SideId != CampaignSnapshotSerializer.FormatSide(side)
                    || element.ReserveStatus != CampaignElementReserveStatus.None
                    || scenario.InitialPlacements.Single(x => x.ElementId == element.ElementId).LocationId != element.CurrentLocationId)
                    throw new InvalidOperationException("Invalid Reserve selection.");
                return new ReserveElementDesignated(prior.CampaignId, version, from, turn, 1, side,
                    reserve.ElementId, CampaignElementReserveStatus.None, CampaignElementReserveStatus.ReserveI,
                    position, ReserveElementDesignated.RequiredSources);
            case CompleteReserveDesignationAction when position.PhaseId == LandPhaseIds.ReserveDesignation:
                return new ReserveDesignationCompleted(prior.CampaignId, version, from, turn, 1,
                    prior.OperationStageOrders.Single().FirstSide, next, ReserveDesignationCompleted.RequiredSources);
            default: throw new InvalidOperationException("The preamble action is not legal at this checkpoint.");
        }
    }

    public static CampaignSnapshotV11 Apply(CampaignSnapshotV11 prior, CampaignEvent value,
        ContentPackV6Artifact artifact, ContentScenario scenario)
    {
        try
        {
            CampaignActionCandidate action = value switch
            {
                InitiativeDetermined => new ResolveInitiativeAction(),
                NoObligationNavalConvoyScheduleResolved => new ResolveNoObligationNavalConvoyScheduleAction(),
                NoObligationTacticalShippingResolved => new ResolveNoObligationTacticalShippingAction(),
                InitiativeOrderDeclared declared when declared.FirstSide == declared.DeclaringHolder => new ActFirstAction(1),
                InitiativeOrderDeclared => new ActLastAction(1),
                WeatherDetermined => new ResolveWeatherAction(),
                NoObligationOrganizationResolved => new ResolveNoObligationOrganizationAction(),
                NoObligationNavalConvoyArrivalResolved => new ResolveNoObligationNavalConvoyArrivalAction(),
                NoObligationFleetAssignmentResolved => new ResolveNoObligationFleetAssignmentAction(),
                NoObligationFleetRepairResolved => new ResolveNoObligationFleetRepairAction(),
                ReserveElementDesignated reserve => new DesignateReserveAction(reserve.ElementId),
                ReserveDesignationCompleted => new CompleteReserveDesignationAction(),
                _ => throw new InvalidCampaignHistoryException("Unsupported preamble event."),
            };
            var expected = Create(prior, artifact, scenario, action);
            if (!CampaignV11PreambleCodec.Serialize(expected).SequenceEqual(CampaignV11PreambleCodec.Serialize(value)))
                throw new InvalidCampaignHistoryException("Preamble event differs from prior authority.");
            var world = prior.World;
            var rng = prior.RandomState;
            var holder = prior.InitiativeHolder;
            var orders = prior.OperationStageOrders;
            var weather = prior.OperationStageWeather;
            var position = CampaignPreambleSequenceBinding.Position(value);
            switch (value)
            {
                case InitiativeDetermined initiative:
                    holder = initiative.Outcome.Holder;
                    rng = new(rng.ContractVersion, rng.AlgorithmId, rng.Seed, initiative.RandomCursorAfter);
                    break;
                case InitiativeOrderDeclared declaration:
                    orders = new[] { new CampaignOperationStageOrder(CampaignOperationStageOrder.CurrentContractVersion, position.GameTurn, 1, declaration.FirstSide, declaration.SecondSide) };
                    break;
                case WeatherDetermined resolved:
                    weather = new[] { resolved.ToState() };
                    rng = new(rng.ContractVersion, rng.AlgorithmId, rng.Seed, resolved.RandomCursorAfter);
                    break;
                case ReserveElementDesignated reserve:
                    world = new CampaignWorldSnapshotV6(6, world.Elements.Select(x => x.ElementId == reserve.ElementId
                        ? new CampaignElementStateV5(x.ElementId, x.CurrentLocationId, CampaignElementReserveStatus.ReserveI,
                            x.OperationalState, x.Components) : x), world.Representations, world.BrokenVehicleLots);
                    break;
                case ReserveDesignationCompleted:
                    position = new(4, position.PositionId, position.GameTurn, position.OperationStage,
                        position.StageId, position.PhaseId, position.SegmentId, position.StepId, position.ActorRole,
                        orders.Single().FirstSide, position.Sources);
                    break;
            }
            var after = new CampaignSnapshotV11(11, prior.CampaignId, value.StateVersion, prior.RulesetHash,
                prior.Setup, world, holder, orders, weather, rng, CampaignPositionV11.FromSequence(position),
                null, new CampaignBreakdownFlow.Idle());
            if (!CampaignSnapshotV11Admission.IsValid(after, artifact, scenario))
                throw new InvalidCampaignHistoryException("Invalid resulting preamble authority.");
            return after;
        }
        catch (InvalidCampaignHistoryException) { throw; }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or ArithmeticException or System.Text.Json.JsonException)
        { throw new InvalidCampaignHistoryException(error.Message); }
    }
    private static LandSide Opposite(LandSide side) => side == LandSide.Axis ? LandSide.Commonwealth : LandSide.Axis;
}
