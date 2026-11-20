using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Visibility;

namespace Cna.Core.Campaigns;

internal sealed class CampaignRepeatMovementOwnerCutV29
{
    private CampaignRepeatMovementOwnerCutV29(string campaignId, LandSide side,
        int cycleOrdinal, IReadOnlyList<MoveElementAction> offers)
    {
        CampaignId = campaignId;
        Side = side;
        CycleOrdinal = cycleOrdinal;
        Offers = Array.AsReadOnly(offers.ToArray());
        ActionIds = Array.AsReadOnly(Offers.Select(value => value.ActionId).ToArray());
        VisibleBytes = SerializeVisible();
    }

    internal string CampaignId { get; }
    internal LandSide Side { get; }
    internal int CycleOrdinal { get; }
    internal IReadOnlyList<MoveElementAction> Offers { get; }
    internal IReadOnlyList<string> ActionIds { get; }
    internal byte[] VisibleBytes { get; }

    internal static CampaignRepeatMovementOwnerCutV29 Project(
        CampaignNativeReserveReleaseStateV27 release,
        CampaignNativeCycleControlV28 control,
        CampaignContentContext context,
        CampaignContactJournal contacts, LandSide side)
    {
        ArgumentNullException.ThrowIfNull(release);
        ArgumentNullException.ThrowIfNull(control);
        if (side != release.ActingSide
            || control.Choice != CampaignNativeCycleControlChoiceV28.Repeat
            || control.CampaignId != release.CampaignId
            || contacts.AuthorityVersion != control.StateVersion)
            throw new InvalidOperationException(
                "Repeated Movement belongs to the accepted owner cut.");
        var offers = CampaignCycleContinuationMovementWitnessV28.OffersAfterRepeat(
            release, control, context, contacts);
        return new(release.CampaignId, side, control.NextCycleOrdinal, offers);
    }

    private byte[] SerializeVisible()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CampaignNativeRepeatMovementV29.CurrentContractVersion);
            writer.WriteString("campaignId", ContentContractGuards.RequireStableId(
                CampaignId, nameof(CampaignId)));
            // Task 020E3 (owner ruling 2026-11-20): lowercase side, matching every other owner cut and
            // the v1 freeze's V27/V28 fix. Unlike V27/V28, V29 action IDs are MoveElementAction
            // semantic IDs, not hashes of these bytes, so retained repeated-Movement steps replay
            // unchanged (DET-001; pinned by CombatCycleDisclosureRepeatMovementTests).
            writer.WriteString("side", CampaignSnapshotSerializer.FormatSide(Side));
            writer.WriteNumber("cycleOrdinal", CycleOrdinal);
            writer.WriteStartArray("offers");
            foreach (var offer in Offers)
                writer.WriteRawValue(MoveElementAction.WriteSemantics(
                    offer.ElementId, offer.OriginLocationId,
                    offer.DestinationLocationId, offer.CostBreakdown));
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
}
