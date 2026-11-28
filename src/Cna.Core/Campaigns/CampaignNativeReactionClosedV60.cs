using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal enum CampaignNativeReactionCloseKind
{
    PlayerDecline = 1,
    ScriptedUnavailable = 2,
    Timeout = 3,
    NoEligibleReactor = 4,
    ParticipantCompleted = 5,
}

internal sealed partial record CampaignNativeReactionClosedV60(
    int ContractVersion, string CampaignId, long PriorStateVersion, string PriorEventHash, int GameTurn,
    CampaignReactionWindowId WindowId, LandSide ReactingSide, string PriorWorldHash, string ResultWorldHash,
    CampaignNativeReactionCloseKind CloseKind, CampaignReactionOpportunityId? OpportunityId,
    string ResumePositionId, CampaignReactionWindow? WindowAfter)
{
    internal const int CurrentContractVersion = 60;
    internal const string EventTypeId = "native-reaction-closed";
    internal long StateVersion => checked(PriorStateVersion + 1);

    internal byte[] SerializeCanonical()
    {
        if (PriorWorldHash != ResultWorldHash || !Enum.IsDefined(CloseKind)
            || (CloseKind == CampaignNativeReactionCloseKind.ParticipantCompleted) != (OpportunityId is not null)
            || (WindowAfter is not null && CloseKind != CampaignNativeReactionCloseKind.ParticipantCompleted))
            throw new JsonException("Invalid native Reaction closure.");
        if (WindowAfter is { } window)
        {
            CampaignNativeReactionCodec.RequireWindowBinding(window, WindowId, ReactingSide, GameTurn, PriorStateVersion);
            if (window.ActiveOpportunityId is not null || !window.ResolvedOpportunityIds.Contains(OpportunityId!)
                || window.ResolvedOpportunityIds.Count == window.FrozenOpportunities.Count
                || window.ReactingPosition.SuspendedMovementPosition.PositionId != ResumePositionId)
                throw new JsonException("Invalid remaining Reaction participant state.");
        }
        return CampaignNativeReactionCodec.Write(ContractVersion, CurrentContractVersion, EventTypeId, CampaignId,
            PriorStateVersion, PriorEventHash, GameTurn, WindowId, ReactingSide, PriorWorldHash, ResultWorldHash, writer =>
            {
                writer.WriteString("closeKind", FormatKind(CloseKind));
                writer.WriteString("opportunityId", OpportunityId?.Value);
                writer.WriteString("resumePositionId", ContentContractGuards.RequireStableId(ResumePositionId, nameof(ResumePositionId)));
                CampaignV11CanonicalCodec.WriteReactionWindow(writer, "windowAfter", WindowAfter);
            });
    }

    internal static CampaignNativeReactionClosedV60 Deserialize(ReadOnlyMemory<byte> bytes) =>
        CampaignNativeReactionCodec.Read(bytes, EventTypeId, ["closeKind", "opportunityId", "resumePositionId", "windowAfter"],
            root => new CampaignNativeReactionClosedV60(root.GetProperty("contractVersion").GetInt32(),
                CampaignNativeReactionCodec.String(root, "campaignId"), root.GetProperty("priorStateVersion").GetInt64(),
                CampaignNativeReactionCodec.String(root, "priorEventHash"), root.GetProperty("gameTurn").GetInt32(),
                new(CampaignNativeReactionCodec.String(root, "windowId")), CampaignSnapshotSerializer.ParseSide(CampaignNativeReactionCodec.String(root, "reactingSide")),
                CampaignNativeReactionCodec.String(root, "priorWorldHash"), CampaignNativeReactionCodec.String(root, "resultWorldHash"),
                ParseKind(CampaignNativeReactionCodec.String(root, "closeKind")),
                root.GetProperty("opportunityId").GetString() is { } opportunity ? new(opportunity) : null,
                CampaignNativeReactionCodec.String(root, "resumePositionId"),
                CampaignV11CanonicalCodec.ParseReactionWindow(root.GetProperty("windowAfter"), allowSecondMovement: true)), value => value.SerializeCanonical());

    private static string FormatKind(CampaignNativeReactionCloseKind kind) => kind switch
    {
        CampaignNativeReactionCloseKind.PlayerDecline => "declined",
        CampaignNativeReactionCloseKind.ScriptedUnavailable => "scriptedUnavailable",
        CampaignNativeReactionCloseKind.Timeout => "timeout",
        CampaignNativeReactionCloseKind.NoEligibleReactor => "noEligibleReactor",
        CampaignNativeReactionCloseKind.ParticipantCompleted => "participantCompleted",
        _ => throw new JsonException("Invalid native Reaction closure kind."),
    };

    private static CampaignNativeReactionCloseKind ParseKind(string kind) => kind switch
    {
        "declined" => CampaignNativeReactionCloseKind.PlayerDecline,
        "scriptedUnavailable" => CampaignNativeReactionCloseKind.ScriptedUnavailable,
        "timeout" => CampaignNativeReactionCloseKind.Timeout,
        "noEligibleReactor" => CampaignNativeReactionCloseKind.NoEligibleReactor,
        "participantCompleted" => CampaignNativeReactionCloseKind.ParticipantCompleted,
        _ => throw new JsonException("Invalid native Reaction closure kind."),
    };
}
