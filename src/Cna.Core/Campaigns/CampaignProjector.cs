using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

public static class CampaignProjector
{
    public static CampaignSnapshot Replay(IEnumerable<CampaignEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        CampaignSnapshot? snapshot = null;

        foreach (var campaignEvent in events)
        {
            snapshot = Apply(snapshot, campaignEvent);
        }

        return snapshot ?? throw new InvalidCampaignHistoryException(
            "Campaign history must contain a creation event.");
    }

    public static CampaignSnapshot Apply(
        CampaignSnapshot? snapshot,
        CampaignEvent campaignEvent)
    {
        ArgumentNullException.ThrowIfNull(campaignEvent);

        if (snapshot is not null && !CampaignSnapshotValidator.IsValid(snapshot))
        {
            throw new InvalidCampaignHistoryException(
                "The prior campaign snapshot is invalid.");
        }

        return campaignEvent switch
        {
            CampaignCreated created => ApplyCreated(snapshot, created),
            InitiativeDetermined determined => ApplyInitiativeDetermined(snapshot, determined),
            CampaignSequenceAdvanced => throw new InvalidCampaignHistoryException(
                "Legacy generic sequence events are not valid version-2 campaign history."),
            _ => throw new InvalidCampaignHistoryException("Unsupported campaign event type."),
        };
    }

    private static CampaignSnapshot ApplyCreated(
        CampaignSnapshot? snapshot,
        CampaignCreated created)
    {
        if (snapshot is not null)
        {
            throw new InvalidCampaignHistoryException(
                "A campaign can contain only one creation event.");
        }

        if (created.ContractVersion != 2
            || created.StateVersion != 1
            || string.IsNullOrWhiteSpace(created.CampaignId)
            || !Cna1979Ruleset.IsCanonicalHash(created.RulesetHash)
            || !CampaignSnapshotValidator.IsValidSetup(created.Setup)
            || created.RandomState is null
            || created.RandomState.ContractVersion != SandtableRandom.ContractVersion
            || !string.Equals(
                created.RandomState.AlgorithmId,
                SandtableRandom.AlgorithmId,
                StringComparison.Ordinal)
            || created.RandomState.NextByteCursor != 0)
        {
            throw new InvalidCampaignHistoryException("The campaign creation event is invalid.");
        }

        var expectedPosition = Cna1979LandSequence.CreateTurn(
            created.Setup.InitialGameTurn)[0];

        if (created.SequencePosition != expectedPosition)
        {
            throw new InvalidCampaignHistoryException("The campaign creation event is invalid.");
        }

        var projected = new CampaignSnapshot(
            2,
            created.CampaignId,
            created.StateVersion,
            created.RulesetHash,
            created.Setup,
            null,
            created.RandomState,
            created.SequencePosition);

        if (!CampaignSnapshotValidator.IsValid(projected))
        {
            throw new InvalidCampaignHistoryException("The campaign creation event is invalid.");
        }

        return projected;
    }

    private static CampaignSnapshot ApplyInitiativeDetermined(
        CampaignSnapshot? snapshot,
        InitiativeDetermined determined)
    {
        if (snapshot is null)
        {
            throw new InvalidCampaignHistoryException(
                "An Initiative event cannot precede campaign creation.");
        }

        InitiativeDetermined expected;

        try
        {
            expected = InitiativeEventFactory.Create(snapshot);
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException
            or InvalidOperationException)
        {
            throw new InvalidCampaignHistoryException(exception.Message);
        }

        if (determined != expected)
        {
            throw new InvalidCampaignHistoryException(
                "The Initiative event is inconsistent with campaign history.");
        }

        var projected = snapshot with
        {
            StateVersion = determined.StateVersion,
            InitiativeHolder = determined.Outcome.Holder,
            RandomState = new RandomStreamState(
                snapshot.RandomState.ContractVersion,
                snapshot.RandomState.AlgorithmId,
                snapshot.RandomState.Seed,
                determined.RandomCursorAfter),
            SequencePosition = determined.SequencePosition,
        };

        if (!CampaignSnapshotValidator.IsValid(projected))
        {
            throw new InvalidCampaignHistoryException(
                "The Initiative event produces invalid campaign state.");
        }

        return projected;
    }
}
