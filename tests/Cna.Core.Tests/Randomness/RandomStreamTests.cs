using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Randomness;

public sealed class RandomStreamTests
{
    [Fact]
    public void CounterStreamMatchesIndependentGoldenBlocks()
    {
        var seedZero = SandtableRandom.Create(0);
        var seedOneBlockOne = new RandomStreamState(
            SandtableRandom.ContractVersion,
            SandtableRandom.AlgorithmId,
            1,
            32);

        var first = ReadBytes(seedZero, 32);
        var second = ReadBytes(seedOneBlockOne, 32);

        Assert.Equal(
            "8311c7e78aaf9de64d3301fb0ed4839c4082a57438da962a518d7fcc22b04dd7",
            Convert.ToHexString(first.Bytes).ToLowerInvariant());
        Assert.Equal(
            "05709046ad6157be8e40c1567beef84d7175d8d0a8ce3cd9fd1ff6bc7098dcc4",
            Convert.ToHexString(second.Bytes).ToLowerInvariant());
        Assert.Equal(32UL, first.State.NextByteCursor);
        Assert.Equal(64UL, second.State.NextByteCursor);
    }

    [Fact]
    public void CounterStreamCrossesBlockBoundaryByByteCursor()
    {
        var state = new RandomStreamState(
            SandtableRandom.ContractVersion,
            SandtableRandom.AlgorithmId,
            42,
            30);

        var result = ReadBytes(state, 6);

        Assert.Equal(
            "28ec479de88e",
            Convert.ToHexString(result.Bytes).ToLowerInvariant());
        Assert.Equal(36UL, result.State.NextByteCursor);
        Assert.Equal(30UL, state.NextByteCursor);
    }

    [Fact]
    public void D6MatchesIndependentGoldenSequence()
    {
        var state = SandtableRandom.Create(0);
        var values = new List<int>();

        for (var index = 0; index < 12; index++)
        {
            var roll = SandtableRandom.RollD6(state);
            values.Add(roll.Value);
            state = roll.State;
        }

        Assert.Equal([6, 6, 2, 4, 1, 2, 2, 3, 6, 4, 2, 6], values);
        Assert.Equal(12UL, state.NextByteCursor);
    }

    [Fact]
    public void D6ConsumesRejectedCandidatesBeforeAcceptedByte()
    {
        var state = new RandomStreamState(
            SandtableRandom.ContractVersion,
            SandtableRandom.AlgorithmId,
            0,
            129);

        var roll = SandtableRandom.RollD6(state);
        var nextRoll = SandtableRandom.RollD6(roll.State);

        Assert.Equal(2, roll.Value);
        Assert.Equal(131UL, roll.State.NextByteCursor);
        Assert.Equal(1, nextRoll.Value);
        Assert.Equal(132UL, nextRoll.State.NextByteCursor);
    }

    [Fact]
    public void StreamRejectsUnknownStateContractsWithoutConsumingBytes()
    {
        var unknownVersion = new RandomStreamState(
            SandtableRandom.ContractVersion + 1,
            SandtableRandom.AlgorithmId,
            0,
            7);
        var unknownAlgorithm = new RandomStreamState(
            SandtableRandom.ContractVersion,
            "sandtable.unknown.v1",
            0,
            7);

        Assert.Throws<ArgumentException>(
            () => SandtableRandom.NextByte(unknownVersion));
        Assert.Throws<ArgumentException>(
            () => SandtableRandom.NextByte(unknownAlgorithm));
        Assert.Equal(7UL, unknownVersion.NextByteCursor);
        Assert.Equal(7UL, unknownAlgorithm.NextByteCursor);
    }

    [Fact]
    public void StreamGuardsCursorOverflow()
    {
        var exhausted = new RandomStreamState(
            SandtableRandom.ContractVersion,
            SandtableRandom.AlgorithmId,
            0,
            ulong.MaxValue);

        Assert.Throws<OverflowException>(() => SandtableRandom.NextByte(exhausted));
        Assert.Throws<OverflowException>(() => SandtableRandom.RollD6(exhausted));
    }

