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
        History = Array.AsReadOnly(history.ToArray());
    }

    internal CampaignSnapshot Snapshot { get; }
    internal CampaignContentContext Context { get; }
    internal IReadOnlyList<CampaignEvent> History { get; }

    public override string ToString() => nameof(ExerciseSession);
}
