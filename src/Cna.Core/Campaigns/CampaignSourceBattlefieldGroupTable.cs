using Cna.Core.Content;

namespace Cna.Core.Campaigns;

// Slice 4a of docs/work/plans/2026-11-24-maintainability-refactor-direction.md, Section
// 2.3: a generic Propose/Confirm/Validate/Require quadruple for the single-group
// mechanics in CampaignSourceBattlefield.cs that share the "clean quadruple" shape (one
// GroupId, no groupId-dispatch parameter, no extra Detach step). Purely additive this
// pass - the hand-written PromoteXxx/ConfirmXxx/ValidateXxx methods on
// CampaignSourceBattlefield are untouched and remain what production code calls; deleting
// them and repointing callers at this table is Slice 4b, a separate future change.
//
// Migrated here (4 groups, confirmed by direct read of CampaignSourceBattlefield.cs):
// FourthIndian, Catanzaro, SeventhArmored, SixteenthBrigade.
//
// ItalianDivisions does NOT migrate, despite the plan doc listing it as "likely" fitting
// pending a full read. A full read of ProposeItalianDivisions/ConfirmItalianDivision/
// ValidateItalianDivisions/RequireItalianDivision shows it is actually shaped like
// **Libyan** (which the doc already correctly excludes as "already parameterized
// differently"): Propose operates on *two* groups at once (cirene-division and
// marmarica-division), Confirm and the private Require both take an explicit groupId
// parameter and dispatch on it via a switch, and Validate iterates both groups. That is
// the dual-group/groupId-dispatch shape, not the single-GroupId clean quadruple this table
// models. Forcing it into GroupedWorldMechanic would misrepresent the finding the plan
// doc asked this slice to confirm - so, like Libyan, it stays hand-written. (It could in
// principle share a future "dual-group" generalization with Libyan, but that is new scope
// beyond this slice, not a 4a migration.)
//
// Per-group idiosyncrasies preserved as data/flags (not approximated) below, found by
// direct comparison of the four RequireXxx/ConfirmXxx bodies:
//  - FourthIndian's Require skips the MotorPool check, the StackValue check, and the
//    "effective occupant" (CampaignGroupedWorld.OccupantsAt) check entirely - the other
//    three all include some form of each. FourthIndian's Confirm also checks
//    `Groups.Count == 1` instead of excluding-this-group-and-comparing, and omits the
//    Detached/ExternalReservations equality checks the other three perform; it compares
//    members with a plain SequenceEqual instead of the AdditionalComponents-aware
//    SameGroupedMembers helper. These are genuine behavioral differences, not naming
//    variants, so they are carried as explicit flags rather than silently normalized.
//  - SeventhArmored's confirmed StackValue is drawn from a set ({3, 5}), not a single
//    value, because CampaignSourceSeventhArmoredGroupedDefinition's own shell-correction
//    path can later move it from 5 to 3 without re-running this battlefield's Confirm.
//  - SixteenthBrigade's initial required status is "world8-initial" (not "source-evidence"
//    like the other three), its occupants are selected by element id rather than by
//    location (other units may share its hex), its "effective occupant" check counts
//    matching-formation occupants rather than requiring exactly one occupant overall, and
//    it alone checks for independent-representation drift against its member ids.
internal sealed record GroupedWorldMechanic(
    string GroupId,
    string SideId,
    string RootId,
    string LocationId,
    IReadOnlyList<string> ExpectedMemberIds,
    string InitialStatus,
    string ProposedStatus,
    string GroupedStatus,
    int ConfirmedStackValue,
    string AlreadyProposedMessage,
    string ConfirmationDriftMessage,
    string OccupancyMessage,
    int? MotorPool = null,
    IReadOnlySet<int>? SustainedStackValues = null,
    bool CheckEffectiveOccupancy = true,
    bool StrictSingleEffectiveOccupant = true,
    bool OccupantsByElementId = false,
    bool CheckIndependentDrift = false,
    bool OtherGroupsCheckedByCount = false,
    bool UseRawMemberEquality = false);

internal sealed partial class CampaignSourceBattlefield
{
    internal static readonly GroupedWorldMechanic FourthIndianMechanic = new(
        GroupId: "4-indian-division", SideId: "commonwealth", RootId: "4-indian-division-hq", LocationId: "d3615",
        ExpectedMemberIds: Array.AsReadOnly(CampaignSourceFourthIndianDefinition.Members.Select(value => value.Id).ToArray()),
        InitialStatus: "source-evidence",
        ProposedStatus: "source-fourth-indian-proposed", GroupedStatus: "source-fourth-indian-grouped",
        ConfirmedStackValue: 5,
        AlreadyProposedMessage: "Fourth Indian disposition must be proposed before confirmation.",
        ConfirmationDriftMessage: "Fourth Indian confirmation changed its proposed world.",
        OccupancyMessage: "Fourth Indian group differs from retained battlefield occupancy.",
        MotorPool: null, SustainedStackValues: null, CheckEffectiveOccupancy: false,
        OtherGroupsCheckedByCount: true, UseRawMemberEquality: true);

