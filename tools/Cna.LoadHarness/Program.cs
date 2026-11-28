using Cna.LoadHarness;
using System.Text.Json;

if (args is ["--help"])
{
    Console.WriteLine(HarnessConfig.Usage);
    Console.WriteLine("Local demo setup: --prepare-demo (used by scripts/demo.sh; creates and redeems two seats, without playing).");
    return 0;
}

try
{
#if CAPACITY_PROBE
    if (args.Length > 0 && args[0] == "--capacity-probe")
        return await CapacityProbe.RunAsync(args[1..]).ConfigureAwait(false);
#endif
    if (args is ["--prepare-demo"])
    {
        // Reuse the same fixture, real HTTP creation, and seat redemption as the load runner,
        // without playing any decisions. The launcher owns the loopback host and its lifetime.
        var demoConfig = HarnessConfig.Parse(["--driver", "http", "--store", "postgres",
            "--base-url", "http://127.0.0.1:5087", "--until", "turn2-reserve", "--combat-gt1",
            "--output", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))]);
        using var stop = new CancellationTokenSource();
        using var control = new HttpRunControl(demoConfig, stop, stop.Token);
        using var driver = new HttpDriver(demoConfig, demoConfig.BaseUrl!,
            Environment.GetEnvironmentVariable("SANDTABLE_LOAD_OPERATOR_SECRET")!, control,
            (_, _, _, _, _) => { }, "demo-");
        var seats = await driver.CreateAsync(Guid.NewGuid().ToString("N")).ConfigureAwait(false);
        // Deliberate interactive output, not load-run diagnostics. Never include these in recordings.
        foreach (var seat in seats)
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                campaignId = seat.CampaignId, audience = seat.Audience.ToString().ToLowerInvariant(),
                epoch = seat.Epoch, secret = seat.Secret, expiresUnixMs = seat.ExpiresUnixMs,
            }));
        return 0;
    }
    var config = HarnessConfig.Parse(args);
    using var cancellation = new CancellationTokenSource();
    Console.CancelKeyPress += (_, eventArgs) =>
    {
        eventArgs.Cancel = true;
        cancellation.Cancel();
    };
    return await HarnessRun.RunAsync(config, cancellation.Token).ConfigureAwait(false);
}
catch (ArgumentException exception)
{
    Console.Error.WriteLine(exception.Message);
    Console.Error.WriteLine(HarnessConfig.Usage);
    return 2;
}
