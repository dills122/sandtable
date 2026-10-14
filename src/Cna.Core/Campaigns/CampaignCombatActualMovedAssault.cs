using System.Text.Json;
namespace Cna.Core.Campaigns;
internal sealed class CombatActualMovedAssaultResult
{
    public byte[] SourceBytes => throw new NotSupportedException();
    public byte[]? EventBytes => throw new NotSupportedException();
    public JsonElement Control => throw new NotSupportedException();
}
internal static class CampaignCombatActualMovedAssault
{
    public static CombatActualMovedAssaultResult ReplayTrustedSource(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request,
        IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown,
        IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, Func<string, byte[]> dependencies)
        => throw new NotSupportedException("Authentic moved Selection/Round/Paid source is not implemented.");
    public static CombatActualMovedAssaultResult ApplyTrustedSource(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request,
        IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown,
        IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid,
        ReadOnlySpan<byte> currentInput, Func<string, byte[]> dependencies, bool admissionEnabled = true)
        => throw new NotSupportedException("Moved cumulative paid assault is not implemented.");

}
