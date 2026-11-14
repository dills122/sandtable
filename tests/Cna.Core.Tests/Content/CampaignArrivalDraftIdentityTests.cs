using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Content;

public sealed class CampaignArrivalDraftIdentityTests
{
    private static readonly string Rules = "sha256:" + new string('a', 64);
    private static readonly string Ruling = "sha256:" + new string('b', 64);

    [Fact]
    public void IdentityCanonicalizesOrderAndCopiesInputsAndBytes()
    {
        var pack = Pack();
        var groups = new List<ScheduledArrivalGroup> { Group("later-b", "unit-b"), Group("later-a", "unit-a") };
        var a = new List<string> { "west", "east" };
        var destinations = new Dictionary<string, IReadOnlyList<string>>
        {
            ["later-b"] = ["west"],
            ["later-a"] = a,
        };
        var identity = CampaignArrivalDraftIdentity.Create(pack, "minimal-lab", groups, destinations, Rules, Ruling);
        var reordered = CampaignArrivalDraftIdentity.Create(pack, "minimal-lab", groups.AsEnumerable().Reverse(),
            new Dictionary<string, IReadOnlyList<string>> { ["later-a"] = ["east", "west"], ["later-b"] = ["west"] }, Rules, Ruling);
        var original = identity.CopyCanonicalBytes();
        Assert.Equal(original, reordered.CopyCanonicalBytes());
        Assert.Equal("sha256:" + Convert.ToHexStringLower(SHA256.HashData(original)), identity.Hash);
        a.Clear();
        groups.Clear();
        destinations.Clear();
        original[0] = 0;
        Assert.Equal(reordered.CopyCanonicalBytes(), identity.CopyCanonicalBytes());
        Assert.Throws<NotSupportedException>(() => ((IList<string>)identity.Destinations["later-a"]).Clear());
    }

    [Fact]
    public void IdentityBytesRehydrateCandidateAndReplayEvents()
    {
        var original = Identity(Pack());
        var command = new CampaignArrivalDraftCommand(1, "command-a", 0, "later-a", LandSide.Axis, 1, 2, "west");
        var committed = CampaignArrivalReplayDraft.Create(original).Decide(command);
        var restored = CampaignArrivalDraftIdentityReader.Read(original.CopyCanonicalBytes(), original.Hash);
        Assert.Equal(original.Hash, restored.Hash);
        Assert.Equal(original.CopyCanonicalBytes(), restored.CopyCanonicalBytes());
        var replayed = CampaignArrivalReplayDraft.Replay(restored, [CampaignArrivalDraftEventCodec.Decode(CampaignArrivalDraftEventCodec.Encode(committed.Event))]);
        Assert.Equal(committed.State.World.Elements.Select(element => element.ElementId), replayed.World.Elements.Select(element => element.ElementId));
        Assert.True(replayed.Decide(command).IsRetry);
    }

    [Fact]
    public void IdentityReaderRejectsNoncanonicalInvalidAndOversizedInputs()
    {
        var identity = Identity(Pack());
        var bytes = identity.CopyCanonicalBytes();
        Assert.Throws<JsonException>(() => CampaignArrivalDraftIdentityReader.Read(bytes, Ruling));
        Assert.Throws<JsonException>(() => CampaignArrivalDraftIdentityReader.Read([.. bytes, 0x20], identity.Hash));
        var json = Encoding.UTF8.GetString(bytes);
        Assert.Throws<JsonException>(() => CampaignArrivalDraftIdentityReader.Read(Encoding.UTF8.GetBytes(json.Replace("\"owner\":\"axis\"", "\"owner\":\"other\"", StringComparison.Ordinal)), identity.Hash));
        Assert.Throws<JsonException>(() => CampaignArrivalDraftIdentityReader.Read(Encoding.UTF8.GetBytes(json.Replace("\"format\":", "\"extra\":0,\"format\":", StringComparison.Ordinal)), identity.Hash));
        Assert.Throws<JsonException>(() => CampaignArrivalDraftIdentityReader.Read(Encoding.UTF8.GetBytes(json.Replace("\"elements\":[\"unit-a\"]", "\"elements\":[\"missing\"]", StringComparison.Ordinal)), identity.Hash));
        Assert.Throws<JsonException>(() => CampaignArrivalDraftIdentityReader.Read(new byte[4194305], identity.Hash));
    }

