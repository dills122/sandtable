namespace Cna.ArchitectureTests;

public sealed class NativeTurnAuthorityTests
{
    // Turn stamps in native submit/replay paths must come from accepted history,
    // even when a stale opening turn would produce the same deterministic result.
    [Theory]
    [InlineData("Exercises/CampaignExercises.cs")]
    [InlineData("Exercises/CampaignExercises.TurnBoundary.cs")]
    [InlineData("Exercises/CampaignStageThreePreambleAuthority.cs")]
    [InlineData("Campaigns/CampaignNativeSecondBreakdownV46.cs")]
    [InlineData("Campaigns/CampaignNativeRepeatAssaultRuntime.cs")]
    public void NativeTurnAuthorityDoesNotReadFrozenSetupTurn(string file)
    {
        DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Sandtable.slnx")))
            directory = directory.Parent;
        Assert.NotNull(directory);
        var source = File.ReadAllText(Path.Combine(directory.FullName, "src", "Cna.Core", file));
        Assert.DoesNotContain("Setup.InitialGameTurn", source, StringComparison.Ordinal);
    }
}
