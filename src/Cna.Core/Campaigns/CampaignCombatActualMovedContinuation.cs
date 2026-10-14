using System.Text.Json;
namespace Cna.Core.Campaigns;
internal sealed class CombatActualMovedContinuationResult
{
    public byte[] SourceBytes => throw new NotSupportedException("N2 source reader missing");
    public byte[] ControlBytes => throw new NotSupportedException("N2 source reader missing");
    public byte[] ProofBytes => throw new NotSupportedException("N2 source reader missing");
    public byte[]? EventBytes => throw new NotSupportedException("N2 source reader missing");
    public JsonElement Control => throw new NotSupportedException("N2 source reader missing");
}
internal static class CampaignCombatActualMovedContinuation
{
    public static CombatActualMovedContinuationResult ReplayTrustedSource(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request,
        IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown,
        IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> release, Func<string, byte[]> dependencies)
        => throw new NotSupportedException("Authentic paid and armed N1 Release/assessment reader missing");
    public static CombatActualMovedContinuationResult ApplyTrustedSource(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request,
        IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown,
        IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> release,
        ReadOnlySpan<byte> currentInput, Func<string, byte[]> dependencies, bool admissionEnabled = true)
        => throw new NotSupportedException("Authentic empty Release/assessment transition missing");
}
