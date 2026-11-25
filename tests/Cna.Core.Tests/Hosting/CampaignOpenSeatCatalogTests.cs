using Cna.Core.Exercises;
using Cna.Core.Hosting;

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
