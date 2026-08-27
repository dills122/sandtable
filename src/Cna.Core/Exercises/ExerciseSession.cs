using System.Collections.Immutable;
using Cna.Core.Campaigns;

namespace Cna.Core.Exercises;

public sealed class ExerciseSession
{
    internal ExerciseSession(
        CampaignSnapshot snapshot,
        CampaignContentContext context,
        IEnumerable<CampaignEvent> history)
    {
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        Context = context ?? throw new ArgumentNullException(nameof(context));
        ArgumentNullException.ThrowIfNull(history);
        History = history as ImmutableList<CampaignEvent> ?? ImmutableList.CreateRange(history);
    }

    internal CampaignSnapshot Snapshot { get; }
    internal CampaignContentContext Context { get; }
    internal ImmutableList<CampaignEvent> History { get; }

    public override string ToString() => nameof(ExerciseSession);
}
