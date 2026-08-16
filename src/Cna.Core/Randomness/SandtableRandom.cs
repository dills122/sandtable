using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Cna.Core.Randomness;

public static class SandtableRandom
{
    public const int ContractVersion = 1;
    public const string AlgorithmId = "sandtable.sha256-counter.v1";
    public const string DomainAscii = "sandtable.random.v1";
    public const int BlockBytes = 32;
    public const int D6AcceptBelow = 252;
    public const int D6Modulo = 6;
    public const int D6Offset = 1;

    private const int CompleteInputBytes = 36;

    public static RandomStreamState Create(ulong seed) => new(
        ContractVersion,
        AlgorithmId,
        seed,
        0);

    public static RandomByteDraw NextByte(RandomStreamState state)
    {
        Validate(state);

        var nextCursor = checked(state.NextByteCursor + 1);
        var blockIndex = state.NextByteCursor / BlockBytes;
        var byteIndex = (int)(state.NextByteCursor % BlockBytes);
        Span<byte> input = stackalloc byte[CompleteInputBytes];
        DomainAscii.AsSpan().CopyToAscii(input);
        input[DomainAscii.Length] = 0;
        BinaryPrimitives.WriteUInt64BigEndian(input[20..28], state.Seed);
        BinaryPrimitives.WriteUInt64BigEndian(input[28..], blockIndex);
        Span<byte> block = stackalloc byte[BlockBytes];
        SHA256.HashData(input, block);

        return new RandomByteDraw(
            block[byteIndex],
            new RandomStreamState(
                state.ContractVersion,
                state.AlgorithmId,
                state.Seed,
                nextCursor));
    }

    public static RandomD6Roll RollD6(RandomStreamState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var current = state;

        while (true)
        {
            var draw = NextByte(current);
            current = draw.State;

            if (draw.Value < D6AcceptBelow)
            {
                return new RandomD6Roll((draw.Value % D6Modulo) + D6Offset, current);
            }
        }
    }

    private static void Validate(RandomStreamState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.ContractVersion != ContractVersion)
        {
            throw new ArgumentException(
                "The random-stream contract version is not supported.",
                nameof(state));
        }

        if (!string.Equals(state.AlgorithmId, AlgorithmId, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The random-stream algorithm is not supported.",
                nameof(state));
        }
    }

    private static void CopyToAscii(this ReadOnlySpan<char> value, Span<byte> destination)
    {
        for (var index = 0; index < value.Length; index++)
        {
            destination[index] = checked((byte)value[index]);
        }
    }
}
