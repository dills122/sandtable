using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

// Regression coverage for the Slice 4a quadruple in CampaignSourceBattlefieldGroupTable.cs
// (docs/work/plans/2026-11-24-maintainability-refactor-direction.md, Section 2.3/7). Before
// this slice, CampaignSourceBattlefield.cs had zero dedicated *Battlefield* test coverage -
// its Propose/Confirm/Validate methods were only reached indirectly through much larger
// end-to-end exercise/session chains. These tests build minimal, synthetic
// CampaignSourceBattlefield/CampaignGroupedWorld fixtures directly (the same lightweight
// pattern CampaignGroupedWorldTests.cs already uses) so each of the 4 migrating groups'
// *existing, hand-written* methods (PromoteFourthIndian/ProposeCatanzaro/etc.) and the *new*
// generic GroupedWorldMechanic-driven methods (ProposeGroup/ConfirmGroup/ValidateGroup) can
// be exercised side by side on the same inputs and asserted byte-identical - both the
// resulting battlefield state and the exact rejection messages. This is the equivalence
// proof Slice 4b (deleting the old methods) will depend on.
public sealed class CampaignSourceBattlefieldGroupTableTests
{
    // xunit [Theory] data must be a public-facing type; GroupedWorldMechanic is internal,
    // so the 4 migrating groups are named here and resolved back to their mechanic (and to
    // the right old hand-written methods) via MechanicByName/Propose/Confirm/Validate below.
    public static IEnumerable<object[]> GroupNames() =>
    [
        ["FourthIndian"], ["Catanzaro"], ["SeventhArmored"], ["SixteenthBrigade"],
    ];

    private static GroupedWorldMechanic MechanicByName(string name) => name switch
    {
        "FourthIndian" => CampaignSourceBattlefield.FourthIndianMechanic,
        "Catanzaro" => CampaignSourceBattlefield.CatanzaroMechanic,
        "SeventhArmored" => CampaignSourceBattlefield.SeventhArmoredMechanic,
        "SixteenthBrigade" => CampaignSourceBattlefield.SixteenthBrigadeMechanic,
        _ => throw new InvalidOperationException("Unknown mechanic."),
    };

    private static CampaignWorldSnapshotV8 Prior()
    {
        var artifact = Cna1979GrazianiCairoSourceExcerpt.Build();
        return CampaignWorldV8Factory.CreateInitial(artifact, Cna1979GrazianiCairoSourceExcerpt.ScenarioId,
            new CampaignWorldV8Initialization(CapabilityPointAmount.Zero, 0, CampaignElementReserveStatus.None));
    }

    private static CampaignSourceBattlefield Battlefield(GroupedWorldMechanic mechanic, string status) =>
        CampaignSourceBattlefield.CreateForTesting(mechanic.ExpectedMemberIds.Select(id =>
            new CampaignSourceBattlefieldEntry(id, mechanic.SideId, mechanic.LocationId, status)));

    private static CampaignGroupedWorld World(GroupedWorldMechanic mechanic, bool confirmed, int motorPoolOverride = 0)
    {
        var prior = Prior();
        return CampaignGroupedWorld.Create(prior, CampaignSourceFourthIndianDefinition.Hash(),
            [Formation(prior, mechanic, confirmed, motorPoolOverride)]);
    }

    // Sixteenth Brigade alone is "world8-initial": unlike the other 3 (which arrive later
    // via source-authority admission), its 4 elements are already independently placed on
    // the real starting map, so CampaignGroupedWorld requires usesRetainedMembers: true and
    // strictly validates the group's members against those already-placed elements - this
    // mirrors CampaignSourceSixteenthBrigadeGroupedDefinition.Create's own production logic.
    private static CampaignGroupedFormation Formation(CampaignWorldSnapshotV8 prior, GroupedWorldMechanic mechanic,
        bool confirmed, int motorPoolOverride = 0)
    {
        var motorPool = motorPoolOverride != 0 ? motorPoolOverride : mechanic.MotorPool ?? 0;
        if (ReferenceEquals(mechanic, CampaignSourceBattlefield.SixteenthBrigadeMechanic))
        {
            var retained = mechanic.ExpectedMemberIds.Select(id =>
            {
                var element = prior.World.Elements.Single(value => value.ElementId == id);
                var component = element.Components.Single();
                var type = component.ComponentId[(id.Length + 1)..];
                return new CampaignGroupedMember(id, id == mechanic.RootId ? null : mechanic.RootId,
                    type == "infantry" ? component.CurrentToe : null, 0, false, type, component.CurrentToe,
                    10, element.OperationalState.CapabilityPointsExpended, element.OperationalState.CohesionLevel);
            });
            return new CampaignGroupedFormation(mechanic.GroupId, mechanic.SideId, mechanic.RootId, mechanic.LocationId,
                confirmed ? mechanic.ConfirmedStackValue : null, motorPool, retained, confirmed, usesRetainedMembers: true);
        }
        var members = mechanic.ExpectedMemberIds.Select(id => new CampaignGroupedMember(id,
            id == mechanic.RootId ? null : mechanic.RootId, null, 0, false, null, null, 10, CapabilityPointAmount.Zero, 0));
        return new CampaignGroupedFormation(mechanic.GroupId, mechanic.SideId, mechanic.RootId, mechanic.LocationId,
            confirmed ? mechanic.ConfirmedStackValue : null, motorPool, members, confirmed);
    }

