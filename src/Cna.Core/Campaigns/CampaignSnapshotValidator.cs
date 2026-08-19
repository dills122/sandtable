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
        if (snapshot.ContractVersion != 4
            || snapshot.StateVersion is < 1 or > 5
            || !IsStableId(snapshot.CampaignId)
            || !IsRulesHash(snapshot.RulesetHash)
            || !IsValidSetup(snapshot.Setup)
            || snapshot.World is null
            || snapshot.World.ContractVersion != CampaignWorldSnapshot.CurrentContractVersion
            || snapshot.OperationStageOrders is null
            || snapshot.OperationStageOrders.Any(order => order is null)
            || snapshot.OperationStageOrders.Select(order => order.OperationStage).Distinct().Count()
                != snapshot.OperationStageOrders.Count
            || !snapshot.OperationStageOrders.SequenceEqual(
                snapshot.OperationStageOrders.OrderBy(order => order.OperationStage))
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
        if (snapshot.StateVersion is < 1 or > 5
            || snapshot.SequencePosition != positions[checked((int)snapshot.StateVersion - 1)])
        {
            return false;
        }

        if (snapshot.StateVersion == 1)
        {
            return snapshot.InitiativeHolder is null
                && snapshot.OperationStageOrders.Count == 0
                && snapshot.RandomState.NextByteCursor == 0;
        }

        try
        {
            var resolution = InitiativeResolver.Resolve(snapshot.Setup.InitialGameTurn,
                snapshot.Setup.InitialInitiative, SandtableRandom.Create(snapshot.RandomState.Seed),
                snapshot.Setup.Sources);
            if (snapshot.InitiativeHolder != resolution.Outcome.Holder
                || snapshot.RandomState != resolution.RandomState)
            {
                return false;
            }
            if (snapshot.StateVersion <= 4) return snapshot.OperationStageOrders.Count == 0;
            if (snapshot.OperationStageOrders.Count != 1) return false;
            var order = snapshot.OperationStageOrders[0];
            if (order.ContractVersion != CampaignOperationStageOrder.CurrentContractVersion
                || order.OperationStage != 1 || order.FirstSide == order.SecondSide)
            {
                return false;
            }
            var holder = snapshot.InitiativeHolder.Value;
            var opponent = holder == LandSide.Axis ? LandSide.Commonwealth : LandSide.Axis;
            return (order.FirstSide == holder && order.SecondSide == opponent)
                || (order.FirstSide == opponent && order.SecondSide == holder);
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
