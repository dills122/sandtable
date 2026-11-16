using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Content;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Campaigns;

// Source-backed turn authority retains the original setup and effective world.
// It does not make incomplete source evidence playable.
internal sealed class CampaignSourceTurnAuthority
{
    internal const int ContractVersion = 1;
    internal const int MapBoundContractVersion = 2;
    internal const int CdSeamContractVersion = 3;
    internal static readonly string StartActionId = SystemActionId("source-turn-start");
    internal static readonly string InitiativeActionId = SystemActionId("source-turn-initiative");
    internal static readonly string CompleteGt1ScheduleActionId = SystemActionId("source-gt1-convoy-schedule-complete");
    internal static readonly string CompleteGt1OrganizationActionId = SystemActionId("source-gt1-organization-complete-no-prior-projects-v1");
    internal static readonly string CompleteGt1ArrivalsActionId = SystemActionId("source-gt1-stage1-arrivals-complete-v1");
    internal static readonly string CompleteGt1FleetActionId = SystemActionId("source-gt1-stage1-fleet-unavailable-v1");
    internal static readonly string CompleteGt1FirstBreakdownActionId = SystemActionId("source-gt1-first-breakdown-no-eligible-vehicles-v1");
    internal static readonly string ResolveStageOneWeatherActionId = SystemActionId("source-gt1-stage1-weather");
    internal static readonly string AnnounceGt1Lane2ActionId = SystemActionId("source-gt1-op1-lane2-announce");
    internal static readonly string AnnounceGt1Lane3ActionId = SystemActionId("source-gt1-op1-lane3-announce");
    internal static readonly string AnnounceGt1MultiLanesActionId = SystemActionId("source-gt1-op1-multi-lanes-announce");
    private static string SystemActionId(string name) =>
        CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes("graziani.source.system-action.v1\0" + name));
    private static readonly IReadOnlyList<RuleReference> InitiativeSources = Array.AsReadOnly<RuleReference>(
        [new(Cna1979GrazianiSourceFacts.ScenariosSourceId, "60.6")]);

    private CampaignSourceTurnAuthority(CampaignSourcePlayableWorld world, long stateVersion,
        LandSequencePosition position, RandomStreamState randomState, LandSide? initiative,
        CampaignSourceGt1SupplyPlanning? gt3Supply = null, CampaignSourceGt1CommonwealthSupplyPlanning? gt5Supply = null,
        int tacticalShippingDeclinedMask = 0, LandSide? stageOneFirstSide = null, WeatherResolution? stageOneWeather = null,
        int voluntaryOrganizationDeclinedMask = 0, CampaignSourceTrainingLedger? training = null,
        CampaignSourceGt1AxisArrival? axisArrival = null, CampaignSourceCommonwealthSupplyWorld? commonwealthSupply = null,
        (int GameTurn, int OperationStage)? fourthIndianPaidStage = null, bool? horseTrainingActive = null,
        CampaignSourceGt1BenghaziArrival? benghaziArrival = null, CampaignSourceGt1MultiArrival? multiArrival = null)
    {
        World = world;
        StateVersion = stateVersion;
        Position = position;
        RandomState = randomState;
        InitiativeHolder = initiative;
        Gt3Supply = gt3Supply;
        Gt5Supply = gt5Supply;
        TacticalShippingDeclinedMask = tacticalShippingDeclinedMask;
        StageOneFirstSide = stageOneFirstSide;
        StageOneWeather = stageOneWeather;
        VoluntaryOrganizationDeclinedMask = voluntaryOrganizationDeclinedMask;
        Training = training ?? CampaignSourceTrainingLedger.CreateSixthAustralian();
        AxisArrival = axisArrival ?? CampaignSourceGt1AxisArrival.Create(world.Source);
        CommonwealthSupply = commonwealthSupply ?? world.Source.CommonwealthSupplyWorld;
        FourthIndianPaidStage = fourthIndianPaidStage;
        HorseTrainingActive = horseTrainingActive;
        BenghaziArrival = benghaziArrival ?? CampaignSourceGt1BenghaziArrival.Create(world.Source, AxisArrival);
        MultiArrival = multiArrival;
    }

    internal CampaignSourcePlayableWorld World { get; }
    internal CampaignSourceAuthorityState Source => World.Source;
    internal long StateVersion { get; }
    internal LandSequencePosition Position { get; }
    internal RandomStreamState RandomState { get; }
    internal LandSide? InitiativeHolder { get; }
    internal CampaignSourceGt1SupplyPlanning? Gt3Supply { get; }
    internal CampaignSourceGt1CommonwealthSupplyPlanning? Gt5Supply { get; }
    internal int TacticalShippingDeclinedMask { get; }
    internal LandSide? StageOneFirstSide { get; }
    internal WeatherResolution? StageOneWeather { get; }
    internal int VoluntaryOrganizationDeclinedMask { get; }
    internal CampaignSourceTrainingLedger Training { get; }
    internal CampaignSourceGt1AxisArrival AxisArrival { get; }
    internal CampaignSourceGt1BenghaziArrival BenghaziArrival { get; }
    internal CampaignSourceGt1MultiArrival? MultiArrival { get; }
    internal CampaignSourceCommonwealthSupplyWorld CommonwealthSupply { get; }
    internal (int GameTurn, int OperationStage)? FourthIndianPaidStage { get; }
    // Null means initial training status remains unknown. A move interrupts
    // any training, without inventing prior progress or current morale.
    internal bool? HorseTrainingActive { get; }
    internal bool HasDeclinedVoluntaryOrganization(LandSide side) =>
        (VoluntaryOrganizationDeclinedMask & (side == LandSide.Axis ? 1 : 2)) != 0;
    internal bool HasDeclinedTacticalShipping(LandSide side) =>
        (TacticalShippingDeclinedMask & (side == LandSide.Axis ? 1 : 2)) != 0;

    internal static CampaignSourceTurnAuthority Start(CampaignSourceAuthorityState source, bool legacy = false, bool cdSeam = false)
    {
        ArgumentNullException.ThrowIfNull(source);
        var world = CampaignSourcePlayableWorld.Create(source, mapIdentityBound: !legacy, cdSeamBound: cdSeam);
        if (!world.IsGameplayReady)
            throw new InvalidOperationException("Historical setup is not ready for gameplay.");
        var position = Cna1979LandSequenceV4.CreateTurn(1)[0];
        return new(world, checked(source.StateVersion + 1), position, source.RandomState, null);
    }

    internal CampaignSourceTurnAuthority ResolveInitiative()
    {
        if (InitiativeHolder is not null || Position != Cna1979LandSequenceV4.CreateTurn(1)[0])
            throw new InvalidOperationException("Source initiative is not the current position.");
        var resolution = InitiativeResolver.Resolve(1, new PredeterminedInitiative(LandSide.Axis), RandomState, InitiativeSources);
        return new(World, checked(StateVersion + 1), Cna1979LandSequenceV4.GetNext(Position),
            resolution.RandomState, resolution.Outcome.Holder, CampaignSourceGt1SupplyPlanning.Create(Source, resolution.RandomState),
            CampaignSourceGt1CommonwealthSupplyPlanning.Create(), training: Training, axisArrival: AxisArrival, commonwealthSupply: CommonwealthSupply, fourthIndianPaidStage: FourthIndianPaidStage, horseTrainingActive: HorseTrainingActive, benghaziArrival: BenghaziArrival, multiArrival: MultiArrival);
    }

    internal CampaignSourceTurnAuthority RollGt3Supply()
    {
        if (Position.GameTurn != 1 || Position.PhaseId != LandPhaseIds.NavalConvoySchedule || Gt3Supply is null
            || (!Gt3Supply.Finished && Gt3Supply.RandomState != RandomState))
            throw new InvalidOperationException("GT3 supply planning is not the current source turn position.");
        var rolled = Gt3Supply.Roll();
        return new(World, checked(StateVersion + 1), Position, rolled.RandomState, InitiativeHolder, rolled, Gt5Supply, training: Training, axisArrival: AxisArrival, commonwealthSupply: CommonwealthSupply, fourthIndianPaidStage: FourthIndianPaidStage, horseTrainingActive: HorseTrainingActive, benghaziArrival: BenghaziArrival, multiArrival: MultiArrival);
    }

    internal CampaignSourceTurnAuthority RollGt5Supply()
    {
        if (Position.GameTurn != 1 || Position.PhaseId != LandPhaseIds.NavalConvoySchedule || Gt5Supply is null)
            throw new InvalidOperationException("GT5 supply planning is not the current source turn position.");
        var rolled = Gt5Supply.Roll(RandomState);
        return new(World, checked(StateVersion + 1), Position, rolled.Random, InitiativeHolder,
            Gt3Supply is { Finished: false } pending ? pending.WithRandomState(rolled.Random) : Gt3Supply, rolled.Plan, training: Training, axisArrival: AxisArrival, commonwealthSupply: CommonwealthSupply, fourthIndianPaidStage: FourthIndianPaidStage, horseTrainingActive: HorseTrainingActive, benghaziArrival: BenghaziArrival, multiArrival: MultiArrival);
    }

    internal CampaignSourceTurnAuthority AdjustGt3Supply(int lane, int stage, int delta)
    {
        RequireGt3Scheduling();
        var adjusted = Gt3Supply!.Adjust(lane, stage, delta);
        return new(World, checked(StateVersion + 1), Position, adjusted.RandomState, InitiativeHolder, adjusted, Gt5Supply, training: Training, axisArrival: AxisArrival, commonwealthSupply: CommonwealthSupply, fourthIndianPaidStage: FourthIndianPaidStage, horseTrainingActive: HorseTrainingActive, benghaziArrival: BenghaziArrival, multiArrival: MultiArrival);
    }

    internal CampaignSourceTurnAuthority FinishGt3Supply()
    {
        RequireGt3Scheduling();
        var finished = Gt3Supply!.Finish();
        return new(World, checked(StateVersion + 1), Position, finished.RandomState, InitiativeHolder, finished, Gt5Supply, training: Training, axisArrival: AxisArrival, commonwealthSupply: CommonwealthSupply, fourthIndianPaidStage: FourthIndianPaidStage, horseTrainingActive: HorseTrainingActive, benghaziArrival: BenghaziArrival, multiArrival: MultiArrival);
    }

    private void RequireGt3Scheduling()
    {
        if (Position.GameTurn != 1 || Position.PhaseId != LandPhaseIds.NavalConvoySchedule || Gt3Supply is null
            || (!Gt3Supply.Finished && Gt3Supply.RandomState != RandomState))
            throw new InvalidOperationException("GT3 supply planning is not the current source turn position.");
    }

    internal CampaignSourceTurnAuthority CompleteGt1Scheduling()
    {
        RequireGt3Scheduling();
        if (Gt3Supply is not { Finished: true } || Gt5Supply?.Die is null)
            throw new InvalidOperationException("Both sides must finish their GT1 supply planning.");
        // Axis chart 20.66 first permits planning Light AA on GT2 and medium
        // trucks on GT6; scenario 60.37 adds no earlier replacement grant.
        // Thus the GT1-to-GT3 planning cut has no Axis replacement or
        // motorization cargo. Existing GT1/GT2 arrivals remain retained.
        // Scenario 60.47 likewise forbids Commonwealth replacement arrivals
        // before November, excluding replacements in GT3 October I.
        return new(World, checked(StateVersion + 1), Cna1979LandSequenceV4.GetNext(Position), RandomState,
            InitiativeHolder, Gt3Supply, Gt5Supply, training: Training, axisArrival: AxisArrival, commonwealthSupply: CommonwealthSupply, fourthIndianPaidStage: FourthIndianPaidStage, horseTrainingActive: HorseTrainingActive, benghaziArrival: BenghaziArrival, multiArrival: MultiArrival);
    }

    internal CampaignSourceTurnAuthority DeclineTacticalShipping(LandSide side)
    {
        if (Position.GameTurn != 1 || Position.PhaseId != LandPhaseIds.TacticalShipping
            || !Enum.IsDefined(side) || HasDeclinedTacticalShipping(side))
            throw new InvalidOperationException("Tactical shipping choice is unavailable.");
        var mask = TacticalShippingDeclinedMask | (side == LandSide.Axis ? 1 : 2);
        var nextPosition = mask == 3 ? Cna1979LandSequenceV4.GetNext(Position) : Position;
        return new(World, checked(StateVersion + 1), nextPosition, RandomState, InitiativeHolder, Gt3Supply, Gt5Supply, mask, training: Training, axisArrival: AxisArrival, commonwealthSupply: CommonwealthSupply, fourthIndianPaidStage: FourthIndianPaidStage, horseTrainingActive: HorseTrainingActive, benghaziArrival: BenghaziArrival, multiArrival: MultiArrival);
    }

    internal CampaignSourceTurnAuthority DeclareStageOneOrder(bool axisFirst)
    {
        if (Position.GameTurn != 1 || Position.OperationStage != 1 || Position.PhaseId != LandPhaseIds.InitiativeDeclaration
            || InitiativeHolder != LandSide.Axis || StageOneFirstSide is not null)
            throw new InvalidOperationException("Stage-one initiative declaration is unavailable.");
        return new(World, checked(StateVersion + 1), Cna1979LandSequenceV4.GetNext(Position), RandomState,
            InitiativeHolder, Gt3Supply, Gt5Supply, TacticalShippingDeclinedMask,
            axisFirst ? LandSide.Axis : LandSide.Commonwealth, training: Training, axisArrival: AxisArrival, commonwealthSupply: CommonwealthSupply, fourthIndianPaidStage: FourthIndianPaidStage, horseTrainingActive: HorseTrainingActive, benghaziArrival: BenghaziArrival, multiArrival: MultiArrival);
    }

    internal CampaignSourceTurnAuthority ResolveStageOneWeather()
    {
        if (Position.GameTurn != 1 || Position.OperationStage != 1 || Position.PhaseId != LandPhaseIds.WeatherDetermination
            || StageOneFirstSide is null || StageOneWeather is not null)
            throw new InvalidOperationException("Stage-one weather is unavailable.");
        var weather = Cna1979Weather.Resolve(1, RandomState);
        return new(World, checked(StateVersion + 1), Cna1979LandSequenceV4.GetNext(Position), weather.RandomState,
            InitiativeHolder, Gt3Supply, Gt5Supply, TacticalShippingDeclinedMask,
            StageOneFirstSide, weather, training: Training, axisArrival: AxisArrival, commonwealthSupply: CommonwealthSupply, fourthIndianPaidStage: FourthIndianPaidStage, horseTrainingActive: HorseTrainingActive, benghaziArrival: BenghaziArrival, multiArrival: MultiArrival);
    }

    internal CampaignSourceTurnAuthority DeclineVoluntaryOrganization(LandSide side)
    {
        if (Position.GameTurn != 1 || Position.OperationStage != 1 || Position.PhaseId != LandPhaseIds.Organization
            || !Enum.IsDefined(side) || HasDeclinedVoluntaryOrganization(side))
            throw new InvalidOperationException("Voluntary organization choice is unavailable.");
        var mask = VoluntaryOrganizationDeclinedMask | (side == LandSide.Axis ? 1 : 2);
        // Stay in Organization: mandatory construction/training completion
        // cannot be treated as absent until source work is fully adjudicated.
        return new(World, checked(StateVersion + 1), Position, RandomState, InitiativeHolder, Gt3Supply, Gt5Supply,
            TacticalShippingDeclinedMask, StageOneFirstSide, StageOneWeather, mask, Training, AxisArrival, CommonwealthSupply, FourthIndianPaidStage, HorseTrainingActive, BenghaziArrival, MultiArrival);
    }

    // Provisional scenario ruling: no construction project was already in
    // progress unless the setup identifies one. The opening stage has no
    // completed in-game training stage to process. This bounded transition
    // requires the Commonwealth to interrupt the documented Australian
    // training; continuing it needs a separate stage-progress authority.
    internal CampaignSourceTurnAuthority CompleteGt1Organization()
    {
        if (Position.GameTurn != 1 || Position.OperationStage != 1 || Position.PhaseId != LandPhaseIds.Organization
            || VoluntaryOrganizationDeclinedMask != 3 || Training.LastRecordedStage != 0 || !Training.Interrupted)
            throw new InvalidOperationException("Both sides must finish opening organization choices.");
        return new(World, checked(StateVersion + 1), Cna1979LandSequenceV4.GetNext(Position), RandomState,
            InitiativeHolder, Gt3Supply, Gt5Supply, TacticalShippingDeclinedMask, StageOneFirstSide,
            StageOneWeather, VoluntaryOrganizationDeclinedMask, Training, AxisArrival, CommonwealthSupply,
            FourthIndianPaidStage, HorseTrainingActive, BenghaziArrival, MultiArrival);
    }

    internal CampaignSourceTurnAuthority VoteSixthAustralianBaseline(LandSide side, bool approve)
    {
        if (Position.GameTurn != 1 || Position.OperationStage != 1 || Position.PhaseId != LandPhaseIds.Organization
            || !Enum.IsDefined(side) || !World.GroupedWorld.Groups.Any(group => group.GroupId == CampaignSourceTrainingLedger.FormationId))
            throw new InvalidOperationException("Australian training baseline vote is unavailable.");
        var vote = Training.SetBaselineVote(side == LandSide.Axis, approve);
        return new(World, checked(StateVersion + 1), Position, RandomState, InitiativeHolder, Gt3Supply, Gt5Supply,
            TacticalShippingDeclinedMask, StageOneFirstSide, StageOneWeather, VoluntaryOrganizationDeclinedMask,
            vote, AxisArrival, CommonwealthSupply, FourthIndianPaidStage, HorseTrainingActive, BenghaziArrival, MultiArrival);
    }

    internal CampaignSourceTurnAuthority InterruptSixthAustralianTraining()
    {
        if (Position.GameTurn != 1 || Position.OperationStage != 1 || Position.PhaseId != LandPhaseIds.Organization
            || HasDeclinedVoluntaryOrganization(LandSide.Commonwealth)
            || !World.GroupedWorld.Groups.Any(group => group.GroupId == CampaignSourceTrainingLedger.FormationId))
            throw new InvalidOperationException("Sixth Australian training choice is unavailable.");
        var interrupted = Training.Interrupt();
        return new(World, checked(StateVersion + 1), Position, RandomState, InitiativeHolder, Gt3Supply, Gt5Supply,
            TacticalShippingDeclinedMask, StageOneFirstSide, StageOneWeather, VoluntaryOrganizationDeclinedMask,
            interrupted, AxisArrival, CommonwealthSupply, FourthIndianPaidStage, HorseTrainingActive, BenghaziArrival, MultiArrival);
    }

    internal CampaignSourceTurnAuthority AnnounceGt1Lane2()
    {
        RequireGt1StageOneArrival();
        var arrival = AxisArrival.Announce();
        return new(World, checked(StateVersion + 1), Position, RandomState, InitiativeHolder, Gt3Supply, Gt5Supply,
            TacticalShippingDeclinedMask, StageOneFirstSide, StageOneWeather, VoluntaryOrganizationDeclinedMask, Training, arrival, CommonwealthSupply, FourthIndianPaidStage, HorseTrainingActive, BenghaziArrival, MultiArrival);
    }

    internal CampaignSourceTurnAuthority DeclineGt1Lane2Attack()
    {
        RequireGt1StageOneArrival();
        var arrival = AxisArrival.DeclineAttackAndDeliver();
        return new(World, checked(StateVersion + 1), Position, RandomState, InitiativeHolder, Gt3Supply, Gt5Supply,
            TacticalShippingDeclinedMask, StageOneFirstSide, StageOneWeather, VoluntaryOrganizationDeclinedMask, Training, arrival, CommonwealthSupply, FourthIndianPaidStage, HorseTrainingActive, BenghaziArrival, MultiArrival);
    }

    internal CampaignSourceTurnAuthority AnnounceGt1Lane3()
    {
        RequireGt1StageOneArrival();
        var arrival = BenghaziArrival.Announce(Source, AxisArrival);
        return new(World, checked(StateVersion + 1), Position, RandomState, InitiativeHolder, Gt3Supply, Gt5Supply,
            TacticalShippingDeclinedMask, StageOneFirstSide, StageOneWeather, VoluntaryOrganizationDeclinedMask,
            Training, AxisArrival, CommonwealthSupply, FourthIndianPaidStage, HorseTrainingActive, arrival, MultiArrival);
    }

    internal CampaignSourceTurnAuthority DeclineGt1Lane3Attack()
    {
        RequireGt1StageOneArrival();
        var result = BenghaziArrival.DeclineAttackAndDeliver(Source, AxisArrival);
        return new(World, checked(StateVersion + 1), Position, RandomState, InitiativeHolder, Gt3Supply, Gt5Supply,
            TacticalShippingDeclinedMask, StageOneFirstSide, StageOneWeather, VoluntaryOrganizationDeclinedMask,
            Training, result.AxisArrival, CommonwealthSupply, FourthIndianPaidStage, HorseTrainingActive, result.Arrival, MultiArrival);
    }

    internal CampaignSourceTurnAuthority AnnounceGt1MultiLanes()
    {
        RequireGt1StageOneArrival();
        if (MultiArrival is not null || BenghaziArrival.Announced)
            throw new InvalidOperationException("Multi-lane announcement already occurred.");
        var announced = CampaignSourceGt1MultiArrival.Announce(Source, AxisArrival);
        return new(World, checked(StateVersion + 1), Position, RandomState, InitiativeHolder, Gt3Supply, Gt5Supply,
            TacticalShippingDeclinedMask, StageOneFirstSide, StageOneWeather, VoluntaryOrganizationDeclinedMask,
            Training, AxisArrival, CommonwealthSupply, FourthIndianPaidStage, HorseTrainingActive, BenghaziArrival, announced);
    }

    internal CampaignSourceTurnAuthority DeclineGt1MultiLaneAttack(int lane)
    {
        RequireGt1StageOneArrival();
        var result = (MultiArrival ?? throw new InvalidOperationException("Lanes are not announced."))
            .DeclineAndDeliver(Source, AxisArrival, lane);
        return new(World, checked(StateVersion + 1), Position, RandomState, InitiativeHolder, Gt3Supply, Gt5Supply,
            TacticalShippingDeclinedMask, StageOneFirstSide, StageOneWeather, VoluntaryOrganizationDeclinedMask,
            Training, result.Axis, CommonwealthSupply, FourthIndianPaidStage, HorseTrainingActive, BenghaziArrival, result.Arrival);
    }

    private void RequireGt1StageOneArrival()
    {
        if (Position.GameTurn != 1 || Position.OperationStage != 1 || Position.PhaseId != LandPhaseIds.NavalConvoyArrival || Position.SegmentId is not null)
            throw new InvalidOperationException("GT1 Op1 Naval Convoy Arrival is not current.");
    }

    internal CampaignSourceTurnAuthority CompleteGt1StageOneArrivals()
    {
        RequireGt1StageOneArrival();
        // The initial CW plan has no operation-stage allocation authority.
        // Only its explicit opening skip proves there is no CW supply due.
        if (Source.CommonwealthSupplyPlans?.Entries.SingleOrDefault(value => value.TargetGameTurn == 1)?.Status != "skipped-opening"
            || World.World8.PendingArrivals.Any(value => value.Arrival.GameTurn == 1 && value.Arrival.OperationStage == 1)
            || World.Battlefield.Pending.Any(value => value.GameTurn == 1 && value.OperationStage == 1))
            throw new InvalidOperationException("Stage-one Commonwealth or unit arrivals remain unresolved.");
        var due = Source.OpeningConvoyPlan.Allocations().Where(value => value.OperationStage == 1).ToArray();
        var delivered = MultiArrival is not null
            ? due.All(value => MultiArrival.IsDue(value.Lane) && MultiArrival.IsDelivered(value.Lane))
            : due.Length == 0 || (due.Length == 1 && (due[0].Lane == 2 && AxisArrival.Delivered
                || due[0].Lane == 3 && BenghaziArrival.Delivered));
        if (!delivered) throw new InvalidOperationException("Stage-one Axis convoy arrivals remain unresolved.");
        return new(World, checked(StateVersion + 1), Cna1979LandSequenceV4.GetNext(Position), RandomState,
            InitiativeHolder, Gt3Supply, Gt5Supply, TacticalShippingDeclinedMask, StageOneFirstSide,
            StageOneWeather, VoluntaryOrganizationDeclinedMask, Training, AxisArrival, CommonwealthSupply,
            FourthIndianPaidStage, HorseTrainingActive, BenghaziArrival, MultiArrival);
    }

    internal CampaignSourceTurnAuthority CompleteGt1StageOneFleet()
    {
        if (Position.GameTurn != 1 || Position.OperationStage != 1
            || Position.PhaseId != LandPhaseIds.CommonwealthFleet || Position.SegmentId != LandSegmentIds.FleetAssignment)
            throw new InvalidOperationException("Stage-one fleet assignment is not current.");
        // Scenario 60.45: Commonwealth ships start undamaged and cannot
        // move until GT2. The supported arrival path permits no attack, so
        // nothing can have damaged them during the opening arrival phase.
        var repair = Cna1979LandSequenceV4.GetNext(Position);
        if (repair.PhaseId != LandPhaseIds.CommonwealthFleet || repair.SegmentId != LandSegmentIds.FleetRepair)
            throw new InvalidOperationException("Fleet sequence differs from the pinned source rules.");
        return new(World, checked(StateVersion + 1), Cna1979LandSequenceV4.GetNext(repair), RandomState,
            InitiativeHolder, Gt3Supply, Gt5Supply, TacticalShippingDeclinedMask, StageOneFirstSide,
            StageOneWeather, VoluntaryOrganizationDeclinedMask, Training, AxisArrival, CommonwealthSupply,
            FourthIndianPaidStage, HorseTrainingActive, BenghaziArrival, MultiArrival);
    }

    internal CampaignSourceTurnAuthority DeclineGt1FirstReserves(LandSide side)
    {
        if (Position.GameTurn != 1 || Position.OperationStage != 1 || Position.PhaseId != LandPhaseIds.ReserveDesignation
            || Position.ActorRole != LandActorRole.FirstActingSide || StageOneFirstSide != side
            || World.World8.World.Elements.Any(value => value.ReserveStatus != CampaignElementReserveStatus.None))
            throw new InvalidOperationException("First-player reserve choice is unavailable.");
        var movement = Cna1979LandSequenceV4.GetNext(Position);
        if (movement.SegmentId != LandSegmentIds.Movement || movement.ActorRole != LandActorRole.FirstActingSide)
            throw new InvalidOperationException("First-player movement sequence differs from the source rules.");
        var materialized = new LandSequencePosition(movement.ContractVersion, movement.PositionId, movement.GameTurn,
            movement.OperationStage, movement.StageId, movement.PhaseId, movement.SegmentId, movement.StepId,
            movement.ActorRole, side, movement.Sources);
        return new(World, checked(StateVersion + 1), materialized, RandomState, InitiativeHolder, Gt3Supply, Gt5Supply,
            TacticalShippingDeclinedMask, StageOneFirstSide, StageOneWeather, VoluntaryOrganizationDeclinedMask,
            Training, AxisArrival, CommonwealthSupply, FourthIndianPaidStage, HorseTrainingActive, BenghaziArrival, MultiArrival);
    }

    internal CampaignOperationStageWeather StageOneMovementWeather()
    {
        var weather = StageOneWeather ?? throw new InvalidOperationException("Stage-one weather is unavailable.");
        if (InitiativeHolder is not { } determiningSide) throw new InvalidOperationException("Stage-one initiative is unavailable.");
        return new CampaignOperationStageWeather(1, 1, 1, determiningSide, weather.Season, weather.FirstDie, weather.SecondDie,
            weather.Kind, weather.Scope, weather.LocationDie, weather.AffectedAreas, 0, 0, 0);
    }

    internal CampaignSourceTurnAuthority MoveFourthIndianOpening(string supplyId)
    {
        var moved = CampaignSourceFourthIndianMovement.Apply(World, CommonwealthSupply, FourthIndianPaidStage,
            Position, StageOneMovementWeather(), supplyId);
        return new(moved.World, checked(StateVersion + 1), Position, RandomState, InitiativeHolder, Gt3Supply, Gt5Supply,
            TacticalShippingDeclinedMask, StageOneFirstSide, StageOneWeather, VoluntaryOrganizationDeclinedMask,
            Training, AxisArrival, moved.Supply, moved.PaidStage, false, BenghaziArrival, MultiArrival);
    }

    internal CampaignSourceTurnAuthority FinishGt1FirstMovement(LandSide side)
    {
        Cna1979LandSequenceV4.RequireMaterializedMovement(Position);
        if (!World.CdSeamBound || Position.GameTurn != 1 || Position.OperationStage != 1
            || Position.ActorRole != LandActorRole.FirstActingSide || Position.ActiveSide != side
            || StageOneFirstSide != side)
            throw new InvalidOperationException("First-player movement completion is unavailable.");
        var catalogPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.PositionId == Position.PositionId);
        var nextPosition = Cna1979LandSequenceV4.GetNext(catalogPosition);
        if (nextPosition.SegmentId != LandSegmentIds.BreakdownDetermination
            || nextPosition.ActorRole != LandActorRole.FirstActingSide)
            throw new InvalidOperationException("Movement successor differs from the source sequence.");
        return new(World, checked(StateVersion + 1), nextPosition, RandomState, InitiativeHolder, Gt3Supply, Gt5Supply,
            TacticalShippingDeclinedMask, StageOneFirstSide, StageOneWeather, VoluntaryOrganizationDeclinedMask,
            Training, AxisArrival, CommonwealthSupply, FourthIndianPaidStage, HorseTrainingActive, BenghaziArrival, MultiArrival);
    }

    internal CampaignSourceTurnAuthority CompleteGt1FirstBreakdown()
    {
        if (!World.CdSeamBound || Position.GameTurn != 1 || Position.OperationStage != 1
            || Position.SegmentId != LandSegmentIds.BreakdownDetermination
            || Position.ActorRole != LandActorRole.FirstActingSide || StageOneFirstSide is null
            || Source.GroupedWorld is not { } initial || Source.Battlefield is not { } initialBattlefield
            || initial.Groups.SelectMany(group => group.Members).Any(member => member.CapabilityPointsExpended != CapabilityPointAmount.Zero)
            || initial.Detached.Any(value => value.Member.CapabilityPointsExpended != CapabilityPointAmount.Zero)
            || initial.Prior.World.Elements.Any(element => element.OperationalState.CapabilityPointsExpended != CapabilityPointAmount.Zero
                || element.OperationalState.MovementEnded is not null))
            throw new InvalidOperationException("First-player breakdown authority is unavailable.");
        if (FourthIndianPaidStage is null)
        {
            if (World.GroupedWorld.Hash() != initial.Hash() || World.Battlefield.Hash() != initialBattlefield.Hash())
                throw new InvalidOperationException("Unaccounted movement prevents breakdown completion.");
        }
        else if (FourthIndianPaidStage == (1, 1) && StageOneFirstSide == LandSide.Commonwealth)
        {
            // This source-pinned move contains no breakdown-eligible tank,
            // armoured reconnaissance or self-propelled gun cohort. Rebuild
            // its exact grouped and battlefield successor before advancing.
            var group = initial.Groups.SingleOrDefault(value => value.GroupId == CampaignSourceFourthIndianMovement.GroupId)
                ?? throw new InvalidOperationException("Fourth Indian source group is missing.");
            if (!CampaignSourceFourthIndianMovement.IsOpeningFormation(group)
                || initial.Detached.Any(value => value.GroupId == group.GroupId)
                || initial.ExternalReservations.Any(value => value.GroupId == group.GroupId))
                throw new InvalidOperationException("Fourth Indian source roster or transport differs from the opening move.");
            var expected = initial.MoveFormation(group.GroupId, CampaignSourceFourthIndianRoad.Destination,
                CampaignSourceFourthIndianMovement.OpeningCosts(group), group.StackValue!.Value, 1, 1);
            var expectedBattlefield = initialBattlefield.MoveFormation(initial, expected, group.GroupId);
            if (World.GroupedWorld.Hash() != expected.Hash() || World.Battlefield.Hash() != expectedBattlefield.Hash())
                throw new InvalidOperationException("Movement differs from the bounded no-breakdown source path.");
        }
        else throw new InvalidOperationException("Unsupported movement history requires breakdown adjudication.");
        var next = Cna1979LandSequenceV4.GetNext(Position);
        if (next.SegmentId != LandSegmentIds.Combat || next.StepId != LandStepIds.PositionDetermination)
            throw new InvalidOperationException("Breakdown successor differs from the source sequence.");
        return new(World, checked(StateVersion + 1), next, RandomState, InitiativeHolder, Gt3Supply, Gt5Supply,
            TacticalShippingDeclinedMask, StageOneFirstSide, StageOneWeather, VoluntaryOrganizationDeclinedMask,
            Training, AxisArrival, CommonwealthSupply, FourthIndianPaidStage, HorseTrainingActive, BenghaziArrival, MultiArrival);
    }

    internal CampaignSourceTurnAuthority ArrivePolish(string destination)
    {
        var world = CampaignSourcePolishArrival.Apply(World, Position, LandSide.Commonwealth, destination);
        return new(world, checked(StateVersion + 1), Position, RandomState, InitiativeHolder, Gt3Supply, Gt5Supply,
            TacticalShippingDeclinedMask, StageOneFirstSide, StageOneWeather, VoluntaryOrganizationDeclinedMask, Training, AxisArrival, CommonwealthSupply, FourthIndianPaidStage, HorseTrainingActive, BenghaziArrival, MultiArrival);
    }

    internal string PolishActionId(string destination) => new CampaignSourceTurnAction(StateVersion, destination).ActionId;

    internal byte[] SerializeCheckpoint()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", World.CdSeamBound ? CdSeamContractVersion : World.MapIdentityBound ? MapBoundContractVersion : ContractVersion);
            writer.WriteString("kind", "source-turn");
            if (World.MapIdentityBound)
            {
                writer.WriteStartObject("historicalMap");
                writer.WriteString("formatId", "sandtable.cna1979-historical-map.v1");
                writer.WriteString("revision", Cna1979HistoricalMap.Revision);
                writer.WriteString("hexSha256", Cna1979HistoricalMap.HexHash);
                writer.WriteString("hexsideSha256", Cna1979HistoricalMap.EdgeHash);
                if (World.CdSeamBound)
                {
                    Cna1979HistoricalMap.RequireCdSeamPolicy();
                    writer.WriteString("cdSeamPolicyId", Cna1979HistoricalMap.CdSeamPolicyId);
                    writer.WriteString("cdSeamPolicyHash", Cna1979HistoricalMap.CdSeamPolicyHash);
                }
                writer.WriteEndObject();
            }
            writer.WriteString("campaignId", Source.Request.CampaignId);
            writer.WriteNumber("stateVersion", StateVersion);
            writer.WriteString("rulesetHash", Source.Request.RulesetHash);
            writer.WriteString("sourceCheckpointHash", CampaignPregameSourceCreation.Digest(Source.SerializeCheckpoint()));
            writer.WriteString("groupedWorldHash", World.GroupedWorld.Hash());
            writer.WriteString("battlefieldHash", World.Battlefield.Hash());
            writer.WriteString("positionId", Position.PositionId);
            writer.WriteNumber("gameTurn", Position.GameTurn);
            writer.WriteNumber("operationStage", Position.OperationStage);
            writer.WriteString("initiativeHolder", InitiativeHolder is null ? null : "axis");
            writer.WriteString("gt3SupplyPlanHash", Gt3Supply?.Hash());
            writer.WriteString("gt5SupplyPlanHash", Gt5Supply?.Hash());
            writer.WriteNumber("tacticalShippingDeclinedMask", TacticalShippingDeclinedMask);
            writer.WriteNumber("voluntaryOrganizationDeclinedMask", VoluntaryOrganizationDeclinedMask);
            writer.WriteString("trainingHash", Training.Hash());
            writer.WriteString("gt1AxisArrivalHash", AxisArrival.Hash());
            writer.WriteString("gt1BenghaziArrivalHash", BenghaziArrival.Hash());
            if (MultiArrival is not null) writer.WriteString("gt1MultiArrivalHash", MultiArrival.Hash());
            writer.WriteString("commonwealthSupplyHash", CommonwealthSupply.Hash());
            if (HorseTrainingActive is bool horseTraining) writer.WriteBoolean("horseTrainingActive", horseTraining); else writer.WriteNull("horseTrainingActive");
            if (FourthIndianPaidStage is { } paidStage)
            {
                writer.WriteStartObject("fourthIndianPaidStage");
                writer.WriteNumber("gameTurn", paidStage.GameTurn); writer.WriteNumber("operationStage", paidStage.OperationStage);
                writer.WriteEndObject();
            }
            else writer.WriteNull("fourthIndianPaidStage");
            writer.WriteString("stageOneFirstSide", StageOneFirstSide switch { LandSide.Axis => "axis", LandSide.Commonwealth => "commonwealth", _ => null });
            if (StageOneWeather is { } weather)
            {
                writer.WriteStartObject("stageOneWeather"); writer.WriteString("season", weather.Season.ToString());
                writer.WriteNumber("firstDie", weather.FirstDie); writer.WriteNumber("secondDie", weather.SecondDie);
                writer.WriteString("kind", weather.Kind.ToString()); writer.WriteString("scope", weather.Scope.ToString());
                if (weather.LocationDie is int locationDie) writer.WriteNumber("locationDie", locationDie); else writer.WriteNull("locationDie");
                writer.WriteStartArray("affectedAreas"); foreach (var area in weather.AffectedAreas) writer.WriteStringValue(area.ToString()); writer.WriteEndArray();
                writer.WriteEndObject();
            }
            else writer.WriteNull("stageOneWeather");
            writer.WriteString("randomAlgorithm", RandomState.AlgorithmId);
            writer.WriteNumber("randomSeed", RandomState.Seed);
            writer.WriteNumber("randomCursor", RandomState.NextByteCursor);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal byte[] SerializeEvent(string eventType, string actionId, long priorVersion, byte[] priorCheckpoint,
        string? destination = null, string? actor = null, int? lane = null, int? stage = null, int? delta = null,
        bool? axisFirst = null, string? supplyId = null, bool? baselineApproved = null, IReadOnlyList<int>? announcedLanes = null)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", World.CdSeamBound ? CdSeamContractVersion : World.MapIdentityBound ? MapBoundContractVersion : ContractVersion);
            writer.WriteString("eventType", eventType);
            writer.WriteString("campaignId", Source.Request.CampaignId);
            writer.WriteNumber("priorStateVersion", priorVersion);
            writer.WriteNumber("stateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(priorCheckpoint));
            writer.WriteString("actionId", actionId);
            writer.WriteString("actor", actor ?? (destination is null ? "system" : "commonwealth"));
            if (destination is not null)
            {
                writer.WriteString("destination", destination);
            }
            if (supplyId is not null) writer.WriteString("supplyId", supplyId);
            if (announcedLanes is not null) { writer.WriteStartArray("announcedLanes"); foreach (var announcedLane in announcedLanes) writer.WriteNumberValue(announcedLane); writer.WriteEndArray(); }
            if (lane is not null) writer.WriteNumber("lane", lane.Value);
            if (stage is not null) writer.WriteNumber("operationStage", stage.Value);
            if (delta is not null) writer.WriteNumber("delta", delta.Value);
            if (axisFirst is not null) writer.WriteBoolean("axisFirst", axisFirst.Value);
            if (baselineApproved is not null) writer.WriteBoolean("baselineApproved", baselineApproved.Value);
            writer.WriteString("checkpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
}
