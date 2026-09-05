using Cna.ExerciseRunner.Commands;

namespace Cna.ExerciseRunner.Tests.Execution;

public sealed class ReactionManeuverTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"sandtable-reaction-{Guid.NewGuid():N}");

    [Fact]
    public void FrozenHistoricalReactionMatrixIsRejectedBeforeCurrentExecution()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var exit = ManeuverRunCommand.Execute(
            ["maneuver", "run", "--manifest", "scenarios/maneuvers/rules-lab.reaction.serial.v2.json",
                "--artifact-root", root], output, error, TestContext.Current.CancellationToken);
        Assert.Equal(ManeuverProcessExitCode.ManifestInvalid, exit);
        Assert.Empty(output.ToString());
        Assert.StartsWith("Maneuver admission failed: ", error.ToString(), StringComparison.Ordinal);
        Assert.False(Directory.Exists(root));
    }

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }
}
