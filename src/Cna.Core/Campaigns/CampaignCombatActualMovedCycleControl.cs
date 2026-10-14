using System.Text.Json;
namespace Cna.Core.Campaigns;
internal sealed class CombatActualMovedCycleControlResult
{
    public JsonElement Control => throw new NotSupportedException("N3 control reader not implemented");
}
internal static class CampaignCombatActualMovedCycleControl
{
    public static CombatActualMovedCycleControlResult ReplayTrustedSource(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request,
        IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown,
        IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> release,
        IReadOnlyList<byte[]> control, Func<string,byte[]> dependencies)
        => throw new NotSupportedException("N3 control reader not implemented");
}
