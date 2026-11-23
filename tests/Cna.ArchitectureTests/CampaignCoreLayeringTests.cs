using Cna.Core.Exercises;
using NetArchTest.Rules;

namespace Cna.ArchitectureTests;

// This is the kind of rule NetArchTest.Rules actually fits well: dependency shape
// between layers, not a single type's member shape (see
// CampaignSnapshotV11SurfaceTests for why that one uses reflection instead).
//
// Cna.Core is the deterministic domain/rules engine; Cna.OrleansHost is the Orleans-
// hosted API/persistence layer built on top of it. The dependency must only ever run
// Host -> Core, never the reverse -- Core staying host-agnostic is what lets it be
// driven by the Exercise Harness, the real HTTP Host, or any future host the same way.
// There is no project reference from Cna.Core to Cna.OrleansHost today, so this would
// already fail to compile if violated directly; this test exists to keep that true
// explicitly and self-documentingly rather than relying on nobody adding one by
// accident, and to catch an indirect reference (e.g. via a shared contracts
// assembly that itself pulled in OrleansHost) that a missing ProjectReference alone
// would not catch.
public sealed class CampaignCoreLayeringTests
{
    [Fact]
    public void CoreHasNoDependencyOnTheOrleansHostLayer()
    {
        var coreAssembly = typeof(CampaignExercises).Assembly;

        var result = Types.InAssembly(coreAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("Cna.OrleansHost")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Cna.Core must not depend on Cna.OrleansHost: " +
            string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? []));
    }
}
