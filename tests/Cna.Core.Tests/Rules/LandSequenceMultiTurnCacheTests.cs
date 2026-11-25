using Cna.Core.Rules;

namespace Cna.Core.Tests.Rules;

public sealed class LandSequenceMultiTurnCacheTests
{
    [Fact]
    public void AlternatingTurnsAndSuccessorLookupsAllocateNoCatalogs()
    {
        var first = Cna1979LandSequence.CreateTurn(1);
        var second = Cna1979LandSequence.CreateTurn(2);
        _ = Cna1979LandSequence.GetNext(first[0]);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            var positions = Cna1979LandSequence.CreateTurn(i % 2 + 1);
            if (!ReferenceEquals(positions, i % 2 == 0 ? first : second))
                Assert.Fail("Mixed-turn cache rebuilt a catalog.");
            if (!ReferenceEquals(Cna1979LandSequence.GetNext(positions[0]), positions[1]))
                Assert.Fail("Successor differs from cached catalog.");
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.True(allocated < 1024, $"Mixed-turn lookups allocated {allocated} bytes.");
    }
}
