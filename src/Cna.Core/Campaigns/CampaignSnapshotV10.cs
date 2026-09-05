using Cna.Core.Content;
using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal enum CampaignPositionV10Kind
{
    Sequence,
    Reaction,
}

internal sealed record CampaignPositionV10
{
    private CampaignPositionV10(
        CampaignPositionV10Kind kind,
        LandSequencePosition? sequencePosition,
        CampaignReactingPosition? reactingPosition)
    {
        var hasSequence = sequencePosition is not null;
        var hasReaction = reactingPosition is not null;
        if (!Enum.IsDefined(kind)
            || hasSequence == hasReaction
            || (kind == CampaignPositionV10Kind.Sequence && !hasSequence)
            || (kind == CampaignPositionV10Kind.Reaction && !hasReaction))
        {
            throw new ArgumentException(
                "A Campaign v10 position must contain exactly the selected position shape.");
        }

        Kind = kind;
        SequencePosition = sequencePosition;
        ReactingPosition = reactingPosition;
    }

    public CampaignPositionV10Kind Kind { get; }

    public LandSequencePosition? SequencePosition { get; }

    public CampaignReactingPosition? ReactingPosition { get; }

    public static CampaignPositionV10 FromSequence(LandSequencePosition position)
    {
        ArgumentNullException.ThrowIfNull(position);
        return new CampaignPositionV10(CampaignPositionV10Kind.Sequence, position, null);
    }

    public static CampaignPositionV10 FromReaction(CampaignReactingPosition position)
    {
        ArgumentNullException.ThrowIfNull(position);
        return new CampaignPositionV10(CampaignPositionV10Kind.Reaction, null, position);
    }
}

internal sealed record CampaignSnapshotV10
{
    public const int CurrentContractVersion = 10;

    public CampaignSnapshotV10(
        int contractVersion,
        string campaignId,
        long stateVersion,
        string rulesetHash,
        CampaignSetupSnapshotV5 setup,
        CampaignWorldSnapshotV5 world,
        LandSide? initiativeHolder,
        IEnumerable<CampaignOperationStageOrder> operationStageOrders,
        IEnumerable<CampaignOperationStageWeather> operationStageWeather,
        RandomStreamState randomState,
        CampaignPositionV10 currentPosition,
        CampaignReactionWindow? reactionWindow)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(contractVersion, CurrentContractVersion);
        ArgumentOutOfRangeException.ThrowIfLessThan(stateVersion, 1);
        CampaignId = ContentContractGuards.RequireStableId(campaignId, nameof(campaignId));
        if (!CampaignSnapshotValidator.IsRulesHash(rulesetHash))
        {
            throw new ArgumentException(
                "A ruleset hash must contain 64 lowercase hexadecimal digits.",
                nameof(rulesetHash));
        }

        ArgumentNullException.ThrowIfNull(setup);
        ArgumentNullException.ThrowIfNull(world);
        if (initiativeHolder is not null && !Enum.IsDefined(initiativeHolder.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(initiativeHolder));
        }

        var orders = ContentContractGuards.CopyValues(
            operationStageOrders,
            nameof(operationStageOrders));
        var weather = ContentContractGuards.CopyValues(
            operationStageWeather,
            nameof(operationStageWeather));
        if (orders.Select(value => (value.GameTurn, value.OperationStage))
                .Distinct().Count() != orders.Length
            || weather.Select(value => (value.GameTurn, value.OperationStage))
                .Distinct().Count() != weather.Length)
        {
            throw new ArgumentException(
                "Campaign operation-stage state must be unique by turn and stage.");
        }

        ArgumentNullException.ThrowIfNull(randomState);
        ArgumentNullException.ThrowIfNull(currentPosition);
        CampaignSequenceV5Guards.RequireCurrentPosition(
            currentPosition.SequencePosition
            ?? currentPosition.ReactingPosition!.SuspendedMovementPosition,
            orders);
        if ((reactionWindow is null
                && currentPosition.Kind != CampaignPositionV10Kind.Sequence)
            || (reactionWindow is not null
                && (currentPosition.Kind != CampaignPositionV10Kind.Reaction
                    || currentPosition.ReactingPosition
                        != reactionWindow.ReactingPosition)))
        {
            throw new ArgumentException(
                "Campaign v10 current position must agree with the nullable Reaction window.");
        }

