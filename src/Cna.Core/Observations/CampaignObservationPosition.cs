using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Observations;

public sealed record CampaignObservationPosition
{
    internal CampaignObservationPosition(
        string positionId,
        int gameTurn,
        int operationStage,
        string stageId,
        string phaseId,
        string? segmentId,
        string? stepId,
        LandActorRole actorRole,
        LandSide? activeSide,
        LandSide? initiativeHolder)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(gameTurn, 1);

        if (operationStage is < 0 or > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(operationStage));
        }

        if (!Enum.IsDefined(actorRole))
        {
            throw new ArgumentOutOfRangeException(nameof(actorRole));
        }

        if (activeSide is not null && !Enum.IsDefined(activeSide.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(activeSide));
        }

        if (initiativeHolder is not null && !Enum.IsDefined(initiativeHolder.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(initiativeHolder));
        }

        if (actorRole == LandActorRole.None && activeSide is not null)
        {
            throw new ArgumentException(
                "A position without an actor role cannot have an active side.",
                nameof(activeSide));
        }

        if (actorRole == LandActorRole.Commonwealth
            && activeSide != LandSide.Commonwealth)
        {
            throw new ArgumentException(
                "A Commonwealth actor role must resolve to the Commonwealth side.",
                nameof(activeSide));
        }

        PositionId = ContentContractGuards.RequireStableId(positionId, nameof(positionId));
        GameTurn = gameTurn;
        OperationStage = operationStage;
        StageId = ContentContractGuards.RequireStableId(stageId, nameof(stageId));
        PhaseId = ContentContractGuards.RequireStableId(phaseId, nameof(phaseId));
        SegmentId = segmentId is null
            ? null
            : ContentContractGuards.RequireStableId(segmentId, nameof(segmentId));
        StepId = stepId is null
            ? null
            : ContentContractGuards.RequireStableId(stepId, nameof(stepId));
        ActorRole = actorRole;
        ActiveSide = activeSide;
        InitiativeHolder = initiativeHolder;
    }

    public string PositionId { get; }

    public int GameTurn { get; }

    public int OperationStage { get; }

    public string StageId { get; }

    public string PhaseId { get; }

    public string? SegmentId { get; }

    public string? StepId { get; }

    public LandActorRole ActorRole { get; }

    public LandSide? ActiveSide { get; }

    public LandSide? InitiativeHolder { get; }
}
