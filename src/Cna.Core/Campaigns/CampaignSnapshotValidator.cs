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

        var initialPosition = Cna1979LandSequence.CreateTurn(snapshot.Setup.InitialGameTurn)[0];

        if (snapshot.InitiativeHolder is null)
        {
            return snapshot.StateVersion == 1
                && snapshot.RandomState.NextByteCursor == 0
                && snapshot.SequencePosition == initialPosition;
        }

        if (snapshot.StateVersion != 2)
        {
            return false;
        }

        try
        {
            var resolution = InitiativeResolver.Resolve(
                snapshot.Setup.InitialGameTurn,
                snapshot.Setup.InitialInitiative,
                SandtableRandom.Create(snapshot.RandomState.Seed),
                snapshot.Setup.Sources);
            var expectedPosition = Cna1979LandSequence.GetNext(initialPosition);

            return snapshot.InitiativeHolder == resolution.Outcome.Holder
                && snapshot.RandomState == resolution.RandomState
                && snapshot.SequencePosition == expectedPosition
                && snapshot.SequencePosition.StageId == LandStageIds.NavalConvoy
                && snapshot.SequencePosition.ActorRole == LandActorRole.None
                && snapshot.SequencePosition.ActiveSide is null;
        }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException or InvalidOperationException)
        {
            return false;
        }
    }

    public static bool IsLocallyValid(CampaignSnapshot snapshot)
    {
        if (snapshot.ContractVersion != 3
            || snapshot.StateVersion < 1
            || !IsStableId(snapshot.CampaignId)
            || !IsRulesHash(snapshot.RulesetHash)
            || !IsValidSetup(snapshot.Setup)
            || snapshot.World is null
            || snapshot.World.ContractVersion != CampaignWorldSnapshot.CurrentContractVersion
            || snapshot.RandomState is null
            || snapshot.RandomState.ContractVersion != SandtableRandom.ContractVersion
            || !string.Equals(snapshot.RandomState.AlgorithmId, SandtableRandom.AlgorithmId, StringComparison.Ordinal)
            || snapshot.SequencePosition is null
            || snapshot.SequencePosition.ContractVersion != Cna1979LandSequence.ContractVersion
            || (snapshot.InitiativeHolder is not null && !Enum.IsDefined(snapshot.InitiativeHolder.Value)))
        {
            return false;
        }

        var initialPosition = Cna1979LandSequence.CreateTurn(snapshot.Setup.InitialGameTurn)[0];

        if (snapshot.InitiativeHolder is null)
        {
            return snapshot.StateVersion == 1
                && snapshot.RandomState.NextByteCursor == 0
                && snapshot.SequencePosition == initialPosition;
        }

        if (snapshot.StateVersion != 2)
        {
            return false;
        }

        try
        {
            var resolution = InitiativeResolver.Resolve(
                snapshot.Setup.InitialGameTurn,
                snapshot.Setup.InitialInitiative,
                SandtableRandom.Create(snapshot.RandomState.Seed),
                snapshot.Setup.Sources);
            return snapshot.InitiativeHolder == resolution.Outcome.Holder
                && snapshot.RandomState == resolution.RandomState
                && snapshot.SequencePosition == Cna1979LandSequence.GetNext(initialPosition);
        }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException or InvalidOperationException)
        {
            return false;
        }
    }

    public static bool IsValidSetup(CampaignSetupSnapshot? setup)
    {
        if (setup is null
            || setup.SchemaVersion != Cna1979SetupCatalog.SchemaVersion
            || string.IsNullOrWhiteSpace(setup.SetupId)
            || string.IsNullOrWhiteSpace(setup.SetupHash)
            || setup.InitialGameTurn is < 1 or > 111
            || setup.InitialInitiative is null
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
                setup.Content,
                setup.Sources);
            return string.Equals(setup.SetupHash, expectedHash, StringComparison.Ordinal);
        }
        catch (ArgumentException)
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
