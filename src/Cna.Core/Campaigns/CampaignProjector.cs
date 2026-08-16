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

        return campaignEvent switch
        {
            CampaignCreated created => ApplyCreated(snapshot, created),
            CampaignSequenceAdvanced advanced => ApplyAdvanced(snapshot, advanced),
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

        if (created.ContractVersion != 1
            || created.StateVersion != 1
            || string.IsNullOrWhiteSpace(created.CampaignId)
            || string.IsNullOrWhiteSpace(created.RulesetHash)
            || !Enum.IsDefined(created.FirstPlayer))
        {
            throw new InvalidCampaignHistoryException("The campaign creation event is invalid.");
        }

        var expectedPosition = Cna1979LandSequence.CreateTurn(1, created.FirstPlayer)[0];

        if (created.SequencePosition != expectedPosition)
        {
            throw new InvalidCampaignHistoryException("The campaign creation event is invalid.");
        }

        var projected = new CampaignSnapshot(
            1,
            created.CampaignId,
            created.StateVersion,
            created.RulesetHash,
            created.Seed,
            created.FirstPlayer,
            created.SequencePosition);

        if (!CampaignSnapshotValidator.IsValid(projected))
        {
            throw new InvalidCampaignHistoryException("The campaign creation event is invalid.");
        }

        return projected;
    }

    private static CampaignSnapshot ApplyAdvanced(
        CampaignSnapshot? snapshot,
        CampaignSequenceAdvanced advanced)
    {
        if (snapshot is null)
        {
            throw new InvalidCampaignHistoryException(
                "A sequence event cannot precede campaign creation.");
        }

        LandSequencePosition expectedPosition;

        try
        {
            expectedPosition = Cna1979LandSequence.GetNext(
                snapshot.SequencePosition,
                snapshot.FirstPlayer);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidCampaignHistoryException(exception.Message);
        }

        if (advanced.ContractVersion != 1
            || !string.Equals(advanced.CampaignId, snapshot.CampaignId, StringComparison.Ordinal)
            || advanced.StateVersion != checked(snapshot.StateVersion + 1)
            || !string.Equals(
                advanced.FromPositionId,
                snapshot.SequencePosition.PositionId,
                StringComparison.Ordinal)
            || advanced.SequencePosition != expectedPosition)
        {
            throw new InvalidCampaignHistoryException(
                "The sequence event is inconsistent with campaign history.");
        }

        var projected = snapshot with
        {
            StateVersion = advanced.StateVersion,
            SequencePosition = advanced.SequencePosition,
        };

        if (!CampaignSnapshotValidator.IsValid(projected))
        {
            throw new InvalidCampaignHistoryException(
                "The sequence event produces invalid campaign state.");
        }

        return projected;
    }
}
