using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Exercises;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignScheduledWorld6Tests
{
    private static CampaignScheduledWorld6Command Command() => new(1, "campaign-a", CampaignScheduledWorld6Phase.SetupHash,
        CampaignScheduledWorld6CreatedCodec.CalculateHash(Creation()), "request-1", 7, LandSide.Axis, "north-west");

    private static CampaignCreationRequest CreationRequest(string setupHash, string campaignId = "campaign-a", ulong seed = 12345) => new(1, campaignId,
        Cna1979Ruleset.Manifest.Hash, seed, CampaignScheduledWorld6Phase.SetupId, setupHash,
        Cna1979SyntheticContentCatalog.ArtifactV6.Identity.PackId, Cna1979SyntheticContentCatalog.ArtifactV6.Identity.Hash,
        "breakdown-truck-lab");

    private static CampaignScheduledWorld6Created Creation() => CampaignScheduledWorld6Creation.Create(CreationRequest(CampaignScheduledWorld6Phase.SetupHash));

    private static CampaignScheduledWorld6PreambleEvents.VerifiedCut Verified(CampaignCreationRequest request, CampaignScheduledWorld6Created creation) =>
        CampaignScheduledWorld6PreambleEvents.Verify(request, creation, CampaignScheduledWorld6PreambleEvents.Create(request, creation)).CompleteCut
        ?? throw new InvalidOperationException("Expected a complete preamble.");

    private static CampaignScheduledWorld6Phase Start() => CampaignScheduledWorld6Phase.Start(
        Verified(CreationRequest(CampaignScheduledWorld6Phase.SetupHash), Creation()));

    [Fact]
    public void CreationBindsCertifiedContentSetupSeedAndPendingWorld()
    {
        var request = CreationRequest(CampaignScheduledWorld6Phase.SetupHash);
        var currentAdmission = CampaignCreationExecution.Execute(request);
        Assert.False(currentAdmission.IsCreated);
        Assert.Equal(CampaignCreationRejectionReason.UnknownSetup, currentAdmission.RejectionReason);
        var created = CampaignScheduledWorld6Creation.Create(request);
        Assert.Equal(1, created.StateVersion);
        Assert.Equal(0UL, created.RandomState.NextByteCursor);
        Assert.Equal(Cna1979LandSequenceV4.CreateTurn(1)[0], created.Position);
        Assert.DoesNotContain(created.World.Elements, value => value.ElementId == "axis-infantry");
        Assert.Equal(created, CampaignScheduledWorld6Creation.Replay(request, created));
        var bytes = CampaignScheduledWorld6CreatedCodec.Encode(created);
        Assert.Equal(created, CampaignScheduledWorld6CreatedCodec.Decode(bytes, request));
        Assert.Throws<JsonException>(() => CampaignScheduledWorld6CreatedCodec.Decode([.. bytes, 0x20], request));
        Assert.Throws<ArgumentException>(() => CampaignScheduledWorld6Creation.Create(CreationRequest("sha256:incorrect")));
        Assert.Throws<InvalidOperationException>(() => CampaignScheduledWorld6Creation.Replay(request, created with { Seed = 54321 }));
        var artifact = Cna1979SyntheticContentCatalog.ArtifactV6;
        var scenario = Assert.Single(artifact.Definition.LegacyDefinition.Scenarios);
        Assert.Throws<InvalidOperationException>(() => CampaignScheduledWorld6Creation.Replay(request,
            created with { World = CampaignWorldV6Factory.CreateInitial(artifact, scenario) }));
        Assert.Throws<InvalidOperationException>(() => CampaignScheduledWorld6CreatedCodec.Encode(
            created with { World = CampaignWorldV6Factory.CreateInitial(artifact, scenario) }));
    }

    [Fact]
    public void SetupRequiresPinnedBytesEvenWithAConsistentForeignHash()
    {
        var bytes = CampaignScheduledWorld6Phase.CopySetupBytes();
        Assert.Equal(CampaignScheduledWorld6Phase.PinnedSetupHash, CampaignScheduledWorld6Phase.SetupHash);
        Assert.True(CampaignScheduledWorld6Phase.IsSupportedSetup(bytes, CampaignScheduledWorld6Phase.SetupHash));
        bytes[0] = 0;
        var foreignHash = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));
        Assert.False(CampaignScheduledWorld6Phase.IsSupportedSetup(bytes, foreignHash));
        Assert.True(CampaignScheduledWorld6Phase.IsSupportedSetup(CampaignScheduledWorld6Phase.CopySetupBytes(), CampaignScheduledWorld6Phase.SetupHash));
    }

    [Fact]
    public void ArrivalRestoresCertifiedWorldAndCombatStateExactly()
    {
        var artifact = Cna1979SyntheticContentCatalog.ArtifactV6;
        var scenario = Assert.Single(artifact.Definition.LegacyDefinition.Scenarios);
        var pending = CampaignScheduledWorld6.CreateInitial();
        Assert.DoesNotContain(pending.Elements, value => value.ElementId == "axis-infantry");
        Assert.DoesNotContain(pending.Representations, value => value.BoundElementIds.Contains("axis-infantry"));
        Assert.False(CampaignWorldV6Validator.IsValidInitial(pending, artifact, scenario));
        var arrived = CampaignScheduledWorld6.Arrive(pending, LandSide.Axis, CampaignScheduledWorld6.ArrivalPosition(), "north-west");
        var certified = CampaignWorldV6Factory.CreateInitial(artifact, scenario);
        Assert.Equal(certified, arrived);
        Assert.True(CampaignWorldV6Validator.IsValidInitial(arrived, artifact, scenario));
        var element = Assert.Single(arrived.Elements, value => value.ElementId == "axis-infantry");
        Assert.Equal(5, Assert.Single(element.Components).CurrentToe);
        Assert.Equal("north-west", element.CurrentLocationId);
        Assert.Throws<ArgumentException>(() => CampaignScheduledWorld6.Arrive(arrived, LandSide.Axis, CampaignScheduledWorld6.ArrivalPosition(), "north-west"));
    }

    [Fact]
    public void ArrivalRejectsWrongActorPhaseDestinationAndModifiedWorld()
    {
        var pending = CampaignScheduledWorld6.CreateInitial();
        var position = CampaignScheduledWorld6.ArrivalPosition();
        Assert.Throws<ArgumentException>(() => CampaignScheduledWorld6.Arrive(pending, LandSide.Commonwealth, position, "north-west"));
        Assert.Throws<ArgumentException>(() => CampaignScheduledWorld6.Arrive(pending, LandSide.Axis, Cna1979LandSequenceV4.CreateTurn(1)[0], "north-west"));
        Assert.Throws<ArgumentException>(() => CampaignScheduledWorld6.Arrive(pending, LandSide.Axis, position, "west"));
        var incomplete = new CampaignWorldSnapshotV6(CampaignWorldSnapshotV6.CurrentContractVersion,
            pending.Elements.Skip(1), pending.Representations, pending.BrokenVehicleLots);
        Assert.Throws<ArgumentException>(() => CampaignScheduledWorld6.Arrive(incomplete, LandSide.Axis, position, "north-west"));
        Assert.Equal(3, pending.Elements.Count);
    }

    [Fact]
    public void PhaseReplaysCertifiedArrivalAndPreservesRetryReceipt()
    {
        var request = CreationRequest(CampaignScheduledWorld6Phase.SetupHash);
        var creation = Creation();
        var proof = Verified(request, creation);
        var phase = CampaignScheduledWorld6Phase.Start(proof);
        var accepted = phase.Decide(Command());
        Assert.Equal(8, accepted.State.Version);
        Assert.Equal(LandSegmentIds.FleetAssignment, accepted.State.Position.SegmentId);
        Assert.Equal("axis-infantry", accepted.Event.ElementId);
        var replayed = CampaignScheduledWorld6Phase.Replay(proof, [accepted.Event]);
        Assert.Equal(accepted.State.World, replayed.World);
        Assert.True(replayed.Decide(Command()).IsRetry);
        Assert.Throws<ArgumentException>(() => replayed.Decide(Command() with { DestinationId = "west" }));
        var otherRequest = CreationRequest(CampaignScheduledWorld6Phase.SetupHash, "campaign-b");
        var otherCreation = CampaignScheduledWorld6Creation.Create(otherRequest);
        var otherCut = Verified(otherRequest, otherCreation);
        Assert.Throws<InvalidOperationException>(() => CampaignScheduledWorld6Phase.Replay(otherCut, [accepted.Event]));
        var otherSeedRequest = CreationRequest(CampaignScheduledWorld6Phase.SetupHash, seed: 54321);
        var otherSeedCreation = CampaignScheduledWorld6Creation.Create(otherSeedRequest);
        var otherSeedCut = Verified(otherSeedRequest, otherSeedCreation);
        Assert.Throws<InvalidOperationException>(() => CampaignScheduledWorld6Phase.Replay(otherSeedCut, [accepted.Event]));
        Assert.Throws<InvalidOperationException>(() => CampaignScheduledWorld6Phase.Replay(proof, [accepted.Event with { Command = accepted.Event.Command with { ExpectedVersion = 8 } }]));
        Assert.Throws<InvalidOperationException>(() => CampaignScheduledWorld6Phase.Replay(proof, [accepted.Event with { ElementId = "axis-truck" }]));
        Assert.Throws<InvalidOperationException>(() => CampaignScheduledWorld6Phase.Replay(proof, [accepted.Event with { RepresentationId = "forged-representation" }]));
        Assert.Throws<InvalidOperationException>(() => CampaignScheduledWorld6Phase.Replay(proof, [accepted.Event with { SuccessorPositionId = "forged-position" }]));
        Assert.Throws<InvalidOperationException>(() => CampaignScheduledWorld6Phase.Replay(proof, [accepted.Event with { ResultVersion = 9 }]));
        Assert.Throws<InvalidOperationException>(() => CampaignScheduledWorld6Phase.Replay(proof,
            [accepted.Event with { Command = accepted.Event.Command with { CreationHash = "sha256:foreign" } }]));
        Assert.Throws<InvalidOperationException>(() => CampaignScheduledWorld6Phase.Replay(proof, [accepted.Event, accepted.Event]));
    }

    [Fact]
    public void CanonicalCheckpointColdRebuildsWorldAndRejectsForgedHistory()
    {
        var request = CreationRequest(CampaignScheduledWorld6Phase.SetupHash);
        var creation = Creation();
        var proof = Verified(request, creation);
        var phase = CampaignScheduledWorld6Phase.Start(proof);
        var initial = CampaignScheduledWorld6CheckpointCodec.Decode(CampaignScheduledWorld6CheckpointCodec.Encode(phase), proof);
        Assert.Equal(phase.World, initial.World);
        var accepted = phase.Decide(Command());
        var eventBytes = CampaignScheduledWorld6EventCodec.Encode(accepted.Event);
        var decodedEvent = CampaignScheduledWorld6EventCodec.Decode(eventBytes);
        Assert.Equal(accepted.Event, decodedEvent);
        var bytes = CampaignScheduledWorld6CheckpointCodec.Encode(accepted.State);
        var restored = CampaignScheduledWorld6CheckpointCodec.Decode(bytes, proof);
        Assert.Equal(accepted.State.World, restored.World);
        Assert.True(restored.Decide(Command()).IsRetry);
        var otherRequest = CreationRequest(CampaignScheduledWorld6Phase.SetupHash, "campaign-b");
        var otherCut = Verified(otherRequest, CampaignScheduledWorld6Creation.Create(otherRequest));
        Assert.Throws<JsonException>(() => CampaignScheduledWorld6CheckpointCodec.Decode(bytes, otherCut));
        Assert.Throws<JsonException>(() => CampaignScheduledWorld6CheckpointCodec.Decode([.. bytes, 0x20], proof));
        var forged = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(bytes).Replace("\"StateVersion\":8", "\"StateVersion\":9", StringComparison.Ordinal));
        Assert.Throws<JsonException>(() => CampaignScheduledWorld6CheckpointCodec.Decode(forged, proof));
        var forgedBase = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(bytes).Replace("\"BaseVersion\":7", "\"BaseVersion\":8", StringComparison.Ordinal));
        Assert.Throws<JsonException>(() => CampaignScheduledWorld6CheckpointCodec.Decode(forgedBase, proof));
        var forgedEvent = CampaignScheduledWorld6EventCodec.Decode(CampaignScheduledWorld6EventCodec.Encode(accepted.Event with { ElementId = "axis-truck" }));
        Assert.Throws<InvalidOperationException>(() => CampaignScheduledWorld6Phase.Replay(proof, [forgedEvent]));
    }

    [Fact]
    public void CertifiedPreambleProvidesTheActualArrivalCursorAndVersion()
    {
        Assert.True(Cna1979BreakdownSetupCatalog.TryGet("rules-lab.breakdown.truck.v1", out var definition));
        var setup = CampaignSetupSnapshotV6.FromDefinition(definition!);
        var artifact = Cna1979SyntheticContentCatalog.ArtifactV6;
        var referenceRequest = new CampaignCreationRequest(1, "campaign-a", Cna1979Ruleset.Manifest.Hash, 12345,
            definition!.SetupId, setup.SetupHash, artifact.Identity.PackId, artifact.Identity.Hash, "breakdown-truck-lab");
        var started = CampaignExercises.Begin(referenceRequest);
        Assert.True(started.IsStarted);
        var session = started.Session!;
        var referenceWorld = session.CurrentSnapshot.World;
        var referenceHashes = new List<string>();
        for (var index = 0; index < 10 && session.CurrentSnapshot.CurrentPosition.SequenceContext.PhaseId != LandPhaseIds.NavalConvoyArrival; index++)
        {
            var actions = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => CampaignExercises.Query(session, audience).ActionSet!)
                .First(set => set.Candidates.Count > 0);
            var action = actions.Candidates.FirstOrDefault(value => value is ActFirstAction) ?? actions.Candidates[0];
            var result = CampaignExercises.Submit(session, new CampaignActionSubmission(1, actions.CampaignId,
                actions.StateVersion, actions.PositionId, actions.Audience, action.ActionId));
            Assert.True(result.IsAccepted);
            referenceHashes.Add("sha256:" + Convert.ToHexStringLower(SHA256.HashData(result.Evidence!.EventRecords.Single())));
            session = result.SuccessorSession!;
            Assert.Equal(referenceWorld, session.CurrentSnapshot.World);
        }
        var cursor = session.CurrentSnapshot.CurrentPosition.SequenceContext;
        Assert.Equal(CampaignScheduledWorld6.ArrivalPosition(), cursor);
        Assert.Equal(7, session.CurrentSnapshot.StateVersion);
        var request = CreationRequest(CampaignScheduledWorld6Phase.SetupHash);
        Assert.Equal(referenceHashes, CampaignScheduledWorld6Preamble.Run(request, Creation()).Trace.Select(step => step.ReferenceEventHash));
        var phase = CampaignScheduledWorld6Phase.Start(Verified(request, Creation()));
        Assert.Equal(session.CurrentSnapshot.StateVersion, phase.Version);
        Assert.Equal(cursor, phase.Position);
        Assert.DoesNotContain(phase.World.Elements, value => value.ElementId == "axis-infantry");
        Assert.Equal(referenceWorld, phase.Decide(Command()).State.World);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(54321)]
    public void NativePreambleMatchesCertifiedEventBytesAcrossSeeds(int seed)
    {
        Assert.True(Cna1979BreakdownSetupCatalog.TryGet("rules-lab.breakdown.truck.v1", out var definition));
        var setup = CampaignSetupSnapshotV6.FromDefinition(definition!);
        var artifact = Cna1979SyntheticContentCatalog.ArtifactV6;
        var referenceRequest = new CampaignCreationRequest(1, "campaign-a", Cna1979Ruleset.Manifest.Hash, (ulong)seed,
            definition!.SetupId, setup.SetupHash, artifact.Identity.PackId, artifact.Identity.Hash, "breakdown-truck-lab");
        var session = CampaignExercises.Begin(referenceRequest).Session!;
        var hashes = new List<string>();
        for (var index = 0; index < 6; index++)
        {
            var actions = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => CampaignExercises.Query(session, audience).ActionSet!)
                .Single(set => set.Candidates.Count > 0);
            var action = actions.Candidates.FirstOrDefault(value => value is ActFirstAction) ?? actions.Candidates[0];
            var result = CampaignExercises.Submit(session, new CampaignActionSubmission(1, actions.CampaignId,
                actions.StateVersion, actions.PositionId, actions.Audience, action.ActionId));
            Assert.True(result.IsAccepted);
            hashes.Add("sha256:" + Convert.ToHexStringLower(SHA256.HashData(result.Evidence!.EventRecords.Single())));
            session = result.SuccessorSession!;
        }
        var request = CreationRequest(CampaignScheduledWorld6Phase.SetupHash, seed: (ulong)seed);
        var actual = CampaignScheduledWorld6Preamble.Run(request, CampaignScheduledWorld6Creation.Create(request));
        Assert.Equal(hashes, actual.Trace.Select(value => value.ReferenceEventHash));
        Assert.Equal(session.CurrentSnapshot.RandomState, actual.RandomState);
        Assert.Equal(session.CurrentSnapshot.OperationStageOrders, actual.Orders);
        Assert.Equal(session.CurrentSnapshot.OperationStageWeather, actual.Weather);
    }

    [Fact]
    public void PrivatePreambleReachesArrivalWithoutExposingTheReferenceWorld()
    {
        var request = CreationRequest(CampaignScheduledWorld6Phase.SetupHash);
        var creation = CampaignScheduledWorld6Creation.Create(request);
        var result = CampaignScheduledWorld6Preamble.Run(request, creation);
        var repeated = CampaignScheduledWorld6Preamble.Run(request, creation);
        var phase = CampaignScheduledWorld6Phase.Start(Verified(request, creation));
        var repeatedPhase = CampaignScheduledWorld6Phase.Start(Verified(request, creation));
        Assert.Equal(7, phase.Version);
        Assert.Equal(CampaignScheduledWorld6.ArrivalPosition(), phase.Position);
        Assert.DoesNotContain(phase.World.Elements, value => value.ElementId == "axis-infantry");
        Assert.Equal(result.RandomState, repeated.RandomState);
        Assert.Equal(LandSide.Axis, result.InitiativeHolder);
        Assert.Single(result.Orders);
        Assert.Single(result.Weather);
        Assert.Equal(result.Orders, repeated.Orders);
        Assert.Equal(result.Weather, repeated.Weather);
        Assert.Equal(6, result.Trace.Count);
        Assert.Equal(Enumerable.Range(2, 6).Select(value => (long)value), result.Trace.Select(value => value.ResultVersion));
        Assert.Equal(result.Trace, repeated.Trace);
        Assert.All(result.Trace, step => Assert.StartsWith("sha256:", step.ReferenceEventHash));
        Assert.Throws<NotSupportedException>(() => ((IList<CampaignOperationStageOrder>)result.Orders).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<CampaignOperationStageWeather>)result.Weather).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<CampaignScheduledWorld6PreambleStep>)result.Trace).Clear());
        Assert.Equal(phase.World, repeatedPhase.World);
        Assert.Equal(8, phase.Decide(Command()).State.Version);
        Assert.Throws<InvalidOperationException>(() => CampaignScheduledWorld6Preamble.Run(request, creation with { Seed = 999 }));
    }

    [Fact]
    public void OnlyCompleteAndAuthenticPreambleEvidenceYieldsAnArrivalCut()
    {
        var request = CreationRequest(CampaignScheduledWorld6Phase.SetupHash);
        var creation = Creation();
        var events = CampaignScheduledWorld6PreambleEvents.Create(request, creation);
        Assert.Equal(6, events.Count);
        for (var count = 0; count < events.Count; count++)
        {
            var partial = CampaignScheduledWorld6PreambleEvents.Verify(request, creation, events.Take(count));
            Assert.Equal(count, partial.VerifiedCount);
            Assert.Null(partial.CompleteCut);
            Assert.Equal(count + 1, partial.Checkpoint.StateVersion);
            Assert.DoesNotContain(partial.Checkpoint.World.Elements, value => value.ElementId == "axis-infantry");
        }
        var complete = CampaignScheduledWorld6PreambleEvents.Verify(request, creation, events);
        Assert.Equal(6, complete.VerifiedCount);
        Assert.NotNull(complete.CompleteCut);
        Assert.Equal(CampaignScheduledWorld6.ArrivalPosition(), complete.Checkpoint.Position);
        Assert.DoesNotContain(complete.Checkpoint.World.Elements, value => value.ElementId == "axis-infantry");
        Assert.Equal(7, CampaignScheduledWorld6Phase.Start(complete.CompleteCut!).Version);
        var decoded = events.Select(value => CampaignScheduledWorld6PreambleEventCodec.Decode(
            CampaignScheduledWorld6PreambleEventCodec.Encode(value))).ToArray();
        Assert.NotNull(CampaignScheduledWorld6PreambleEvents.Verify(request, creation, decoded).CompleteCut);
        var encoded = CampaignScheduledWorld6PreambleEventCodec.Encode(events[0]);
        Assert.Throws<JsonException>(() => CampaignScheduledWorld6PreambleEventCodec.Decode([.. encoded, 0x20]));
        Assert.Throws<InvalidOperationException>(() => CampaignScheduledWorld6PreambleEvents.Verify(request, creation, events.Reverse()));
        Assert.Throws<InvalidOperationException>(() => CampaignScheduledWorld6PreambleEvents.Verify(request, creation, [.. events, events[^1]]));
        var changed = events.ToArray();
        changed[2] = changed[2] with { ReferenceEventHash = "sha256:forged" };
        Assert.Throws<InvalidOperationException>(() => CampaignScheduledWorld6PreambleEvents.Verify(request, creation, changed));
        var other = CreationRequest(CampaignScheduledWorld6Phase.SetupHash, seed: 54321);
        Assert.Throws<InvalidOperationException>(() => CampaignScheduledWorld6PreambleEvents.Verify(other, CampaignScheduledWorld6Creation.Create(other), events));
    }

    [Fact]
    public void EveryPreambleCheckpointReplaysAndRejectsForeignOrAlteredBytes()
    {
        var request = CreationRequest(CampaignScheduledWorld6Phase.SetupHash);
        var creation = Creation();
        var events = CampaignScheduledWorld6PreambleEvents.Create(request, creation);
        for (var count = 0; count <= events.Count; count++)
        {
            var bytes = CampaignScheduledWorld6PreambleCheckpointCodec.Encode(request, creation, events.Take(count));
            var restored = CampaignScheduledWorld6PreambleCheckpointCodec.Decode(bytes, request, creation);
            Assert.Equal(count, restored.VerifiedCount);
            Assert.Equal(count + 1, restored.Checkpoint.StateVersion);
            Assert.DoesNotContain(restored.Checkpoint.World.Elements, value => value.ElementId == "axis-infantry");
            Assert.Equal(count == events.Count, restored.CompleteCut is not null);
        }
        var completeBytes = CampaignScheduledWorld6PreambleCheckpointCodec.Encode(request, creation, events);
        var wrongSeed = CreationRequest(CampaignScheduledWorld6Phase.SetupHash, seed: 54321);
        Assert.Throws<JsonException>(() => CampaignScheduledWorld6PreambleCheckpointCodec.Decode(completeBytes, wrongSeed,
            CampaignScheduledWorld6Creation.Create(wrongSeed)));
        Assert.Throws<JsonException>(() => CampaignScheduledWorld6PreambleCheckpointCodec.Decode([.. completeBytes, 0x20], request, creation));
        var forged = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(completeBytes).Replace("\"stateVersion\":7", "\"stateVersion\":8", StringComparison.Ordinal));
        Assert.Throws<JsonException>(() => CampaignScheduledWorld6PreambleCheckpointCodec.Decode(forged, request, creation));
    }

    [Fact]
    public void World6OfferIsOnlyVisibleToItsEligibleOwner()
    {
        var phase = Start();
        var offer = Assert.Single(phase.QueryOffers(LandSide.Axis));
        Assert.Equal("axis-infantry", offer.ElementId);
        Assert.Equal("north-west", offer.DestinationId);
        Assert.Empty(phase.QueryOffers(LandSide.Commonwealth));
        var accepted = phase.Decide(Command());
        Assert.Empty(accepted.State.QueryOffers(LandSide.Axis));
        Assert.Empty(accepted.State.QueryOffers(LandSide.Commonwealth));
        Assert.Throws<ArgumentOutOfRangeException>(() => phase.QueryOffers((LandSide)999));
    }

    [Fact]
    public void SuccessorSnapshotPreservesPreambleStateAcrossArrivalAndRecovery()
    {
        var proof = Verified(CreationRequest(CampaignScheduledWorld6Phase.SetupHash), Creation());
        var initial = CampaignScheduledWorld6Snapshot.AtArrival(proof);
        var accepted = initial.Decide(Command());
        Assert.Equal(8, accepted.State.StateVersion);
        Assert.Equal(initial.RandomState, accepted.State.RandomState);
        Assert.Equal(initial.InitiativeHolder, accepted.State.InitiativeHolder);
        Assert.Equal(initial.Orders, accepted.State.Orders);
        Assert.Equal(initial.Weather, accepted.State.Weather);
        var restored = CampaignScheduledWorld6Snapshot.DecodeCheckpoint(accepted.State.EncodeCheckpoint(), proof);
        Assert.Equal(accepted.State.World, restored.World);
        Assert.Equal(accepted.State.RandomState, restored.RandomState);
        Assert.Equal(accepted.State.Position, restored.Position);
        Assert.True(restored.Decide(Command()).IsRetry);
        var otherRequest = CreationRequest(CampaignScheduledWorld6Phase.SetupHash, seed: 54321);
        var otherCut = Verified(otherRequest, CampaignScheduledWorld6Creation.Create(otherRequest));
        Assert.Throws<JsonException>(() => CampaignScheduledWorld6Snapshot.DecodeCheckpoint(accepted.State.EncodeCheckpoint(), otherCut));
    }

    [Fact]
    public void CoreBridgeCreatesAdvancesArrivesAndImportsWithoutExposingPrivateSessionState()
    {
        var request = CreationRequest(CampaignScheduledWorld6Phase.SetupHash);
        var started = CampaignScheduledWorld6Bridge.Begin(request);
        var creationBytes = started.EventBytes;
        var initialCheckpoint = started.CheckpointBytes;
        var session = started.Session;
        Assert.Empty(typeof(CampaignScheduledWorld6Session).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance));
        Assert.Empty(CampaignScheduledWorld6Bridge.QueryOffers(session, LandSide.Axis));
        Assert.Empty(CampaignScheduledWorld6Bridge.QueryOffers(session, LandSide.Commonwealth));
        var records = new List<byte[]>();
        var evidence = new List<CampaignScheduledWorld6Evidence>();
        for (var index = 0; index < 6; index++)
        {
            var advanced = CampaignScheduledWorld6Bridge.AdvancePreamble(session);
            records.Add(advanced.EventBytes);
            evidence.Add(new CampaignScheduledWorld6Evidence(advanced.CommandBytes, advanced.EventBytes,
                advanced.ReceiptBytes, advanced.CheckpointBytes));
            session = CampaignScheduledWorld6Bridge.Import(request, creationBytes, records, null, advanced.CheckpointBytes);
            Assert.Empty(CampaignScheduledWorld6Bridge.QueryOffers(session, LandSide.Commonwealth));
        }
        Assert.Equal("axis-infantry", Assert.Single(CampaignScheduledWorld6Bridge.QueryOffers(session, LandSide.Axis)).ElementId);
        Assert.Throws<ArgumentException>(() => CampaignScheduledWorld6Bridge.Arrive(session, "request-1", 7, LandSide.Commonwealth, "north-west"));
        var accepted = CampaignScheduledWorld6Bridge.Arrive(session, "request-1", 7, LandSide.Axis, "north-west");
        Assert.False(accepted.IsRetry);
        evidence.Add(new CampaignScheduledWorld6Evidence(accepted.CommandBytes, accepted.EventBytes,
            accepted.ReceiptBytes, accepted.CheckpointBytes));
        var restored = CampaignScheduledWorld6Bridge.Import(request, creationBytes, records, accepted.EventBytes, accepted.CheckpointBytes);
        var journalRestored = CampaignScheduledWorld6Bridge.Restore(request, creationBytes, initialCheckpoint, evidence);
        Assert.Empty(CampaignScheduledWorld6Bridge.QueryOffers(restored, LandSide.Axis));
        Assert.Empty(CampaignScheduledWorld6Bridge.QueryOffers(restored, LandSide.Commonwealth));
        var retry = CampaignScheduledWorld6Bridge.Arrive(restored, "request-1", 7, LandSide.Axis, "north-west");
        Assert.True(retry.IsRetry);
        Assert.True(CampaignScheduledWorld6Bridge.Arrive(journalRestored, "request-1", 7, LandSide.Axis, "north-west").IsRetry);
        Assert.Equal(accepted.EventBytes, retry.EventBytes);
        Assert.Equal(accepted.CheckpointBytes, retry.CheckpointBytes);
        Assert.Equal(accepted.ReceiptBytes, retry.ReceiptBytes);
        Assert.Throws<ArgumentException>(() => CampaignScheduledWorld6Bridge.Arrive(restored, "request-1", 7, LandSide.Axis, "west"));
        Assert.Throws<InvalidOperationException>(() => CampaignScheduledWorld6Bridge.Import(request, creationBytes, records.AsEnumerable().Reverse(), null, initialCheckpoint));
        Assert.Throws<InvalidOperationException>(() => CampaignScheduledWorld6Bridge.Import(request, creationBytes, records.Take(5), accepted.EventBytes, accepted.CheckpointBytes));
        var copy = accepted.EventBytes;
        copy[0] ^= 1;
        Assert.NotEqual(copy, accepted.EventBytes);
        var forgedReceipt = accepted.ReceiptBytes;
        forgedReceipt[0] ^= 1;
        var forgedEvidence = evidence.Take(6).Append(new CampaignScheduledWorld6Evidence(accepted.CommandBytes,
            accepted.EventBytes, forgedReceipt, accepted.CheckpointBytes));
        Assert.Throws<InvalidOperationException>(() => CampaignScheduledWorld6Bridge.Restore(request, creationBytes, initialCheckpoint, forgedEvidence));
    }

    [Fact]
    public void NativePreambleEvidenceReplaysAndRejectsAlteredOrMixedRecords()
    {
        var request = CreationRequest(CampaignScheduledWorld6Phase.SetupHash);
        var started = CampaignScheduledWorld6Bridge.Begin(request);
        var session = started.Session;
        var evidence = new List<CampaignScheduledWorld6Evidence>();
        CampaignScheduledWorld6Step? legacySecond = null;
        for (var index = 0; index < 6; index++)
        {
            if (index == 1) legacySecond = CampaignScheduledWorld6Bridge.AdvancePreamble(session);
            var step = CampaignScheduledWorld6Bridge.AdvanceNativePreamble(session);
            Assert.True(CampaignScheduledWorld6Bridge.IsNativePreambleEvent(step.EventBytes));
            using var document = JsonDocument.Parse(step.EventBytes);
            var native = Convert.FromBase64String(document.RootElement.GetProperty("NativeEventBase64").GetString()!);
            Assert.NotEmpty(native);
            evidence.Add(new CampaignScheduledWorld6Evidence(step.CommandBytes, step.EventBytes, step.ReceiptBytes, step.CheckpointBytes));
            session = step.Session;
        }
        var restored = CampaignScheduledWorld6Bridge.Restore(request, started.EventBytes, started.CheckpointBytes, evidence);
        Assert.Single(CampaignScheduledWorld6Bridge.QueryOffers(restored, LandSide.Axis));
        Assert.Empty(CampaignScheduledWorld6Bridge.QueryOffers(restored, LandSide.Commonwealth));
        var forged = evidence.ToArray();
        using var eventDocument = JsonDocument.Parse(forged[2].EventBytes);
        var nativeBase64 = eventDocument.RootElement.GetProperty("NativeEventBase64").GetString()!;
        var alteredBase64 = (nativeBase64[0] == 'A' ? "B" : "A") + nativeBase64[1..];
        var changed = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(forged[2].EventBytes)
            .Replace(nativeBase64, alteredBase64, StringComparison.Ordinal));
        var forgedReceipt = CampaignScheduledWorld6EvidenceCodec.Receipt(request.CampaignId, 4, changed);
        forged[2] = new CampaignScheduledWorld6Evidence(forged[2].CommandBytes, changed, forgedReceipt, forged[2].CheckpointBytes);
        Assert.Throws<InvalidOperationException>(() => CampaignScheduledWorld6Bridge.Restore(request, started.EventBytes, started.CheckpointBytes, forged));
        var mixed = evidence.ToArray();
        mixed[1] = new CampaignScheduledWorld6Evidence(legacySecond!.CommandBytes, legacySecond.EventBytes,
            legacySecond.ReceiptBytes, legacySecond.CheckpointBytes);
        Assert.Throws<InvalidOperationException>(() => CampaignScheduledWorld6Bridge.Restore(request, started.EventBytes, started.CheckpointBytes, mixed));
    }
}
