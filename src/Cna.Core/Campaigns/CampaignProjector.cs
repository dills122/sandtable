using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal static class CampaignProjector
{
    public static CampaignSnapshot Replay(
        IEnumerable<CampaignEvent> events,
        CampaignContentContext context)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(context);
        CampaignSnapshot? snapshot = null;

        foreach (var campaignEvent in events)
        {
            snapshot = Apply(snapshot, campaignEvent, context);
        }

        return snapshot ?? throw new InvalidCampaignHistoryException(
            "Campaign history must contain a creation event.");
    }

    public static CampaignSnapshot Apply(
        CampaignSnapshot? snapshot,
        CampaignEvent campaignEvent,
        CampaignContentContext context)
    {
        ArgumentNullException.ThrowIfNull(campaignEvent);
        ArgumentNullException.ThrowIfNull(context);

        if (snapshot is not null && !CampaignSnapshotValidator.IsValid(snapshot, context))
        {
            throw new InvalidCampaignHistoryException("The prior campaign snapshot is invalid.");
        }

        return campaignEvent switch
        {
            CampaignCreated created => ApplyCreated(snapshot, created, context),
            InitiativeDetermined determined => ApplyInitiativeDetermined(snapshot, determined, context),
            NoObligationNavalConvoyScheduleResolved resolved => ApplySchedule(snapshot, resolved, context),
            NoObligationTacticalShippingResolved resolved => ApplyTactical(snapshot, resolved, context),
            InitiativeOrderDeclared declared => ApplyDeclaration(snapshot, declared, context),
            CampaignSequenceAdvanced => throw new InvalidCampaignHistoryException(
                "Legacy generic sequence events are not valid version-3 campaign history."),
            _ => throw new InvalidCampaignHistoryException("Unsupported campaign event type."),
        };
    }

    private static CampaignSnapshot ApplyCreated(
        CampaignSnapshot? snapshot,
        CampaignCreated created,
        CampaignContentContext context)
    {
        if (snapshot is not null)
        {
            throw new InvalidCampaignHistoryException("A campaign can contain only one creation event.");
        }

        if (created.ContractVersion != 4
            || created.StateVersion != 1
            || string.IsNullOrWhiteSpace(created.CampaignId)
            || !Cna1979Ruleset.IsCanonicalHash(created.RulesetHash)
            || !CampaignSnapshotValidator.IsValidSetup(created.Setup)
            || created.Setup.Content != context.Selection
            || created.InitialWorld is null
            || !CampaignWorldValidator.IsValidInitial(
                created.InitialWorld,
                context.Artifact,
                context.Scenario)
            || created.RandomState is null
            || created.RandomState.ContractVersion != SandtableRandom.ContractVersion
            || !string.Equals(created.RandomState.AlgorithmId, SandtableRandom.AlgorithmId, StringComparison.Ordinal)
            || created.RandomState.NextByteCursor != 0)
        {
            throw new InvalidCampaignHistoryException("The campaign creation event is invalid.");
        }

        var expectedPosition = Cna1979LandSequence.CreateTurn(created.Setup.InitialGameTurn)[0];

        if (created.SequencePosition != expectedPosition)
        {
            throw new InvalidCampaignHistoryException("The campaign creation event is invalid.");
        }

        var projected = new CampaignSnapshot(
            4,
            created.CampaignId,
            created.StateVersion,
            created.RulesetHash,
            created.Setup,
            created.InitialWorld,
            null,
            [],
            created.RandomState,
            created.SequencePosition);

        if (!CampaignSnapshotValidator.IsValid(projected, context))
        {
            throw new InvalidCampaignHistoryException("The campaign creation event is invalid.");
        }

        return projected;
    }

    private static CampaignSnapshot ApplyInitiativeDetermined(
        CampaignSnapshot? snapshot,
        InitiativeDetermined determined,
        CampaignContentContext context)
    {
        if (snapshot is null)
        {
            throw new InvalidCampaignHistoryException(
                "An Initiative event cannot precede campaign creation.");
        }

        InitiativeDetermined expected;

        try
        {
            expected = InitiativeEventFactory.Create(snapshot);
        }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException or InvalidOperationException)
        {
            throw new InvalidCampaignHistoryException(exception.Message);
        }

        if (determined != expected)
        {
            throw new InvalidCampaignHistoryException(
                "The Initiative event is inconsistent with campaign history.");
        }

        var projected = snapshot with
        {
            StateVersion = determined.StateVersion,
            InitiativeHolder = determined.Outcome.Holder,
            RandomState = new RandomStreamState(
                snapshot.RandomState.ContractVersion,
                snapshot.RandomState.AlgorithmId,
                snapshot.RandomState.Seed,
                determined.RandomCursorAfter),
            SequencePosition = determined.SequencePosition,
        };

        if (!CampaignSnapshotValidator.IsValid(projected, context))
        {
            throw new InvalidCampaignHistoryException(
                "The Initiative event produces invalid campaign state.");
        }

        return projected;
    }

    private static CampaignSnapshot ApplySchedule(CampaignSnapshot? snapshot,
        NoObligationNavalConvoyScheduleResolved resolved, CampaignContentContext context) =>
        ApplyExpectedAdvance(snapshot, resolved, context, OpeningPreambleEventFactory.CreateSchedule);

    private static CampaignSnapshot ApplyTactical(CampaignSnapshot? snapshot,
        NoObligationTacticalShippingResolved resolved, CampaignContentContext context) =>
        ApplyExpectedAdvance(snapshot, resolved, context, OpeningPreambleEventFactory.CreateTactical);

    private static CampaignSnapshot ApplyExpectedAdvance<TEvent>(CampaignSnapshot? snapshot,
        TEvent actual, CampaignContentContext context, Func<CampaignSnapshot, TEvent> create)
        where TEvent : OpeningPreambleAdvanced
    {
        if (snapshot is null) throw new InvalidCampaignHistoryException("A preamble event cannot precede creation.");
        TEvent expected;
        try { expected = create(snapshot); }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException or InvalidOperationException)
        { throw new InvalidCampaignHistoryException(exception.Message); }
        if (!CampaignEventSerializer.Serialize(actual).SequenceEqual(
            CampaignEventSerializer.Serialize(expected)))
            throw new InvalidCampaignHistoryException("The preamble event is inconsistent with campaign history.");
        var projected = snapshot with
        {
            StateVersion = actual.StateVersion,
            SequencePosition = actual.SequencePosition,
        };
        if (!CampaignSnapshotValidator.IsValid(projected, context))
            throw new InvalidCampaignHistoryException("The preamble event produces invalid campaign state.");
        return projected;
    }

    private static CampaignSnapshot ApplyDeclaration(CampaignSnapshot? snapshot,
        InitiativeOrderDeclared declared, CampaignContentContext context)
    {
        if (snapshot is null) throw new InvalidCampaignHistoryException("A declaration cannot precede creation.");
        var choice = declared.FirstSide == declared.DeclaringHolder
            ? InitiativeOrderChoice.ActFirst
            : InitiativeOrderChoice.ActLast;
        var command = new DeclareInitiativeOrder(snapshot.StateVersion,
            snapshot.SequencePosition.PositionId, declared.OperationStage, declared.DeclaringHolder, choice);
        InitiativeOrderDeclared expected;
        try { expected = OpeningPreambleEventFactory.CreateDeclaration(snapshot, command); }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException or InvalidOperationException)
        { throw new InvalidCampaignHistoryException(exception.Message); }
        if (!CampaignEventSerializer.Serialize(declared).SequenceEqual(
            CampaignEventSerializer.Serialize(expected)))
            throw new InvalidCampaignHistoryException("The declaration is inconsistent with campaign history.");
        var order = new CampaignOperationStageOrder(CampaignOperationStageOrder.CurrentContractVersion,
            declared.OperationStage, declared.FirstSide, declared.SecondSide);
        var projected = snapshot with
        {
            StateVersion = declared.StateVersion,
            OperationStageOrders = Array.AsReadOnly(snapshot.OperationStageOrders.Append(order)
                .OrderBy(value => value.OperationStage).ToArray()),
            SequencePosition = declared.SequencePosition,
        };
        if (!CampaignSnapshotValidator.IsValid(projected, context))
            throw new InvalidCampaignHistoryException("The declaration produces invalid campaign state.");
        return projected;
    }
}
