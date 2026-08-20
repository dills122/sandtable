using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal abstract record OpeningPreambleAdvanced : CampaignEvent
{
    protected OpeningPreambleAdvanced(string campaignId, long stateVersion, string fromPositionId,
        LandSequencePosition sequencePosition, IReadOnlyList<RuleReference> sources)
        : base(1, campaignId, stateVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fromPositionId);
        ArgumentNullException.ThrowIfNull(sequencePosition);
        ArgumentNullException.ThrowIfNull(sources);
        var copy = sources.ToArray();
        if (copy.Length == 0 || copy.Any(source => source is null) || copy.Distinct().Count() != copy.Length)
        {
            throw new ArgumentException("Sources must be non-empty, non-null, and unique.", nameof(sources));
        }
        FromPositionId = fromPositionId;
        SequencePosition = sequencePosition;
        Sources = Array.AsReadOnly(copy.OrderBy(source => source.SourceId, StringComparer.Ordinal)
            .ThenBy(source => source.Locator, StringComparer.Ordinal).ToArray());
    }

    public string FromPositionId { get; }
    public LandSequencePosition SequencePosition { get; }
    public IReadOnlyList<RuleReference> Sources { get; }
}

internal sealed record NoObligationNavalConvoyScheduleResolved : OpeningPreambleAdvanced
{
    public NoObligationNavalConvoyScheduleResolved(string campaignId, long stateVersion,
        string fromPositionId, LandSequencePosition sequencePosition, IReadOnlyList<RuleReference> sources)
        : base(campaignId, stateVersion, fromPositionId, sequencePosition, sources) { }
}

internal sealed record NoObligationTacticalShippingResolved : OpeningPreambleAdvanced
{
    public NoObligationTacticalShippingResolved(string campaignId, long stateVersion,
        string fromPositionId, LandSequencePosition sequencePosition, IReadOnlyList<RuleReference> sources)
        : base(campaignId, stateVersion, fromPositionId, sequencePosition, sources) { }
}

internal sealed record InitiativeOrderDeclared : OpeningPreambleAdvanced
{
    public InitiativeOrderDeclared(string campaignId, long stateVersion, string fromPositionId,
        LandSequencePosition sequencePosition, int operationStage, LandSide declaringHolder,
        LandSide firstSide, LandSide secondSide, IReadOnlyList<RuleReference> sources)
        : base(campaignId, stateVersion, fromPositionId, sequencePosition, sources)
    {
        _ = new CampaignOperationStageOrder(CampaignOperationStageOrder.CurrentContractVersion,
            sequencePosition.GameTurn, operationStage, firstSide, secondSide);
        if (!Enum.IsDefined(declaringHolder)) throw new ArgumentOutOfRangeException(nameof(declaringHolder));
        OperationStage = operationStage;
        DeclaringHolder = declaringHolder;
        FirstSide = firstSide;
        SecondSide = secondSide;
    }

    public int OperationStage { get; }
    public LandSide DeclaringHolder { get; }
    public LandSide FirstSide { get; }
    public LandSide SecondSide { get; }
}
