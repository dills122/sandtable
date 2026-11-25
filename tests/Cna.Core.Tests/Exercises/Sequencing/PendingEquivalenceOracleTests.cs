using Cna.Core.Exercises;
using Cna.Core.Exercises.Sequencing;
using Cna.Core.Rules;
using Xunit;

namespace Cna.Core.Tests.Exercises.Sequencing;

// S2: differential oracle. Old resolver = ExerciseSession.Pending (the cascade). New resolver =
// PendingFromCursor over Cna1979LandSequenceV4.CreateTurn. Fixture sets: every S0 scenario sample
// (real sessions, every accepted step) plus every PendingFixtures session (#581, one per subtype).
// When a Stage 3 slice adds a PendingDecision subtype without a derivation row, these fail naming it.
public sealed class PendingEquivalenceOracleTests
{
    // Subtypes no fixture drives today. Shrinks as slices land fixtures; a subtype missing from both the
    // observed set and this list fails the completeness test, and a listed subtype that IS observed
    // fails it too (so the list cannot rot).
    private static readonly string[] NotDriven = ["StageEntryMovePending", "StageEntrySecondMovePending", "ThirdStageMovePending"];

    private sealed record Row(string Fixture, string Label, ExerciseSession Session, string? Actual, string PositionId,
        IReadOnlyList<string> NonNull);

    private static IEnumerable<Row> Rows()
    {
        foreach (var sample in SequencingScenarios.All.SelectMany(s => s.Samples))
            yield return new Row(sample.Scenario, sample.Label, sample.Session, sample.PendingName, sample.PositionId, sample.NonNullCurrentFields);
        foreach (var pair in PendingFixtures.All.OrderBy(p => p.Key.Name, StringComparer.Ordinal))
        {
            var session = pair.Value();
            yield return new Row("PendingFixtures", pair.Key.Name, session, session.Pending?.GetType().Name,
                CampaignExercises.QueryCheckpoint(session).PositionId, []);
        }
    }

    private static string Describe(Row row, string? derived) =>
        $"{row.Fixture}/{row.Label}: actual={row.Actual ?? "<null>"} derived={derived ?? "<null>"} "
        + $"position={row.PositionId} nonNull=[{string.Join(",", row.NonNull)}]";

    [Fact]
    public void DerivedPendingEqualsTheCascadeOverEveryFixtureSession()
    {
        var rows = Rows().ToList();
        var mismatches = rows.Select(row => (row, derived: PendingFromCursor.Derive(row.Session)))
            .Where(x => x.derived != x.row.Actual).Select(x => Describe(x.row, x.derived)).ToList();
        Assert.True(rows.Count > 150, $"too few oracle rows: {rows.Count}");
        if (Environment.GetEnvironmentVariable("SEQUENCING_DUMP") is { Length: > 0 } dump)
            File.WriteAllLines(dump + ".mismatch", mismatches.Append(
                $"rows={rows.Count} fixtures={rows.Select(r => r.Fixture).Distinct().Count()} distinctSubtypes={rows.Select(r => r.Actual).Where(x => x is not null).Distinct().Count()}"));
        Assert.Empty(mismatches);
    }

    [Fact]
    public void EverySubtypeIsDerivableOrExceptionListedAndObservedOrNamedNotDriven()
    {
        var subtypes = typeof(PendingDecision).Assembly.GetTypes()
            .Where(t => t.BaseType == typeof(PendingDecision)).Select(t => t.Name).Order(StringComparer.Ordinal).ToArray();
        var turn = Cna1979LandSequenceV4.CreateTurn(1);
        var derivable = turn.SelectMany(p => Enum.GetValues<PendingLastEvent>()
                .Select(e => PendingFromCursor.Derive(turn, p.PositionId, e)))
            .Where(x => x is not null).Select(x => x!).ToHashSet(StringComparer.Ordinal);
        var unmapped = subtypes.Where(t => !derivable.Contains(t) && !PendingExceptionTable.Unobservable.Contains(t)).ToArray();
        Assert.True(unmapped.Length == 0,
            "PendingDecision subtype(s) with no derivation row and no exception entry (add one row to the "
            + "Pending*Table files in src/Cna.Core/Exercises/Sequencing): " + string.Join(", ", unmapped));
        Assert.All(PendingExceptionTable.Unobservable, t => Assert.Contains(t, subtypes)); // stale exception entry

        var observed = Rows().Select(r => r.Actual).Where(x => x is not null).Select(x => x!).ToHashSet(StringComparer.Ordinal);
        var neverObserved = subtypes.Where(t => !observed.Contains(t) && !PendingExceptionTable.Unobservable.Contains(t)).ToArray();
        Assert.True(neverObserved.Order(StringComparer.Ordinal).SequenceEqual(NotDriven.Order(StringComparer.Ordinal)),
            "Subtypes with no driven fixture (add a fixture or list under NotDriven): "
            + string.Join(", ", neverObserved.Except(NotDriven)) + " | NotDriven but now observed: "
            + string.Join(", ", NotDriven.Where(observed.Contains)));
    }

    [Fact]
    public void EveryCatalogPositionTheFixturesVisitIsAccountedForAndTheUnvisitedSetIsPinned()
    {
        var visited = Rows().Select(r => r.PositionId).ToHashSet(StringComparer.Ordinal);
        var turn = Cna1979LandSequenceV4.CreateTurn(1);
        var catalog = turn.Select(p => p.PositionId).ToHashSet(StringComparer.Ordinal);
        Assert.All(visited, v => Assert.Contains(v, catalog));
        var unvisited = turn.Select(p => p.PositionId).Where(p => !visited.Contains(p)).ToList();
        // Pinned by count, with the list printed on drift: shrinking is progress, growing means a catalog
        // position (new Stage 3 slice) has no fixture.
        Assert.True(unvisited.Count == ExpectedUnvisited,
            $"unvisited catalog positions {unvisited.Count} (expected {ExpectedUnvisited}):\n" + string.Join("\n", unvisited));
    }

    private const int ExpectedUnvisited = 39;
}
