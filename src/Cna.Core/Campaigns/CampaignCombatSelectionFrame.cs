using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// Carries the accepted Combat selection and its actual campaign state. The
// original V11 snapshot remains available only for turn-wide source facts;
// later cycles use their retained World and random cursor, never a recreated V11.
internal sealed record CampaignCombatSelectionFacts(
    string Choice, LandSide Side, string ActingElementId,
    string? ContactHandle, string? ApparentLocationId,
    string? BoundRepresentationId, string ActionId,
    string PositionId, string NextPositionId);

internal sealed record CampaignCombatSelectionFrame(
    CampaignSnapshotV11 PriorSnapshot,
    CampaignWorldSnapshotV6 World,
    RandomStreamState RandomState,
    long StateVersion,
    CampaignCombatSelectionFacts Decision,
    byte[] CanonicalSelection,
    bool IsRepeat)
{
    internal string CampaignId => PriorSnapshot.CampaignId;

    public static implicit operator CampaignCombatSelectionFrame(
        CampaignNativeCombatDecisionStateV12 selected) =>
        new(selected.PriorSnapshot, selected.PriorSnapshot.World,
            selected.PriorSnapshot.RandomState, selected.StateVersion,
            new(selected.Decision.Choice, selected.Decision.Side,
                selected.Decision.ActingElementId,
                selected.Decision.ContactHandle,
                selected.Decision.ApparentLocationId,
                selected.Decision.BoundRepresentationId,
                selected.Decision.ActionId,
                selected.Decision.PositionId,
                selected.Decision.NextPositionId),
            selected.SerializeCanonical(), false);

    internal static CampaignCombatSelectionFrame FromRepeat(
        CampaignSnapshotV11 initial,
        CampaignNativeRepeatCombatDecisionV31 selected,
        CampaignWorldSnapshotV6 world,
        RandomStreamState randomState)
    {
        if (selected.CampaignId != initial.CampaignId
            || selected.StateVersion <= initial.StateVersion
            || selected.Choice is not ("select-contact-assault"
                or "finish-combat-without-attack"))
            throw new InvalidOperationException(
                "Repeated Combat frame lacks accepted campaign authority.");
        return new(initial, world, randomState, selected.StateVersion,
            new(selected.Choice, selected.Side, selected.ActingElementId,
                selected.ContactHandle, selected.ApparentLocationId,
                selected.BoundRepresentationId, selected.ActionId,
                selected.PositionId, selected.NextPositionId),
            selected.SerializeCanonical(), true);
    }

    internal byte[] SerializeCanonical() => CanonicalSelection;
}