        ContractVersion = contractVersion;
        StateVersion = stateVersion;
        RulesetHash = rulesetHash;
        Setup = setup;
        World = world;
        InitiativeHolder = initiativeHolder;
        OperationStageOrders = Array.AsReadOnly(orders
            .OrderBy(value => value.GameTurn)
            .ThenBy(value => value.OperationStage)
            .ToArray());
        OperationStageWeather = Array.AsReadOnly(weather
            .OrderBy(value => value.GameTurn)
            .ThenBy(value => value.OperationStage)
            .ToArray());
        RandomState = randomState;
        CurrentPosition = currentPosition;
        ReactionWindow = reactionWindow;
    }

    public int ContractVersion { get; }

    public string CampaignId { get; }

    public long StateVersion { get; }

    public string RulesetHash { get; }

    public CampaignSetupSnapshotV5 Setup { get; }

    public CampaignWorldSnapshotV5 World { get; }

    public LandSide? InitiativeHolder { get; }

    public IReadOnlyList<CampaignOperationStageOrder> OperationStageOrders { get; }

    public IReadOnlyList<CampaignOperationStageWeather> OperationStageWeather { get; }

    public RandomStreamState RandomState { get; }

    public CampaignPositionV10 CurrentPosition { get; }

    public CampaignReactionWindow? ReactionWindow { get; }

    public bool Equals(CampaignSnapshotV10? other) => ReferenceEquals(this, other)
        || (other is not null
            && ContractVersion == other.ContractVersion
            && string.Equals(CampaignId, other.CampaignId, StringComparison.Ordinal)
            && StateVersion == other.StateVersion
            && string.Equals(RulesetHash, other.RulesetHash, StringComparison.Ordinal)
            && Setup == other.Setup
            && World == other.World
            && InitiativeHolder == other.InitiativeHolder
            && OperationStageOrders.SequenceEqual(other.OperationStageOrders)
            && OperationStageWeather.SequenceEqual(other.OperationStageWeather)
            && RandomState == other.RandomState
            && CurrentPosition == other.CurrentPosition
            && ReactionWindow == other.ReactionWindow);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ContractVersion);
        hash.Add(CampaignId, StringComparer.Ordinal);
        hash.Add(StateVersion);
        hash.Add(RulesetHash, StringComparer.Ordinal);
        hash.Add(Setup);
        hash.Add(World);
        hash.Add(InitiativeHolder);
        foreach (var order in OperationStageOrders) hash.Add(order);
        foreach (var weather in OperationStageWeather) hash.Add(weather);
        hash.Add(RandomState);
        hash.Add(CurrentPosition);
        hash.Add(ReactionWindow);
        return hash.ToHashCode();
    }
}

internal static class CampaignSnapshotV10Validator
{
    public static bool IsValid(
        CampaignSnapshotV10? snapshot,
        ContentPackV5Artifact artifact,
        ContentScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(scenario);
        if (snapshot is null
            || snapshot.ContractVersion != CampaignSnapshotV10.CurrentContractVersion
            || !Cna1979Ruleset.IsCanonicalHash(snapshot.RulesetHash)
            || snapshot.Setup.Content.Pack != artifact.Identity
            || !string.Equals(snapshot.Setup.Content.ScenarioId, scenario.ScenarioId,
                StringComparison.Ordinal)
            || snapshot.Setup.InitialGameTurn != scenario.Start.GameTurn
            || snapshot.Setup.StageEntry.OperationStage != scenario.Start.OperationStage
            || !CampaignWorldV5Validator.IsValid(snapshot.World, artifact, scenario)
            || snapshot.RandomState.ContractVersion != SandtableRandom.ContractVersion
            || !string.Equals(snapshot.RandomState.AlgorithmId, SandtableRandom.AlgorithmId,
                StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            CampaignSequenceV5Guards.RequireCurrentPosition(
                snapshot.CurrentPosition.SequencePosition
                ?? snapshot.CurrentPosition.ReactingPosition!.SuspendedMovementPosition,
                snapshot.OperationStageOrders);
        }
        catch (ArgumentException)
        {
            return false;
        }

        if (snapshot.ReactionWindow is null)
        {
            return snapshot.CurrentPosition.Kind == CampaignPositionV10Kind.Sequence;
        }

        try
        {
            var window = snapshot.ReactionWindow;
            window.ValidateIdentities(snapshot.CampaignId, snapshot.RulesetHash);
            if (snapshot.CurrentPosition.Kind != CampaignPositionV10Kind.Reaction
                || snapshot.CurrentPosition.ReactingPosition != window.ReactingPosition
                || window.TriggerCommittedStateVersion > snapshot.StateVersion)
            {
                return false;
            }

            var contentSides = artifact.Definition.LegacyDefinition.Elements.ToDictionary(
                value => value.ElementId,
                value => value.SideId,
                StringComparer.Ordinal);
            return HasSide(
                    window.TriggerAuthority.TriggeringRepresentation,
                    window.PhasingSide,
                    requireLocationMatch: true)
                && window.FrozenOpportunities.All(value =>
                    HasSide(
                        value.ReactingRepresentation,
                        window.ReactingSide,
                        requireLocationMatch: false));

            bool HasSide(
                CampaignMapRepresentationState representation,
                LandSide side,
                bool requireLocationMatch)
            {
                var current = snapshot.World.Representations.SingleOrDefault(value =>
                    string.Equals(value.RepresentationId, representation.RepresentationId,
                        StringComparison.Ordinal));
                return current is not null
                    && current.BindingKind == representation.BindingKind
                    && current.BoundElementIds.SequenceEqual(representation.BoundElementIds)
                    && (!requireLocationMatch
                        || string.Equals(current.CurrentLocationId,
                            representation.CurrentLocationId, StringComparison.Ordinal))
                    && representation.BoundElementIds.All(elementId =>
                    contentSides.TryGetValue(elementId, out var sideId)
                    && string.Equals(sideId, side switch
                    {
                        LandSide.Axis => "axis",
                        LandSide.Commonwealth => "commonwealth",
                        _ => throw new ArgumentOutOfRangeException(nameof(side)),
                    }, StringComparison.Ordinal));
            }
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
