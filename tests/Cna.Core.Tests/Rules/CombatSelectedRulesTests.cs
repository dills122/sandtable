using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Rules;

public sealed class CombatSelectedRulesTests
{
    private static readonly int[] AmendmentCoordinates = [34, 35, 36];
    private static readonly int[] DifferentialWeights = [1, 68, 1158, 68, 1];
    private static readonly int[] CaptureShares = [10, 25, 33, 50, 50, 75];
    private static readonly JsonSerializerOptions DefinitionJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter<CombatRole>(JsonNamingPolicy.CamelCase) },
    };

    [Fact]
    public void AllThirtySixMoraleCoordinatesMatchIndependentExpectedRow()
    {
        int[] expected =
        [
            1, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, -1,
        ];
        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], Cna1979CombatAdjudication.LookupMoraleAdjustment(
                (i / 6 + 1) * 10 + i % 6 + 1));
        }
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(17)]
    [InlineData(18)]
    [InlineData(20)]
    [InlineData(67)]
    [InlineData(111)]
    [InlineData(int.MaxValue)]
    public void NonDiceMoraleCoordinatesReject(int coordinate) =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Cna1979CombatAdjudication.LookupMoraleAdjustment(coordinate));

    [Fact]
    public void CompleteTypedDefinitionMatchesEveryFrozenMetadataAndTableField()
    {
        using var fixture = ReadFixture("combat-rules-inputs-v1.json");
        var golden = fixture.RootElement.GetProperty("goldens").EnumerateArray()
            .Single(value => value.GetProperty("kind").GetString() == "RulesInput");
        var actual = System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(
            Cna1979CombatAdjudication.Definition, DefinitionJsonOptions));
        var expected = System.Text.Json.Nodes.JsonNode.Parse(golden.GetProperty("canonicalUtf8").GetString()!);
        Assert.True(System.Text.Json.Nodes.JsonNode.DeepEquals(expected, actual));
        Assert.Equal("sha256:fafb24792c9e3f774c368f85c02d1d068f84c9c78e67bf8324723257d0f13029",
            Cna1979CombatAdjudication.ContentHash);
        Assert.Equal(30395, golden.GetProperty("byteCount").GetInt32());
        Assert.Equal(9, Cna1979Ruleset.ContractVersion);
        Assert.DoesNotContain(Cna1979Ruleset.Manifest.Artifacts,
            value => value.ArtifactId == Cna1979CombatAdjudication.ArtifactId);
    }

    [Fact]
    public void AllLossCellsMatchIndependentOpticalValuesWithOnlyThreeAmendedGaps()
    {
        using var source = ReadFixture("combat-selected-source-v1.json");
        var count = 0;
        var amendments = 0;
        foreach (var role in Enum.GetValues<CombatRole>())
        {
            for (var differential = -2; differential <= 2; differential++)
            {
                var values = source.RootElement.GetProperty("optical_loss_percent_by_coordinate")
                    .GetProperty($"{RoleName(role)}:{differential}");
                foreach (var coordinate in Coordinates())
                {
                    var value = values[CoordinateIndex(coordinate)];
                    if (value.ValueKind == JsonValueKind.Null)
                    {
                        Assert.Equal(CombatRole.Defender, role);
                        Assert.Equal(2, differential);
                        Assert.Contains(coordinate, AmendmentCoordinates);
                        amendments++;
                    }
                    Assert.Equal(value.ValueKind == JsonValueKind.Null ? 10 : value.GetInt32(),
                        Cna1979CombatAdjudication.LookupLossPercent(role, differential, coordinate));
                    count++;
                }
            }
        }
        Assert.Equal(360, count);
        Assert.Equal(3, amendments);
        Assert.Equal(15, Cna1979CombatAdjudication.LookupLossPercent(CombatRole.Defender, 0, 23));
        Assert.Equal(10, Cna1979CombatAdjudication.LookupLossPercent(CombatRole.Defender, 1, 23));
    }

    [Fact]
    public void EveryMoralePairAndReachableAssaultCaptureRefusalPathMatchesIndependentSource()
    {
        using var source = ReadFixture("combat-selected-source-v1.json");
        var data = source.RootElement;
        var weights = new int[5];
        foreach (var attacker in Coordinates())
        {
            foreach (var defender in Coordinates())
            {
                var expected = ExpectedMorale(attacker) - ExpectedMorale(defender);
                Assert.Equal(expected, Cna1979CombatAdjudication.CalculateDifferential(attacker, defender));
                weights[expected + 2]++;
            }
        }
        Assert.Equal(DifferentialWeights, weights);
        Assert.Equal(1296, weights.Sum());
        var joint = 0;
        var rows = 0;
        var weightedCapturePaths = 0;
        for (var differential = -2; differential <= 2; differential++)
        {
            var (am, dm) = MoralePair(differential);
            foreach (var ac in Coordinates())
            {
                foreach (var dc in Coordinates())
                {
                    joint++;
                    var engaged = Includes(data, "attacker_engaged_sums", differential, Sum(ac));
                    var retreat = Includes(data, "defender_retreat_one_hex_sums", differential, Sum(dc));
                    var attackerCapture = Includes(data, "attacker_capture_sums", differential, Sum(ac));
                    var defenderCapture = Includes(data, "defender_capture_sums", differential, Sum(dc));
                    Assert.False(attackerCapture && defenderCapture);
                    var capture = attackerCapture || defenderCapture;
                    if (capture)
                    {
                        weightedCapturePaths += weights[differential + 2];
                    }
                    for (var refusal = 0; refusal <= (retreat ? 1 : 0); refusal++)
                    {
                        for (var die = 1; die <= (capture ? 6 : 1); die++)
                        {
                            var share = capture ? data.GetProperty("capture_share_percent_by_die")[die - 1].GetInt32() : 0;
                            var ap = ExpectedPercent(data, CombatRole.Attacker, differential, ac);
                            var dp = ExpectedPercent(data, CombatRole.Defender, differential, dc);
                            var actual = Cna1979CombatAdjudication.Resolve(am, dm, ac, dc,
                                refusal == 1, capture ? die : null);
                            Assert.Equal(differential, actual.Differential);
                            Assert.Equal(ap, actual.AttackerLossPercent);
                            Assert.Equal(dp, actual.DefenderBaseLossPercent);
                            Assert.Equal(dp + 10 * refusal, actual.DefenderLossPercent);
                            Assert.Equal(engaged, actual.RawEngaged);
                            Assert.Equal(retreat ? 1 : 0, actual.DefenderRetreatHexes);
                            Assert.Equal(refusal == 1, actual.RefusedRetreat);
                            Assert.Equal(attackerCapture ? CombatRole.Attacker : defenderCapture ? CombatRole.Defender : (CombatRole?)null,
                                actual.CapturedRole);
                            Assert.Equal(capture ? share : (int?)null, actual.CaptureSharePercent);
                            AssertLoss(actual.Attacker, (ap + 9) / 10, attackerCapture ? share : 0);
                            AssertLoss(actual.Defender, (dp + 10 * refusal) / 10, defenderCapture ? share : 0);
                            rows++;
                        }
                    }
                }
            }
        }
        Assert.Equal(6480, joint);
        Assert.Equal(8840, rows);
        Assert.Equal(44208, weightedCapturePaths);
    }

    [Fact]
    public void RoundingCaptureRefusalAndRawEffectsKeepTheirSeparateMeanings()
    {
        var attacker = Cna1979CombatAdjudication.Resolve(12, 12, 11, 66);
        Assert.Equal(25, attacker.AttackerLossPercent);
        Assert.Equal(3, attacker.Attacker.TotalLoss);
        var defender = Cna1979CombatAdjudication.Resolve(12, 12, 66, 33);
        Assert.Equal(5, defender.DefenderBaseLossPercent);
        Assert.Equal(0, defender.Defender.TotalLoss);
        Assert.Equal(1, defender.DefenderRetreatHexes);
        Assert.True(defender.RawEngaged);
        var refusal = Cna1979CombatAdjudication.Resolve(11, 12, 66, 13, true);
        Assert.Equal(20, refusal.DefenderBaseLossPercent);
        Assert.Equal(3, refusal.Defender.TotalLoss);
        Assert.Equal(3, refusal.Defender.LossDp);
        var captured = Cna1979CombatAdjudication.Resolve(12, 12, 66, 11, captureDie: 2);
        Assert.Equal(2, captured.Defender.TotalLoss);
        Assert.Equal(1, captured.Defender.CapturedLoss);
        Assert.Equal(1, captured.Defender.DestroyedLoss);
        var captureThree = Cna1979CombatAdjudication.Resolve(66, 11, 11, 66, captureDie: 4);
        Assert.Equal(3, captureThree.Attacker.TotalLoss);
        Assert.Equal(2, captureThree.Attacker.CapturedLoss);
        Assert.False(Cna1979CombatAdjudication.Resolve(12, 12, 56, 66).RawEngaged);
        Assert.Equal(CaptureShares,
            Cna1979CombatAdjudication.Definition.CaptureShares.Select(value => value.Percent));
    }

    [Theory]
    [InlineData(34, 1)]
    [InlineData(35, 0)]
    [InlineData(36, 0)]
    public void AmendmentChangesOnlyMissingCellsAndKeepsRetreatCoordinate(int coordinate, int retreat)
    {
        var value = Cna1979CombatAdjudication.Resolve(11, 66, 66, coordinate);
        Assert.Equal(10, value.DefenderBaseLossPercent);
        Assert.Equal(1, value.Defender.TotalLoss);
        Assert.Equal(retreat, value.DefenderRetreatHexes);
        if (retreat == 1)
        {
            Assert.Equal(2, Cna1979CombatAdjudication.Resolve(11, 66, 66, coordinate, true).Defender.TotalLoss);
        }
    }

    [Fact]
    public void InvalidRoleDifferentialCoordinatesCaptureAndRefusalReject()
    {
        foreach (var role in new[] { (CombatRole)(-1), (CombatRole)2 })
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Cna1979CombatAdjudication.LookupLossPercent(role, 0, 11));
        }
        foreach (var diff in new[] { int.MinValue, -3, 3, int.MaxValue })
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Cna1979CombatAdjudication.LookupLossPercent(CombatRole.Attacker, diff, 11));
        }
        foreach (var coordinate in new[] { int.MinValue, 0, 10, 17, 18, 20, 67, 111, int.MaxValue })
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Cna1979CombatAdjudication.LookupLossPercent(CombatRole.Attacker, 0, coordinate));
            Assert.Throws<ArgumentOutOfRangeException>(() => Cna1979CombatAdjudication.Resolve(coordinate, 12, 66, 66));
            Assert.Throws<ArgumentOutOfRangeException>(() => Cna1979CombatAdjudication.Resolve(12, coordinate, 66, 66));
            Assert.Throws<ArgumentOutOfRangeException>(() => Cna1979CombatAdjudication.Resolve(12, 12, coordinate, 66));
            Assert.Throws<ArgumentOutOfRangeException>(() => Cna1979CombatAdjudication.Resolve(12, 12, 66, coordinate));
        }
        Assert.Throws<ArgumentException>(() => Cna1979CombatAdjudication.Resolve(12, 12, 66, 11));
        Assert.Throws<ArgumentException>(() => Cna1979CombatAdjudication.Resolve(66, 11, 11, 66));
        Assert.Throws<ArgumentException>(() => Cna1979CombatAdjudication.Resolve(12, 12, 66, 66, captureDie: 1));
        Assert.Throws<ArgumentException>(() => Cna1979CombatAdjudication.Resolve(12, 12, 66, 66, true));
        foreach (var die in new[] { int.MinValue, 0, 7, int.MaxValue })
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Cna1979CombatAdjudication.Resolve(12, 12, 66, 11, captureDie: die));
        }
    }

    [Fact]
    public void EveryNestedCollectionOwnsItsInputAndCannotBeMutatedThroughReturnedView()
    {
        var original = Cna1979CombatAdjudication.Definition;
        int[] coordinates = [34, 35, 36];
        var amendment = new CombatSourceAmendment("CMB-SRC-RUL-001", CombatRole.Defender, 2, coordinates, 10);
        coordinates[0] = 11;
        Assert.Equal(34, amendment.Coordinates[0]);
        int[] engaged = [9], retreat = [5], ac = [2], dc = [3];
        var effect = new CombatEffectRow(0, engaged, retreat, ac, dc);
        engaged[0] = retreat[0] = ac[0] = dc[0] = 12;
        Assert.Equal(9, effect.AttackerEngagedSums[0]);
        Assert.Equal(5, effect.DefenderRetreatOneHexSums[0]);
        Assert.Equal(2, effect.AttackerCaptureSums[0]);
        Assert.Equal(3, effect.DefenderCaptureSums[0]);
        string[] purposes = ["attacker.morale.tens"], conditional = ["attacker.capture.share"];
        var procedure = new CombatProcedure("p", "s", 252, 6, purposes, conditional);
        purposes[0] = conditional[0] = "changed";
        Assert.Equal("attacker.morale.tens", procedure.OrderedPurposes[0]);
        Assert.Equal("attacker.capture.share", procedure.ConditionalCapturePurposes[0]);
        var sources = original.Sources.ToArray();
        var evidence = original.SourceEvidence.ToArray();
        var morale = original.Morale.ToArray();
        var losses = original.Losses.ToArray();
        var effects = original.Effects.ToArray();
        var shares = original.CaptureShares.ToArray();
        var policies = original.Policies.ToArray();
        var copy = new CombatRulesInputDefinition(original.SchemaVersion, original.ArtifactId, original.ProfileId,
            sources, evidence, amendment, morale, losses, effects, shares, procedure, original.Costs,
            original.Settlement, policies);
        sources[0] = new RuleReference("changed", "changed");
        evidence[0] = new CombatSourceEvidence("changed", "changed");
        morale[0] = new CombatMoraleCell(11, 9);
        losses[0] = new CombatLossCell(CombatRole.Attacker, -2, 11, 99);
        effects[0] = effect;
        shares[0] = new CombatCaptureShare(1, 99);
        policies[0] = new CombatPolicy("changed", 9, "changed");
        Assert.Equal(original.Sources, copy.Sources);
        Assert.Equal(original.SourceEvidence, copy.SourceEvidence);
        Assert.Equal(original.Morale, copy.Morale);
        Assert.Equal(original.Losses, copy.Losses);
        Assert.Equal(original.Effects, copy.Effects);
        Assert.Equal(original.CaptureShares, copy.CaptureShares);
        Assert.Equal(original.Policies, copy.Policies);
        AssertReadOnly(copy.Sources); AssertReadOnly(copy.SourceEvidence); AssertReadOnly(copy.Morale);
        AssertReadOnly(copy.Losses); AssertReadOnly(copy.Effects); AssertReadOnly(copy.CaptureShares);
        AssertReadOnly(copy.Policies); AssertReadOnly(amendment.Coordinates);
        AssertReadOnly(effect.AttackerEngagedSums); AssertReadOnly(effect.DefenderRetreatOneHexSums);
        AssertReadOnly(effect.AttackerCaptureSums); AssertReadOnly(effect.DefenderCaptureSums);
        AssertReadOnly(procedure.OrderedPurposes); AssertReadOnly(procedure.ConditionalCapturePurposes);
    }

    private static void AssertReadOnly<T>(IReadOnlyList<T> values)
    {
        var list = Assert.IsAssignableFrom<IList<T>>(values);
        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => list[0] = values[0]);
    }

    private static void AssertLoss(CombatRoleLoss actual, int loss, int share)
    {
        var captured = (int)decimal.Ceiling(loss * share / 100m);
        Assert.Equal(loss, actual.TotalLoss);
        Assert.Equal(captured, actual.CapturedLoss);
        Assert.Equal(loss - captured, actual.DestroyedLoss);
        Assert.Equal(10 - loss, actual.RemainingToe);
        Assert.Equal(loss >= 3 ? 3 : 0, actual.LossDp);
        Assert.InRange(actual.TotalLoss, 0, 3);
        Assert.InRange(actual.CapturedLoss, 0, actual.TotalLoss);
        Assert.Equal(10, actual.RemainingToe + actual.CapturedLoss + actual.DestroyedLoss);
    }

    private static int ExpectedPercent(JsonElement source, CombatRole role, int differential, int coordinate)
    {
        var value = source.GetProperty("optical_loss_percent_by_coordinate")
            .GetProperty($"{RoleName(role)}:{differential}")[CoordinateIndex(coordinate)];
        return value.ValueKind == JsonValueKind.Null ? 10 : value.GetInt32();
    }

    private static bool Includes(JsonElement source, string field, int differential, int sum) =>
        source.GetProperty(field).GetProperty(differential.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .EnumerateArray().Any(value => value.GetInt32() == sum);

    private static JsonDocument ReadFixture(string name) => JsonDocument.Parse(File.ReadAllBytes(
        Path.Combine(AppContext.BaseDirectory, "Rules", "Fixtures", name)));
    private static IEnumerable<int> Coordinates() => Enumerable.Range(1, 6)
        .SelectMany(tens => Enumerable.Range(1, 6).Select(ones => tens * 10 + ones));
    private static int CoordinateIndex(int coordinate) => (coordinate / 10 - 1) * 6 + coordinate % 10 - 1;
    private static int Sum(int coordinate) => coordinate / 10 + coordinate % 10;
    private static int ExpectedMorale(int coordinate) => coordinate == 11 ? 1 : coordinate == 66 ? -1 : 0;
    private static string RoleName(CombatRole role) => role == CombatRole.Attacker ? "attacker" : "defender";
    private static (int Attacker, int Defender) MoralePair(int differential) => differential switch
    {
        -2 => (66, 11),
        -1 => (66, 12),
        0 => (12, 12),
        1 => (11, 12),
        2 => (11, 66),
        _ => throw new ArgumentOutOfRangeException(nameof(differential)),
    };
}
