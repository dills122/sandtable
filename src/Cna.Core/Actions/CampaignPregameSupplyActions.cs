namespace Cna.Core.Actions;

/// <summary>Request the seeded Axis GT2 pre-game supply availability result.</summary>
public sealed record PregameAxisSupplyRollAction : CampaignActionCandidate
{
    internal PregameAxisSupplyRollAction() : base("pregame-axis-gt2-supply-roll") { }
}

/// <summary>Adjust one private, provisional GT2 supply allocation.</summary>
public sealed record PregameAxisSupplyAllocationAction : CampaignActionCandidate
{
    internal PregameAxisSupplyAllocationAction(int lane, int operationStage, int delta)
        : base(delta == 1 ? "pregame-axis-gt2-supply-add" : "pregame-axis-gt2-supply-remove",
            $"lane-{lane}-stage-{operationStage}")
    {
        if (lane is not (2 or 3 or 6) || operationStage is < 1 or > 3 || delta is not (1 or -1))
            throw new ArgumentException("Invalid pre-game allocation action.");
        Lane = lane;
        Stage = operationStage;
        Delta = delta;
    }
    public int Lane { get; }
    public int Stage { get; }
    public int Delta { get; }
}
