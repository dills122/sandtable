using System.Collections.ObjectModel;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal abstract record ReserveDesignationEvent : CampaignEvent
{
    protected ReserveDesignationEvent(
        string campaignId,
        long stateVersion,
        string fromPositionId,
        int gameTurn,
        int operationStage,
        LandSide actingSide,
        LandSequencePosition sequencePosition,
        IReadOnlyList<RuleReference> sources)
        : base(1, campaignId, stateVersion)
    {
        ArgumentNullException.ThrowIfNull(sequencePosition);
        ArgumentNullException.ThrowIfNull(sources);

        _ = ContentContractGuards.RequireStableId(campaignId, nameof(campaignId));
        FromPositionId = ContentContractGuards.RequireStableId(
            fromPositionId,
            nameof(fromPositionId));
        GameTurn = gameTurn;
        OperationStage = operationStage;
        ActingSide = actingSide;
        SequencePosition = sequencePosition;
        Sources = new ReadOnlyCollection<RuleReference>(sources.ToArray());
    }

    public string FromPositionId { get; }

    public int GameTurn { get; }

    public int OperationStage { get; }

    public LandSide ActingSide { get; }

    public LandSequencePosition SequencePosition { get; }

    public IReadOnlyList<RuleReference> Sources { get; }

    internal abstract void ValidateContract();

    protected void ValidateCommon(
        LandSequencePosition expectedPosition,
        IReadOnlyList<RuleReference> expectedSources)
    {
        _ = ContentContractGuards.RequireStableId(CampaignId, nameof(CampaignId));
        if (ContractVersion != 1
            || StateVersion < 11
            || GameTurn < 1
            || OperationStage != 1
            || !Enum.IsDefined(ActingSide)
            || !string.Equals(
                FromPositionId,
                ReservePosition(GameTurn).PositionId,
                StringComparison.Ordinal)
            || SequencePosition != expectedPosition
            || Sources.Any(source => source is null)
            || !Sources.SequenceEqual(expectedSources))
        {
            throw new ArgumentException(
                "The Reserve designation event authority binding is invalid.");
        }
    }

    internal static LandSequencePosition ReservePosition(int gameTurn) =>
        Cna1979LandSequence.CreateTurn(gameTurn).Single(value =>
            value.OperationStage == 1
            && value.PhaseId == LandPhaseIds.ReserveDesignation
            && value.SegmentId is null
            && value.ActorRole == LandActorRole.FirstActingSide);

    public virtual bool Equals(ReserveDesignationEvent? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && EqualityContract == other.EqualityContract
            && ContractVersion == other.ContractVersion
            && string.Equals(CampaignId, other.CampaignId, StringComparison.Ordinal)
            && StateVersion == other.StateVersion
            && string.Equals(FromPositionId, other.FromPositionId, StringComparison.Ordinal)
            && GameTurn == other.GameTurn
            && OperationStage == other.OperationStage
            && ActingSide == other.ActingSide
            && SequencePosition == other.SequencePosition
            && Sources.SequenceEqual(other.Sources));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(EqualityContract);
        hash.Add(ContractVersion);
        hash.Add(CampaignId, StringComparer.Ordinal);
        hash.Add(StateVersion);
        hash.Add(FromPositionId, StringComparer.Ordinal);
        hash.Add(GameTurn);
        hash.Add(OperationStage);
        hash.Add(ActingSide);
        hash.Add(SequencePosition);
        foreach (var source in Sources) hash.Add(source);
        return hash.ToHashCode();
    }
}

internal sealed record ReserveElementDesignated : ReserveDesignationEvent
{
    internal static IReadOnlyList<RuleReference> RequiredSources { get; } =
        Array.AsReadOnly<RuleReference>(
        [
            new("spi-1979-land-rules", "18.11"),
            new("spi-1979-land-rules", "18.12"),
            new("spi-1979-land-rules", "18.15"),
        ]);

    public ReserveElementDesignated(
        string campaignId,
        long stateVersion,
        string fromPositionId,
        int gameTurn,
        int operationStage,
        LandSide actingSide,
        string elementId,
        CampaignElementReserveStatus priorStatus,
        CampaignElementReserveStatus resultingStatus,
        LandSequencePosition sequencePosition,
        IReadOnlyList<RuleReference> sources)
        : base(campaignId, stateVersion, fromPositionId, gameTurn, operationStage,
            actingSide, sequencePosition, sources)
    {
        ElementId = ContentContractGuards.RequireStableId(elementId, nameof(elementId));
        PriorStatus = priorStatus;
        ResultingStatus = resultingStatus;
        ValidateContract();
    }

    public string ElementId { get; }

    public CampaignElementReserveStatus PriorStatus { get; }

    public CampaignElementReserveStatus ResultingStatus { get; }

    internal override void ValidateContract()
    {
        ValidateCommon(ReservePosition(GameTurn), RequiredSources);
        _ = ContentContractGuards.RequireStableId(ElementId, nameof(ElementId));
        if (PriorStatus != CampaignElementReserveStatus.None
            || ResultingStatus != CampaignElementReserveStatus.ReserveI)
        {
            throw new ArgumentException(
                "A designation event must record exactly None to Reserve I.");
        }
    }
}