    [Fact]
    public void SyntheticProfileAcceptsOnlyItsRepositoryConstructedIdentity()
    {
        var pinned = CampaignArrivalSyntheticProfile.Identity;
        Assert.Equal(CampaignArrivalSyntheticProfile.ProfileHash, pinned.Hash);
        var restored = CampaignArrivalSyntheticProfile.Resolve(pinned.CopyCanonicalBytes(), pinned.Hash);
        var different = Identity(Pack());
        Assert.Equal(pinned.Hash, restored.Hash);
        Assert.Throws<ArgumentException>(() => CampaignArrivalSyntheticProfile.Resolve(different.CopyCanonicalBytes(), different.Hash));
        Assert.Throws<ArgumentException>(() => CampaignArrivalSyntheticProfile.Resolve(pinned.CopyCanonicalBytes(), different.Hash));
        var initial = CampaignArrivalReplayDraft.Create(restored);
        Assert.DoesNotContain(initial.World.Elements, element => element.ElementId == "axis-arrival-lab");
        var accepted = initial.Decide(new(1, "arrival-command", 0, "axis-arrival-group", LandSide.Axis, 1, 1, "south-west"));
        Assert.Contains(accepted.State.World.Elements, element => element.ElementId == "axis-arrival-lab" && element.CurrentLocationId == "south-west");
        Assert.Throws<ArgumentException>(() => initial.Decide(new(1, "wrong-slot", 0, "axis-arrival-group", LandSide.Axis, 1, 2, "south-west")));
    }

    [Fact]
    public void HashBindsContentScheduleDestinationsAndRules()
    {
        var pack = Pack();
        var original = Identity(pack);
        Assert.NotEqual(original.Hash, Identity(pack, stage: 3).Hash);
        Assert.NotEqual(original.Hash, Identity(pack, destination: "east").Hash);
        Assert.NotEqual(original.Hash, Identity(pack, rules: Ruling).Hash);
        Assert.NotEqual(original.Hash, Identity(pack, ruling: Rules).Hash);
        var changedElement = Element("unit-a", 19);
        var changedPack = ContentTestData.Copy(pack, elements: [pack.Elements.Single(x => x.ElementId == "axis-element"), changedElement, Element("unit-b")]);
        Assert.NotEqual(original.Hash, Identity(changedPack).Hash);
    }

    [Fact]
    public void InvalidCandidatesAndDestinationsFailClosed()
    {
        var pack = Pack();
        var groups = new[] { Group("later-a", "unit-a"), Group("later-b", "unit-b") };
        Assert.Throws<ArgumentException>(() => CampaignArrivalDraftIdentity.Create(pack, "minimal-lab", groups,
            new Dictionary<string, IReadOnlyList<string>> { ["later-a"] = ["west"] }, Rules, Ruling));
        Assert.Throws<ArgumentException>(() => CampaignArrivalDraftIdentity.Create(pack, "minimal-lab", groups,
            new Dictionary<string, IReadOnlyList<string>> { ["later-a"] = ["unknown"], ["later-b"] = ["west"] }, Rules, Ruling));
        Assert.Throws<ArgumentException>(() => CampaignArrivalDraftIdentity.Create(pack, "minimal-lab", groups,
            new Dictionary<string, IReadOnlyList<string>> { ["later-a"] = ["west", "west"], ["later-b"] = ["west"] }, Rules, Ruling));
        Assert.Throws<ArgumentException>(() => Identity(pack, rules: "bad-hash"));
        Assert.Throws<ArgumentException>(() => CampaignArrivalDraftIdentity.Create(pack, "minimal-lab",
            [Group("later-a", "unit-a", source: "other"), Group("later-b", "unit-b")], Destinations(), Rules, Ruling));
        Assert.Throws<ArgumentException>(() => CampaignArrivalDraftIdentity.Create(pack, "minimal-lab",
            [Group("later-a", "unit-a", locator: "\ud800"), Group("later-b", "unit-b")], Destinations(), Rules, Ruling));
        Assert.Throws<ArgumentException>(() => CampaignArrivalDraftIdentity.Create(pack, "minimal-lab",
            [Group("later-a", "unit-a", side: LandSide.Commonwealth), Group("later-b", "unit-b")], Destinations(), Rules, Ruling));
        Assert.Throws<InvalidContentPackException>(() => ContentPackArtifact.Create(pack));
    }

    [Fact]
    public void ExtractedWriterPreservesLegacyCanonicalBytesForValidContent()
    {
        var valid = ContentTestData.CreateMinimalPack();
        Assert.Equal(ContentPackSerializer.SerializeCanonical(valid), ContentPackSerializer.SerializeDraftDefinition(valid));
    }

