using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;

namespace Cna.Core.Tests.Campaigns;

public sealed class P01OwnerVisibleCodecV1Tests
{
    [Fact]
    public void AllSixApprovedPrivateVectorsMatchExactCanonicalBytesAndReferences()
    {
        using var fixture = Fixture();
        var cases = fixture.RootElement.GetProperty("cases").EnumerateArray().ToArray();
        Assert.Equal(6, cases.Length);
        foreach (var row in cases)
        {
            var expected = row.GetProperty("expected");
            var decisionBytes = Encoding.ASCII.GetBytes(expected.GetProperty("decisionSeedCanonical").GetString()!);
            var aliasBytes = Encoding.ASCII.GetBytes(expected.GetProperty("aliasSeedCanonical").GetString()!);
            var decision = P01OwnerVisibleCodecV1.ReadDecision(decisionBytes);
            var alias = P01OwnerVisibleCodecV1.ReadAlias(aliasBytes);
            var target = new P01OwnerTargetSeedV1(decision.Context, row.GetProperty("ownerHex").GetString()!);
            var targetBytes = P01OwnerVisibleCodecV1.SerializeTarget(target);

            Assert.Equal(decisionBytes, P01OwnerVisibleCodecV1.SerializeDecision(decision));
            Assert.Equal(aliasBytes, P01OwnerVisibleCodecV1.SerializeAlias(alias));
            Assert.Equal(target, P01OwnerVisibleCodecV1.ReadTarget(targetBytes));
            Assert.Equal(expected.GetProperty("targetRef").GetString(),
                P01OwnerVisibleCodecV1.TargetRef(decision.Context, target.LocationId));
            Assert.Equal(expected.GetProperty("decisionRef").GetString(), P01OwnerVisibleCodecV1.DecisionRef(decision));
            Assert.Equal(expected.GetProperty("alias").GetString(),
                P01OwnerVisibleCodecV1.ValidateOpening(decision, alias, target.LocationId));
        }
    }

    [Fact]
    public void NoncanonicalAndDuplicateBytesRejectAtEverySeedBoundary()
    {
        var row = First();
        var decision = row.Decision;
        var alias = row.Alias;
        var target = P01OwnerVisibleCodecV1.SerializeTarget(new(row.Seed.Context, row.Hex));
        foreach (var altered in new[]
        {
            decision.Insert(0, " "), decision + " ", decision + "{}",
            decision.Replace("{\"context\":", "{\"context\":{},\"context\":", StringComparison.Ordinal),
            decision.Replace("{\"campaignId\":", "{\"campaignId\":\"bad\",\"campaignId\":", StringComparison.Ordinal),
            decision.Replace("{\"gameTurn\":", "{\"gameTurn\":2,\"gameTurn\":", StringComparison.Ordinal),
            decision.Replace("\"cycleRef\":", "\"unknown\":1,\"cycleRef\":", StringComparison.Ordinal),
            decision.Replace("\"context\":", "\"kind\":\"selection\",\"context\":", StringComparison.Ordinal),
            decision.Replace(",\"positionId\":", ", \"positionId\":", StringComparison.Ordinal),
            decision.Replace("\"openingRevision\":1", "\"openingRevision\":1.0", StringComparison.Ordinal),
            decision.Replace("\"openingRevision\":1", "\"openingRevision\":true", StringComparison.Ordinal),
            decision.Replace("\"openingRevision\":1", "\"openingRevision\":0", StringComparison.Ordinal),
            decision.Replace("\"kind\":\"selection\"", "\"kind\":\"close-assault-full\"", StringComparison.Ordinal),
            decision.Replace("\"campaignId\":\"campaign-demo\"", "\"campaignId\":\"bad\\nsource\"", StringComparison.Ordinal),
        })
            Assert.ThrowsAny<JsonException>(() => P01OwnerVisibleCodecV1.ReadDecision(Encoding.UTF8.GetBytes(altered)));

        Assert.ThrowsAny<JsonException>(() => P01OwnerVisibleCodecV1.ReadDecision(
            Encoding.UTF8.GetBytes(decision.Replace("campaign-demo", "caf\u00e9", StringComparison.Ordinal))));
        Assert.Throws<JsonException>(() => P01OwnerVisibleCodecV1.ReadDecision(new byte[65537]));
        Assert.Throws<JsonException>(() => P01OwnerVisibleCodecV1.ReadDecision([]));

        foreach (var altered in new[]
        {
            alias.Replace("{\"decisionRef\":", "{\"decisionRef\":\"bad\",\"decisionRef\":", StringComparison.Ordinal),
            alias.Replace(",\"openingRevision\":", ", \"openingRevision\":", StringComparison.Ordinal),
            alias.Replace("close-assault-full", "selection", StringComparison.Ordinal),
            alias.Replace("\"openingRevision\":1", "\"openingRevision\":0", StringComparison.Ordinal),
        })
            Assert.ThrowsAny<JsonException>(() => P01OwnerVisibleCodecV1.ReadAlias(Encoding.UTF8.GetBytes(altered)));

        var targetText = Encoding.ASCII.GetString(target);
        Assert.Throws<JsonException>(() => P01OwnerVisibleCodecV1.ReadTarget(Encoding.ASCII.GetBytes(
            targetText.Replace(",\"locationId\":", ",\"locationId\":\"bad\",\"locationId\":", StringComparison.Ordinal))));
        Assert.Throws<JsonException>(() => P01OwnerVisibleCodecV1.ReadTarget([.. target, (byte)'\n']));
    }

