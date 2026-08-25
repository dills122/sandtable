using System.Diagnostics.CodeAnalysis;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Setups;

internal static class Cna1979SetupCatalog
{
    public const int SchemaVersion = 5;
    public const string PredeterminedSetupId = "rules-lab.initiative.predetermined";
    public const string ContestedSetupId = "rules-lab.initiative.contested";

    public static RuleReference PredeterminedSourceReference { get; } = new(
        "sandtable-rules-lab",
        "initiative.predetermined-axis.v1");

    public static RuleReference ContestedSourceReference { get; } = new(
        "sandtable-rules-lab",
        "initiative.contested-turn-43.v1");

    internal static RuleReference OpeningPreambleSourceReference { get; } = new(
        "sandtable-rules-lab",
        "opening-preamble.no-naval-convoy-obligations.v1");

    internal static CampaignOpeningPreamblePolicy OpeningPreamblePolicy { get; } = new(
        CampaignOpeningPreamblePolicy.CurrentContractVersion,
        CampaignOpeningPreambleKind.NoOpeningNavalConvoyObligations,
        [OpeningPreambleSourceReference]);

    internal static RuleReference WeatherPolicySourceReference { get; } = new(
        "sandtable-rules-lab",
        "weather.no-immediate-effect-subjects.v1");

    internal static CampaignWeatherPolicy WeatherPolicy { get; } = new(
        CampaignWeatherPolicy.CurrentContractVersion,
        CampaignWeatherPolicyKind.NoImmediateWeatherEffectSubjects,
        [WeatherPolicySourceReference]);

    public static IReadOnlyList<CampaignSetupDefinition> Definitions { get; } =
        Array.AsReadOnly<CampaignSetupDefinition>(
        [
            new(
                SchemaVersion,
                PredeterminedSetupId,
                "Rules Lab: Predetermined Axis Initiative",
                true,
                1,
                new PredeterminedInitiative(LandSide.Axis),
                OpeningPreamblePolicy,
                WeatherPolicy,
                CreateStageEntryPolicy(1),
                new CampaignContentSelection(
                    Cna1979SyntheticContentCatalog.Artifact.Identity,
                    "movement-contact-lab"),
                [PredeterminedSourceReference]),
            new(
                SchemaVersion,
                ContestedSetupId,
                "Rules Lab: Contested Initiative",
                true,
                43,
                new ContestedInitiative(new AxisInitiativeSourceFacts(
                    AxisInitiativeLocation.OffMapOrUnavailable,
                    [AxisInitiativeLocation.QualifyingGameMap])),
                OpeningPreamblePolicy,
                WeatherPolicy,
                CreateStageEntryPolicy(43),
                new CampaignContentSelection(
                    Cna1979SyntheticContentCatalog.Artifact.Identity,
                    "initiative-contested-lab"),
                [ContestedSourceReference]),
        ]);

    public static bool TryGet(
        string? setupId,
        [NotNullWhen(true)] out CampaignSetupDefinition? definition)
    {
        definition = Definitions.SingleOrDefault(candidate => string.Equals(
            candidate.SetupId,
            setupId,
            StringComparison.Ordinal));
        return definition is not null;
    }

    internal static bool IsAdmittedWeatherPolicy(CampaignWeatherPolicy? policy) =>
        policy is not null
        && policy.ContractVersion == CampaignWeatherPolicy.CurrentContractVersion
        && policy.Kind == CampaignWeatherPolicyKind.NoImmediateWeatherEffectSubjects
        && policy.Sources.SequenceEqual([WeatherPolicySourceReference]);

    internal static bool IsAdmittedStageEntryPolicy(
        CampaignStageEntryPolicy? policy,
        int initialGameTurn) =>
        policy is not null
        && policy.ContractVersion == CampaignStageEntryPolicy.CurrentContractVersion
        && policy.GameTurn == initialGameTurn
        && policy.OperationStage == 1
        && policy.Organization == StageEntryObligationKind.ExplicitNone
        && policy.NavalConvoyArrival == StageEntryObligationKind.ExplicitNone
        && policy.FleetAssignment == StageEntryObligationKind.ExplicitNone
        && policy.FleetRepair == StageEntryObligationKind.ExplicitNone
        && policy.Sources.SequenceEqual([CampaignStageEntryPolicy.SourceReference]);

    private static CampaignStageEntryPolicy CreateStageEntryPolicy(int gameTurn) => new(
        CampaignStageEntryPolicy.CurrentContractVersion,
        gameTurn,
        1,
        StageEntryObligationKind.ExplicitNone,
        StageEntryObligationKind.ExplicitNone,
        StageEntryObligationKind.ExplicitNone,
        StageEntryObligationKind.ExplicitNone,
        [CampaignStageEntryPolicy.SourceReference]);
}
