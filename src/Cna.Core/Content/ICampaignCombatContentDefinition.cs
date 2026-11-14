namespace Cna.Core.Content;

/// <summary>Read-only combat facts shared by versioned campaign content formats.</summary>
public interface ICampaignCombatContentDefinition
{
    ContentPackDefinition LegacyDefinition { get; }
    IReadOnlyList<ContentElementCombatFacts> ElementCombatFacts { get; }
    IReadOnlyList<ContentInitialPlacementCombatFacts> InitialPlacementCombatFacts { get; }
}
