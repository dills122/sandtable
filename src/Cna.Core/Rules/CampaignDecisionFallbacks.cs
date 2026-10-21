namespace Cna.Core.Rules;

// Canonical fallbacks shared by normal campaign decisions and historical
// selected-input configuration. A missing decision never gains a fabricated
// owner action or an implicit budget.
internal static class CampaignDecisionFallbacks
{
    internal static readonly IReadOnlyDictionary<string, string> Required =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["custody"] = "leave-unguarded",
            ["cycle-control"] = "finish-phase",
            ["force-assignment"] = "cancel-incomplete-voluntary-round",
            ["rba"] = "cancel-without-decline",
            ["reserve-release"] = "convert-unresolved-i-retain-ii",
            ["retreat"] = "refuse-retreat",
            ["selection"] = "no-selection",
        };
}