    [Fact]
    public void SameStateIsDeterministicAndSelectedSeedsDiverge()
    {
        var first = SandtableRandom.RollD6(SandtableRandom.Create(0));
        var repeated = SandtableRandom.RollD6(SandtableRandom.Create(0));
        var differentSeed = SandtableRandom.RollD6(SandtableRandom.Create(1));

        Assert.Equal(first, repeated);
        Assert.NotEqual(first.Value, differentSeed.Value);
    }

    [Fact]
    public void RandomProcedureDefinitionCopiesAndComparesCollectionsStructurally()
    {
        var canonical = Cna1979RandomProcedure.CanonicalDefinition;
        var procedures = canonical.Procedures.ToList();
        var sources = canonical.Sources.Reverse().ToList();
        var equivalent = new RandomProcedureDefinition(
            canonical.SchemaVersion,
            canonical.AlgorithmId,
            canonical.DomainAscii,
            canonical.SeparatorByte,
            canonical.IntegerEncoding,
            canonical.BlockBytes,
            canonical.D6AcceptBelow,
            canonical.D6Modulo,
            canonical.D6Offset,
            procedures,
            sources);

        procedures.Clear();
        sources.Clear();

        Assert.Equal(
            ["initiative-determination", "weather-determination"],
            equivalent.Procedures.Select(value => value.ProcedureId));
        Assert.Equal(
            [
                Cna1979RandomProcedure.NormalizationSourceReference,
                Cna1979RandomProcedure.WeatherSourceReference,
                Cna1979RandomProcedure.OpposedDiceSourceReference,
            ],
            equivalent.Sources);
        Assert.Equal(canonical, equivalent);
        Assert.Equal(canonical.GetHashCode(), equivalent.GetHashCode());
    }

    [Fact]
    public void RandomProcedureArtifactIsCanonicalAndHashSensitive()
    {
        var canonical = Cna1979RandomProcedure.CanonicalDefinition;
        var artifact = Cna1979RandomProcedure.CreateArtifact();
        var reorderedSources = new RandomProcedureDefinition(
            canonical.SchemaVersion,
            canonical.AlgorithmId,
            canonical.DomainAscii,
            canonical.SeparatorByte,
            canonical.IntegerEncoding,
            canonical.BlockBytes,
            canonical.D6AcceptBelow,
            canonical.D6Modulo,
            canonical.D6Offset,
            canonical.Procedures,
            canonical.Sources.Reverse().ToArray());
        var changedOrder = new RandomProcedureDefinition(
            canonical.SchemaVersion,
            canonical.AlgorithmId,
            canonical.DomainAscii,
            canonical.SeparatorByte,
            canonical.IntegerEncoding,
            canonical.BlockBytes,
            canonical.D6AcceptBelow,
            canonical.D6Modulo,
            canonical.D6Offset,
            canonical.Procedures.Reverse().ToArray(),
            canonical.Sources);

        var baseline = Cna1979RandomProcedure.CalculateContentHash(canonical);
        var reorderedHash = Cna1979RandomProcedure.CalculateContentHash(reorderedSources);
        var changedHash = Cna1979RandomProcedure.CalculateContentHash(changedOrder);

        Assert.Equal("cna-1979.1.random-procedure", artifact.ArtifactId);
        Assert.Equal(baseline, artifact.ContentHash);
        Assert.Matches("^sha256:[0-9a-f]{64}$", baseline);
        Assert.Equal(baseline, reorderedHash);
        Assert.NotEqual(baseline, changedHash);
        Assert.Matches("^sha256:[0-9a-f]{64}$", baseline);
        Assert.Equal(canonical.Sources, artifact.Sources);
    }

    private static (byte[] Bytes, RandomStreamState State) ReadBytes(
        RandomStreamState initialState,
        int count)
    {
        var bytes = new byte[count];
        var state = initialState;

        for (var index = 0; index < count; index++)
        {
            var draw = SandtableRandom.NextByte(state);
            bytes[index] = draw.Value;
            state = draw.State;
        }

        return (bytes, state);
    }
}
