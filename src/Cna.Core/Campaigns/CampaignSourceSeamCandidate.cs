using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;

namespace Cna.Core.Campaigns;

internal enum CampaignSourceSeamCertainty { Unknown, ProvisionalNear, ProvisionalFar }
internal readonly record struct CampaignSourceSeamAssessment(CampaignSourceSeamCertainty Certainty, int? CandidateDistance, bool PublishedDistanceConflict);
internal readonly record struct CampaignSourceSeamOutlier(string First, string Second, int PrintedDistance, int CandidateDistance);

// A source-linked hypothesis, never an alias, terrain or adjacency certificate.
// Land 4.1's 39xx/01xx wording remains unreconciled with §37.42's 33-hex
// statement. Callers may use a near result only to veto a separation proof.
internal static class CampaignSourceSeamCandidate
{
    internal const string PolicyId = "graziani.source-seam-33-column-candidate.v1";
    internal const string ContentHash = "sha256:87f2a62c9034c2a683bef169f5879e65283fb4eab06b5955dfa7cbe17fd43d74";
    internal const string MapSourceHash = "sha256:8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030";
    internal const string LandRulesSourceHash = "sha256:b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99";
    internal const string AirRulesSourceHash = "sha256:9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4";
    private static readonly string[] ChartLocations =
    ["a2703", "a1816", "a4827", "a2629", "a4130", "b5504", "b3715", "b4921", "b5925", "c2803",
     "c4807", "c1014", "c3019", "c4321", "d2903", "d3714", "d3516", "e3613", "e2418", "e1829"];
    // Lower triangle, copied from the original Air/Logistics §37.42 chart.
    private static readonly string[] ChartRows =
    ["", "18", "35 30", "27 17 22", "34 25 7 15", "48 39 13 29 14", "50 41 26 24 20 20",
     "62 53 27 36 28 20 12", "71 61 36 45 37 23 22 10", "67 57 52 41 46 46 26 26 31",
     "81 71 46 55 47 40 31 20 21 20", "86 68 72 59 66 66 46 46 49 20 38",
     "84 75 67 58 61 61 41 41 42 17 21 20", "92 83 62 67 59 56 42 36 37 25 16 33 13",
     "100 91 84 74 77 80 58 58 59 33 38 31 17 22", "115 106 91 89 85 85 65 65 66 48 45 46 31 29 15",
     "116 107 94 90 88 88 68 68 69 49 48 47 32 32 16 3", "147 138 124 121 118 118 98 98 99 79 78 78 63 62 47 33 31",
     "149 137 135 122 130 129 109 109 110 83 89 76 68 73 51 44 41 12", "163 145 149 136 143 143 123 123 124 98 103 85 82 87 65 58 55 25 14"];
    private static readonly System.Collections.ObjectModel.ReadOnlyCollection<CampaignSourceSeamOutlier> Conflicts = BuildOutliers();
    private static readonly bool Verified = Verify();
    internal static IReadOnlyList<CampaignSourceSeamOutlier> PublishedOutliers => Conflicts;

    // Exact raw chart identifiers only; this does not normalize map aliases.
    internal static bool TryPublishedDistance(string first, string second, out int distance)
    {
        distance = 0;
        var firstIndex = Array.IndexOf(ChartLocations, first);
        var secondIndex = Array.IndexOf(ChartLocations, second);
        if (firstIndex < 0 || secondIndex < 0 || firstIndex == secondIndex) return false;
        var row = Math.Max(firstIndex, secondIndex);
        var column = Math.Min(firstIndex, secondIndex);
        var values = ChartRows[row].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        distance = int.Parse(values[column], CultureInfo.InvariantCulture);
        return true;
    }

