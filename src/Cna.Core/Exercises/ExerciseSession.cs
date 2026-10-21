using System.Collections.Immutable;
using Cna.Core.Campaigns;
using Cna.Core.Visibility;

namespace Cna.Core.Exercises;

public sealed class ExerciseSession
{
    internal ExerciseSession(CampaignSnapshotV11 snapshot, CampaignContentContext context,
        IEnumerable<object> history, CampaignContactJournal? contacts = null)
    {
        CurrentSnapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        Context = context ?? throw new ArgumentNullException(nameof(context));
        ArgumentNullException.ThrowIfNull(history);
        CurrentHistory = history as ImmutableList<object> ?? ImmutableList.CreateRange(history);
        CurrentContacts = contacts ?? CampaignContactJournal.Empty(snapshot.CampaignId);
    }
    internal CampaignSnapshotV11 CurrentSnapshot { get; }
    internal CampaignContentContext Context { get; }
    internal ImmutableList<object> CurrentHistory { get; }
    internal CampaignContactJournal CurrentContacts { get; }
    public override string ToString() => nameof(ExerciseSession);
}
