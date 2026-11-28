using Cna.LoadHarness;

if (args is ["--help"])
{
    Console.WriteLine(HarnessConfig.Usage);
    return 0;
}

try
{
#if CAPACITY_PROBE
    if (args.Length > 0 && args[0] == "--capacity-probe")
        return await CapacityProbe.RunAsync(args[1..]).ConfigureAwait(false);
#endif
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
