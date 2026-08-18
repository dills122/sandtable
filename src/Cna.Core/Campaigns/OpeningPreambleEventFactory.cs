using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Campaigns;

internal static class OpeningPreambleEventFactory
{
    private static IReadOnlyList<RuleReference> ConvoySources { get; } = Array.AsReadOnly(
    [
        Cna1979LandSequence.SourceReference,
        Cna1979SetupCatalog.OpeningPreambleSourceReference,
    ]);

    private static IReadOnlyList<RuleReference> DeclarationSources { get; } = Array.AsReadOnly(
    [
        Cna1979LandSequence.SourceReference,
        Cna1979LandSequence.InitiativeSideSourceReference,
        Cna1979LandSequence.StageChoiceSourceReference,
        new RuleReference("spi-1979-land-rules", "7.16"),
    ]);

    public static NoObligationNavalConvoyScheduleResolved CreateSchedule(CampaignSnapshot snapshot)
    {
        RequirePolicy(snapshot);
        var next = Cna1979LandSequence.GetNext(snapshot.SequencePosition);
        if (snapshot.SequencePosition.PhaseId != LandPhaseIds.NavalConvoySchedule
            || next.PhaseId != LandPhaseIds.TacticalShipping)
        {
            throw new InvalidOperationException("Naval Convoy Schedule must advance to Tactical Shipping.");
        }
        return new NoObligationNavalConvoyScheduleResolved(snapshot.CampaignId,
            checked(snapshot.StateVersion + 1), snapshot.SequencePosition.PositionId, next, ConvoySources);
    }

    public static NoObligationTacticalShippingResolved CreateTactical(CampaignSnapshot snapshot)
    {
        RequirePolicy(snapshot);
        var next = Cna1979LandSequence.GetNext(snapshot.SequencePosition);
        if (snapshot.SequencePosition.PhaseId != LandPhaseIds.TacticalShipping
            || next.PhaseId != LandPhaseIds.InitiativeDeclaration
            || next.OperationStage != 1)
        {
            throw new InvalidOperationException("Tactical Shipping must advance to Operation Stage 1 Initiative Declaration.");
        }
        return new NoObligationTacticalShippingResolved(snapshot.CampaignId,
            checked(snapshot.StateVersion + 1), snapshot.SequencePosition.PositionId, next, ConvoySources);
    }

    public static InitiativeOrderDeclared CreateDeclaration(
        CampaignSnapshot snapshot,
        DeclareInitiativeOrder command)
    {
        var holder = snapshot.InitiativeHolder
            ?? throw new InvalidOperationException("Initiative must be determined before declaration.");
        if (snapshot.SequencePosition.PhaseId != LandPhaseIds.InitiativeDeclaration
            || snapshot.SequencePosition.OperationStage != command.OperationStage
            || command.OperationStage != 1
            || command.DeclaringSide != holder
            || !Enum.IsDefined(command.Choice))
        {
            throw new InvalidOperationException("The Initiative declaration is not valid at this checkpoint.");
        }
        var opponent = holder == LandSide.Axis ? LandSide.Commonwealth : LandSide.Axis;
        var first = command.Choice == InitiativeOrderChoice.ActFirst ? holder : opponent;
        var second = first == holder ? opponent : holder;
        var next = Cna1979LandSequence.GetNext(snapshot.SequencePosition);
        if (next.PhaseId != LandPhaseIds.WeatherDetermination || next.OperationStage != 1)
        {
            throw new InvalidOperationException("Initiative Declaration must advance to Weather Determination.");
        }
        return new InitiativeOrderDeclared(snapshot.CampaignId, checked(snapshot.StateVersion + 1),
            snapshot.SequencePosition.PositionId, next, 1, holder, first, second, DeclarationSources);
    }

    private static void RequirePolicy(CampaignSnapshot snapshot)
    {
        var policy = snapshot.Setup.OpeningPreamble;
        if (policy.ContractVersion != CampaignOpeningPreamblePolicy.CurrentContractVersion
            || policy.Kind != CampaignOpeningPreambleKind.NoOpeningNavalConvoyObligations
            || !policy.Sources.SequenceEqual([Cna1979SetupCatalog.OpeningPreambleSourceReference]))
        {
            throw new InvalidOperationException("The setup does not admit no-obligation convoy resolution.");
        }
    }
}
