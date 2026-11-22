using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignOperationStageOrder
{
    public const int CurrentContractVersion = 2;

    public CampaignOperationStageOrder(
        int contractVersion,
        int gameTurn,
        int operationStage,
        LandSide firstSide,
        LandSide secondSide)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(contractVersion, CurrentContractVersion);
        if (gameTurn is < 1 or > 111) throw new ArgumentOutOfRangeException(nameof(gameTurn));
        if (operationStage is < 1 or > 3) throw new ArgumentOutOfRangeException(nameof(operationStage));
        if (!Enum.IsDefined(firstSide)) throw new ArgumentOutOfRangeException(nameof(firstSide));
        if (!Enum.IsDefined(secondSide) || secondSide == firstSide)
        {
            throw new ArgumentOutOfRangeException(nameof(secondSide));
        }
        ContractVersion = contractVersion;
        GameTurn = gameTurn;
        OperationStage = operationStage;
        FirstSide = firstSide;
        SecondSide = secondSide;
    }

    public int ContractVersion { get; }
    public int GameTurn { get; }
    public int OperationStage { get; }
    public LandSide FirstSide { get; }
    public LandSide SecondSide { get; }

    // MTS-A (2026-11-22, docs/work/plans/2026-11-22-multi-turn-multi-stage-scoping.md):
    // added so the native "Second*" decision-point classes (V43-V46) can resolve
    // which stage-relative role a given side holds generically instead of
    // hardcoding LandActorRole.SecondActingSide. Returns null when the side is
    // neither of this order's two acting sides.
    internal LandActorRole? ActingRole(LandSide side) =>
        side == FirstSide ? LandActorRole.FirstActingSide
        : side == SecondSide ? LandActorRole.SecondActingSide
        : null;
}
