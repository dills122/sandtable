using System.Globalization;
using System.Text;
using System.Text.Json;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Execution;

namespace Cna.ExerciseRunner.Tests.Artifacts;

public sealed class SeedLedgerCodecTests
{
    [Fact]
    public void StandaloneLedgerHasExactCanonicalVersionOneBytes()
    {
        var ledger = ExerciseSeedLedger.Create(
            ExerciseRunIdentity.Standalone("organization-boundary", 0));

        var bytes = SeedLedgerCodec.Serialize(ledger);

        Assert.Equal(GoldenLedger, Encoding.UTF8.GetString(bytes));
        var roundTrip = SeedLedgerCodec.Deserialize(bytes);
        Assert.Equal(6, roundTrip.Entries.Count);
        Assert.Equal(
            new[]
            {
                (ExerciseSeedDomain.Umpire, (ExerciseSeedRole?)null),
                (ExerciseSeedDomain.Controller, ExerciseSeedRole.System),
                (ExerciseSeedDomain.Controller, ExerciseSeedRole.Axis),
                (ExerciseSeedDomain.Controller, ExerciseSeedRole.Commonwealth),
                (ExerciseSeedDomain.ArtifactSampling, (ExerciseSeedRole?)null),
                (ExerciseSeedDomain.DiagnosticSampling, (ExerciseSeedRole?)null),
            },
            roundTrip.Entries.Select(entry => (entry.Domain, entry.Role)));
        Assert.Equal(bytes, SeedLedgerCodec.Serialize(roundTrip));
    }

    [Fact]
    public void ReaderRejectsExtraReorderedCorruptAndNoncanonicalEntries()
    {
        string[] invalid =
        [
            GoldenLedger.Replace("{\"contractVersion\":1,", "{\"extra\":true,\"contractVersion\":1,", StringComparison.Ordinal),
            GoldenLedger.Replace("\"contractVersion\":1,\"schemeId\"", "\"schemeId\":\"duplicate\",\"contractVersion\":1,\"schemeId\"", StringComparison.Ordinal),
            GoldenLedger.Replace("\"contractVersion\":1,\"schemeId\":\"sandtable.exercise-seeds.v1\"", "\"schemeId\":\"sandtable.exercise-seeds.v1\",\"contractVersion\":1", StringComparison.Ordinal),
            GoldenLedger.Replace("sha256:cebc", "sha256:debc", StringComparison.Ordinal),
            GoldenLedger.Replace("14897027430899522375", "14897027430899522374", StringComparison.Ordinal),
            GoldenLedger.Replace("\"domain\":\"umpire\",\"role\":null,\"canonicalMaterial\":{", "\"domain\":\"artifact-sampling\",\"role\":null,\"canonicalMaterial\":{", StringComparison.Ordinal),
            GoldenLedger.Replace("\"domain\":\"umpire\",\"role\":null}", "\"role\":null,\"domain\":\"umpire\"}", StringComparison.Ordinal),
        ];

        Assert.All(invalid, value => Assert.Throws<JsonException>(() =>
            SeedLedgerCodec.Deserialize(Encoding.UTF8.GetBytes(value))));
    }

    [Fact]
    public void CanonicalBytesAreIndependentOfCurrentCulture()
    {
        var ledger = ExerciseSeedLedger.Create(new ExerciseRunIdentity(
            ulong.MaxValue,
            "maneuver-alpha",
            7,
            "pair-a"));
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ar-SA");

            var first = SeedLedgerCodec.Serialize(ledger);
            var second = SeedLedgerCodec.Serialize(SeedLedgerCodec.Deserialize(first));

            Assert.Equal(first, second);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public void EveryDomainAndRoleHasASeparatedDigestAndDerivedSeed()
    {
        var entries = ExerciseSeedLedger.Create(new ExerciseRunIdentity(
            42,
            "maneuver-alpha",
            3,
            "pair-a")).Entries;

        Assert.Equal(entries.Count, entries.Select(entry => entry.Digest).Distinct().Count());
        Assert.Equal(entries.Count, entries.Select(entry => entry.DerivedSeed).Distinct().Count());
    }

    private static string GoldenLedger => File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "Artifacts", "Fixtures", "seed-ledger-v1.json"),
        Encoding.UTF8).TrimEnd('\n');
}
