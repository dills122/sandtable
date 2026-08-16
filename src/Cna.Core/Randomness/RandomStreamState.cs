namespace Cna.Core.Randomness;

public sealed record RandomStreamState
{
    public RandomStreamState(
        int contractVersion,
        string algorithmId,
        ulong seed,
        ulong nextByteCursor)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(contractVersion, 1);
        ArgumentException.ThrowIfNullOrWhiteSpace(algorithmId);

        ContractVersion = contractVersion;
        AlgorithmId = algorithmId;
        Seed = seed;
        NextByteCursor = nextByteCursor;
    }

    public int ContractVersion { get; }

    public string AlgorithmId { get; }

    public ulong Seed { get; }

    public ulong NextByteCursor { get; }
}

public sealed record RandomByteDraw
{
    public RandomByteDraw(byte value, RandomStreamState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        Value = value;
        State = state;
    }

    public byte Value { get; }

    public RandomStreamState State { get; }
}

public sealed record RandomD6Roll
{
    public RandomD6Roll(int value, RandomStreamState state)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 6);
        ArgumentNullException.ThrowIfNull(state);

        Value = value;
        State = state;
    }

    public int Value { get; }

    public RandomStreamState State { get; }
}
