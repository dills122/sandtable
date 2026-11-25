using System.Net;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Hosting;
using Cna.OrleansHost.Api;

namespace Cna.OrleansHost.Tests;

// Issue #621 gap 1-3: the read-only "what is open for my seat" derivation
// (POST /api/v2/campaigns/{id}/openings/open), driven over real HTTP.
public sealed partial class CampaignDurableAuthorityJournalTests
{
    private static readonly string[] OpenForSeatTables =
        ["campaigns", "accepted_steps", "owner_openings", "owner_actions", "seats"];

    private static List<(string OpeningStage, string ActionStage, string[] Elements, bool RuleNotImplemented)>
        ParseOpen(string body)
    {
        using var json = JsonDocument.Parse(body);
        Assert.Equal(2, json.RootElement.GetProperty("contractVersion").GetInt32());
        return json.RootElement.GetProperty("open").EnumerateArray().Select(entry => (
            entry.GetProperty("openingStage").GetString()!,
            entry.GetProperty("actionStage").GetString()!,
            entry.GetProperty("actingElementIds").EnumerateArray().Select(value => value.GetString()!).ToArray(),
            entry.GetProperty("ruleNotImplemented").GetBoolean())).ToList();
    }

    [Fact]
    public async Task OpenForSeatListsOnlyTheRealOpeningsAndWritesNothing()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var request = CombatInfantryRequest();
        await using var outward = await OwnerHttpHarness.Start(journal, request.CampaignId);
        await outward.CreateCampaign(request, Timing(request), OperatorSecret);
        var axis = await outward.IssueAndRedeemSeat(CampaignActionAudience.Axis, OperatorSecret);
        var commonwealth = await outward.IssueAndRedeemSeat(CampaignActionAudience.Commonwealth, OperatorSecret);

        async Task<List<(string OpeningStage, string ActionStage, string[] Elements, bool RuleNotImplemented)>> Open(
            CampaignSeatCredential seat)
        {
            var (status, body) = await outward.OpenForSeatResponse(seat);
            Assert.True(status == HttpStatusCode.OK, $"open-for-seat failed: {status} {body}");
            return ParseOpen(body);
        }

        string[][]? Snapshot() => JournalFixture.UsesMarten ? null
            : OpenForSeatTables.Select(table => IntegratedCampaignRows(fixture.Path, table)).ToArray();

        // Exactly one seat is the initiative winner; only it sees the declaration.
        var before = Snapshot();
        var axisOpen = await Open(axis);
        var commonwealthOpen = await Open(commonwealth);
        var after = Snapshot();
        if (before is not null)
            for (var index = 0; index < before.Length; index++)
                Assert.True(before[index].SequenceEqual(after![index]),
                    $"open-for-seat wrote to {OpenForSeatTables[index]}.");
        var (winner, winnerOpen, loserOpen) = axisOpen.Count > 0
            ? (axis, axisOpen, commonwealthOpen) : (commonwealth, commonwealthOpen, axisOpen);
        Assert.Empty(loserOpen);
        var initiative = Assert.Single(winnerOpen);
        Assert.Equal("initiative-order", initiative.OpeningStage);
        Assert.Equal("initiative-order", initiative.ActionStage);
        Assert.Equal(["land.initiative-order"], initiative.Elements);
        Assert.False(initiative.RuleNotImplemented);

        // Repeatable: same answer twice (no state advanced by asking).
        var again = Assert.Single(await Open(winner));
        Assert.Equal((initiative.OpeningStage, initiative.ActionStage, initiative.RuleNotImplemented),
            (again.OpeningStage, again.ActionStage, again.RuleNotImplemented));
        Assert.Equal(initiative.Elements, again.Elements);

        // Every listed (stage, element) is really openable; an unlisted stage is not.
        var (listedStatus, _) = await outward.OpeningResponse(winner, initiative.Elements[0], initiative.OpeningStage);
        Assert.Equal(HttpStatusCode.OK, listedStatus);
        var (unlistedStatus, _) = await outward.OpeningResponse(winner, "land.reserve-designation", "reserve-designation");
        Assert.Equal(HttpStatusCode.Conflict, unlistedStatus);