    [Fact]
    public void DeferredContributionCreatesWorldValuesOnceOnlyAtItsScheduledSlot()
    {
        var identity = Identity(Pack());
        var empty = new CampaignWorldSnapshot(CampaignWorldSnapshot.CurrentContractVersion, [], []);
        var draft = CampaignDeferredWorldDraft.Create(identity, empty, "later-a", LandSide.Axis, 1, 2, "west");
        var element = Assert.Single(draft.Elements);
        var representation = Assert.Single(draft.Representations);
        Assert.Equal("unit-a", element.ElementId);
        Assert.Equal("west", element.CurrentLocationId);
        Assert.Equal("west", representation.CurrentLocationId);
        Assert.Equal(["unit-a"], representation.BoundElementIds);
        Assert.Empty(empty.Elements);
        var repeated = CampaignDeferredWorldDraft.Create(identity, empty, "later-a", LandSide.Axis, 1, 2, "west");
        Assert.Equal(representation.RepresentationId, Assert.Single(repeated.Representations).RepresentationId);
        var arrived = new CampaignWorldSnapshot(CampaignWorldSnapshot.CurrentContractVersion, draft.Elements, draft.Representations);
        Assert.Throws<ArgumentException>(() => CampaignDeferredWorldDraft.Create(identity, arrived, "later-a", LandSide.Axis, 1, 2, "west"));
        var orphanBinding = new CampaignWorldSnapshot(CampaignWorldSnapshot.CurrentContractVersion, [],
            [new CampaignMapRepresentationState("existing", "west", CampaignMapRepresentationBindingKind.IndependentElement, ["unit-a"])]);
        Assert.Throws<ArgumentException>(() => CampaignDeferredWorldDraft.Create(identity, orphanBinding, "later-a", LandSide.Axis, 1, 2, "west"));
        Assert.Throws<ArgumentException>(() => CampaignDeferredWorldDraft.Create(identity, empty, "later-a", LandSide.Commonwealth, 1, 2, "west"));
        Assert.Throws<ArgumentException>(() => CampaignDeferredWorldDraft.Create(identity, empty, "later-a", LandSide.Axis, 1, 3, "west"));
        Assert.Throws<ArgumentException>(() => CampaignDeferredWorldDraft.Create(identity, empty, "later-a", LandSide.Axis, 1, 2, "east"));
        Assert.Throws<ArgumentException>(() => CampaignDeferredWorldDraft.Create(identity, empty, "missing", LandSide.Axis, 1, 2, "west"));
    }

    [Fact]
    public void DraftHistoryRebuildsWorldAndIdempotentReceipts()
    {
        var identity = Identity(Pack());
        var first = new CampaignArrivalDraftCommand(1, "command-a", 0, "later-a", LandSide.Axis, 1, 2, "west");
        var second = new CampaignArrivalDraftCommand(1, "command-b", 1, "later-b", LandSide.Axis, 1, 2, "west");
        var initial = CampaignArrivalReplayDraft.Create(identity);
        var baseArtifact = ContentPackArtifact.Create(ContentTestData.CreateMinimalPack());
        var baseWorld = CampaignWorldFactory.CreateInitial(baseArtifact, Assert.Single(baseArtifact.Definition.Scenarios));
        Assert.Equal(Assert.Single(baseWorld.Elements).CurrentLocationId, Assert.Single(initial.World.Elements).CurrentLocationId);
        Assert.Equal(Assert.Single(baseWorld.Representations).RepresentationId, Assert.Single(initial.World.Representations).RepresentationId);
        var a = initial.Decide(first);
        var b = a.State.Decide(second);
        Assert.Equal("axis-element", Assert.Single(initial.World.Elements).ElementId);
        var replayed = CampaignArrivalReplayDraft.Replay(identity, [a.Event, b.Event]);
        Assert.Equal(2, replayed.Version);
        Assert.Collection(replayed.World.Elements,
            element => Assert.Equal("axis-element", element.ElementId),
            element => Assert.Equal("unit-a", element.ElementId), element => Assert.Equal("unit-b", element.ElementId));
        Assert.Equal(b.State.World.Representations.Select(rep => rep.RepresentationId), replayed.World.Representations.Select(rep => rep.RepresentationId));
        var retry = replayed.Decide(first);
        Assert.True(retry.IsRetry);
        Assert.Same(replayed, retry.State);
        Assert.Throws<ArgumentException>(() => replayed.Decide(first with { DestinationId = "east" }));
        Assert.Throws<ArgumentException>(() => replayed.Decide(first with { CommandId = "different", ExpectedVersion = 2 }));
        Assert.Throws<ArgumentException>(() => a.State.Decide(second with { ExpectedVersion = 0 }));
    }

