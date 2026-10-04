using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Rules;

// Isolate the allocation measurement from other tests evicting the last-turn cache.
[CollectionDefinition(nameof(LandSequenceCacheTests), DisableParallelization = true)]
public sealed class LandSequenceCacheTestGroup;

[Collection(nameof(LandSequenceCacheTests))]
public sealed class LandSequenceCacheTests
{
    [Theory]
    [InlineData(1, "a858da5acc6c1d1f2cef4cc4ce4cbda231ffd08ee8d0aa28b345efaa2d2605aa")]
    [InlineData(2, "3cdcab05c9d55bba1ee101fd65bc203b8bd4f2be35fa62b8cdf9f4f2980a9ef1")]
    [InlineData(6, "9ab7d2a023ec3be24ee3ee0570cc102fbfb33e2be6c4ade8911adebb82dccc78")]
    [InlineData(int.MaxValue, "5473d52cdbeba3c293c194463259c99aefbb4f889ffee49a852a8ef1afa63f64")]
    public void ColdWarmAndEvictedTurnsRetainBaselineBytes(int turn, string hash)
    {
        _ = Cna1979LandSequence.CreateTurn(7);
        var cold = Cna1979LandSequence.CreateTurn(turn);
        var warm = Cna1979LandSequence.CreateTurn(turn);
        _ = Cna1979LandSequence.CreateTurn(7);
        var evicted = Cna1979LandSequence.CreateTurn(turn);
        foreach (var positions in new[] { cold, warm, evicted })
            Assert.Equal(hash, Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(positions))));
    }

    [Fact]
    public void RepeatedSameTurnRequestsAvoidRebuildingCatalog()
    {
        _ = Cna1979LandSequence.CreateTurn(1);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 100; i++)
            _ = Cna1979LandSequence.CreateTurn(1);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // Allocation budget, not a machine-dependent wall-clock threshold.
        Assert.True(allocated < 1024, $"Repeated catalog reads allocated {allocated} bytes.");
    }

    [Fact]
    public void SharedCatalogAndNestedSourcesRejectCallerMutation()
    {
        var positions = Cna1979LandSequence.CreateTurn(1);
        var before = JsonSerializer.SerializeToUtf8Bytes(positions);
        var list = Assert.IsAssignableFrom<IList<LandSequencePosition>>(positions);
        Assert.Throws<NotSupportedException>(() => list[0] = positions[1]);
        Assert.Throws<NotSupportedException>(() => list.Clear());
        foreach (var position in positions)
        {
            var sources = Assert.IsAssignableFrom<IList<RuleReference>>(position.Sources);
            Assert.Throws<NotSupportedException>(() => sources[0] = new RuleReference("changed", "changed"));
        }
        Assert.Equal(before, JsonSerializer.SerializeToUtf8Bytes(Cna1979LandSequence.CreateTurn(1)));
    }

    [Fact]
    public void ConcurrentDifferentTurnsRetainTheirOwnCatalogAndSuccessors()
    {
        Parallel.For(0, 128, index =>
        {
            var turn = index % 4 == 0 ? int.MaxValue : index + 1;
            var positions = Cna1979LandSequence.CreateTurn(turn);
            Assert.All(positions, position => Assert.Equal(turn, position.GameTurn));
            Assert.Equal(positions[1], Cna1979LandSequence.GetNext(positions[0]));
            if (turn == int.MaxValue)
                Assert.Throws<OverflowException>(() => Cna1979LandSequence.GetNext(positions[^1]));
            else
                Assert.Equal(turn + 1, Cna1979LandSequence.GetNext(positions[^1]).GameTurn);
        });
    }

    [Fact]
    public void WarmCatalogStillRejectsInvalidTurnsAndForgedPositions()
    {
        var first = Cna1979LandSequence.CreateTurn(1)[0];
        Assert.Throws<ArgumentOutOfRangeException>(() => Cna1979LandSequence.CreateTurn(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Cna1979LandSequence.CreateTurn(-1));
        var forged = new LandSequencePosition(first.ContractVersion, first.PositionId, first.GameTurn,
            first.OperationStage, first.StageId, first.PhaseId, first.SegmentId, first.StepId,
            LandActorRole.FirstActingSide, null, first.Sources);
        Assert.Throws<ArgumentException>(() => Cna1979LandSequence.GetNext(forged));
    }
}
