using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

public sealed record CampaignSnapshot(
    int ContractVersion,
    string CampaignId,
    long StateVersion,
    string RulesetHash,
    CampaignSetupSnapshot Setup,
    LandSide? InitiativeHolder,
    RandomStreamState RandomState,
    LandSequencePosition SequencePosition)
{
    public int GameTurn => SequencePosition.GameTurn;
    public int OperationStage => SequencePosition.OperationStage;
    public LandSide? ActiveSide => SequencePosition.ActiveSide;
    public string PhaseId => SequencePosition.PhaseId;
    public string? SegmentId => SequencePosition.SegmentId;
}