    [Fact]
    public void TypedBoundariesAndBindingRejectInvalidOrCrossSeatInputs()
    {
        var row = First();
        var decision = row.Seed;
        var alias = P01OwnerVisibleCodecV1.ReadAlias(Encoding.ASCII.GetBytes(row.Alias));
        Assert.Throws<JsonException>(() => P01OwnerVisibleCodecV1.DecisionRef(decision with { Kind = "close-assault-full" }));
        Assert.Throws<JsonException>(() => P01OwnerVisibleCodecV1.DecisionRef(decision with { OpeningRevision = 0 }));
        Assert.Throws<JsonException>(() => P01OwnerVisibleCodecV1.DecisionRef(decision with { CycleRef = "sha256:" + new string('G', 64) }));
        Assert.Throws<JsonException>(() => P01OwnerVisibleCodecV1.DecisionRef(decision with
        {
            Context = decision.Context with { Cycle = decision.Context.Cycle with { GameTurn = 112 } },
        }));
        Assert.Throws<JsonException>(() => P01OwnerVisibleCodecV1.DecisionRef(decision with
        {
            Context = decision.Context with { Cycle = decision.Context.Cycle with { Ordinal = 0 } },
        }));
        Assert.Throws<JsonException>(() => P01OwnerVisibleCodecV1.CandidateRef(alias with { CapabilityKind = "selection" }));
        Assert.Throws<JsonException>(() => P01OwnerVisibleCodecV1.ValidateOpening(decision, alias, "hex-0406"));
        Assert.Throws<JsonException>(() => P01OwnerVisibleCodecV1.ValidateOpening(decision,
            alias with { OwnParticipantRef = "pub." + new string('b', 64) }, row.Hex));
        Assert.Throws<JsonException>(() => P01OwnerVisibleCodecV1.ValidateOpening(decision with
        {
            Context = decision.Context with { Audience = "commonwealth" },
        }, alias, row.Hex));
    }

    private static (P01OwnerDecisionSeedV1 Seed, string Decision, string Alias, string Hex) First()
    {
        using var fixture = Fixture();
        var row = fixture.RootElement.GetProperty("cases")[0];
        var expected = row.GetProperty("expected");
        var decision = expected.GetProperty("decisionSeedCanonical").GetString()!;
        return (P01OwnerVisibleCodecV1.ReadDecision(Encoding.ASCII.GetBytes(decision)), decision,
            expected.GetProperty("aliasSeedCanonical").GetString()!, row.GetProperty("ownerHex").GetString()!);
    }

    private static JsonDocument Fixture() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(
        AppContext.BaseDirectory, "Campaigns", "Fixtures", "combat-p01-owner-decision-proposal-v1.json")));
}
