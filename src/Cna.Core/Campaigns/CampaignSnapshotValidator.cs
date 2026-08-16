using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal static class CampaignSnapshotValidator
{
    public static bool IsValid(CampaignSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (snapshot.ContractVersion != 1
            || snapshot.StateVersion < 1
            || string.IsNullOrWhiteSpace(snapshot.CampaignId)
            || !Cna1979Ruleset.IsCanonicalHash(snapshot.RulesetHash)
            || !Enum.IsDefined(snapshot.FirstPlayer)
            || snapshot.SequencePosition is null
            || snapshot.SequencePosition.ContractVersion != Cna1979LandSequence.ContractVersion)
        {
            return false;
        }

        IReadOnlyList<LandSequencePosition> validPositions;

        try
        {
            validPositions = Cna1979LandSequence.CreateTurn(
                snapshot.SequencePosition.GameTurn,
                snapshot.FirstPlayer);
        }
        catch (ArgumentException)
        {
            return false;
        }

        return validPositions.Contains(snapshot.SequencePosition);
    }
}
