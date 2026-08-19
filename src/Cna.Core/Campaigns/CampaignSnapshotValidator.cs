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
            || !CampaignWorldValidator.IsValidInitial(snapshot.World, context.Artifact, context.Scenario))
        {
            return false;
        }

        return IsCheckpointValid(snapshot);
    }

    public static bool IsLocallyValid(CampaignSnapshot snapshot)
    {
        if (snapshot.ContractVersion != 5
            || snapshot.StateVersion is < 1 or > 6
            || !IsStableId(snapshot.CampaignId)
            || !IsRulesHash(snapshot.RulesetHash)
            || !IsValidSetup(snapshot.Setup)
            || snapshot.World is null
            || snapshot.World.ContractVersion != CampaignWorldSnapshot.CurrentContractVersion
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

        return IsCheckpointValid(snapshot);
    }

    public static bool IsValidSetup(CampaignSetupSnapshot? setup)
    {
        if (setup is null
            || setup.SchemaVersion != Cna1979SetupCatalog.SchemaVersion
            || setup.SetupId is not ("rules-lab.initiative.predetermined"
                or "rules-lab.initiative.contested")
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
                setup.Content,
                setup.Sources);
            return string.Equals(setup.SetupHash, expectedHash, StringComparison.Ordinal);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool IsCheckpointValid(CampaignSnapshot snapshot)
    {
        var positions = Cna1979LandSequence.CreateTurn(snapshot.Setup.InitialGameTurn);
        if (snapshot.StateVersion is < 1 or > 6
            || snapshot.SequencePosition != positions[checked((int)snapshot.StateVersion - 1)])
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
