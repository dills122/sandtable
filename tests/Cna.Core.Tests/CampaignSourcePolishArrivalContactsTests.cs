using Cna.Core.Content;
using Cna.Core.Visibility;

namespace Cna.Core.Tests;

public sealed class CampaignSourcePolishArrivalContactsTests
{
    [Fact]
    public void TryCairoAdjacentMatchesTheHistoricalMapTranscriptionForEveryKeyedHex()
    {
        // These five hexes (Map E / Cairo, PDF p.5) are also present in
        // Cna1979HistoricalMap's sheet-lettered transcription (sheet E).
        // TryCairoAdjacent must agree with NeighborsOf exactly - there is no
        // second, independently-maintained copy of this geometry anymore.
        var keyed = new[] { "e1730", "e1829", "e1830", "e1930", "e1931" };
        foreach (var first in keyed)
        {
            var expectedNeighbors = Cna1979HistoricalMap.Instance.NeighborsOf(first);
            Assert.NotEmpty(expectedNeighbors);
            foreach (var second in expectedNeighbors.Concat(keyed).Distinct(StringComparer.Ordinal))
            {
                if (first == second) continue;
                var expected = expectedNeighbors.Contains(second, StringComparer.Ordinal);
                Assert.True(CampaignSourcePolishArrivalContacts.TryCairoAdjacent(first, second, out var forward));
                Assert.Equal(expected, forward);
            }
        }
    }

    [Fact]
    public void TryCairoAdjacentCannotResolveAForeignOrMalformedId()
    {
        // Neither side is a known arrival-adjacent hex, or the counterpart to
        // a known hex isn't a plausible Map E id - the function can't make a
        // definitive adjacency claim, so it reports "unresolved" (false), not
        // a false "definitely not adjacent".
        Assert.False(CampaignSourcePolishArrivalContacts.TryCairoAdjacent("e1730", "a1816", out var foreign));
        Assert.False(foreign);
        Assert.False(CampaignSourcePolishArrivalContacts.TryCairoAdjacent("e1730", "short", out var tooShort));
        Assert.False(tooShort);
        Assert.False(CampaignSourcePolishArrivalContacts.TryCairoAdjacent("a1816", "a1817", out _));
        Assert.True(CampaignSourcePolishArrivalContacts.TryCairoAdjacent("e1730", "e1730", out var self));
        Assert.True(self);
    }
}
