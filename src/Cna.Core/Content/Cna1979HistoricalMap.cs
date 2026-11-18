using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Cna.Core.Content;

internal sealed record Cna1979HistoricalEdge(IReadOnlyList<string> Features, string? HigherHex);
internal sealed record Cna1979HistoricalHex(string Terrain, string? Settlement, bool Coastal);

// Immutable transcription, not a certification of every rule or section seam.
// IDs retain the printed sheet and row/column; unknown and missing are distinct
// from clear terrain and unmarked hexsides.
internal sealed class Cna1979HistoricalMap
{
    internal const string Revision = "c8769224721feb252cef530a2099cb44efbd6f28";
    internal const string HexHash = "ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568";
    internal const string EdgeHash = "124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c";
    internal const string CdSeamPolicyId = "graziani.cd-seam.v1";
    internal const string CdSeamPolicyHash = "sha256:e997623ecbe55d5cc8e5655061e5fc961ee9f7006df5cc383038b2286e7cc622";
    private const string CdSeamCanonical = "graziani.cd-seam.v1\nc3533|d3501|track\nc3633|d3601|unfinished-railroad\nc4033|d3901|track\nsource=sha256:8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030\n";
    private readonly Dictionary<string, Cna1979HistoricalHex> hexes;
    private readonly Dictionary<(string, string), Cna1979HistoricalEdge> edges;
    private readonly bool cdSeamVerified;

    private static readonly Lazy<Cna1979HistoricalMap> Shared = new(Load);
    private static readonly Cna1979HistoricalEdge CdTrack = new(Array.AsReadOnly(new[] { "track" }), null);
    private static readonly Cna1979HistoricalEdge CdUnfinishedRail = new(Array.AsReadOnly(new[] { "unfinished-railroad" }), null);
    internal static Cna1979HistoricalMap Instance => Shared.Value;
    internal int HexCount => hexes.Count;

    private Cna1979HistoricalMap(Dictionary<string, Cna1979HistoricalHex> hexes, Dictionary<(string, string), Cna1979HistoricalEdge> edges,
        bool cdSeamVerified)
    {
        this.hexes = hexes;
        this.edges = edges;
        this.cdSeamVerified = cdSeamVerified;
    }

    internal Cna1979HistoricalHex? HexAt(string id) => hexes.GetValueOrDefault(Normalize(id));
    internal string? TerrainAt(string id) => HexAt(id)?.Terrain;

    // Same-sheet neighbors only, using the chart's own odd-row stagger (see
    // SameSheetAdjacent). Cross-sheet seam neighbors are not enumerable from
    // this transcription alone and are never included here.
    internal IReadOnlyList<string> NeighborsOf(string id)
    {
        var normalized = Normalize(id);
        if (normalized.Length != 5 || !hexes.ContainsKey(normalized)) return Array.Empty<string>();
        var section = normalized[0];
        var row = int.Parse(normalized.AsSpan(1, 2), System.Globalization.CultureInfo.InvariantCulture);
        var column = int.Parse(normalized.AsSpan(3, 2), System.Globalization.CultureInfo.InvariantCulture);
        var columnShift = row % 2 == 1 ? -1 : 1;
        Span<(int Row, int Column)> candidates = stackalloc (int, int)[]
        {
            (row, column - 1), (row, column + 1),
            (row - 1, column), (row - 1, column + columnShift),
            (row + 1, column), (row + 1, column + columnShift),
        };
        var result = new List<string>(6);
        foreach (var (candidateRow, candidateColumn) in candidates)
        {
            // Columns are 1-indexed on sheets A-E, but Malta's sheet (M) is
            // transcribed 0-indexed (M0500..M1306 use column 00). Column 0 is
            // therefore a valid candidate in general; only negative columns
            // (and negative/zero/over-range rows, which no sheet uses) are
            // truly out of range. The hexes.ContainsKey check below still
            // rejects any candidate, including column 00, that isn't an
            // actual hex on this id's own sheet.
            if (candidateRow is < 1 or > 99 || candidateColumn is < 0 or > 99) continue;
            var candidate = $"{section}{candidateRow:D2}{candidateColumn:D2}";
            // Every other location id in this engine is lowercase; this
            // transcription's own storage is uppercase (Normalize), so the
            // boundary is crossed here, not left for callers to notice.
            if (hexes.ContainsKey(candidate)) result.Add(candidate.ToLowerInvariant());
        }
        return Array.AsReadOnly(result.ToArray());
    }

