using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Hosting;

public sealed class CampaignOpenSeatCatalogTests
{
    private static Type[] PendingSubtypes() => typeof(PendingDecision).Assembly.GetTypes()
        .Where(type => !type.IsAbstract && typeof(PendingDecision).IsAssignableFrom(type))
        .OrderBy(type => type.FullName, StringComparer.Ordinal).ToArray();

    // When this fails, a PendingDecision subtype was added without a catalog row. Fix: add ONE line to
    // CampaignOpenSeatCatalog.Rows in src/Cna.Core/Hosting/CampaignOpenSeatCatalog.cs:
    //   [typeof(NewPending)] = CampaignOpenSeatRow.Unwired("why no Host opening exists yet");
    // (or .Stages("host-stage", ...) once the opening kind is wired, or .SystemOnly("why")).
    [Fact]
    public void EveryPendingDecisionSubtypeHasExactlyOneCatalogRow()
    {
        var subtypes = PendingSubtypes();
        Assert.NotEmpty(subtypes);
        var missing = subtypes.Where(type => !CampaignOpenSeatCatalog.Rows.ContainsKey(type))
            .Select(type => type.Name).ToArray();
        Assert.True(missing.Length == 0,
            "PendingDecision subtypes without a CampaignOpenSeatCatalog row: " + string.Join(", ", missing)
            + ". Add `[typeof(X)] = CampaignOpenSeatRow.Unwired(\"reason\")` (or Stages/SystemOnly) to CampaignOpenSeatCatalog.Rows.");
        var stale = CampaignOpenSeatCatalog.Rows.Keys.Where(type => !subtypes.Contains(type))
            .Select(type => type.Name).ToArray();
        Assert.True(stale.Length == 0, "CampaignOpenSeatCatalog rows for removed subtypes: " + string.Join(", ", stale));
    }

    [Fact]
    public void EveryCursorStageIsRegisteredAndStageThreeMatchesStageTwo()
    {
        foreach (var position in Cna1979LandSequence.CreateTurn(1))
        {
            var stages = CampaignOpenSeatCatalog.StagesAt(position, true);
            if (stages is null) continue;
            Assert.All(stages, stage => Assert.Contains(stage, CampaignOpenSeatCatalog.ReferencedStageNames));
            if (position.OperationStage == 3)
            {
                var stageTwo = Cna1979LandSequence.CreateTurn(1).Single(p => p.OperationStage == 2
                    && p.ActorRole == position.ActorRole && p.PhaseId == position.PhaseId
                    && p.SegmentId == position.SegmentId && p.StepId == position.StepId);
                Assert.Equal(CampaignOpenSeatCatalog.StagesAt(stageTwo, true), stages);
            }
        }
    }

    [Theory]
    [InlineData(2)]
    [InlineData(13)]
    public void LaterTurnCatalogExposesOnlyTheTwoSupportedOwnerPositions(int gameTurn)
    {
        foreach (var position in Cna1979LandSequence.CreateTurn(gameTurn))
        {
            var expected = position.PositionId switch
            {
                "land.position.operation-1.initiative-declaration" => new[] { "stage-entry-initiative-declaration" },
                "land.position.operation-1.first-player.reserve-designation" => new[] { "stage-entry-reserve" },
                _ => Array.Empty<string>(),
            };
            Assert.Equal(expected, CampaignOpenSeatCatalog.StagesAt(position, false));
            Assert.All(expected, stage => Assert.Contains(stage, CampaignOpenSeatCatalog.ReferencedStageNames));
        }
    }

    [Fact]
    public void EveryRowNamesStagesOrAnExceptionReasonNeverBothNeverNeither()
    {
        foreach (var (type, row) in CampaignOpenSeatCatalog.Rows)
        {
            var hasStages = row.StageNames.Count > 0;
            var hasReason = !string.IsNullOrWhiteSpace(row.ExceptionReason);
            Assert.True(hasStages ^ hasReason, $"{type.Name} must name stages XOR give an exception reason.");
            Assert.Equal(row.StageNames.Count, row.StageNames.Distinct(StringComparer.Ordinal).Count());
        }
    }
}
