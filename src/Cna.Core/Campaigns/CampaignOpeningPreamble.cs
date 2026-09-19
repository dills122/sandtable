using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>Four dormant creation-rooted edges. Weather and later families require their own replay.</summary>
internal static class CampaignOpeningPreamble
{
    public static CampaignOpeningPreambleState Replay(CampaignCombatCreationRequest request,
        ReadOnlySpan<byte> createdBytes, IReadOnlyList<byte[]> events)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(events);
        if (events.Count > 4) throw new JsonException("Opening preamble admits at most four events.");
        // This packet's independently certified registry context is fixed, unlike the broader C2 codec.
        if (request.Context.Setup.SetupHash != "sha256:22495e26528c2f4db8335d3b4041194aef26295f30aab0e4e6f384ff17069848" ||
            request.Context.Configuration.Hash != "sha256:3de30c451ba7e81d4fdde6493e07d4d06110209a89035d9e59bba738f0aeba34")
            throw new JsonException("Opening preamble requires the frozen first-turn Axis creation context.");
        var creation = CampaignCreationSnapshotV12.Create(createdBytes, request);
        var state = new CampaignOpeningPreambleState(creation, 1, creation.ChroniclePrefix,
            creation.SequencePosition, null, [], [], []);
        foreach (var retained in events)
        {
            if (retained is null || retained.Length is 0 or > 1_048_576)
                throw new JsonException("Opening event is missing or exceeds byte limit.");
            var bytes = retained.ToArray();
            var input = CampaignOpeningPreambleCodec.ReadEventInput(bytes);
            var (after, emitted) = Emit(state, input);
            if (!bytes.AsSpan().SequenceEqual(CampaignOpeningPreambleCodec.SerializeEvent(emitted)))
                throw new JsonException("Opening event differs from replay-derived canonical authority.");
            state = after;
        }
        return state;
    }

    public static CampaignOpeningPreambleResult Apply(CampaignCombatCreationRequest request,
        ReadOnlySpan<byte> createdBytes, IReadOnlyList<byte[]> events, CampaignOpeningPreambleInput input)
    {
        var state = Replay(request, createdBytes, events);
        Authorize(state, input); // Authorization precedes receipt lookup, including terminal retries.
        foreach (var accepted in state.Events)
        {
            if (accepted.Input.Command.ExpectedPriorVersion != input.Command.ExpectedPriorVersion) continue;
            if (accepted.Input != input) throw new JsonException("Conflicting opening command retry.");
            return new CampaignOpeningPreambleResult(state, CampaignOpeningPreambleCodec.SerializeEvent(accepted), true);
        }
        var (after, emitted) = Emit(state, input);
        return new CampaignOpeningPreambleResult(after, CampaignOpeningPreambleCodec.SerializeEvent(emitted), false);
    }

    public static CampaignOpeningPreambleState ReadState(ReadOnlySpan<byte> bytes,
        CampaignCombatCreationRequest request, ReadOnlySpan<byte> createdBytes, IReadOnlyList<byte[]> events)
    {
        if (bytes.Length > 1_048_576) throw new JsonException("Opening state exceeds byte limit.");
        var expected = Replay(request, createdBytes, events);
        if (!bytes.SequenceEqual(CampaignOpeningPreambleCodec.SerializeState(expected)))
            throw new JsonException("Opening projection differs from separately supplied creation and history.");
        return expected;
    }

    public static CampaignOpeningPreambleInput Command(CampaignOpeningPreambleState state,
        InitiativeOrderChoice? choice = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.StateVersion is < 1 or > 4) throw new JsonException("No opening successor at this cut.");
        var kind = (CampaignOpeningCommandKind)(state.StateVersion - 1);
        var declaration = kind == CampaignOpeningCommandKind.InitiativeOrder;
        return new CampaignOpeningPreambleInput(new CampaignOpeningPreambleCommand(
            kind == CampaignOpeningCommandKind.Initiative ? 3 : 2, kind,
            state.Creation.CreationReceipt.CreationBinding, state.Creation.CreationReceipt.CreationEventHash,
            state.StateVersion, state.SequencePosition.PositionId, declaration ? 1 : null,
            declaration ? state.InitiativeHolder : null, choice),
            declaration ? CampaignOpeningPreambleActor.Axis : CampaignOpeningPreambleActor.System);
    }

    private static void Authorize(CampaignOpeningPreambleState state, CampaignOpeningPreambleInput input)
    {
        _ = CampaignOpeningPreambleCodec.SerializeInput(input);
        var declaration = input.Command.Kind == CampaignOpeningCommandKind.InitiativeOrder;
        if (input.Actor != (declaration ? CampaignOpeningPreambleActor.Axis : CampaignOpeningPreambleActor.System) ||
            (declaration && state.InitiativeHolder != LandSide.Axis))
            throw new JsonException("Opening command actor does not own this transition.");
    }

    private static (CampaignOpeningPreambleState State, CampaignOpeningPreambleEvent Event) Emit(
        CampaignOpeningPreambleState state, CampaignOpeningPreambleInput input)
    {
        Authorize(state, input);
        if (state.StateVersion is < 1 or > 4 || state.SequencePosition != Position((int)state.StateVersion - 1) ||
            input != Command(state, input.Command.Choice))
            throw new JsonException("Opening command does not match current causal occurrence.");
        if (state.StateVersion == 4 ? input.Command.Choice is not (InitiativeOrderChoice.ActFirst or InitiativeOrderChoice.ActLast)
                : input.Command.Choice is not null)
            throw new JsonException("Unsupported opening command choice.");
        var next = Position((int)state.StateVersion);
        CampaignOpeningPreambleEvent emitted = input.Command.Kind switch
        {
            CampaignOpeningCommandKind.Initiative => new CampaignOpeningInitiativeEvent(state, input, next,
                Sources(state.Creation.Setup.Sources.Concat(Land("7.12", "7.15"))), LandSide.Axis),
            CampaignOpeningCommandKind.ConvoySchedule or CampaignOpeningCommandKind.TacticalShipping =>
                new CampaignOpeningAdvanceEvent(state, input, next,
                    Sources(state.Creation.Setup.OpeningPreamble.Sources.Concat(Land("5.2")))),
            CampaignOpeningCommandKind.InitiativeOrder => new CampaignOpeningOrderEvent(state, input, next,
                Sources(Land("5.2", "7.11", "7.14", "7.16")), LandSide.Axis,
                input.Command.Choice == InitiativeOrderChoice.ActFirst ? LandSide.Axis : LandSide.Commonwealth,
                input.Command.Choice == InitiativeOrderChoice.ActFirst ? LandSide.Commonwealth : LandSide.Axis),
            _ => throw new JsonException("Unsupported opening event kind."),
        };
        var eventBytes = CampaignOpeningPreambleCodec.SerializeEvent(emitted);
        var receipt = new CampaignOpeningPreambleReceipt(
            CampaignOpeningPreambleCodec.Hash(CampaignOpeningPreambleCodec.SerializeInput(input)),
            CampaignOpeningPreambleCodec.Hash(eventBytes), emitted.ReceiptId, input.Actor, emitted.StateVersion);
        var orders = emitted is CampaignOpeningOrderEvent order
            ? state.Orders.Append(new CampaignOpeningPreambleOrder(1, 1, order.FirstSide, order.SecondSide))
            : state.Orders;
        return (new CampaignOpeningPreambleState(state.Creation, emitted.StateVersion,
            CampaignOpeningPreambleCodec.EventPrefix(state.Prefix, eventBytes), next, LandSide.Axis,
            orders, state.Receipts.Append(receipt), state.Events.Append(emitted)), emitted);
    }

    private static LandSequencePosition Position(int index)
    {
        // The first five entries of catalog5 retain the predecessor opening positions and sources.
        var position = Cna1979LandSequence.CreateTurn(1)[index];
        return new LandSequencePosition(5, position.PositionId, position.GameTurn, position.OperationStage,
            position.StageId, position.PhaseId, position.SegmentId, position.StepId,
            position.ActorRole, null, position.Sources);
    }
    private static IEnumerable<RuleReference> Land(params string[] locators) =>
        locators.Select(locator => new RuleReference("spi-1979-land-rules", locator));
    private static System.Collections.ObjectModel.ReadOnlyCollection<RuleReference> Sources(IEnumerable<RuleReference> sources) =>
        Array.AsReadOnly(sources.OrderBy(value => value.SourceId, StringComparer.Ordinal)
            .ThenBy(value => value.Locator, StringComparer.Ordinal).ToArray());
}
