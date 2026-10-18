using Cna.Core.Actions;

namespace Cna.OrleansHost;

// Trusted-host records only. Neither this binding nor a Core command is an HTTP response.
[GenerateSerializer]
public sealed record TemporaryAuthenticatedSeat(
    [property: Id(0)] string OperatorId,
    [property: Id(1)] string CampaignId,
    [property: Id(2)] CampaignActionAudience Audience,
    [property: Id(3)] string SessionGeneration);

[GenerateSerializer]
public sealed record TemporaryOutwardActionRequest(
    [property: Id(0)] int ContractVersion,
    [property: Id(1)] string CampaignId,
    [property: Id(2)] string RequestId,
    [property: Id(3)] string SessionGeneration,
    [property: Id(4)] string ExpectedPositionId,
    [property: Id(5)] string ActionId,
    [property: Id(6)] int ActionContractVersion,
    [property: Id(7)] string ActionSetToken);

[GenerateSerializer]
public sealed record TemporaryOutwardActionReply(
    [property: Id(0)] TemporaryCampaignStatus Status,
    [property: Id(1)] string Reason,
    [property: Id(2)] string? AudienceRevision = null);

internal static class TemporaryOutwardActionBinding
{
    internal static bool Valid(TemporaryAuthenticatedSeat seat,
        TemporaryOutwardActionRequest request) =>
        seat is not null && request is not null
        && seat.Audience is CampaignActionAudience.Axis or CampaignActionAudience.Commonwealth
        && !string.IsNullOrWhiteSpace(seat.OperatorId) && seat.OperatorId.Length <= 128
        && seat.CampaignId == request.CampaignId
        && CanonicalGuid(seat.SessionGeneration)
        && seat.SessionGeneration == request.SessionGeneration
        && request.ContractVersion == 1 && request.ActionContractVersion == 1
        && CanonicalGuid(request.RequestId) && CanonicalGuid(request.ActionId)
        && CanonicalGuid(request.ActionSetToken)
        && !string.IsNullOrWhiteSpace(request.ExpectedPositionId)
        && request.ExpectedPositionId.Length <= 128;

    private static bool CanonicalGuid(string value) =>
        Guid.TryParseExact(value, "D", out var parsed)
        && parsed.ToString("D") == value;
}