    // Old vs. new: the hand-written Propose/Confirm methods for each of the 4 groups,
    // invoked on the SAME starting battlefield as the new generic ones, must produce
    // identical resulting entries.
    [Fact]
    public void FourthIndianOldAndNewProposeConfirmProduceIdenticalBattlefields() =>
        AssertOldAndNewAgree(CampaignSourceBattlefield.FourthIndianMechanic,
            (b, w) => b.PromoteFourthIndian(w), (b, p, n) => b.ConfirmFourthIndian(p, n), (b, w) => b.ValidateFourthIndian(w));

    [Fact]
    public void CatanzaroOldAndNewProposeConfirmProduceIdenticalBattlefields() =>
        AssertOldAndNewAgree(CampaignSourceBattlefield.CatanzaroMechanic,
            (b, w) => b.ProposeCatanzaro(w), (b, p, n) => b.ConfirmCatanzaro(p, n), (b, w) => b.ValidateCatanzaro(w));

    [Fact]
    public void SeventhArmoredOldAndNewProposeConfirmProduceIdenticalBattlefields() =>
        AssertOldAndNewAgree(CampaignSourceBattlefield.SeventhArmoredMechanic,
            (b, w) => b.ProposeSeventhArmored(w), (b, p, n) => b.ConfirmSeventhArmored(p, n), (b, w) => b.ValidateSeventhArmored(w));

    [Fact]
    public void SixteenthBrigadeOldAndNewProposeConfirmProduceIdenticalBattlefields() =>
        AssertOldAndNewAgree(CampaignSourceBattlefield.SixteenthBrigadeMechanic,
            (b, w) => b.ProposeSixteenthBrigade(w), (b, p, n) => b.ConfirmSixteenthBrigade(p, n), (b, w) => b.ValidateSixteenthBrigade(w));

    private static void AssertOldAndNewAgree(GroupedWorldMechanic mechanic,
        Func<CampaignSourceBattlefield, CampaignGroupedWorld, CampaignSourceBattlefield> oldPropose,
        Func<CampaignSourceBattlefield, CampaignGroupedWorld, CampaignGroupedWorld, CampaignSourceBattlefield> oldConfirm,
        Action<CampaignSourceBattlefield, CampaignGroupedWorld> oldValidate)
    {
        var initial = Battlefield(mechanic, mechanic.InitialStatus);
        var unconfirmedWorld = World(mechanic, confirmed: false);

        var oldProposed = oldPropose(initial, unconfirmedWorld);
        var newProposed = initial.ProposeGroup(unconfirmedWorld, mechanic);
        Assert.Equal(oldProposed.Entries, newProposed.Entries);

        // Propose only transitions the battlefield's entries; the grouped world's group
        // stays unconfirmed (DispositionConfirmed: false) until ConfirmDisposition runs, so
        // `unconfirmedWorld` itself is still the right "prior" world for Confirm.
        var confirmedWorld = unconfirmedWorld.ConfirmDisposition(mechanic.GroupId, mechanic.ConfirmedStackValue);

        var oldConfirmed = oldConfirm(oldProposed, unconfirmedWorld, confirmedWorld);
        var newConfirmed = newProposed.ConfirmGroup(unconfirmedWorld, confirmedWorld, mechanic);
        Assert.Equal(oldConfirmed.Entries, newConfirmed.Entries);

        oldValidate(oldConfirmed, confirmedWorld);
        oldConfirmed.ValidateGroup(confirmedWorld, mechanic);
    }

    [Theory]
    [MemberData(nameof(GroupNames))]
    public void ProposingAnAlreadyConfirmedGroupRejectsIdenticallyOldAndNew(string groupName)
    {
        var mechanic = MechanicByName(groupName);
        var battlefield = Battlefield(mechanic, mechanic.InitialStatus);
        var confirmedWorld = World(mechanic, confirmed: true);

        var oldException = Assert.Throws<InvalidOperationException>(() => Propose(battlefield, mechanic, confirmedWorld));
        var newException = Assert.Throws<InvalidOperationException>(() => battlefield.ProposeGroup(confirmedWorld, mechanic));
        Assert.Equal(mechanic.AlreadyProposedMessage, oldException.Message);
        Assert.Equal(oldException.Message, newException.Message);
    }

    [Theory]
    [MemberData(nameof(GroupNames))]
    public void ConfirmingBeforeProposingRejectsIdenticallyOldAndNew(string groupName)
    {
        var mechanic = MechanicByName(groupName);
        var battlefield = Battlefield(mechanic, mechanic.InitialStatus);
        var unconfirmedWorld = World(mechanic, confirmed: false);
        var confirmedWorld = unconfirmedWorld.ConfirmDisposition(mechanic.GroupId, mechanic.ConfirmedStackValue);

        var oldException = Assert.Throws<InvalidOperationException>(() => Confirm(battlefield, mechanic, unconfirmedWorld, confirmedWorld));
        var newException = Assert.Throws<InvalidOperationException>(() => battlefield.ConfirmGroup(unconfirmedWorld, confirmedWorld, mechanic));
        Assert.Equal(mechanic.OccupancyMessage, oldException.Message);
        Assert.Equal(oldException.Message, newException.Message);
    }