    internal static CampaignSourceSeamAssessment Classify(string first, string second)
    {
        _ = Verified;
        if (!TryPoint(first, out var section1, out var row1, out var column1)
            || !TryPoint(second, out var section2, out var row2, out var column2) || section1 == section2)
            return new(CampaignSourceSeamCertainty.Unknown, null, false);
        var x = row2 - row1;
        var y = (column2 + 33 * (section2 - 'a') - (row2 + (row2 & 1)) / 2)
              - (column1 + 33 * (section1 - 'a') - (row1 + (row1 & 1)) / 2);
        var distance = Math.Max(Math.Abs(x), Math.Max(Math.Abs(y), Math.Abs(x + y)));
        var conflict = Conflicts.Any(value => value.First == first && value.Second == second || value.First == second && value.Second == first);
        return new(distance <= 3 ? CampaignSourceSeamCertainty.ProvisionalNear : CampaignSourceSeamCertainty.ProvisionalFar, distance, conflict);
    }
    private static bool TryPoint(string id, out char section, out int row, out int column)
    {
        section = '\0'; row = 0; column = 0;
        if (id is null || id.Length != 5 || id[0] is < 'a' or > 'e'
            || !int.TryParse(id.AsSpan(1, 2), NumberStyles.None, CultureInfo.InvariantCulture, out row)
            || !int.TryParse(id.AsSpan(3, 2), NumberStyles.None, CultureInfo.InvariantCulture, out column)
            || row == 0 || column == 0) return false;
        section = id[0]; return true;
    }
    private static System.Collections.ObjectModel.ReadOnlyCollection<CampaignSourceSeamOutlier> BuildOutliers()
    {
        var result = new List<CampaignSourceSeamOutlier>();
        for (var i = 0; i < ChartLocations.Length; i++)
        {
            var printed = ChartRows[i].Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(value => int.Parse(value, CultureInfo.InvariantCulture)).ToArray();
            if (printed.Length != i) throw new InvalidOperationException("Printed distance triangle is incomplete.");
            for (var j = 0; j < i; j++)
            {
                var first = ChartLocations[i]; var second = ChartLocations[j];
                var distance = CandidateDistance(first, second);
                if (distance != printed[j]) result.Add(new(first, second, printed[j], distance));
            }
        }
        return result.AsReadOnly();
    }
    private static int CandidateDistance(string first, string second)
    {
        _ = TryPoint(first, out var s1, out var r1, out var c1); _ = TryPoint(second, out var s2, out var r2, out var c2);
        var x = r2 - r1; var y = c2 + 33 * (s2 - 'a') - (r2 + (r2 & 1)) / 2 - c1 - 33 * (s1 - 'a') + (r1 + (r1 & 1)) / 2;
        return Math.Max(Math.Abs(x), Math.Max(Math.Abs(y), Math.Abs(x + y)));
    }
    internal static byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 1); writer.WriteString("policyId", PolicyId);
            writer.WriteString("mapSourceHash", MapSourceHash); writer.WriteString("landRulesSourceHash", LandRulesSourceHash);
            writer.WriteString("airRulesSourceHash", AirRulesSourceHash); writer.WriteString("sourceConflict", "land-4.1-39xx-versus-air-37.42-33-hexes");
            writer.WriteString("certainty", "provisional-never-certifies-alias-adjacency-terrain-or-route");
            writer.WriteNumber("sectionColumnOffset", 33); writer.WriteString("grid", "odd-row-stagger");
            writer.WriteNumber("candidateNearVetoDistance", 3); writer.WriteNumber("printedPairs", 190); writer.WriteNumber("matchingPairs", 179);
            writer.WriteStartArray("chart"); for (var i = 0; i < ChartLocations.Length; i++)
            { writer.WriteStartObject(); writer.WriteString("location", ChartLocations[i]); writer.WriteString("lowerTriangle", ChartRows[i]); writer.WriteEndObject(); }
            writer.WriteEndArray(); writer.WriteStartArray("outliers"); foreach (var value in Conflicts)
            { writer.WriteStartObject(); writer.WriteString("first", value.First); writer.WriteString("second", value.Second); writer.WriteNumber("printed", value.PrintedDistance); writer.WriteNumber("candidate", value.CandidateDistance); writer.WriteEndObject(); }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal static string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
    private static bool Verify() => Hash() == ContentHash && Conflicts.Count == 11
        ? true : throw new InvalidOperationException("Seam candidate differs from its pinned source profile.");
}
