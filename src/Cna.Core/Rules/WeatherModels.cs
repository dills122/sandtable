using Cna.Core.Randomness;

namespace Cna.Core.Rules;

internal enum WeatherSeason
{
    Fall = 1,
    Winter = 2,
    Spring = 3,
    Summer = 4,
}

internal enum WeatherKind
{
    Normal = 1,
    Hot = 2,
    Sandstorm = 3,
    Rainstorm = 4,
}

internal enum WeatherScope
{
    None = 1,
    Global = 2,
    ListedAreas = 3,
}

internal enum WeatherArea
{
    A = 1,
    B = 2,
    C = 3,
    D = 4,
    E = 5,
}

internal sealed record WeatherResolution(
    WeatherSeason Season,
    int FirstDie,
    int SecondDie,
    WeatherKind Kind,
    WeatherScope Scope,
    int? LocationDie,
    IReadOnlyList<WeatherArea> AffectedAreas,
    RandomStreamState RandomState);
