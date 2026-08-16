namespace Cna.Core.Rules;

public sealed record LandSequencePosition
{
    public LandSequencePosition(
        int contractVersion,
        string positionId,
        int gameTurn,
        int operationStage,
        string stageId,
        string phaseId,
        string? segmentId,
        string? stepId,
        RuleReference source,
        LandSide? activeSide)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(contractVersion, 1);
        ArgumentException.ThrowIfNullOrWhiteSpace(positionId);
        ArgumentOutOfRangeException.ThrowIfLessThan(gameTurn, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(operationStage);
        ArgumentException.ThrowIfNullOrWhiteSpace(stageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(phaseId);
        ArgumentNullException.ThrowIfNull(source);

        ContractVersion = contractVersion;
        PositionId = positionId;
        GameTurn = gameTurn;
        OperationStage = operationStage;
        StageId = stageId;
        PhaseId = phaseId;
        SegmentId = segmentId;
        StepId = stepId;
        Source = source;
        ActiveSide = activeSide;
    }

    public int ContractVersion { get; }

    public string PositionId { get; }

    public int GameTurn { get; }

    public int OperationStage { get; }

    public string StageId { get; }

    public string PhaseId { get; }

    public string? SegmentId { get; }

    public string? StepId { get; }

    public RuleReference Source { get; }

    public LandSide? ActiveSide { get; }
}
