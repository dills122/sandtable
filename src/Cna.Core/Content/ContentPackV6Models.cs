namespace Cna.Core.Content;

public sealed class ContentPackV6Definition
{
    public const int SchemaVersion = 6;
    public const string CanonicalFormatId = "sandtable.content-json.v5";
    public const string SupportedCapabilityProfileId = "sandtable.capability.breakdown-truck-battalion.v1";
    public const string CombatCapabilityId = "land.combat-components";

    public ContentPackV6Definition(
        ContentPackDefinition legacyDefinition,
        IEnumerable<ContentElementCombatFacts> elementCombatFacts,
        IEnumerable<ContentInitialPlacementCombatFacts> initialPlacementCombatFacts,
        string capabilityProfileId)
    {
        ArgumentNullException.ThrowIfNull(legacyDefinition);
        var elementCopy = ContentContractGuards.CopyValues(
            elementCombatFacts,
            nameof(elementCombatFacts));
        var placementCopy = ContentContractGuards.CopyValues(
            initialPlacementCombatFacts,
            nameof(initialPlacementCombatFacts));

        CapabilityProfileId = capabilityProfileId;
        LegacyDefinition = legacyDefinition;
        ContractSchemaVersion = SchemaVersion;
        FormatId = CanonicalFormatId;
        Capabilities = Array.AsReadOnly(legacyDefinition.Capabilities
            .Append(CombatCapabilityId)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray());
        ElementCombatFacts = Array.AsReadOnly(elementCopy
            .OrderBy(value => value.ElementId, StringComparer.Ordinal)
            .ToArray());
        InitialPlacementCombatFacts = Array.AsReadOnly(placementCopy
            .OrderBy(value => value.ScenarioId, StringComparer.Ordinal)
            .ThenBy(value => value.ElementId, StringComparer.Ordinal)
            .ToArray());
    }

    public string CapabilityProfileId { get; }

    public int ContractSchemaVersion { get; }

    public string FormatId { get; }

    public string PackId => LegacyDefinition.PackId;

    public string RulesetId => LegacyDefinition.RulesetId;

    public ContentPackDefinition LegacyDefinition { get; }

    public IReadOnlyList<string> Capabilities { get; }

    public IReadOnlyList<ContentElementCombatFacts> ElementCombatFacts { get; }

    public IReadOnlyList<ContentInitialPlacementCombatFacts> InitialPlacementCombatFacts { get; }
}
