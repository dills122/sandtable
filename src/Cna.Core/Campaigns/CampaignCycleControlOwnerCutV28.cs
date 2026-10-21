using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Visibility;

namespace Cna.Core.Campaigns;

internal sealed class CampaignCycleControlOwnerCutV28
{
    private CampaignCycleControlOwnerCutV28(string campaignId, LandSide side,
        int cycleOrdinal, IReadOnlyList<CampaignNativeCycleControlChoiceV28> choices)
    {
        CampaignId = campaignId;
        Side = side;
        CycleOrdinal = cycleOrdinal;
        Choices = Array.AsReadOnly(choices.ToArray());
        VisibleBytes = SerializeVisible();
        ActionIds = Array.AsReadOnly(Choices.Select(choice =>
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(
                (byte[])[.. VisibleBytes, 0, .. Encoding.UTF8.GetBytes(choice.ToString())])))
            .ToArray());
    }

    internal string CampaignId { get; }
    internal LandSide Side { get; }
    internal int CycleOrdinal { get; }
    internal IReadOnlyList<CampaignNativeCycleControlChoiceV28> Choices { get; }
    internal byte[] VisibleBytes { get; }
    internal IReadOnlyList<string> ActionIds { get; }

    internal static CampaignCycleControlOwnerCutV28 Project(
        CampaignNativeReserveReleaseStateV27 release,
        CampaignContentContext context,
        CampaignContactJournal contacts, LandSide side)
    {
        ArgumentNullException.ThrowIfNull(release);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(contacts);
        if (side != release.ActingSide || !release.Completed
            || contacts.AuthorityVersion != release.StateVersion)
            throw new InvalidOperationException("Cycle control belongs to the completed owner cut.");
        var choices = new List<CampaignNativeCycleControlChoiceV28>();
        if (release.CycleOrdinal is >= 1 and <= 2
            && release.HasRetainedCombatRandomCursor
            && CampaignCycleContinuationMovementWitnessV28.Find(
                release, context, contacts) is not null)
            choices.Add(CampaignNativeCycleControlChoiceV28.Repeat);
        choices.Add(CampaignNativeCycleControlChoiceV28.Finish);
        return new(release.CampaignId, side, release.CycleOrdinal, choices);
    }

    private byte[] SerializeVisible()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CampaignNativeCycleControlV28.CurrentContractVersion);
            writer.WriteString("campaignId", ContentContractGuards.RequireStableId(
                CampaignId, nameof(CampaignId)));
            writer.WriteString("side", Side.ToString());
            writer.WriteNumber("cycleOrdinal", CycleOrdinal);
            writer.WriteStartArray("choices");
            foreach (var choice in Choices)
                writer.WriteStringValue(choice.ToString());
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
}
