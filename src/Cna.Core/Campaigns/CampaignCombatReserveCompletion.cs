using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>History-derived completion codec; no standalone base/cache admission or terminal projection.</summary>
internal static class CampaignCombatReserveCompletion
{
    internal static readonly System.Collections.ObjectModel.ReadOnlyCollection<RuleReference> Sources = Array.AsReadOnly<RuleReference>(
        [new("spi-1979-land-rules", "18.11"), new("spi-1979-land-rules", "5.2.reserve-designation")]);

    public static CampaignCombatReserveCompletionInput CreateCommand(CampaignCombatCreationRequest request,
        ReadOnlySpan<byte> createdBytes, IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weatherEvents,
        IReadOnlyList<byte[]> stageEvents, IReadOnlyList<byte[]> designationEvents) => Command(
            DeriveBase(request, createdBytes, preamble, weatherEvents, stageEvents, designationEvents));

    public static CampaignCombatReserveCompletionEvidence Create(CampaignCombatCreationRequest request,
        ReadOnlySpan<byte> createdBytes, IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weatherEvents,
        IReadOnlyList<byte[]> stageEvents, IReadOnlyList<byte[]> designationEvents, CampaignCombatReserveCompletionInput input) =>
        Generate(DeriveBase(request, createdBytes, preamble, weatherEvents, stageEvents, designationEvents), input);

    public static CampaignCombatReserveCompletionEvidence ReadEvent(ReadOnlySpan<byte> bytes,
        CampaignCombatCreationRequest request, ReadOnlySpan<byte> createdBytes, IReadOnlyList<byte[]> preamble,
        IReadOnlyList<byte[]> weatherEvents, IReadOnlyList<byte[]> stageEvents, IReadOnlyList<byte[]> designationEvents)
    {
        if (bytes.Length is 0 or > 1_048_576) throw new JsonException("Missing or oversized Reserve completion event.");
        var retained = bytes.ToArray();
        var basis = DeriveBase(request, createdBytes, preamble, weatherEvents, stageEvents, designationEvents);
        var result = Generate(basis, CampaignCombatReserveCompletionCodec.ReadEventInput(retained));
        if (!retained.AsSpan().SequenceEqual(result.EventBytes))
            throw new JsonException("Completion differs from canonical creation-rooted authority.");
        return result;
    }

    private static CampaignCombatOpeningBase DeriveBase(CampaignCombatCreationRequest request, ReadOnlySpan<byte> createdBytes,
        IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weatherEvents, IReadOnlyList<byte[]> stageEvents,
        IReadOnlyList<byte[]> designationEvents)
    {
        var predecessor = CampaignCombatReserveDesignation.Replay(request, createdBytes, preamble, weatherEvents, stageEvents, designationEvents);
        return new(predecessor.Stage.Weather.Opening.Creation.CreationReceipt.Request, predecessor);
    }

    private static CampaignCombatReserveCompletionInput Command(CampaignCombatOpeningBase basis) => new(new(2,
        "complete-reserve-designation", basis.Hash, basis.Predecessor.StateVersion, basis.Predecessor.SequencePosition.PositionId),
        basis.Predecessor.FirstActingSide == LandSide.Axis ? CampaignOpeningPreambleActor.Axis : CampaignOpeningPreambleActor.Commonwealth);

    private static CampaignCombatReserveCompletionEvidence Generate(CampaignCombatOpeningBase basis, CampaignCombatReserveCompletionInput input)
    {
        _ = CampaignCombatReserveCompletionCodec.SerializeInput(input);
        var expected = Command(basis);
        if (input.Actor != expected.Actor) throw new JsonException("Only trusted first acting side may complete Reserve designation.");
        if (input != expected) throw new JsonException("Completion input differs from history-derived base and occurrence.");
        var request = basis.Request;
        var setup = request.Context.Setup;
        var authority = new CampaignCombatCycleAuthority(1, request.CampaignId, request.Context.RulesetHash,
            setup.SetupId, setup.SetupHash, setup.Artifact.Identity.PackId, setup.Artifact.Identity.Hash,
            setup.Scenario.ScenarioId, 1, 1, "first-acting-side", basis.Predecessor.FirstActingSide, 1,
            checked(basis.Predecessor.StateVersion + 1), basis.Predecessor.Prefix, request.Context.Configuration.Hash);
        var position = Cna1979LandSequence.CreateTurn(1).Single(p =>
            p.PositionId == "land.position.operation-1.first-player.movement-and-combat.movement");
        var movement = new LandSequencePosition(5, position.PositionId, position.GameTurn, position.OperationStage,
            position.StageId, position.PhaseId, position.SegmentId, position.StepId, position.ActorRole,
            position.ActiveSide, position.Sources);
        return new(new(basis, input, movement, authority));
    }
}
