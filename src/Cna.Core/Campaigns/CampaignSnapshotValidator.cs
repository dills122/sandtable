using Cna.Core.Content;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Campaigns;

internal static class CampaignSnapshotValidator
{
    public static bool IsValid(CampaignSnapshot snapshot, CampaignContentContext context)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(context);

        if (!IsLocallyValid(snapshot)
            || !Cna1979Ruleset.IsCanonicalHash(snapshot.RulesetHash)
            || snapshot.Setup.Content != context.Selection
            || snapshot.Setup.InitialGameTurn != context.Scenario.Start.GameTurn
            || !IsContextAuthoritativelyValid(snapshot, context))
        {
            return false;
        }

        return true;
    }

    public static bool IsLocallyValid(CampaignSnapshot snapshot)
    {
        if (snapshot.ContractVersion != CampaignSnapshot.CurrentContractVersion
            || snapshot.StateVersion < 1
            || !IsStableId(snapshot.CampaignId)
            || !IsRulesHash(snapshot.RulesetHash)
            || !IsValidSetup(snapshot.Setup)
            || !CampaignWorldValidator.IsLocallyValid(
                snapshot.World,
                snapshot.Setup.InitialGameTurn,
                snapshot.Setup.StageEntry.OperationStage)
            || !CampaignOperationStageOrderCodec.IsStructurallyValid(
                snapshot.OperationStageOrders)
            || !CampaignOperationStageWeatherCodec.IsStructurallyValid(
                snapshot.OperationStageWeather)
            || snapshot.RandomState is null
            || snapshot.RandomState.ContractVersion != SandtableRandom.ContractVersion
            || !string.Equals(snapshot.RandomState.AlgorithmId, SandtableRandom.AlgorithmId, StringComparison.Ordinal)
            || snapshot.SequencePosition is null
            || snapshot.SequencePosition.ContractVersion != Cna1979LandSequence.ContractVersion
            || (snapshot.InitiativeHolder is not null && !Enum.IsDefined(snapshot.InitiativeHolder.Value)))
        {
            return false;
        }

        return IsLocallyCheckpointValid(snapshot);
    }

    public static bool IsValidSetup(CampaignSetupSnapshot? setup)
    {
        if (setup is null
            || setup.SchemaVersion != Cna1979SetupCatalog.SchemaVersion
            || !Cna1979SetupCatalog.TryGet(setup.SetupId, out var catalogDefinition)
            || string.IsNullOrWhiteSpace(setup.SetupId)
            || string.IsNullOrWhiteSpace(setup.SetupHash)
            || setup.InitialGameTurn is < 1 or > 111
            || setup.InitialInitiative is null
            || setup.OpeningPreamble is null
            || setup.OpeningPreamble.ContractVersion
                != CampaignOpeningPreamblePolicy.CurrentContractVersion
            || setup.OpeningPreamble.Kind
                != CampaignOpeningPreambleKind.NoOpeningNavalConvoyObligations
            || setup.OpeningPreamble.Sources.Count != 1
            || setup.OpeningPreamble.Sources[0]
                != Cna1979SetupCatalog.OpeningPreambleSourceReference
            || !Cna1979SetupCatalog.IsAdmittedWeatherPolicy(setup.Weather)
            || !Cna1979SetupCatalog.IsAdmittedStageEntryPolicy(
                setup.StageEntry,
                setup.InitialGameTurn)
            || setup.Content is null
            || setup.Sources is null
            || setup.Sources.Count == 0)
        {
            return false;
        }

        try
        {
            var expectedHash = CampaignSetupHash.Calculate(
                setup.SchemaVersion,
                setup.SetupId,
                setup.IsSynthetic,
                setup.InitialGameTurn,
                setup.InitialInitiative,
                setup.OpeningPreamble,
                setup.Weather,
                setup.StageEntry,
                setup.Content,
                setup.Sources);
            return string.Equals(setup.SetupHash, expectedHash, StringComparison.Ordinal)
                && HasExactCatalogAuthority(setup, catalogDefinition);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool HasExactCatalogAuthority(
        CampaignSetupSnapshot setup,
        CampaignSetupDefinition definition) =>
        setup.SchemaVersion == definition.SchemaVersion
        && string.Equals(setup.SetupId, definition.SetupId, StringComparison.Ordinal)
        && setup.IsSynthetic == definition.IsSynthetic
        && setup.InitialGameTurn == definition.InitialGameTurn
        && setup.InitialInitiative == definition.InitialInitiative
        && setup.OpeningPreamble == definition.OpeningPreamble
        && setup.Weather == definition.Weather
        && setup.StageEntry == definition.StageEntry
        && setup.Sources.SequenceEqual(definition.Sources);

    private static bool IsContextAuthoritativelyValid(
        CampaignSnapshot snapshot,
        CampaignContentContext context)
    {
        if (!IsLocallyCheckpointValid(snapshot))
        {
            return false;
        }

        var isReserve = snapshot.SequencePosition.PhaseId
            == LandPhaseIds.ReserveDesignation;
        var isMovement = snapshot.SequencePosition.PhaseId
                == LandPhaseIds.MovementAndCombat
            && snapshot.SequencePosition.SegmentId == LandSegmentIds.Movement;

        if (!isReserve && !isMovement)
        {
            return CampaignWorldValidator.IsValidInitial(
                snapshot.World,
                context.Artifact,
                context.Scenario);
        }

        try
        {
            return CampaignWorldValidator.IsValidReserveDesignation(
                snapshot.World,
                context.Artifact,
                context.Scenario,
                FirstActingSideResolver.Resolve(snapshot));
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static bool IsLocallyCheckpointValid(CampaignSnapshot snapshot)
    {
        var positions = Cna1979LandSequence.CreateTurn(snapshot.Setup.InitialGameTurn);
        const int reserveEntryStateVersion = 10;
        var reservePosition = positions[reserveEntryStateVersion - 1];
        var movementPosition = Cna1979LandSequence.GetNext(reservePosition);
        var reserveICount = snapshot.World.Elements.Count(element =>
            element.ReserveStatus == CampaignElementReserveStatus.ReserveI);
        var containsUnsupportedStatus = snapshot.World.Elements.Any(element =>
            element.ReserveStatus == CampaignElementReserveStatus.ReserveII);

        if (containsUnsupportedStatus
            || (snapshot.StateVersion < reserveEntryStateVersion
                && (snapshot.SequencePosition
                        != positions[checked((int)snapshot.StateVersion - 1)]
                    || reserveICount != 0))
            || (snapshot.StateVersion >= reserveEntryStateVersion
                && !((snapshot.SequencePosition == reservePosition
                        && snapshot.StateVersion
                            == reserveEntryStateVersion + reserveICount)
                    || (snapshot.SequencePosition == movementPosition
                        && snapshot.StateVersion
                            == reserveEntryStateVersion + reserveICount + 1))))
        {
            return false;
        }

        if (snapshot.StateVersion >= reserveEntryStateVersion
            && (snapshot.SequencePosition.ActorRole != LandActorRole.FirstActingSide
                || snapshot.SequencePosition.ActiveSide is not null))
        {
            return false;
        }

        if (snapshot.StateVersion == 1)
        {
            return snapshot.InitiativeHolder is null
                && snapshot.OperationStageOrders.Count == 0
                && snapshot.OperationStageWeather.Count == 0
                && snapshot.RandomState.NextByteCursor == 0;
        }

        try
        {
            var resolution = InitiativeResolver.Resolve(snapshot.Setup.InitialGameTurn,
                snapshot.Setup.InitialInitiative, SandtableRandom.Create(snapshot.RandomState.Seed),
                snapshot.Setup.Sources);
            if (snapshot.InitiativeHolder != resolution.Outcome.Holder)
            {
                return false;
            }
            if (snapshot.StateVersion <= 4)
            {
                return snapshot.OperationStageOrders.Count == 0
                    && snapshot.OperationStageWeather.Count == 0
                    && snapshot.RandomState == resolution.RandomState;
            }
            if (snapshot.OperationStageOrders.Count != 1) return false;
            var order = snapshot.OperationStageOrders[0];
            if (order.ContractVersion != CampaignOperationStageOrder.CurrentContractVersion
                || order.GameTurn != snapshot.GameTurn
                || order.OperationStage != 1
                || order.FirstSide == order.SecondSide)
            {
                return false;
            }
            var holder = snapshot.InitiativeHolder.Value;
            var opponent = holder == LandSide.Axis ? LandSide.Commonwealth : LandSide.Axis;
            if (!((order.FirstSide == holder && order.SecondSide == opponent)
                || (order.FirstSide == opponent && order.SecondSide == holder)))
            {
                return false;
            }
            if (snapshot.StateVersion == 5)
            {
                return snapshot.OperationStageWeather.Count == 0
                    && snapshot.RandomState == resolution.RandomState;
            }
            if (snapshot.OperationStageWeather.Count != 1) return false;
            var weather = snapshot.OperationStageWeather[0];
            var expected = Cna1979Weather.Resolve(snapshot.GameTurn, resolution.RandomState);
            return weather.GameTurn == snapshot.GameTurn
                && weather.OperationStage == snapshot.OperationStage
                && weather.DeterminingSide == holder
                && weather.Season == expected.Season
                && weather.FirstDie == expected.FirstDie
                && weather.SecondDie == expected.SecondDie
                && weather.Kind == expected.Kind
                && weather.Scope == expected.Scope
                && weather.LocationDie == expected.LocationDie
                && weather.AffectedAreas.SequenceEqual(expected.AffectedAreas)
                && snapshot.RandomState == expected.RandomState;
        }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException or InvalidOperationException)
        {
            return false;
        }
    }

    internal static bool IsRulesHash(string? value)
    {
        if (value is null
            || value.Length != 64)
        {
            return false;
        }

        foreach (var character in value)
        {
            if (character is not (>= '0' and <= '9') and not (>= 'a' and <= 'f'))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsStableId(string? value)
    {
        try
        {
            _ = ContentContractGuards.RequireStableId(value!, nameof(value));
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
