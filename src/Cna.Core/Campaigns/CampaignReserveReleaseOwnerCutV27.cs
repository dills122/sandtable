using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// A reserve member is owned by one side. Neither its visible offers nor the
// action IDs contain the private World hash or an authority version.
internal sealed class CampaignReserveReleaseOwnerCutV27
{
    private CampaignReserveReleaseOwnerCutV27(string campaignId, LandSide side,
        int cycleOrdinal, CampaignReserveReleaseMember member)
    {
        CampaignId = campaignId;
        Side = side;
        CycleOrdinal = cycleOrdinal;
        Member = member;
        VisibleBytes = SerializeVisible();
        ActionIds = Array.AsReadOnly(member.Choices.Select(choice =>
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(
                (byte[])[.. VisibleBytes, 0, .. Encoding.UTF8.GetBytes(choice.ToString())]))).ToArray());
    }

    internal string CampaignId { get; }
    internal LandSide Side { get; }
    internal int CycleOrdinal { get; }
    internal CampaignReserveReleaseMember Member { get; }
    internal byte[] VisibleBytes { get; }
    internal IReadOnlyList<string> ActionIds { get; }

    internal static CampaignReserveReleaseOwnerCutV27 Project(
        CampaignNativeReserveReleaseStateV27 state,
        CampaignContentContext context, LandSide side, string elementId)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(context);
        if (state.Completed || side != state.ActingSide)
            throw new InvalidOperationException("Reserve Release is not offered to this side.");
        var pending = state.Pending(context);
        var member = pending.Count == 0 ? null : pending[0];
        if (member is null || member.ElementId != elementId)
            throw new InvalidOperationException("No ordered owned reserve member is pending.");
        return new(state.CampaignId, side, state.CycleOrdinal, member);
    }

    private byte[] SerializeVisible()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CampaignNativeReserveReleaseEventV27.CurrentContractVersion);
            writer.WriteString("campaignId", ContentContractGuards.RequireStableId(
                CampaignId, nameof(CampaignId)));
            writer.WriteString("side", Side.ToString());
            writer.WriteNumber("cycleOrdinal", CycleOrdinal);
            writer.WriteString("elementId", ContentContractGuards.RequireStableId(
                Member.ElementId, nameof(Member.ElementId)));
            writer.WriteString("reserveStatus", Member.Status.ToString());
            writer.WriteStartArray("choices");
            foreach (var choice in Member.Choices)
                writer.WriteStringValue(choice.ToString());
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
}
