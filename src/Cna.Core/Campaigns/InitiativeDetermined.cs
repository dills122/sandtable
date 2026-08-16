using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

public sealed record InitiativeDetermined : CampaignEvent
{
    public InitiativeDetermined(
        string campaignId,
        long stateVersion,
        string fromPositionId,
        InitiativeOutcome outcome,
        string randomAlgorithmId,
        ulong randomCursorBefore,
        ulong randomCursorAfter,
        LandSequencePosition sequencePosition,
        IReadOnlyList<RuleReference> sources) : base(2, campaignId, stateVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(campaignId);
        ArgumentOutOfRangeException.ThrowIfLessThan(stateVersion, 1);
        ArgumentException.ThrowIfNullOrWhiteSpace(fromPositionId);
        ArgumentNullException.ThrowIfNull(outcome);
        ArgumentException.ThrowIfNullOrWhiteSpace(randomAlgorithmId);

        ArgumentOutOfRangeException.ThrowIfLessThan(
            randomCursorAfter,
            randomCursorBefore);

        ArgumentNullException.ThrowIfNull(sequencePosition);
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

        FromPositionId = fromPositionId;
        Outcome = outcome;
        RandomAlgorithmId = randomAlgorithmId;
        RandomCursorBefore = randomCursorBefore;
        RandomCursorAfter = randomCursorAfter;
        SequencePosition = sequencePosition;
        Sources = Array.AsReadOnly(sourceCopy
            .OrderBy(source => source.SourceId, StringComparer.Ordinal)
            .ThenBy(source => source.Locator, StringComparer.Ordinal)
            .ToArray());
    }

    public string FromPositionId { get; }
    public InitiativeOutcome Outcome { get; }
    public string RandomAlgorithmId { get; }
    public ulong RandomCursorBefore { get; }
    public ulong RandomCursorAfter { get; }
    public LandSequencePosition SequencePosition { get; }
    public IReadOnlyList<RuleReference> Sources { get; }

    public bool Equals(InitiativeDetermined? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && ContractVersion == other.ContractVersion
            && string.Equals(CampaignId, other.CampaignId, StringComparison.Ordinal)
            && StateVersion == other.StateVersion
            && string.Equals(FromPositionId, other.FromPositionId, StringComparison.Ordinal)
            && Outcome == other.Outcome
            && string.Equals(
                RandomAlgorithmId,
                other.RandomAlgorithmId,
                StringComparison.Ordinal)
            && RandomCursorBefore == other.RandomCursorBefore
            && RandomCursorAfter == other.RandomCursorAfter
            && SequencePosition == other.SequencePosition
            && Sources.SequenceEqual(other.Sources));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ContractVersion);
        hash.Add(CampaignId, StringComparer.Ordinal);
        hash.Add(StateVersion);
        hash.Add(FromPositionId, StringComparer.Ordinal);
        hash.Add(Outcome);
        hash.Add(RandomAlgorithmId, StringComparer.Ordinal);
        hash.Add(RandomCursorBefore);
        hash.Add(RandomCursorAfter);
        hash.Add(SequencePosition);

        foreach (var source in Sources)
        {
            hash.Add(source);
        }

        return hash.ToHashCode();
    }
}