        // Advance one real step (act-first) and the answer moves with the game: the preamble's
        // next opening appears for the acting seat, and nothing is stale for the other seat.
        var (revision, refs, visible) = await QueryOpeningForOpenSeatTest(outward, winner, "land.initiative-order", "initiative-order");
        var choices = visible.GetProperty("choices").EnumerateArray().Select(value => value.GetString()).ToArray();
        var actFirst = Array.IndexOf(choices, "act-first");
        Assert.True(actFirst >= 0);
        var (submitStatus, submitBody) = await outward.SubmitResponse(winner, "land.initiative-order",
            "initiative-order", revision, refs[actFirst]);
        Assert.True(submitStatus == HttpStatusCode.OK, submitBody);
        var winnerNow = await Open(winner);
        var loserNow = await Open(winner == axis ? commonwealth : axis);
        Assert.DoesNotContain(winnerNow.Concat(loserNow), entry => entry.OpeningStage == "initiative-order");
        var next = Assert.Single(winnerNow.Concat(loserNow), entry => entry.OpeningStage == "reserve-designation");
        Assert.Equal(["land.reserve-designation"], next.Elements);
    }

    private static async Task<(long Revision, string[] Refs, JsonElement Visible)> QueryOpeningForOpenSeatTest(
        OwnerHttpHarness outward, CampaignSeatCredential credential, string actor, string stage)
    {
        var (status, body) = await outward.OpeningResponse(credential, actor, stage);
        Assert.True(status == HttpStatusCode.OK, body);
        using var json = JsonDocument.Parse(body);
        using var visible = JsonDocument.Parse(Convert.FromBase64String(
            json.RootElement.GetProperty("visibleCutBase64").GetString()!));
        return (json.RootElement.GetProperty("revision").GetInt64(),
            json.RootElement.GetProperty("publicActionRefs").EnumerateArray().Select(v => v.GetString()!).ToArray(),
            visible.RootElement.Clone());
    }

    [Fact]
    public async Task OpenForSeatRejectsForgedWrongCampaignAndNonSameOriginCallers()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var request = CombatInfantryRequest();
        await using var outward = await OwnerHttpHarness.Start(journal, request.CampaignId);
        await outward.CreateCampaign(request, Timing(request), OperatorSecret);
        var axis = await outward.IssueAndRedeemSeat(CampaignActionAudience.Axis, OperatorSecret);

        var forged = axis with { Secret = new string('a', axis.Secret.Length) };
        Assert.Equal(HttpStatusCode.Unauthorized, (await outward.OpenForSeatResponse(forged)).Status);
        var wrongEpoch = axis with { Epoch = axis.Epoch + 1 };
        Assert.Equal(HttpStatusCode.Unauthorized, (await outward.OpenForSeatResponse(wrongEpoch)).Status);
        var wrongCampaign = axis with { CampaignId = "another-campaign" };
        Assert.Equal(HttpStatusCode.Unauthorized, (await outward.OpenForSeatResponse(wrongCampaign)).Status);
        // The same Development + loopback + same-origin gate as every other v2 route (not relaxed).
        Assert.Equal(HttpStatusCode.Forbidden, (await outward.OpenForSeatResponse(axis, sameOrigin: false)).Status);
    }

    [Fact]
    public void OpeningStageTableCoversEveryOwnerOpeningKindOnceAndEveryCatalogStage()
    {
        var kinds = CampaignDurableAuthorityJournal.OpeningStages.Select(stage => stage.Kind).ToArray();
        Assert.Equal(Enum.GetValues<CampaignOwnerOpeningKind>().Order().ToArray(), kinds.Order().ToArray());
        Assert.Equal(kinds.Length, kinds.Distinct().Count());
        var names = CampaignDurableAuthorityJournal.OpeningStages.Select(stage => stage.OpeningStage).ToArray();
        Assert.Equal(names.Length, names.Distinct(StringComparer.Ordinal).Count());
        Assert.All(CampaignOpenSeatCatalog.ReferencedStageNames, name => Assert.Contains(name, names));
        // The one stage whose opening and action names differ is data, not a client special case.
        var rba = Assert.Single(CampaignDurableAuthorityJournal.OpeningStages,
            stage => stage.OpeningStage != stage.ActionStage);
        Assert.Equal(("retreat-before-assault", "rba-decline"), (rba.OpeningStage, rba.ActionStage));
    }
}
