using Cna.ExerciseRunner.Commands;

namespace Cna.ExerciseRunner;

internal static class Program
{
    public static int Main(string[] args)
    {
        using var cancellation = new CancellationTokenSource();
        ConsoleCancelEventHandler handler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };
        Console.CancelKeyPress += handler;
        try
        {
            return Execute(
                args,
                Console.Out,
                Console.Error,
                cancellation.Token);
        }
        finally
        {
            Console.CancelKeyPress -= handler;
        }
    }

    internal static int Execute(
        string[] args,
        TextWriter standardOutput,
        TextWriter standardError,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (args.Length > 0 && string.Equals(args[0], "maneuver", StringComparison.Ordinal))
            return (int)ManeuverRunCommand.Execute(
                args,
                standardOutput,
                standardError,
                cancellationToken);
        return (int)ExerciseRunCommand.Execute(
            args,
            standardOutput,
            standardError,
            cancellationToken);
    }
}
