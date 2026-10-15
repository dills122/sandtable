using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
namespace Cna.Core.Campaigns;

internal sealed class CombatActualMovedRepeatMovementResult
{
    public JsonElement Control { get; }
}

internal static class CampaignCombatActualMovedRepeatMovement
{
    public static CombatActualMovedRepeatMovementResult ApplyTrustedSource(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request,
        IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown, IReadOnlyList<byte[]> selection,
        IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> release, IReadOnlyList<byte[]> control,
        IReadOnlyList<byte[]> movement, ReadOnlySpan<byte> input, Func<string, byte[]> dependencies, bool admissionEnabled = true)
    {
        var packet = JsonNode.Parse(source)!.AsObject();
        var parent = CampaignCombatActualMovedCycleControl.ReplayTrustedSource(Encoding.ASCII.GetBytes(packet["controlSourceCanonicalUtf8"]!.GetValue<string>()), request,
            prefix, lifecycle, breakdown, selection, round, paid, release, control, dependencies);
        if (parent.Control.GetProperty("phase").GetString() != "repeat-intent-pending") throw new JsonException("CMB-MRM-004");
        // RED checkpoint: authentic parent succeeds; actual publication capability is absent.
        throw new JsonException("CMB-MRM-007");
    }
}
