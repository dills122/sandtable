using System.Collections.Immutable;
using Cna.Core.Campaigns;

namespace Cna.Core.Exercises;

public sealed class ExerciseSession
{
    internal ExerciseSession(
        CampaignSnapshot snapshot,
        CampaignContentContext context,
        IEnumerable<CampaignEvent> history)
        : this(
            CampaignV10LegacyBridge.FromLegacySnapshot(snapshot, context),
            context,
            ConvertHistory(history, context),
            history)
    {
    }

    internal ExerciseSession(
        CampaignSnapshotV10 snapshot,
        CampaignContentContext context,
        IEnumerable<object> history)
        : this(snapshot, context, history, history.OfType<CampaignEvent>())
    {
    }

    internal ExerciseSession(
        CampaignSnapshotV10 snapshot,
        CampaignContentContext context,
        IEnumerable<object> history,
        IEnumerable<CampaignEvent> legacyHistory,
        CampaignSnapshot? legacySnapshot = null)
    {
        CurrentSnapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        Context = context ?? throw new ArgumentNullException(nameof(context));
        ArgumentNullException.ThrowIfNull(history);
        CurrentHistory = history as ImmutableList<object> ?? ImmutableList.CreateRange(history);
        ArgumentNullException.ThrowIfNull(legacyHistory);
        History = legacyHistory as ImmutableList<CampaignEvent>
            ?? ImmutableList.CreateRange(legacyHistory);
        Snapshot = legacySnapshot ?? CampaignV10LegacyBridge.ToLegacy(snapshot, context);
    }

    internal CampaignSnapshot Snapshot { get; }
    internal CampaignSnapshotV10 CurrentSnapshot { get; }
    internal CampaignContentContext Context { get; }
    internal ImmutableList<CampaignEvent> History { get; }
    internal ImmutableList<object> CurrentHistory { get; }

    private static IEnumerable<object> ConvertHistory(
        IEnumerable<CampaignEvent> history,
        CampaignContentContext context)
    {
        ArgumentNullException.ThrowIfNull(history);
        var artifact = context.ArtifactV5
            ?? throw new InvalidOperationException("A current campaign requires Content Pack v5.");
        foreach (var campaignEvent in history)
        {
            if (campaignEvent is CampaignCreated created)
            {
                yield return CampaignCreationV9Factory.Create(
                    created.CampaignId,
                    created.RulesetHash,
                    created.Setup,
                    artifact,
                    context.Scenario,
                    created.RandomState,
                    created.SequencePosition);
                continue;
            }

            if (campaignEvent is ElementMoved)
            {
                throw new InvalidOperationException(
                    "Legacy ElementMoved v1 cannot become current history.");
            }

            yield return campaignEvent;
        }
    }

    public override string ToString() => nameof(ExerciseSession);
}
