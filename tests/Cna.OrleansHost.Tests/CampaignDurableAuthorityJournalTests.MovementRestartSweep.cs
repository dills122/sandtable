using System.Diagnostics;
using System.Text.Json;
using Cna.Core.Exercises;
using Cna.OrleansHost.Api;

namespace Cna.OrleansHost.Tests;

// Post-E2E adversarial scenario testing (2026-11-22, round 2), restart-recovery sweep: the fourth
// and fifth owner openings this release added (CompleteMovementSegmentAction/
// StopElementMovementAction, PR #511) and the UX-001 System-audience auto-drain they depend on
// (PR #508) are the newest code in the release and had never been put through this project's own
// genuine-process-crash recovery sweep -- the same GrazianiHostStressTests.cs mechanism (a real
// child OS process, a clock that pauses and is killed at an exact point, never a managed
// exception/disposal standing in for a crash) already proves for Source actions. Every existing
// restart test for these two openings (MovementSegmentCompletion.cs's own
// *AcceptanceSurvivesColdRestartAndDrivesTheGameForward facts) only reopens a fresh *same-process*
// journal handle after the action was already fully accepted -- a materially weaker claim than a
// genuinely separate process recovering from a kill at the exact post-commit instant.
//
// Generalizes GrazianiHostStressTests.cs's --source-owner-crash-probe mechanism to two new
// FoundationChild child modes (tests/Cna.OrleansHost.FoundationChild/Program.cs):
//   --movement-owner-crash-probe: same AfterCommit-pause idiom, calling
//     SubmitOwnerMovementSegmentCompletion instead of SubmitSourceActions. Only the AfterCommit
//     variant is meaningful here (scoped deliberately): MovementSegmentCompletion is a one-shot
//     decision with no deadline/timeout round of its own (unlike Source/RBA/Force/Retreat), so
//     there is no mid-transaction clock read during the commit itself to pause a "before write"
//     variant on.
//   --post-drain-owner-crash-probe: queries the Combat Selection opening (the query that performs
//     the System-audience Breakdown auto-drain as a side effect, per
//     CampaignDurableAuthorityJournal.Openings.cs's own C3b remarks) and pauses/crashes right
//     after that query returns -- covering "right after the auto-drain fires but before the next
//     real request" directly, rather than only ever observing the drain fire for the first time
//     on an already-cold-reopened journal (the existing coverage's actual shape).
public sealed partial class CampaignDurableAuthorityJournalTests
{
    [Theory]
    [InlineData(false)] // AdvanceToMovementSegmentCompletion: offers "complete-movement-segment".
    [InlineData(true)] // AdvanceToActiveMovementRoute: offers "stop-element-movement" instead.
    public async Task MovementSegmentCompletionOrStopOwnerCrashRecoversExactlyOnce(bool activeRoute)
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var (request, acting, _) = activeRoute
            ? AdvanceToActiveMovementRoute(journal)
            : AdvanceToMovementSegmentCompletion(journal);
        var opening = journal.QueryMovementSegmentCompletionOwnerOpening(acting);
        Assert.Single(opening.PublicActionRefs);
        var reference = opening.PublicActionRefs[0];
        var beforeVersion = CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId)).StateVersion;

        // The real crash: a genuinely separate OS process commits the submission, is paused
        // right after the commit returns (durably on disk), and is killed there by this test --
        // never a managed exception/disposal standing in for process death.
        await KillMovementOwnerProbe(fixture.Path, acting, opening.Revision, reference);

        // Exactly one accepted step landed from the crashed process's commit (the submission
        // itself; MovementSegmentCompletion has no separate "entry" step the way RBA/Force/
        // Retreat/Custody rounds do), and the crashed process's write is visible to a fresh
        // same-process reopen even though its own in-process receipt never reached a caller.
        var reopened = CampaignDurableAuthorityJournal.OpenExisting(fixture.Path);
        var afterCrash = CampaignExercises.QueryCheckpoint(reopened.ReadVerified(request.CampaignId));
        Assert.True(afterCrash.StateVersion > beforeVersion);
        Assert.True(CampaignExercises.Reconstruct(reopened.ReadVerified(request.CampaignId)).IsVerified);

        // A real retry (the only way the crashed process's own caller could ever have recovered
        // a response) is idempotent: same canonical receipt, no second accepted step.
        var accepted = reopened.SubmitOwnerMovementSegmentCompletion(acting, opening.Revision, reference);
        Assert.True(accepted.IsAccepted);
        var retry = CampaignDurableAuthorityJournal.OpenExisting(fixture.Path)
            .SubmitOwnerMovementSegmentCompletion(acting, opening.Revision, reference);
        Assert.True(retry.IsAccepted);
        Assert.Equal(accepted.CanonicalReceipt, retry.CanonicalReceipt);

        // And a genuinely fresh third process (not just this test's own in-process handle) reads
        // back the identical, continued state.
        await AssertMovementRestartFreshProcessReplay(fixture.Path, request.CampaignId, afterCrash.StateVersion);
    }

    [Fact]
    public async Task ColdRestartRightAfterSystemAutoDrainFiresButBeforeNextRealRequest()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var (request, acting, _) = AdvanceToMovementSegmentCompletion(journal);
        var opening = journal.QueryMovementSegmentCompletionOwnerOpening(acting);
        Assert.True(journal.SubmitOwnerMovementSegmentCompletion(acting, opening.Revision,
            opening.PublicActionRefs[0]).IsAccepted);

        // Breakdown (System audience) has not run yet: nothing has queried past Movement
        // Segment Completion.
        var beforeDrain = CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId));
        var combatElementId = acting.Audience == Cna.Core.Actions.CampaignActionAudience.Axis
            ? "axis-infantry" : "commonwealth-infantry";

        // The real crash: a genuinely separate OS process performs the one query that drains
        // Breakdown as a side effect, and is killed right after that query returns (durably
        // committed), before any response reaches a real caller.
        await KillPostDrainProbe(fixture.Path, acting, combatElementId);

        var reopened = CampaignDurableAuthorityJournal.OpenExisting(fixture.Path);
        var afterDrain = CampaignExercises.QueryCheckpoint(reopened.ReadVerified(request.CampaignId));
        Assert.True(afterDrain.StateVersion > beforeDrain.StateVersion);
        Assert.True(CampaignExercises.Reconstruct(reopened.ReadVerified(request.CampaignId)).IsVerified);

        // The next real request lands on a cold-reopened journal that already has the drain
        // applied: it must see the same, already-drained Combat Selection opening again --
        // querying it a second time must not re-run the drain or duplicate any step.
        var combat = reopened.QueryCombatOwnerOpening(acting, combatElementId);
        Assert.NotEmpty(combat.PublicActionRefs);
        var sameAgain = CampaignDurableAuthorityJournal.OpenExisting(fixture.Path)
            .QueryCombatOwnerOpening(acting, combatElementId);
        Assert.Equal(combat.Revision, sameAgain.Revision);
        Assert.Equal(combat.VisibleBytes, sameAgain.VisibleBytes);
        Assert.Equal(combat.PublicActionRefs, sameAgain.PublicActionRefs);
        Assert.Equal(afterDrain.StateVersion,
            CampaignExercises.QueryCheckpoint(CampaignDurableAuthorityJournal.OpenExisting(fixture.Path)
                .ReadVerified(request.CampaignId)).StateVersion);
    }

    private static async Task KillMovementOwnerProbe(string path, CampaignSeatCredential credential,
        long revision, string reference)
    {
        using var process = StartFoundationChild("--movement-owner-crash-probe");
        var stderr = process.StandardError.ReadToEndAsync();
        try
        {
            await process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(new
            {
                Path = path,
                Credential = credential,
                Revision = revision,
                Reference = reference,
                NowUnixMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                AfterCommit = true,
            }));
            await process.StandardInput.FlushAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            var marker = await process.StandardOutput.ReadLineAsync(timeout.Token);
            if (marker is null) throw new InvalidOperationException($"Movement crash child exited before pause: {await stderr}");
            using var json = JsonDocument.Parse(marker);
            Assert.Equal("movement-after-commit", json.RootElement.GetProperty("Phase").GetString());
            process.Kill(true);
            await process.WaitForExitAsync(timeout.Token);
        }
        finally { if (!process.HasExited) process.Kill(true); }
    }

    private static async Task KillPostDrainProbe(string path, CampaignSeatCredential credential, string actingElementId)
    {
        using var process = StartFoundationChild("--post-drain-owner-crash-probe");
        var stderr = process.StandardError.ReadToEndAsync();
        try
        {
            await process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(new
            {
                Path = path,
                Credential = credential,
                ActingElementId = actingElementId,
                NowUnixMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            }));
            await process.StandardInput.FlushAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            var marker = await process.StandardOutput.ReadLineAsync(timeout.Token);
            if (marker is null) throw new InvalidOperationException($"Post-drain crash child exited before pause: {await stderr}");
            using var json = JsonDocument.Parse(marker);
            Assert.Equal("post-drain-after-query", json.RootElement.GetProperty("Phase").GetString());
            process.Kill(true);
            await process.WaitForExitAsync(timeout.Token);
        }
        finally { if (!process.HasExited) process.Kill(true); }
    }

    private static async Task AssertMovementRestartFreshProcessReplay(string path, string campaignId, long expectedVersion)
    {
        using var process = StartFoundationChild("--source-authority-read");
        var stderr = process.StandardError.ReadToEndAsync();
        var stdout = process.StandardOutput.ReadToEndAsync();
        try
        {
            await process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(new { Path = path, CampaignId = campaignId }));
            process.StandardInput.Close();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            await process.WaitForExitAsync(timeout.Token);
            Assert.True(process.ExitCode == 0, $"Replay child failed: {await stderr}");
            using var json = JsonDocument.Parse(await stdout);
            Assert.Equal(process.Id, json.RootElement.GetProperty("ProcessId").GetInt32());
            Assert.NotEqual(Environment.ProcessId, process.Id);
            Assert.Equal(expectedVersion, json.RootElement.GetProperty("StateVersion").GetInt64());
        }
        finally { if (!process.HasExited) process.Kill(true); }
    }

    private static Process StartFoundationChild(string mode)
    {
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Name;
        var child = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..",
            "Cna.OrleansHost.FoundationChild", configuration, "Cna.OrleansHost.FoundationChild.dll"));
        Assert.True(File.Exists(child), $"Built native child missing: {child}");
        var start = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
        { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        start.ArgumentList.Add(child);
        start.ArgumentList.Add(mode);
        return Process.Start(start) ?? throw new InvalidOperationException($"Cannot start child in mode {mode}.");
    }
}