    internal static readonly GroupedWorldMechanic CatanzaroMechanic = new(
        GroupId: CampaignSourceCatanzaroGroupedDefinition.GroupId, SideId: "axis",
        RootId: CampaignSourceCatanzaroGroupedDefinition.RootId,
        LocationId: Cna1979GrazianiSourceFacts.CatanzaroInitialChartCandidate.LocationId,
        ExpectedMemberIds: Array.AsReadOnly(Cna1979GrazianiSourceFacts.CatanzaroInitialChartCandidate.ElementIds.ToArray()),
        InitialStatus: "source-evidence",
        ProposedStatus: "source-catanzaro-proposed", GroupedStatus: "source-catanzaro-grouped",
        ConfirmedStackValue: 5,
        AlreadyProposedMessage: "Catanzaro must first be proposed.",
        ConfirmationDriftMessage: "Catanzaro confirmation changed its proposed world.",
        OccupancyMessage: "Catanzaro grouped world differs from retained battlefield occupancy.",
        MotorPool: 35, SustainedStackValues: new HashSet<int> { 5 });

    internal static readonly GroupedWorldMechanic SeventhArmoredMechanic = new(
        GroupId: CampaignSourceSeventhArmoredGroupedDefinition.GroupId, SideId: "commonwealth",
        RootId: CampaignSourceSeventhArmoredGroupedDefinition.RootId,
        LocationId: Cna1979GrazianiSourceFacts.SeventhArmoredDivisionInitialCandidate.LocationId,
        ExpectedMemberIds: Array.AsReadOnly(Cna1979GrazianiSourceFacts.SeventhArmoredDivisionInitialCandidate.ElementIds.ToArray()),
        InitialStatus: "source-evidence",
        ProposedStatus: "source-seventh-armored-proposed", GroupedStatus: "source-seventh-armored-grouped",
        ConfirmedStackValue: 5,
        AlreadyProposedMessage: "Seventh Armored must first be proposed.",
        ConfirmationDriftMessage: "Seventh Armored confirmation changed its proposed world.",
        OccupancyMessage: "Seventh Armored grouped world differs from retained battlefield occupancy.",
        MotorPool: 10, SustainedStackValues: new HashSet<int> { 3, 5 });

    internal static readonly GroupedWorldMechanic SixteenthBrigadeMechanic = new(
        GroupId: CampaignSourceSixteenthBrigadeGroupedDefinition.GroupId, SideId: "commonwealth",
        RootId: CampaignSourceSixteenthBrigadeGroupedDefinition.RootId,
        LocationId: Cna1979GrazianiSourceFacts.SixteenthInfantryBrigadeInitialCandidate.LocationId,
        ExpectedMemberIds: Array.AsReadOnly(Cna1979GrazianiSourceFacts.SixteenthInfantryBrigadeInitialCandidate.ElementIds.ToArray()),
        InitialStatus: "world8-initial",
        ProposedStatus: "source-sixteenth-brigade-proposed", GroupedStatus: "source-sixteenth-brigade-grouped",
        ConfirmedStackValue: 2,
        AlreadyProposedMessage: "Sixteenth Brigade must first be proposed.",
        ConfirmationDriftMessage: "Sixteenth Brigade confirmation changed its proposed world.",
        OccupancyMessage: "Sixteenth Brigade grouped world differs from retained battlefield occupancy.",
        MotorPool: 20, SustainedStackValues: new HashSet<int> { 2 },
        StrictSingleEffectiveOccupant: false, OccupantsByElementId: true, CheckIndependentDrift: true);

    // Test-only construction hook: the primary constructor is private and the only
    // production entry point is Create(CampaignSourceAuthorityState), which requires a
    // fully assembled pregame state. This lets tests build a battlefield with handcrafted
    // entries to exercise the quadruple methods (old and new) in isolation.
    internal static CampaignSourceBattlefield CreateForTesting(IEnumerable<CampaignSourceBattlefieldEntry> entries,
        IEnumerable<CampaignSourcePendingGroup>? pending = null) => new(entries, pending ?? []);