    [Fact]
    public void DraftReplayRejectsForgedMissingReorderedOrRepeatedEvents()
    {
        var identity = Identity(Pack());
        var a = CampaignArrivalReplayDraft.Create(identity).Decide(new(1, "command-a", 0, "later-a", LandSide.Axis, 1, 2, "west"));
        var b = a.State.Decide(new(1, "command-b", 1, "later-b", LandSide.Axis, 1, 2, "west"));
        Assert.Throws<InvalidOperationException>(() => CampaignArrivalReplayDraft.Replay(identity, [b.Event]));
        Assert.Throws<InvalidOperationException>(() => CampaignArrivalReplayDraft.Replay(identity, [b.Event, a.Event]));
        Assert.Throws<InvalidOperationException>(() => CampaignArrivalReplayDraft.Replay(identity, [a.Event, a.Event]));
        Assert.Throws<InvalidOperationException>(() => CampaignArrivalReplayDraft.Replay(Identity(Pack(), stage: 3), [a.Event]));
        Assert.Throws<InvalidOperationException>(() => CampaignArrivalReplayDraft.Replay(identity,
            [new CampaignArrivalDraftEvent(1, identity.Hash, a.Event.Command, 1, ["unit-b"])]));
        Assert.Throws<InvalidOperationException>(() => CampaignArrivalReplayDraft.Replay(identity,
            [new CampaignArrivalDraftEvent(1, identity.Hash, a.Event.Command with { Actor = LandSide.Commonwealth }, 1, ["unit-a"])]));
    }

    [Fact]
    public void EncodedDraftEventsRoundTripAndStillRequireReplayValidation()
    {
        var identity = Identity(Pack());
        var accepted = CampaignArrivalReplayDraft.Create(identity).Decide(new(1, "command-a", 0, "later-a", LandSide.Axis, 1, 2, "west"));
        var bytes = CampaignArrivalDraftEventCodec.Encode(accepted.Event);
        var decoded = CampaignArrivalDraftEventCodec.Decode(bytes);
        Assert.Equal(bytes, CampaignArrivalDraftEventCodec.Encode(decoded));
        Assert.Equal(1, CampaignArrivalReplayDraft.Replay(identity, [decoded]).Version);
        Assert.Throws<JsonException>(() => CampaignArrivalDraftEventCodec.Decode([.. bytes, 0x20]));
        var forged = new CampaignArrivalDraftEvent(1, identity.Hash, decoded.Command, 1, ["unit-b"]);
        var forgedBytes = CampaignArrivalDraftEventCodec.Encode(forged);
        Assert.Throws<InvalidOperationException>(() => CampaignArrivalReplayDraft.Replay(identity, [CampaignArrivalDraftEventCodec.Decode(forgedBytes)]));
    }

    [Fact]
    public void LaterSlotsCannotBypassPendingArrivals()
    {
        var identity = CampaignArrivalDraftIdentity.Create(Pack(), "minimal-lab",
            [Group("later-a", "unit-a", stage: 2), Group("later-b", "unit-b", stage: 3)], Destinations(), Rules, Ruling);
        var initial = CampaignArrivalReplayDraft.Create(identity);
        var late = new CampaignArrivalDraftCommand(1, "late", 0, "later-b", LandSide.Axis, 1, 3, "west");
        Assert.Throws<ArgumentException>(() => initial.Decide(late));
        var first = initial.Decide(new(1, "early", 0, "later-a", LandSide.Axis, 1, 2, "west"));
        var second = first.State.Decide(late with { ExpectedVersion = 1 });
        Assert.Equal(2, CampaignArrivalReplayDraft.Replay(identity, [first.Event, second.Event]).Version);

        var sameSlot = CampaignArrivalReplayDraft.Create(Identity(Pack()));
        var bFirst = sameSlot.Decide(new(1, "b-first", 0, "later-b", LandSide.Axis, 1, 2, "west"));
        var aSecond = bFirst.State.Decide(new(1, "a-second", 1, "later-a", LandSide.Axis, 1, 2, "west"));
        Assert.Equal(2, aSecond.State.Version);
    }

    private static CampaignArrivalDraftIdentity Identity(ContentPackDefinition pack, int stage = 2,
        string destination = "west", string? rules = null, string? ruling = null) =>
        CampaignArrivalDraftIdentity.Create(pack, "minimal-lab", [Group("later-a", "unit-a", stage: stage), Group("later-b", "unit-b")],
            Destinations(destination), rules ?? Rules, ruling ?? Ruling);

    private static Dictionary<string, IReadOnlyList<string>> Destinations(string destination = "west") =>
        new() { ["later-a"] = [destination], ["later-b"] = ["west"] };

    private static ContentPackDefinition Pack()
    {
        var pack = ContentTestData.CreateMinimalPack();
        return ContentTestData.Copy(pack, elements: [.. pack.Elements, Element("unit-a"), Element("unit-b")]);
    }

    private static ContentCombatElement Element(string id, int capability = 20) =>
        new(id, "axis", "axis-formation", "land.organization.battalion",
            Cna1979Movement.MotorizedMobilityId, capability, ContentPlacementMode.Independent,
            ContentTestData.Origin("element." + id));

    private static ScheduledArrivalGroup Group(string id, string unit, int stage = 2,
        string source = "sandtable-rules-lab", string locator = "arrival.test", LandSide side = LandSide.Axis) =>
        new(id, side, 1, stage, [unit], [new RuleReference(source, locator)]);
}