internal sealed record ReserveDesignationCompleted : ReserveDesignationEvent
{
    internal static IReadOnlyList<RuleReference> RequiredSources { get; } =
        Array.AsReadOnly(Cna1979Reserve.CreateEmptySelectionRuling().Sources.ToArray());

    public ReserveDesignationCompleted(
        string campaignId,
        long stateVersion,
        string fromPositionId,
        int gameTurn,
        int operationStage,
        LandSide actingSide,
        LandSequencePosition sequencePosition,
        IReadOnlyList<RuleReference> sources)
        : base(campaignId, stateVersion, fromPositionId, gameTurn, operationStage,
            actingSide, sequencePosition, sources)
    {
        ValidateContract();
    }

    internal override void ValidateContract()
    {
        var reserve = ReservePosition(GameTurn);
        var movement = Cna1979LandSequence.GetNext(reserve);
        if (movement.GameTurn != GameTurn
            || movement.OperationStage != OperationStage
            || movement.PhaseId != LandPhaseIds.MovementAndCombat
            || movement.SegmentId != LandSegmentIds.Movement
            || movement.ActorRole != LandActorRole.FirstActingSide
            || movement.ActiveSide is not null)
        {
            throw new ArgumentException(
                "Reserve completion must advance to first-side Movement.");
        }

        ValidateCommon(movement, RequiredSources);
    }
}

internal static class CampaignReserveEventFactory
{
    public static ReserveElementDesignated CreateDesignation(
        CampaignSnapshot snapshot,
        CampaignContentContext context,
        DesignateReserveElement command)
    {
        ValidateAuthority(snapshot, context, command.ContractVersion,
            command.ExpectedStateVersion, command.ExpectedPositionId, command.ActingSide);
        _ = ContentContractGuards.RequireStableId(command.ElementId, nameof(command.ElementId));

        var content = context.Artifact.Definition.Elements.Where(value => string.Equals(
            value.ElementId,
            command.ElementId,
            StringComparison.Ordinal)).ToArray();
        var world = snapshot.World.Elements.Where(value => string.Equals(
            value.ElementId,
            command.ElementId,
            StringComparison.Ordinal)).ToArray();
        var placements = context.Scenario.InitialPlacements.Where(value => string.Equals(
            value.ElementId,
            command.ElementId,
            StringComparison.Ordinal)).ToArray();

        if (content.Length != 1
            || content[0].PlacementMode != ContentPlacementMode.Independent
            || !string.Equals(content[0].SideId, SideId(command.ActingSide),
                StringComparison.Ordinal)
            || world.Length != 1
            || placements.Length != 1
            || !string.Equals(world[0].CurrentLocationId, placements[0].LocationId,
                StringComparison.Ordinal)
            || world[0].ReserveStatus != CampaignElementReserveStatus.None)
        {
            throw new InvalidOperationException(
                "The element is not eligible for Reserve designation.");
        }

        return new ReserveElementDesignated(
            snapshot.CampaignId,
            checked(snapshot.StateVersion + 1),
            snapshot.SequencePosition.PositionId,
            snapshot.GameTurn,
            snapshot.OperationStage,
            command.ActingSide,
            command.ElementId,
            CampaignElementReserveStatus.None,
            CampaignElementReserveStatus.ReserveI,
            snapshot.SequencePosition,
            ReserveElementDesignated.RequiredSources);
    }

    public static ReserveDesignationCompleted CreateCompletion(
        CampaignSnapshot snapshot,
        CampaignContentContext context,
        CompleteReserveDesignation command)
    {
        ValidateAuthority(snapshot, context, command.ContractVersion,
            command.ExpectedStateVersion, command.ExpectedPositionId, command.ActingSide);
        var successor = Cna1979LandSequence.GetNext(snapshot.SequencePosition);

        return new ReserveDesignationCompleted(
            snapshot.CampaignId,
            checked(snapshot.StateVersion + 1),
            snapshot.SequencePosition.PositionId,
            snapshot.GameTurn,
            snapshot.OperationStage,
            command.ActingSide,
            successor,
            ReserveDesignationCompleted.RequiredSources);
    }

    private static void ValidateAuthority(
        CampaignSnapshot snapshot,
        CampaignContentContext context,
        int contractVersion,
        long expectedStateVersion,
        string expectedPositionId,
        LandSide actingSide)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(context);
        if (!CampaignSnapshotValidator.IsValid(snapshot, context)
            || contractVersion != 1
            || expectedStateVersion != snapshot.StateVersion
            || !string.Equals(expectedPositionId, snapshot.SequencePosition.PositionId,
                StringComparison.Ordinal)
            || snapshot.SequencePosition != ReserveDesignationEvent.ReservePosition(
                snapshot.GameTurn)
            || !Enum.IsDefined(actingSide)
            || FirstActingSideResolver.Resolve(snapshot) != actingSide)
        {
            throw new InvalidOperationException(
                "Reserve designation authority is not admitted.");
        }
    }

    private static string SideId(LandSide side) => side switch
    {
        LandSide.Axis => "axis",
        LandSide.Commonwealth => "commonwealth",
        _ => throw new ArgumentOutOfRangeException(nameof(side)),
    };
}