    internal static void RequireCdSeamPolicy()
    {
        var hash = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(CdSeamCanonical)));
        if (hash != CdSeamPolicyHash) throw new InvalidOperationException("C/D seam policy identity changed.");
    }

    internal static bool IsCdSeamCrossing(string first, string second) =>
        (string.Equals(first, "c3533", StringComparison.OrdinalIgnoreCase) && string.Equals(second, "d3501", StringComparison.OrdinalIgnoreCase))
        || (string.Equals(first, "d3501", StringComparison.OrdinalIgnoreCase) && string.Equals(second, "c3533", StringComparison.OrdinalIgnoreCase))
        || (string.Equals(first, "c3633", StringComparison.OrdinalIgnoreCase) && string.Equals(second, "d3601", StringComparison.OrdinalIgnoreCase))
        || (string.Equals(first, "d3601", StringComparison.OrdinalIgnoreCase) && string.Equals(second, "c3633", StringComparison.OrdinalIgnoreCase))
        || (string.Equals(first, "c4033", StringComparison.OrdinalIgnoreCase) && string.Equals(second, "d3901", StringComparison.OrdinalIgnoreCase))
        || (string.Equals(first, "d3901", StringComparison.OrdinalIgnoreCase) && string.Equals(second, "c4033", StringComparison.OrdinalIgnoreCase));

    // Null = unknown, cross-sheet or non-adjacent. Empty = adjacent with no
    // recorded feature, according to this transcription only.
    internal IReadOnlyList<string>? FeaturesBetween(string first, string second, bool useCdSeam = false)
        => EdgeBetween(first, second, useCdSeam)?.Features;

    internal Cna1979HistoricalEdge? EdgeBetween(string first, string second, bool useCdSeam = false)
    {
        var a = Normalize(first);
        var b = Normalize(second);
        if (useCdSeam && IsCdSeamCrossing(a, b))
        {
            RequireCdSeamPolicy();
            if (!cdSeamVerified) throw new InvalidOperationException("Verified C/D seam boundaries differ from the pinned map.");
            return a == "C3633" || b == "C3633" ? CdUnfinishedRail : CdTrack;
        }
        if (!hexes.ContainsKey(a) || !hexes.ContainsKey(b) || !SameSheetAdjacent(a, b)) return null;
        var key = string.CompareOrdinal(a, b) < 0 ? (a, b) : (b, a);
        return edges.GetValueOrDefault(key) ?? new Cna1979HistoricalEdge(Array.Empty<string>(), null);
    }

    private static bool SameSheetAdjacent(string a, string b)
    {
        if (a.Length != 5 || b.Length != 5 || a[0] != b[0]) return false;
        var ar = int.Parse(a.AsSpan(1, 2), System.Globalization.CultureInfo.InvariantCulture);
        var ac = int.Parse(a.AsSpan(3, 2), System.Globalization.CultureInfo.InvariantCulture);
        var br = int.Parse(b.AsSpan(1, 2), System.Globalization.CultureInfo.InvariantCulture);
        var bc = int.Parse(b.AsSpan(3, 2), System.Globalization.CultureInfo.InvariantCulture);
        if (ar == br) return Math.Abs(ac - bc) == 1;
        if (Math.Abs(ar - br) != 1) return false;
        return ar % 2 == 1 ? bc == ac || bc == ac - 1 : bc == ac || bc == ac + 1;
    }

    private static string Normalize(string id) => (id ?? throw new ArgumentNullException(nameof(id))).ToUpperInvariant();

    private static Cna1979HistoricalMap Load()
    {
        using var hexDocument = Read("hexes", HexHash);
        using var edgeDocument = Read("hexsides", EdgeHash);
        var hexes = new Dictionary<string, Cna1979HistoricalHex>(StringComparer.Ordinal);
        foreach (var item in hexDocument.RootElement.GetProperty("hexes").EnumerateArray())
            hexes.Add(item.GetProperty("id").GetString()!, new Cna1979HistoricalHex(
                item.GetProperty("terrain").GetString()!, item.GetProperty("settlement").GetString(),
                item.GetProperty("coastal").GetBoolean()));
        var edges = new Dictionary<(string, string), Cna1979HistoricalEdge>();
        var cdSides = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in edgeDocument.RootElement.GetProperty("hexsides").EnumerateArray())
        {
            if (item.GetProperty("b").ValueKind == JsonValueKind.Null)
            {
                var edgeId = item.GetProperty("a").GetString() + "/" + item.GetProperty("side").GetString();
                if (edgeId is "C3533/E" or "D3501/W" or "C4033/SE" or "D3901/NW" or "C3633/E" or "D3601/W")
                {
                    var features = item.GetProperty("features").EnumerateArray().Select(value => value.GetString()).ToArray();
                    var expected = edgeId is "C3633/E" or "D3601/W" ? "unfinished-railroad" : "track";
                    if (features.Length == 1 && features[0] == expected && item.GetProperty("up").ValueKind == JsonValueKind.Null)
                        cdSides.Add(edgeId);
                }
                continue;
            }
            var a = item.GetProperty("a").GetString()!;
            var b = item.GetProperty("b").GetString()!;
            if (!hexes.ContainsKey(a) || !hexes.ContainsKey(b) || !SameSheetAdjacent(a, b))
                throw new InvalidOperationException("Historical map contains an invalid hexside.");
            var key = string.CompareOrdinal(a, b) < 0 ? (a, b) : (b, a);
            var higher = item.GetProperty("up").GetString();
            if (higher is not null && higher != a && higher != b)
                throw new InvalidOperationException("Historical map has an invalid higher hex.");
            edges.Add(key, new Cna1979HistoricalEdge(Array.AsReadOnly(item.GetProperty("features")
                .EnumerateArray().Select(value => value.GetString()!).ToArray()), higher));
        }
        if (hexes.Count != 7073) throw new InvalidOperationException("Historical map coverage changed.");
        return new(hexes, edges, cdSides.Count == 6 && hexes.ContainsKey("C3533") && hexes.ContainsKey("D3501")
            && hexes.ContainsKey("C4033") && hexes.ContainsKey("D3901") && hexes.ContainsKey("C3633") && hexes.ContainsKey("D3601"));
    }

    private static JsonDocument Read(string name, string expectedHash)
    {
        using var stream = typeof(Cna1979HistoricalMap).Assembly.GetManifestResourceStream(
            $"Cna.Core.Content.HistoricalMap.{name}.v1.json")
            ?? throw new InvalidOperationException("Historical map resource is missing.");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        var data = bytes.ToArray();
        return ParseVerified(data, expectedHash);
    }

    internal static JsonDocument ParseVerified(byte[] data, string expectedHash)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(expectedHash);
        if (!Convert.ToHexStringLower(SHA256.HashData(data)).Equals(expectedHash, StringComparison.Ordinal))
            throw new InvalidOperationException("Historical map resource hash changed.");
        return JsonDocument.Parse(data);
    }
}
