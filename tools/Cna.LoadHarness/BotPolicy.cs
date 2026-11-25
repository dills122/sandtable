using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Execution;

namespace Cna.LoadHarness;

// SplitMix64 with rejection sampling. The only entropy is the role's Runner ledger entry.
internal sealed class BotPolicy(string policy, ExerciseSeedLedger ledger, ExerciseSeedRole role)
{
    private ulong state = ledger.Entries.Single(entry => entry.Domain == ExerciseSeedDomain.Controller
        && entry.Role == role).DerivedSeed;

    internal int Choose(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        if (policy == "first") return 0;
        if (policy == "last") return count - 1;
        var bound = (ulong)count;
        var threshold = unchecked(0UL - bound) % bound;
        ulong value;
        do { value = Next(); } while (value < threshold);
        return (int)(value % bound);
    }

    private ulong Next()
    {
        unchecked
        {
            state += 0x9e3779b97f4a7c15UL;
            var value = state;
            value = (value ^ (value >> 30)) * 0xbf58476d1ce4e5b9UL;
            value = (value ^ (value >> 27)) * 0x94d049bb133111ebUL;
            return value ^ (value >> 31);
        }
    }
}
