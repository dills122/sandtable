using System.Globalization;

namespace Cna.LoadHarness;

internal sealed record HarnessConfig(int Campaigns, string Policy, ulong RootSeed, int MaxStepsPerCampaign,
    string Output, string Store, string Until, int HotPool, string Driver, Uri? BaseUrl,
    int RampSeconds, int ThinkMsMin, int ThinkMsMax, int MaxInflight, double? StopSubmitP95Ms, double? StopErrorRate, bool VerifyState = false)
{
    internal const string Usage = "Usage: cna-load --output <new-directory> [--campaigns 1..16 (inproc) or 1..100 (http)] " +
        "[--policy first|last|random-legal] [--root-seed 7] [--max-steps-per-campaign 50|112] " +
        "[--store sqlite|postgres] [--until stage2|turn2-reserve] [--hot-pool 16] " +
        "[--driver inproc|http] [--base-url http://127.0.0.1:port] [--ramp-seconds 0] " +
        "[--think-ms-min 0] [--think-ms-max 0] [--max-inflight 16] " +
        "[--stop-submit-p95-ms ms] [--stop-error-rate fraction] [--verify-state]";

    internal static HarnessConfig Parse(string[] args)
    {
        var options = new Dictionary<string, string>(StringComparer.Ordinal);
        var verifyState = false;
        for (var i = 0; i < args.Length;)
        {
            if (args[i] == "--verify-state")
            {
                if (verifyState) throw new ArgumentException("Duplicate --verify-state.");
                verifyState = true;
                i++;
                continue;
            }
            if (i + 1 == args.Length || args[i] is not ("--output" or "--campaigns" or "--policy" or "--root-seed" or "--max-steps-per-campaign" or "--store" or "--until" or "--hot-pool" or "--driver" or "--base-url" or "--ramp-seconds"
                or "--think-ms-min" or "--think-ms-max" or "--max-inflight" or "--stop-submit-p95-ms" or "--stop-error-rate")
                || !options.TryAdd(args[i], args[i + 1]))
                throw new ArgumentException("Unknown, duplicate or missing option: " + args[i]);
            i += 2;
        }
        var driver = options.GetValueOrDefault("--driver", "inproc");
        if (driver is not ("inproc" or "http")) throw new ArgumentException("Invalid --driver.");
        var campaigns = ParseInt("--campaigns", 1, driver == "http" ? 100 : 16, "1");
        var store = options.GetValueOrDefault("--store", "sqlite");
        if (store is not ("sqlite" or "postgres")) throw new ArgumentException("Invalid --store.");
        var until = options.GetValueOrDefault("--until", "stage2");
        if (until is not ("stage2" or "turn2-reserve")) throw new ArgumentException("Invalid --until.");
        var stepLimit = until == "stage2" ? 50 : 112;
        var steps = ParseInt("--max-steps-per-campaign", 1, stepLimit, stepLimit.ToString(CultureInfo.InvariantCulture));
        var hotPool = ParseInt("--hot-pool", 1, 16, "16");
        var policy = options.GetValueOrDefault("--policy", "last");
        if (policy is not ("first" or "last" or "random-legal")) throw new ArgumentException("Invalid --policy.");
        if (!ulong.TryParse(options.GetValueOrDefault("--root-seed", "7"), NumberStyles.None,
                CultureInfo.InvariantCulture, out var seed)) throw new ArgumentException("Invalid --root-seed.");
        if (!options.TryGetValue("--output", out var output) || string.IsNullOrWhiteSpace(output))
            throw new ArgumentException("--output is required.");
        output = Path.GetFullPath(output);
        if (Directory.Exists(output) || File.Exists(output)) throw new ArgumentException("--output must not exist.");
        Uri? baseUrl = null;
        var httpOptions = new[] { "--base-url", "--ramp-seconds", "--think-ms-min", "--think-ms-max",
            "--max-inflight", "--stop-submit-p95-ms", "--stop-error-rate" };
        if (driver != "http" && (verifyState || httpOptions.Any(options.ContainsKey)))
            throw new ArgumentException("HTTP traffic options require --driver http.");
        if (options.TryGetValue("--base-url", out var url))
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out baseUrl) || baseUrl.Scheme != "http"
                || !baseUrl.IsLoopback || baseUrl.AbsolutePath != "/" || baseUrl.UserInfo.Length != 0
                || baseUrl.Query.Length != 0 || baseUrl.Fragment.Length != 0)
                throw new ArgumentException("--base-url must be a loopback HTTP origin.");
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SANDTABLE_LOAD_OPERATOR_SECRET")))
                throw new ArgumentException("--base-url requires SANDTABLE_LOAD_OPERATOR_SECRET.");
        }
        var ramp = ParseInt("--ramp-seconds", 0, 3600, "0");
        var minThink = ParseInt("--think-ms-min", 0, 60000, "0");
        var maxThink = ParseInt("--think-ms-max", minThink, 60000, minThink.ToString(CultureInfo.InvariantCulture));
        var maxInflight = ParseInt("--max-inflight", 1, 100, "16");
        return new(campaigns, policy, seed, steps, output, store, until, hotPool, driver, baseUrl,
            ramp, minThink, maxThink, maxInflight, Threshold("--stop-submit-p95-ms", double.MaxValue),
            Threshold("--stop-error-rate", 1, allowZero: true), verifyState);

        double? Threshold(string key, double max, bool allowZero = false)
        {
            if (!options.TryGetValue(key, out var input)) return null;
            if (!double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                || !double.IsFinite(value) || value < 0 || (!allowZero && value == 0) || value > max)
                throw new ArgumentException($"Invalid {key}.");
            return value;
        }

        int ParseInt(string key, int min, int max, string fallback)
        {
            if (!int.TryParse(options.GetValueOrDefault(key, fallback), NumberStyles.None,
                    CultureInfo.InvariantCulture, out var value) || value < min || value > max)
                throw new ArgumentException($"{key} must be between {min} and {max}.");
            return value;
        }
    }
}
