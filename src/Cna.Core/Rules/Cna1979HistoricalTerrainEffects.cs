namespace Cna.Core.Rules;

// Terrain Effects Chart (SPI 8.37) entries for the hex-terrain vocabulary used
// by the historical map transcription. This covers enterability only (can a
// mobility class ever enter the hex terrain) for Zones of Control projection;
// it is not a movement-cost or breakdown engine. Terrain ids absent here
// (including "sea") are never assumed enterable.
public static class Cna1979HistoricalTerrainEffects
{
    private static readonly RuleReference Chart = new("spi-1979-land-rules", "8.37");

    // null means not established for that mobility class here (never treated
    // as either enterable or prohibited) rather than a guessed answer.
    private static readonly Dictionary<string, (bool? NonMotorized, bool? Motorized)> Enterability =
        new Dictionary<string, (bool?, bool?)>(StringComparer.Ordinal)
        {
            ["clear"] = (true, true),
            ["gravel"] = (true, true),
            ["delta"] = (true, true),
            ["desert"] = (true, true),
            ["heavy-vegetation"] = (true, true),
            ["rough"] = (true, true),
            ["mountain"] = (true, true),
            ["major-city"] = (true, true),
            // Land units cannot enter a sea hex; omitted entirely from SPI
            // 8.37 because it is not a land-movement terrain type.
            ["sea"] = (false, false),
            // Footnote 2: enterable by track only for light trucks, recce and
            // motorcycle infantry; no other motorized unit. Treated as
            // non-motorized-enterable, motorized-unresolved at this coarse
            // mobility split rather than guessed either way.
            ["salt-marsh"] = (true, null),
            // "May enter only on road or railroad" — hex terrain alone never
            // admits entry; a specific road/railroad hexside is required and
            // is not evaluated here.
            ["swamp"] = (null, null),
        };

    public static bool TryIsEnterable(string? terrainId, string mobilityId, out bool enterable, out IReadOnlyList<RuleReference> sources)
    {
        sources = [Chart];
        enterable = false;
        if (terrainId is null || !Cna1979Movement.IsSupportedMobilityId(mobilityId)
            || !Enterability.TryGetValue(terrainId, out var rule))
            return false;
        var known = mobilityId == Cna1979Movement.MotorizedMobilityId ? rule.Motorized : rule.NonMotorized;
        if (known is not { } value) return false;
        enterable = value;
        return true;
    }
}
