using System.Text.Json.Serialization;

namespace Cna.Core.Rules;

public sealed record LandSequencePosition
{
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
        LandActorRole actorRole,
        LandSide? activeSide,
        IReadOnlyList<RuleReference> sources)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(contractVersion, 1);
        ArgumentException.ThrowIfNullOrWhiteSpace(positionId);
        ArgumentOutOfRangeException.ThrowIfLessThan(gameTurn, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(operationStage);
        ArgumentException.ThrowIfNullOrWhiteSpace(stageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(phaseId);

        if (!Enum.IsDefined(actorRole))
        {
            throw new ArgumentOutOfRangeException(nameof(actorRole));
        }

        if (activeSide is not null && !Enum.IsDefined(activeSide.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(activeSide));
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
        ActorRole = actorRole;
        ActiveSide = activeSide;
        Sources = Array.AsReadOnly(sourceCopy
            .OrderBy(source => source.SourceId, StringComparer.Ordinal)
            .ThenBy(source => source.Locator, StringComparer.Ordinal)
            .ToArray());
    }

    public int ContractVersion { get; }
    public string PositionId { get; }
    public int GameTurn { get; }
    public int OperationStage { get; }
    public string StageId { get; }
    public string PhaseId { get; }
    public string? SegmentId { get; }
    public string? StepId { get; }
    public LandActorRole ActorRole { get; }
    public LandSide? ActiveSide { get; }
    public IReadOnlyList<RuleReference> Sources { get; }

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
            && ActorRole == other.ActorRole
            && ActiveSide == other.ActiveSide
            && Sources.SequenceEqual(other.Sources));

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
        hash.Add(ActorRole);
        hash.Add(ActiveSide);

        foreach (var source in Sources)
        {
            hash.Add(source);
        }

        return hash.ToHashCode();
    }
}
