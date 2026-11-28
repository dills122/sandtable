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
    [InlineData("Campaigns/CampaignNativeReactionCut.cs")]
    [InlineData("Campaigns/CampaignNativeReactionMoveV59.Factory.cs")]
    [InlineData("Campaigns/CampaignNativeReactionClosedV60.Factory.cs")]
    [InlineData("Campaigns/CampaignNativeReactionMoveV59.cs")]
    [InlineData("Campaigns/CampaignNativeReactionClosedV60.cs")]
    [InlineData("Campaigns/CampaignNativeReactionCodec.cs")]
    [InlineData("Actions/CampaignNativeReactionActions.cs")]
    [InlineData("Exercises/CampaignExercises.NativeReaction.cs")]
    [InlineData("Exercises/ExerciseSession.NativeReaction.cs")]
    public void NativeTurnAuthorityDoesNotReadFrozenSetupTurn(string file)
    {
        DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Sandtable.slnx")))
            directory = directory.Parent;
        Assert.NotNull(directory);
        var source = File.ReadAllText(Path.Combine(directory.FullName, "src", "Cna.Core", file));
        Assert.DoesNotContain("Setup.InitialGameTurn", source, StringComparison.Ordinal);
        if (file.Contains("NativeReaction", StringComparison.Ordinal))
        {
            // Scope must bind from the retained position rather than individual turn/stage cases.
            Assert.DoesNotMatch(@"(?i)(?:gameTurn|operationStage)\s*(?:==|!=|is)\s*\d+", source);
            Assert.DoesNotMatch(@"CreateTurn\(\s*\d+\s*\)", source);
        }
    }
}
