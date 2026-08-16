using System.Collections.ObjectModel;
using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

public sealed record InitiativeResolution
{
    public InitiativeResolution(
        InitiativeOutcome outcome,
        RandomStreamState randomState,
        IReadOnlyList<RuleReference> sources)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        ArgumentNullException.ThrowIfNull(randomState);
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

        Outcome = outcome;
        RandomState = randomState;
        Sources = Array.AsReadOnly(sourceCopy
            .OrderBy(source => source.SourceId, StringComparer.Ordinal)
            .ThenBy(source => source.Locator, StringComparer.Ordinal)
            .ToArray());
    }

    public InitiativeOutcome Outcome { get; }
    public RandomStreamState RandomState { get; }
    public IReadOnlyList<RuleReference> Sources { get; }

    public bool Equals(InitiativeResolution? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && Outcome == other.Outcome
            && RandomState == other.RandomState
            && Sources.SequenceEqual(other.Sources));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Outcome);
        hash.Add(RandomState);

        foreach (var source in Sources)
        {
            hash.Add(source);
        }

        return hash.ToHashCode();
    }
}
