using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Actions;

/// <summary>A source-turn owner choice bound to the exact campaign revision.</summary>
public sealed record CampaignSourceTurnAction : CampaignActionCandidate
{
    internal CampaignSourceTurnAction(long stateVersion, string destination)
        : this(stateVersion, "source-polish-arrival", destination, null, null, null) { }

    private CampaignSourceTurnAction(long stateVersion, string kind, string? destination, int? lane, int? stage, int? delta)
        : base(kind, Semantics(stateVersion, kind, destination, lane, stage, delta))
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(stateVersion, 1);
        StateVersion = stateVersion;
        Destination = destination is null ? null : ContentContractGuards.RequireStableId(destination, nameof(destination));
        Lane = lane; Stage = stage; Delta = delta;
    }

    internal static CampaignSourceTurnAction MoveFourthIndianOpening(long stateVersion, string supplyId) => new(stateVersion, "source-fourth-indian-opening-move", supplyId, null, null, null);
    internal static CampaignSourceTurnAction RollGt3Supply(long stateVersion) => new(stateVersion, "source-axis-gt3-supply-roll", null, null, null, null);
    internal static CampaignSourceTurnAction RollGt5Supply(long stateVersion) => new(stateVersion, "source-commonwealth-gt5-supply-roll", null, null, null, null);
    internal static CampaignSourceTurnAction DeclineTacticalShipping(long stateVersion) => new(stateVersion, "source-tactical-shipping-decline", null, null, null, null);
    internal static CampaignSourceTurnAction DeclineVoluntaryOrganization(long stateVersion) => new(stateVersion, "source-voluntary-organization-decline", null, null, null, null);
    internal static CampaignSourceTurnAction DeclineFirstReserves(long stateVersion) => new(stateVersion, "source-stage1-first-reserves-decline", null, null, null, null);
    internal static CampaignSourceTurnAction FinishFirstMovement(long stateVersion) => new(stateVersion, "source-stage1-first-movement-finish", null, null, null, null);
    internal static CampaignSourceTurnAction ApproveSixthAustralianBaseline(long stateVersion) => new(stateVersion, "source-australian-baseline-approve", null, null, null, null);
    internal static CampaignSourceTurnAction WithdrawSixthAustralianBaseline(long stateVersion) => new(stateVersion, "source-australian-baseline-withdraw", null, null, null, null);
    internal static CampaignSourceTurnAction InterruptSixthAustralianTraining(long stateVersion) => new(stateVersion, "source-sixth-australian-training-interrupt", null, null, null, null);
    internal static CampaignSourceTurnAction DeclineGt1Lane6Attack(long stateVersion) => new(stateVersion, "source-gt1-lane6-attack-decline", null, null, null, null);
    internal static CampaignSourceTurnAction DeclineGt1Lane3Attack(long stateVersion) => new(stateVersion, "source-gt1-lane3-attack-decline", null, null, null, null);
    internal static CampaignSourceTurnAction DeclineGt1Lane2Attack(long stateVersion) => new(stateVersion, "source-gt1-lane2-attack-decline", null, null, null, null);
    internal static CampaignSourceTurnAction ActFirst(long stateVersion) => new(stateVersion, "source-stage1-act-first", null, null, null, null);
    internal static CampaignSourceTurnAction ActLast(long stateVersion) => new(stateVersion, "source-stage1-act-last", null, null, null, null);
    internal static CampaignSourceTurnAction AdjustGt3Supply(long stateVersion, int lane, int stage, int delta) =>
        new(stateVersion, "source-axis-gt3-supply-adjust", null, lane, stage, delta);
    internal static CampaignSourceTurnAction FinishGt3Supply(long stateVersion) => new(stateVersion, "source-axis-gt3-supply-finish", null, null, null, null);

    internal long StateVersion { get; }
    public string? Destination { get; }
    public int? Lane { get; }
    public int? Stage { get; }
    public int? Delta { get; }

    private static byte[] Semantics(long stateVersion, string kind, string? destination, int? lane, int? stage, int? delta)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(stateVersion, 1);
        if (kind is not ("source-polish-arrival" or "source-axis-gt3-supply-roll" or "source-axis-gt3-supply-adjust" or "source-axis-gt3-supply-finish" or "source-commonwealth-gt5-supply-roll" or "source-tactical-shipping-decline" or "source-voluntary-organization-decline" or "source-stage1-first-reserves-decline" or "source-stage1-first-movement-finish" or "source-gt1-lane2-attack-decline" or "source-gt1-lane3-attack-decline" or "source-gt1-lane6-attack-decline" or "source-sixth-australian-training-interrupt" or "source-australian-baseline-approve" or "source-australian-baseline-withdraw" or "source-fourth-indian-opening-move" or "source-stage1-act-first" or "source-stage1-act-last")
            || (kind is "source-polish-arrival" or "source-fourth-indian-opening-move") != (destination is not null)
            || (kind == "source-axis-gt3-supply-adjust" ? lane is not (2 or 3 or 6) || stage is < 1 or > 3 or null || delta is not (-1 or 1)
                : lane is not null || stage is not null || delta is not null))
            throw new ArgumentException("Unsupported source turn action.");
        if (destination is not null) destination = ContentContractGuards.RequireStableId(destination, nameof(destination));
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 1);
            writer.WriteString("kind", kind);
            writer.WriteNumber("stateVersion", stateVersion); writer.WriteString("destination", destination);
            if (lane is not null) { writer.WriteNumber("lane", lane.Value); writer.WriteNumber("operationStage", stage!.Value); writer.WriteNumber("delta", delta!.Value); }
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
}
