using System.Diagnostics.CodeAnalysis;
using Cna.Core.Rules;

namespace Cna.Core.Setups;

public static class Cna1979SetupCatalog
{
    public const int SchemaVersion = 1;
    public const string PredeterminedSetupId = "rules-lab.initiative.predetermined";
    public const string ContestedSetupId = "rules-lab.initiative.contested";

    public static RuleReference PredeterminedSourceReference { get; } = new(
        "sandtable-rules-lab",
        "initiative.predetermined-axis.v1");

    public static RuleReference ContestedSourceReference { get; } = new(
        "sandtable-rules-lab",
        "initiative.contested-turn-43.v1");

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
}
