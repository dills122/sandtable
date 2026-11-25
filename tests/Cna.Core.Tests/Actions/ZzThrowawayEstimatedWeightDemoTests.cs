namespace Cna.Core.Tests.Actions;

public sealed class ZzThrowawayEstimatedWeightDemoTests
{
    [Fact]
    public void BrandNewTestNeedsNoManifestEntry() => Assert.Equal(4, 2 + 2);

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void BrandNewTheoryRowsNeedNoManifestEntry(int value) => Assert.True(value > 0);
}
