using Cna.Core.Content;

namespace Cna.Tests.Shared;

// Existing tests share the production source-backed excerpt; it is not a playable setup.
internal static class GrazianiSourceExcerpt
{
    internal const string ScenarioId = Cna1979GrazianiCairoSourceExcerpt.ScenarioId;
    internal static ContentSourceScheduledCombatPackArtifact Build() =>
        Cna1979GrazianiCairoSourceExcerpt.Build();
}
