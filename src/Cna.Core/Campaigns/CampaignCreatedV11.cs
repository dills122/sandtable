using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>Dormant creation value, derived exclusively from a validated request.</summary>
internal sealed class CampaignCreatedV11
{
    private CampaignCreatedV11(CampaignCombatCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Request = request;
        var setup = request.Context.Setup;
        InitialWorld = CampaignWorldV7Factory.CreateInitial(setup.Artifact, setup.Scenario,
            setup.CombatInitialization, request.CreationBinding);
        // Catalog5 retains the predecessor preamble entry. No full catalog5 traversal is activated.
        var predecessor = Cna1979LandSequence.CreateTurn(setup.InitialGameTurn)[0];
        SequencePosition = new LandSequencePosition(5, predecessor.PositionId, setup.InitialGameTurn,
            predecessor.OperationStage, predecessor.StageId, predecessor.PhaseId, predecessor.SegmentId,
            predecessor.StepId, predecessor.ActorRole, predecessor.ActiveSide, predecessor.Sources);
    }

    public CampaignCombatCreationRequest Request { get; }
    public CampaignWorldSnapshotV7 InitialWorld { get; }
    public LandSequencePosition SequencePosition { get; }

    public static CampaignCreatedV11 Create(CampaignCombatCreationRequest request) => new(request);
}
