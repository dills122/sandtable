using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignElementOperationalState
{
    public CampaignElementOperationalState(
        int ledgerGameTurn,
        int ledgerOperationStage,
        CapabilityPointAmount capabilityPointsExpended,
        int cohesionLevel)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(ledgerGameTurn, 1);
        if (ledgerOperationStage is < 1 or > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(ledgerOperationStage));
        }

        ArgumentNullException.ThrowIfNull(capabilityPointsExpended);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(cohesionLevel, 10);

        LedgerGameTurn = ledgerGameTurn;
        LedgerOperationStage = ledgerOperationStage;
        CapabilityPointsExpended = capabilityPointsExpended;
        CohesionLevel = cohesionLevel;
    }

    public int LedgerGameTurn { get; }

    public int LedgerOperationStage { get; }

    public CapabilityPointAmount CapabilityPointsExpended { get; }

    public int CohesionLevel { get; }
}
