using System.Net;
using Cna.Core.Actions;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.OrleansHost.Api;

namespace Cna.OrleansHost.Tests;

public sealed partial class CampaignDurableAuthorityJournalTests
{
    // Singleton/per-element fallback candidates following the prior successful stage.
    // The element template uses a seat's own lab fixture prefix, not opposing state.
    private static readonly (string Previous, string Stage, string Element)[] LoadHarnessFallbacks =
    [
        ("truck-convoy-pass", "post-combat-pass", "land.post-combat-pass"),
        ("post-combat-pass", "post-combat-pass", "land.post-combat-pass"),
        ("post-combat-pass", "second-reserve", "land.second-reserve"),
        ("post-combat-pass", "stage-entry-initiative-declaration", "land.stage-entry-initiative-declaration"),
        ("second-reserve", "second-movement-completion", "land.second-movement-completion"),
        ("second-movement-completion", "combat-selection", "{seat}-infantry"),
        ("combat-selection", "cycle-control", "land.cycle-control"),
        ("cycle-control", "truck-convoy-pass", "land.truck-convoy-pass"),
    ];

    [Fact]
    public async Task LoadHarnessSpikeDiscoversStageOneAndRepeatsOrderedChoices()
    {
        var first = await TraceLoadHarnessStageOne();
        var second = await TraceLoadHarnessStageOne();
        Assert.Equal(first.Rows, second.Rows);
        Assert.Equal(first.Cuts, second.Cuts);
        Assert.Equal(first.Submissions, second.Submissions);
        Assert.Equal(first.References.Length, second.References.Length);
        Assert.NotEqual(first.References[0], second.References[0]);
        TestContext.Current.TestOutputHelper?.WriteLine(string.Join('\n', first.Rows));
        TestContext.Current.TestOutputHelper?.WriteLine($"{first.Submissions} submissions; {first.Cuts.Length} openings");
    }

    private static async Task<(string[] Rows, string[] Cuts, string[] References, int Submissions)>
        TraceLoadHarnessStageOne()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create(new ManualClock());
        var request = CombatInfantryRequest();
        await using var outward = await OwnerHttpHarness.Start(journal, request.CampaignId);
        await outward.CreateCampaign(request, Timing(request), OperatorSecret);
        CampaignSeatCredential[] seats =
        [
            await outward.IssueAndRedeemSeat(CampaignActionAudience.Axis, OperatorSecret),
            await outward.IssueAndRedeemSeat(CampaignActionAudience.Commonwealth, OperatorSecret),
        ];
        var rows = new List<string>();
        var cuts = new List<string>();
        var references = new List<string>();
        var previousStage = "";
        for (var step = 0; step < 50; step++)
        {
            var openings = new List<(CampaignSeatCredential Seat, string Stage, string ActionStage, string Element, bool Unsupported)>();
            foreach (var seat in seats)
            {
                var (status, body) = await outward.OpenForSeatResponse(seat);
                Assert.True(status == HttpStatusCode.OK, body);
                foreach (var entry in ParseOpen(body))
                    foreach (var element in entry.Elements)
                        openings.Add((seat, entry.OpeningStage, entry.ActionStage, element, entry.RuleNotImplemented));
            }
            rows.Add($"{step}: " + string.Join("; ", openings.Select(opening =>
                $"{opening.Seat.Audience}/{opening.Stage}/{opening.ActionStage}/{opening.Element}/{opening.Unsupported}")));
            // Discovery can miss a real opening. Confirm the known singleton fallback over HTTP.
            if (openings.Count == 0)
            {
                foreach (var (_, stage, template) in LoadHarnessFallbacks.Where(row => row.Previous == previousStage))
                {
                    foreach (var seat in seats)
                    {
                        var element = template.Replace("{seat}", seat.Audience.ToString().ToLowerInvariant(), StringComparison.Ordinal);
                        var (status, body) = await outward.OpeningResponse(seat, element, stage);
                        rows.Add($"{step}: fallback {seat.Audience}/{stage}/{(int)status}");
                        Assert.True(status is HttpStatusCode.OK or HttpStatusCode.Conflict, body);
                        if (status == HttpStatusCode.OK)
                            openings.Add((seat, stage, stage, element, false));
                    }
                    if (openings.Count > 0) break;
                }
                Assert.True(openings.Count > 0, string.Join('\n', rows) + "\nCheckpoint: " +
                    CampaignExercises.QueryCheckpoint(fixture.Open().ReadVerified(request.CampaignId)).PositionId);
            }
            if (openings.Any(opening => opening.Stage == "stage-entry-initiative-declaration"))
            {
                var verified = fixture.Open().ReadVerified(request.CampaignId);
                Assert.True(CampaignExercises.Reconstruct(verified).IsVerified);
                Assert.Equal("land.position.operation-2.initiative-declaration",
                    CampaignExercises.QueryCheckpoint(verified).PositionId);
                return (rows.ToArray(), cuts.ToArray(), references.ToArray(), step);
            }

            // Observe every advertised choice for each seat, then pick the last opening and last
            // index. No stage-specific game policy, hidden state or reference-value sorting.
            var available = new List<(CampaignSeatCredential Seat, string ActionStage, string Element, long Revision, string[] Refs)>();
            foreach (var opening in openings.Where(opening => !opening.Unsupported))
            {
                var cut = await QueryOpeningForOpenSeatTest(outward, opening.Seat, opening.Element, opening.Stage);
                var again = await QueryOpeningForOpenSeatTest(outward, opening.Seat, opening.Element, opening.Stage);
                Assert.Equal(cut.Revision, again.Revision);
                Assert.Equal(cut.Refs, again.Refs);
                Assert.Equal(cut.Visible.GetRawText(), again.Visible.GetRawText());
                Assert.NotEmpty(cut.Refs);
                Assert.Equal(cut.Refs.Length, cut.Refs.Distinct(StringComparer.Ordinal).Count());
                cuts.Add($"{step}/{opening.Seat.Audience}/{opening.Stage}/{opening.Element}/{cut.Revision}/{cut.Refs.Length}/{cut.Visible.GetRawText()}");
                references.AddRange(cut.Refs);
                available.Add((opening.Seat, opening.ActionStage, opening.Element, cut.Revision, cut.Refs));
            }
            Assert.NotEmpty(available);
            var selected = available[^1];
            var (submitStatus, submitBody) = await outward.SubmitResponse(selected.Seat,
                selected.Element, selected.ActionStage, selected.Revision, selected.Refs[^1]);
            Assert.True(submitStatus == HttpStatusCode.OK, $"Step {step}: {selected.ActionStage}: {submitBody}");
            previousStage = selected.ActionStage;
        }
        throw new InvalidOperationException("Stage 2 was not reached within 50 owner submissions.\n" + string.Join('\n', rows));
    }
}
