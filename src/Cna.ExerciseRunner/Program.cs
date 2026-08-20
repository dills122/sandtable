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
            return (int)ExerciseRunCommand.Execute(
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
}
