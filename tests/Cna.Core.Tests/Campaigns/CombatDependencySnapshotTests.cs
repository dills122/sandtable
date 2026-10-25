using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatDependencySnapshotTests
{
    private static readonly string[] Paths = ["first", "second"];
    private static byte[] Data(string revision, string path) => Encoding.UTF8.GetBytes(revision + "/" + path);
    private static string Hash(byte[] value) => Convert.ToHexStringLower(SHA256.HashData(value));
    private static Dictionary<string, IReadOnlyDictionary<string, string>> Revisions() =>
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal)
        {
            ["legacy"] = Paths.ToDictionary(p => p, p => Hash(Data("legacy", p)), StringComparer.Ordinal),
            ["refreshed"] = Paths.ToDictionary(p => p, p => Hash(Data("refreshed", p)), StringComparer.Ordinal)
        };

    [Theory]
    [InlineData("legacy")]
    [InlineData("refreshed")]
    public void CompleteRevisionOwnsBytesAndReturnsIndependentCopies(string revision)
    {
        var data = Paths.ToDictionary(p => p, p => Data(revision, p));
        var snapshot = CombatDependencySnapshot.Capture(p => data[p], Revisions(), 1024, "rejected");
        Assert.Equal(revision, snapshot.Revision);
        data["first"][0] ^= 1;
        var returned = snapshot.Read("first");
        returned[0] ^= 1;
        Assert.Equal(Data(revision, "first"), snapshot.Read("first"));
        Assert.Equal(Hash(Data(revision, "first")), snapshot.HashFor("first"));
    }

    [Fact]
    public void IndividuallyRegisteredBytesFromDifferentRevisionsAreRejected()
    {
        Assert.Throws<JsonException>(() => CombatDependencySnapshot.Capture(
            p => Data(p == "first" ? "legacy" : "refreshed", p), Revisions(), 1024, "rejected"));
    }

    [Theory]
    [InlineData("legacy")]
    [InlineData("refreshed")]
    public void ConsumptionRevalidationTraversesParentsAndOwnsCurrentBytes(string revision)
    {
        var data = Paths.ToDictionary(p => p, p => Data(revision, p));
        var calls = new List<string>();
        var missing = false;
        byte[] Read(string path)
        {
            calls.Add(path);
            if (missing) throw new FileNotFoundException(path);
            return data[path];
        }
        var parent = CombatDependencySnapshot.Capture(Read, Revisions(), 1024, "parent");
        var child = CombatDependencySnapshot.Capture(parent.Read, Revisions(), 1024, "child");
        var grandchild = CombatDependencySnapshot.Capture(child.Read, Revisions(), 1024, "consumer");
        calls.Clear();
        var owned = grandchild.ReadRevalidated("first");
        Assert.Equal("first", Assert.Single(calls));
        Assert.Equal(Data(revision, "first"), owned);
        Assert.NotSame(data["first"], owned);
        owned[0] ^= 1;
        Assert.Equal(Data(revision, "first"), grandchild.ReadRevalidated("first"));
        data["first"][0] ^= 1;
        Assert.Equal("consumer", Assert.Throws<JsonException>(() => grandchild.ReadRevalidated("first")).Message);
        Assert.Equal(Data(revision, "first"), grandchild.Read("first"));
        data["first"] = Data(revision, "first");
        missing = true;
        Assert.Equal("consumer", Assert.Throws<JsonException>(() => grandchild.ReadRevalidated("first")).Message);
        calls.Clear();
        Assert.Throws<JsonException>(() => grandchild.ReadRevalidated("unknown"));
        Assert.Empty(calls);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("missing")]
    [InlineData("oversized")]
    [InlineData("null")]
    public void InvalidAcquisitionRejectsBeforeExposingSnapshot(string fault)
    {
        byte[] Read(string path) => fault switch
        {
            "missing" => throw new IOException("missing"),
            "oversized" => new byte[1025],
            "null" => null!,
            _ => Data("unknown", path)
        };
        var error = Assert.Throws<JsonException>(() => CombatDependencySnapshot.Capture(Read, Revisions(), 1024, "rejected"));
        Assert.Equal("rejected", error.Message);
    }

    [Fact]
    public void ExplicitRevisionCannotFallBackToAnotherRegisteredRevision()
    {
        Assert.Throws<JsonException>(() => CombatDependencySnapshot.Capture(
            p => Data("legacy", p), Revisions(), 1024, "rejected", "refreshed"));
        Assert.Throws<JsonException>(() => CombatDependencySnapshot.Capture(
            p => Data("legacy", p), Revisions(), 1024, "rejected", "unknown"));
    }

    [Fact]
    public void NestedAcquisitionPreservesRootRevisionForIdenticalSubset()
    {
        var data = Data("shared", "first");
        var revisions = new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["legacy"] = new Dictionary<string, string> { ["first"] = Hash(data) },
            ["refreshed"] = new Dictionary<string, string> { ["first"] = Hash(data) }
        };
        var parent = CombatDependencySnapshot.Capture(_ => data, revisions, 1024, "rejected", "refreshed");
        var child = CombatDependencySnapshot.Capture(parent.Read, revisions, 1024, "rejected");
        Assert.Equal("refreshed", child.Revision);
        Assert.Throws<JsonException>(() => CombatDependencySnapshot.Capture(parent.Read, revisions, 1024, "rejected", "legacy"));
    }

    [Fact]
    public void CallerCannotChangeRegistrationDuringAcquisition()
    {
        var legacy = Paths.ToDictionary(p => p, p => Hash(Data("legacy", p)));
        var revisions = new Dictionary<string, IReadOnlyDictionary<string, string>> { ["legacy"] = legacy };
        Assert.Throws<JsonException>(() => CombatDependencySnapshot.Capture(p =>
        {
            legacy[p] = Hash(Data("forged", p));
            return Data("forged", p);
        }, revisions, 1024, "rejected"));
    }

    [Fact]
    public async Task ConcurrentRevisionsNeverShareSelectionState()
    {
        await Task.WhenAll(Enumerable.Range(0, 32).Select(i => Task.Run(() =>
        {
            var revision = i % 2 == 0 ? "legacy" : "refreshed";
            var snapshot = CombatDependencySnapshot.Capture(p => Data(revision, p), Revisions(), 1024, "rejected");
            Assert.Equal(revision, snapshot.Revision);
            Assert.Equal(Data(revision, "second"), snapshot.Read("second"));
        })));
    }
}