    [Theory]
    [MemberData(nameof(GroupNames))]
    public void ConfirmingWithTheWrongStackValueRejectsIdenticallyOldAndNew(string groupName)
    {
        var mechanic = MechanicByName(groupName);
        var battlefield = Battlefield(mechanic, mechanic.ProposedStatus);
        var unconfirmedWorld = World(mechanic, confirmed: false);
        var wrongStackValue = mechanic.ConfirmedStackValue + 100;
        var driftedWorld = unconfirmedWorld.ConfirmDisposition(mechanic.GroupId, wrongStackValue);

        var oldException = Assert.Throws<InvalidOperationException>(() => Confirm(battlefield, mechanic, unconfirmedWorld, driftedWorld));
        var newException = Assert.Throws<InvalidOperationException>(() => battlefield.ConfirmGroup(unconfirmedWorld, driftedWorld, mechanic));
        Assert.Equal(mechanic.ConfirmationDriftMessage, oldException.Message);
        Assert.Equal(oldException.Message, newException.Message);
    }

    [Theory]
    [MemberData(nameof(GroupNames))]
    public void ValidatingAWorldWithAWrongMotorPoolRejectsIdenticallyOldAndNew(string groupName)
    {
        var mechanic = MechanicByName(groupName);
        if (mechanic.MotorPool is null) return; // FourthIndian's Require does not check MotorPool at all - nothing to assert.
        var battlefield = Battlefield(mechanic, mechanic.ProposedStatus);
        var prior = Prior();
        var drifted = CampaignGroupedWorld.Create(prior, CampaignSourceFourthIndianDefinition.Hash(),
            [Formation(prior, mechanic, confirmed: false, motorPoolOverride: mechanic.MotorPool.Value + 5)]);

        var oldException = Assert.Throws<InvalidOperationException>(() => Validate(battlefield, mechanic, drifted));
        var newException = Assert.Throws<InvalidOperationException>(() => battlefield.ValidateGroup(drifted, mechanic));
        Assert.Equal(mechanic.OccupancyMessage, oldException.Message);
        Assert.Equal(oldException.Message, newException.Message);
    }

    private static CampaignSourceBattlefield Propose(CampaignSourceBattlefield battlefield, GroupedWorldMechanic mechanic, CampaignGroupedWorld world) =>
        mechanic.GroupId switch
        {
            "4-indian-division" => battlefield.PromoteFourthIndian(world),
            _ when mechanic.GroupId == CampaignSourceCatanzaroGroupedDefinition.GroupId => battlefield.ProposeCatanzaro(world),
            _ when mechanic.GroupId == CampaignSourceSeventhArmoredGroupedDefinition.GroupId => battlefield.ProposeSeventhArmored(world),
            _ when mechanic.GroupId == CampaignSourceSixteenthBrigadeGroupedDefinition.GroupId => battlefield.ProposeSixteenthBrigade(world),
            _ => throw new InvalidOperationException("Unknown mechanic."),
        };

    private static CampaignSourceBattlefield Confirm(CampaignSourceBattlefield battlefield, GroupedWorldMechanic mechanic,
        CampaignGroupedWorld prior, CampaignGroupedWorld next) => mechanic.GroupId switch
        {
            "4-indian-division" => battlefield.ConfirmFourthIndian(prior, next),
            _ when mechanic.GroupId == CampaignSourceCatanzaroGroupedDefinition.GroupId => battlefield.ConfirmCatanzaro(prior, next),
            _ when mechanic.GroupId == CampaignSourceSeventhArmoredGroupedDefinition.GroupId => battlefield.ConfirmSeventhArmored(prior, next),
            _ when mechanic.GroupId == CampaignSourceSixteenthBrigadeGroupedDefinition.GroupId => battlefield.ConfirmSixteenthBrigade(prior, next),
            _ => throw new InvalidOperationException("Unknown mechanic."),
        };

    private static void Validate(CampaignSourceBattlefield battlefield, GroupedWorldMechanic mechanic, CampaignGroupedWorld world)
    {
        switch (mechanic.GroupId)
        {
            case "4-indian-division": battlefield.ValidateFourthIndian(world); return;
            case var id when id == CampaignSourceCatanzaroGroupedDefinition.GroupId: battlefield.ValidateCatanzaro(world); return;
            case var id when id == CampaignSourceSeventhArmoredGroupedDefinition.GroupId: battlefield.ValidateSeventhArmored(world); return;
            case var id when id == CampaignSourceSixteenthBrigadeGroupedDefinition.GroupId: battlefield.ValidateSixteenthBrigade(world); return;
            default: throw new InvalidOperationException("Unknown mechanic.");
        }
    }
}
