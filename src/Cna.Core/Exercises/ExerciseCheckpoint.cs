using Cna.Core.Actions;
using Cna.Core.Campaigns;

namespace Cna.Core.Exercises;

public sealed class ExerciseCheckpoint
{
    public const int CurrentContractVersion = 1;

    internal ExerciseCheckpoint(CampaignSnapshotV10 snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ContractVersion = CurrentContractVersion;
        CampaignId = snapshot.CampaignId;
        StateVersion = snapshot.StateVersion;
        RulesetHash = snapshot.RulesetHash;
        PositionId = CampaignCurrentActionExecution.CurrentPositionId(snapshot);
    }

    public int ContractVersion { get; }
    public string CampaignId { get; }
    public long StateVersion { get; }
    public string RulesetHash { get; }
    public string PositionId { get; }
}