    internal CampaignSourceBattlefield ProposeGroup(CampaignGroupedWorld world, GroupedWorldMechanic mechanic)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(mechanic);
        RequireGroup(world, mechanic, mechanic.InitialStatus);
        var group = world.Groups.Single(value => value.GroupId == mechanic.GroupId);
        if (group.DispositionConfirmed) throw new InvalidOperationException(mechanic.AlreadyProposedMessage);
        var ids = mechanic.ExpectedMemberIds.ToHashSet(StringComparer.Ordinal);
        return new CampaignSourceBattlefield(entries.Select(value => ids.Contains(value.ElementId)
            ? value with { AuthorityStatus = mechanic.ProposedStatus } : value), pending,
            cairoSetupIncluded, cairoSetupOriginHash, axisFreeArtilleryOriginHash,
            axisCorpsArtilleryOriginHash, matruhOriginHash, cwFreeGunsOriginHash);
    }

    internal CampaignSourceBattlefield ConfirmGroup(CampaignGroupedWorld priorWorld,
        CampaignGroupedWorld nextWorld, GroupedWorldMechanic mechanic)
    {
        ArgumentNullException.ThrowIfNull(priorWorld);
        ArgumentNullException.ThrowIfNull(nextWorld);
        ArgumentNullException.ThrowIfNull(mechanic);
        RequireGroup(priorWorld, mechanic, mechanic.ProposedStatus);
        var before = priorWorld.Groups.Single(value => value.GroupId == mechanic.GroupId);
        var after = nextWorld.Groups.Single(value => value.GroupId == before.GroupId);
        var memberMismatch = mechanic.UseRawMemberEquality
            ? !before.Members.SequenceEqual(after.Members)
            : !SameGroupedMembers(before.Members, after.Members);
        var otherStateMismatch = mechanic.OtherGroupsCheckedByCount
            ? priorWorld.Groups.Count != 1 || nextWorld.Groups.Count != 1
            : !priorWorld.Groups.Where(value => value.GroupId != before.GroupId).SequenceEqual(nextWorld.Groups.Where(value => value.GroupId != before.GroupId))
                || !priorWorld.Detached.SequenceEqual(nextWorld.Detached) || !priorWorld.ExternalReservations.SequenceEqual(nextWorld.ExternalReservations);
        if (before.DispositionConfirmed || !after.DispositionConfirmed
            || before.StackValue is not null || after.StackValue != mechanic.ConfirmedStackValue
            || priorWorld.Prior != nextWorld.Prior || priorWorld.ContentHash != nextWorld.ContentHash
            || before.RootId != after.RootId || before.SideId != after.SideId || before.LocationId != after.LocationId
            || before.MotorPool != after.MotorPool || memberMismatch || otherStateMismatch)
            throw new InvalidOperationException(mechanic.ConfirmationDriftMessage);
        var ids = before.Members.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        return new CampaignSourceBattlefield(entries.Select(value => ids.Contains(value.ElementId)
            ? value with { AuthorityStatus = mechanic.GroupedStatus } : value), pending,
            cairoSetupIncluded, cairoSetupOriginHash, axisFreeArtilleryOriginHash,
            axisCorpsArtilleryOriginHash, matruhOriginHash, cwFreeGunsOriginHash);
    }

    internal void ValidateGroup(CampaignGroupedWorld world, GroupedWorldMechanic mechanic)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(mechanic);
        var group = world.Groups.Single(value => value.GroupId == mechanic.GroupId);
        RequireGroup(world, mechanic, group.DispositionConfirmed ? mechanic.GroupedStatus : mechanic.ProposedStatus);
    }

    private void RequireGroup(CampaignGroupedWorld world, GroupedWorldMechanic mechanic, string status)
    {
        ArgumentNullException.ThrowIfNull(world);
        var ids = mechanic.ExpectedMemberIds.Order(StringComparer.Ordinal).ToArray();
        var group = world.Groups.SingleOrDefault(value => value.GroupId == mechanic.GroupId);
        var occupants = (mechanic.OccupantsByElementId
            ? entries.Where(value => ids.Contains(value.ElementId, StringComparer.Ordinal))
            : entries.Where(value => value.LocationId == mechanic.LocationId)).ToArray();
        var effective = world.OccupantsAt(mechanic.LocationId);
        var effectiveMismatch = mechanic.CheckEffectiveOccupancy && (mechanic.StrictSingleEffectiveOccupant
            ? effective.Count != 1 || effective[0].Formation?.GroupId != mechanic.GroupId
            : effective.Count(value => value.Formation?.GroupId == mechanic.GroupId) != 1);
        var independentDrift = mechanic.CheckIndependentDrift
            && effective.Any(value => value.Independent?.BoundElementIds.Any(id => ids.Contains(id, StringComparer.Ordinal)) == true);
        if (group is null || group.SideId != mechanic.SideId || group.LocationId != mechanic.LocationId
            || group.RootId != mechanic.RootId
            || (mechanic.MotorPool is { } motorPool && group.MotorPool != motorPool)
            || (mechanic.SustainedStackValues is not null && (group.DispositionConfirmed
                    ? !mechanic.SustainedStackValues.Contains(group.StackValue ?? int.MinValue)
                    : group.StackValue is not null))
            || !group.Members.Select(value => value.ElementId).Order(StringComparer.Ordinal).SequenceEqual(ids)
            || occupants.Length != ids.Length
            || !occupants.Select(value => value.ElementId).Order(StringComparer.Ordinal).SequenceEqual(ids)
            || occupants.Any(value => value.SideId != mechanic.SideId
                || (mechanic.OccupantsByElementId && value.LocationId != mechanic.LocationId)
                || value.AuthorityStatus != status)
            || effectiveMismatch || independentDrift)
            throw new InvalidOperationException(mechanic.OccupancyMessage);
    }
}
