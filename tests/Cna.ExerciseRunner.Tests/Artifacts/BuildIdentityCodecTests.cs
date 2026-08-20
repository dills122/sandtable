using System.Text;
using System.Text.Json;
using Cna.Core.Rules;
using Cna.ExerciseRunner.Artifacts;

namespace Cna.ExerciseRunner.Tests.Artifacts;

public sealed class BuildIdentityCodecTests
{
    [Fact]
    public void IdentityHasExactCanonicalVersionOneBytesAndStrictRoundTrip()
    {
        var identity = new BuildIdentity(
            ExerciseBuildMode.Baseline,
            "1111111111111111111111111111111111111111",
            "2222222222222222222222222222222222222222",
            false,
            Hash('0'),
            ".NET 10.0.11",
            "arm64",
            "arm64",
            Cna1979Ruleset.Manifest.Hash,
            Hash('b'),
            Hash('c'),
            "sandtable.exercise-seeds.v1",
            true,
            true,
            [new BuildArtifactIdentity("runner.dll", 12, Hash('d'))]);

        var bytes = BuildIdentityCodec.Serialize(identity);

        Assert.Equal(
            $"{{\"contractVersion\":1,\"schemeId\":\"sandtable.exercise-build-identity.v1\",\"buildMode\":\"baseline\",\"headCommit\":\"1111111111111111111111111111111111111111\",\"headTree\":\"2222222222222222222222222222222222222222\",\"dirty\":false,\"porcelainSha256\":\"{Hash('0')}\",\"frameworkDescription\":\".NET 10.0.11\",\"osArchitecture\":\"arm64\",\"processArchitecture\":\"arm64\",\"rulesetHash\":\"{Cna1979Ruleset.Manifest.Hash}\",\"configurationHash\":\"{Hash('b')}\",\"manifestHash\":\"{Hash('c')}\",\"seedSchemeId\":\"sandtable.exercise-seeds.v1\",\"baselineEligible\":true,\"reproducible\":true,\"artifacts\":[{{\"name\":\"runner.dll\",\"sizeBytes\":12,\"sha256\":\"{Hash('d')}\"}}]}}",
            Encoding.UTF8.GetString(bytes));
        Assert.Equal(bytes, BuildIdentityCodec.Serialize(BuildIdentityCodec.Deserialize(bytes)));

        var json = Encoding.UTF8.GetString(bytes);
        string[] invalid =
        [
            json.Replace("{\"contractVersion\":1,", "{\"extra\":true,\"contractVersion\":1,", StringComparison.Ordinal),
            json.Replace("\"contractVersion\":1", "\"contractVersion\":99", StringComparison.Ordinal),
            json.Replace("\"baselineEligible\":true", "\"baselineEligible\":false", StringComparison.Ordinal),
            json.Replace("\"buildMode\":\"baseline\"", "\"buildMode\":\"unknown\"", StringComparison.Ordinal),
        ];
        Assert.All(invalid, value => Assert.Throws<JsonException>(() =>
            BuildIdentityCodec.Deserialize(Encoding.UTF8.GetBytes(value))));
    }

    private static string Hash(char value) => $"sha256:{new string(value, 64)}";
}
