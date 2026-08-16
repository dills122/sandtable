using System.Text.Json.Serialization;

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
        LandSide? activeSide) : this(
            contractVersion,
            positionId,
            gameTurn,
            operationStage,
            stageId,
            phaseId,
            segmentId,
            stepId,
            [source],
            activeSide)
    {
    }

    [JsonConstructor]
    public LandSequencePosition(
        int contractVersion,
        string positionId,
        int gameTurn,
        int operationStage,
        string stageId,
        string phaseId,
        string? segmentId,
        string? stepId,
        IReadOnlyList<RuleReference> sources,
        LandSide? activeSide)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(contractVersion, 1);
        ArgumentException.ThrowIfNullOrWhiteSpace(positionId);
        ArgumentOutOfRangeException.ThrowIfLessThan(gameTurn, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(operationStage);
        ArgumentException.ThrowIfNullOrWhiteSpace(stageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(phaseId);
        ArgumentNullException.ThrowIfNull(sources);

        var sourceCopy = sources.ToArray();

        if (sourceCopy.Length == 0 || sourceCopy.Any(source => source is null))
        {
            throw new ArgumentException(
                "At least one non-null source reference is required.",
                nameof(sources));
        }

        if (sourceCopy.Distinct().Count() != sourceCopy.Length)
        {
            throw new ArgumentException(
                "Duplicate source references are not allowed.",
                nameof(sources));
        }

        ContractVersion = contractVersion;
        PositionId = positionId;
        GameTurn = gameTurn;
        OperationStage = operationStage;
        StageId = stageId;
        PhaseId = phaseId;
        SegmentId = segmentId;
        StepId = stepId;
        Sources = Array.AsReadOnly(sourceCopy
            .OrderBy(source => source.SourceId, StringComparer.Ordinal)
            .ThenBy(source => source.Locator, StringComparer.Ordinal)
            .ToArray());
        ActiveSide = activeSide;
    }

    public LandSequencePosition(
        int contractVersion,
        string positionId,
        int gameTurn,
        int operationStage,
        string stageId,
        string phaseId,
        string? segmentId,
        string? stepId,
        IEnumerable<RuleReference> sources,
        LandSide? activeSide) : this(
            contractVersion,
            positionId,
            gameTurn,
            operationStage,
            stageId,
            phaseId,
            segmentId,
            stepId,
            sources?.ToArray() ?? throw new ArgumentNullException(nameof(sources)),
            activeSide)
    {
    }

    public int ContractVersion { get; }

    public string PositionId { get; }

    public int GameTurn { get; }

    public int OperationStage { get; }

    public string StageId { get; }

    public string PhaseId { get; }

    public string? SegmentId { get; }

    public string? StepId { get; }

    public RuleReference Source => Sources[0];

    public IReadOnlyList<RuleReference> Sources { get; }

    public LandSide? ActiveSide { get; }

    public bool Equals(LandSequencePosition? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && ContractVersion == other.ContractVersion
            && string.Equals(PositionId, other.PositionId, StringComparison.Ordinal)
            && GameTurn == other.GameTurn
            && OperationStage == other.OperationStage
            && string.Equals(StageId, other.StageId, StringComparison.Ordinal)
            && string.Equals(PhaseId, other.PhaseId, StringComparison.Ordinal)
            && string.Equals(SegmentId, other.SegmentId, StringComparison.Ordinal)
            && string.Equals(StepId, other.StepId, StringComparison.Ordinal)
            && Sources.SequenceEqual(other.Sources)
            && ActiveSide == other.ActiveSide);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ContractVersion);
        hash.Add(PositionId, StringComparer.Ordinal);
        hash.Add(GameTurn);
        hash.Add(OperationStage);
        hash.Add(StageId, StringComparer.Ordinal);
        hash.Add(PhaseId, StringComparer.Ordinal);
        hash.Add(SegmentId, StringComparer.Ordinal);
        hash.Add(StepId, StringComparer.Ordinal);

        foreach (var source in Sources)
        {
            hash.Add(source);
        }

        hash.Add(ActiveSide);
        return hash.ToHashCode();
    }
}
