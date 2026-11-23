using System.Reflection;
using Cna.Core.Exercises;

namespace Cna.ArchitectureTests;

// Protects the "don't widen CampaignSnapshotV11" architecture ruling from the
// 2026-11-22 snapshot-representation fork (Option 2/hybrid: generalize the native
// event chain for Stage 2+, never widen CampaignSnapshotV11 to represent it).
//
// Scoping note (honest, not the originally-specified shape): the owner-synced
// direction doc (docs/work/plans/2026-11-23-codebase-overhaul-direction.md, Section
// 5) asked for a NetArchTest dependency-based rule ("no file outside
// CampaignSnapshotV11*.cs constructs a new CampaignSnapshotV11", or, as a documented
// fallback, "no new public API surface on CampaignSnapshotV11 itself"). A
// dependency-based rule was tried first and rejected: CampaignSnapshotV11 is the
// live, pervasively-used snapshot type throughout Stage 1 by design (CampaignExercises,
// ExerciseSession, CampaignHostAdapter, every V11 projector, etc. all legitimately
// reference it), so "who depends on this type" is enormous and has nothing to do with
// "was this type widened" -- a dependency-shape tool answers a different question
// than the one this ruling is actually about. What the ruling actually protects is
// the type's own *shape*: whether it gained a new member since the ruling, which is
// a reflection-based golden-member-list check, not a NetArchTest assertion.
//
// This is deliberately a plain reflection test, not a NetArchTest.Rules assertion --
// see CampaignCoreLayeringTests in this same project for where NetArchTest actually
// fits this codebase well (dependency-shape, not member-shape).
public sealed class CampaignSnapshotV11SurfaceTests
{
    // The exact, currently-shipped public property surface of
    // Cna.Core.Campaigns.CampaignSnapshotV11, captured 2026-11-23. If this test fails,
    // CampaignSnapshotV11 gained or lost a property -- for a *gain*, that is very
    // likely the "widen CampaignSnapshotV11 to represent Stage 2+" change the owner's
    // ruling says not to make; confirm that is not what happened before updating this
    // list. A *loss* is far less likely to be the dangerous direction, but still
    // deserves the same scrutiny before this list is updated to match.
    // Values are PropertyInfo.PropertyType.ToString() exactly as reflection reports them
    // today -- captured directly, not hand-typed, to avoid a mismatched-format false
    // failure. Note EqualityContract (the compiler-generated record member) does not
    // appear: on this sealed record it is not public, so the BindingFlags.Public filter
    // below already excludes it, same as this list does.
    private static readonly (string Name, string TypeName)[] ExpectedProperties =
    [
        ("ContractVersion", "System.Int32"),
        ("CampaignId", "System.String"),
        ("StateVersion", "System.Int64"),
        ("RulesetHash", "System.String"),
        ("Setup", "Cna.Core.Campaigns.CampaignSetupSnapshotV6"),
        ("World", "Cna.Core.Campaigns.CampaignWorldSnapshotV6"),
        ("InitiativeHolder", "System.Nullable`1[Cna.Core.Rules.LandSide]"),
        ("OperationStageOrders", "System.Collections.Generic.IReadOnlyList`1[Cna.Core.Campaigns.CampaignOperationStageOrder]"),
        ("OperationStageWeather", "System.Collections.Generic.IReadOnlyList`1[Cna.Core.Campaigns.CampaignOperationStageWeather]"),
        ("RandomState", "Cna.Core.Randomness.RandomStreamState"),
        ("CurrentPosition", "Cna.Core.Campaigns.CampaignPositionV11"),
        ("ReactionWindow", "Cna.Core.Campaigns.CampaignReactionWindow"),
        ("BreakdownFlow", "Cna.Core.Campaigns.CampaignBreakdownFlow"),
    ];

    [Fact]
    public void CampaignSnapshotV11HasNotGainedOrLostAProperty()
    {
        var coreAssembly = typeof(CampaignExercises).Assembly;
        var type = coreAssembly.GetType("Cna.Core.Campaigns.CampaignSnapshotV11", throwOnError: true)!;

        var actual = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(p => (p.Name, TypeName: p.PropertyType.ToString()))
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .ToArray();

        var expected = ExpectedProperties
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected, actual);
    }
}
