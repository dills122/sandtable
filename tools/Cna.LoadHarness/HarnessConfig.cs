using System.Globalization;

namespace Cna.LoadHarness;

internal sealed record HarnessConfig(int Campaigns, string Policy, ulong RootSeed, int MaxStepsPerCampaign,
    string Output)
{
    internal const string Usage = "Usage: cna-load --output <new-directory> [--campaigns 1..16] " +
        "[--policy first|last|random-legal] [--root-seed 7] [--max-steps-per-campaign 50]";

    internal static HarnessConfig Parse(string[] args)
    {
        var options = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 0; i < args.Length; i += 2)
        {
            if (i + 1 == args.Length || args[i] is not ("--output" or "--campaigns" or "--policy" or "--root-seed" or "--max-steps-per-campaign")
                || !options.TryAdd(args[i], args[i + 1]))
                throw new ArgumentException("Unknown, duplicate or missing option: " + args[i]);
        }
        var campaigns = ParseInt("--campaigns", 1, 16, "1");
        var steps = ParseInt("--max-steps-per-campaign", 1, 50, "50");
        var policy = options.GetValueOrDefault("--policy", "last");
        if (policy is not ("first" or "last" or "random-legal")) throw new ArgumentException("Invalid --policy.");
        if (!ulong.TryParse(options.GetValueOrDefault("--root-seed", "7"), NumberStyles.None,
                CultureInfo.InvariantCulture, out var seed)) throw new ArgumentException("Invalid --root-seed.");
        if (!options.TryGetValue("--output", out var output) || string.IsNullOrWhiteSpace(output))
            throw new ArgumentException("--output is required.");
        output = Path.GetFullPath(output);
        if (Directory.Exists(output) || File.Exists(output)) throw new ArgumentException("--output must not exist.");
        return new(campaigns, policy, seed, steps, output);

        int ParseInt(string key, int min, int max, string fallback)
        {
            if (!int.TryParse(options.GetValueOrDefault(key, fallback), NumberStyles.None,
                    CultureInfo.InvariantCulture, out var value) || value < min || value > max)
                throw new ArgumentException($"{key} must be between {min} and {max}.");
            return value;
        }
    }
}
