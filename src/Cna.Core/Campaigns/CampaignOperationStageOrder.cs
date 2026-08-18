using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignOperationStageOrder
{
    public const int CurrentContractVersion = 1;

    public CampaignOperationStageOrder(int contractVersion, int operationStage,
        LandSide firstSide, LandSide secondSide)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(contractVersion, CurrentContractVersion);
        if (operationStage is < 1 or > 3) throw new ArgumentOutOfRangeException(nameof(operationStage));
        if (!Enum.IsDefined(firstSide)) throw new ArgumentOutOfRangeException(nameof(firstSide));
        if (!Enum.IsDefined(secondSide) || secondSide == firstSide)
        {
            throw new ArgumentOutOfRangeException(nameof(secondSide));
        }
        ContractVersion = contractVersion;
        OperationStage = operationStage;
        FirstSide = firstSide;
        SecondSide = secondSide;
    }

    public int ContractVersion { get; }
    public int OperationStage { get; }
    public LandSide FirstSide { get; }
    public LandSide SecondSide { get; }
}
