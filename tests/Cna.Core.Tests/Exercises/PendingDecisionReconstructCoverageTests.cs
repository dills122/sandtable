using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Exercises;

// Issue #581 / audit proposal 2: every PendingDecision subtype reachable through a
// real driven session must (a) Reconstruct to IsVerified, (b) answer QueryCheckpoint
// without throwing, and (c) return InvalidHistory (not throw) after a one-byte tamper
// of the record that step produced. An exhaustiveness test reflects over every
// PendingDecision subtype and fails when one has no fixture-backed case and no
// explicit allow-list entry.
//
// Fixtures: PendingFixtures.Sessions drives one real session through Operation
// Stage 1 and Stage 2 (both acting sides, six-mechanic preamble included) and keeps
// the first session observed for each PendingDecision subtype. Adding a subtype
// that the driver already reaches needs no code. To add a subtype reached some other
// way (for example the Stage 3 preamble after #570 merges), add ONE line to
// PendingFixtures.ExtraFixtures.
public sealed class PendingDecisionReconstructCoverageTests
{
    // Subtypes deliberately without a fixture. Key = type, value = why.
    private const string NoAttackReason =
        "Unreachable through public flows: CompleteXCombatNoAttack installs the no-attack completion "
        + "together with the Assault runtime, so Pending resolves to the later XAssaultPending. Only a "
        + "hand-called WithXNoAttack (guard-only tests) yields this pending, and that needs a real "
        + "World/Position pair no existing With* fixture supplies.";
    private const string StageEntryMoveReason =
        "No fixture: this driver only reaches movement through the host owner-draft API, which exists "
        + "for the Operation Stage 1 second side only. Stage Entry Move needs a hand-built movement cut; "
        + "follow-up fixture, one line in PendingFixtures.ExtraFixtures.";

    private static readonly Dictionary<Type, string> UnreachableAllowList = new()
    {
        [typeof(SecondNoAttackPending)] = NoAttackReason,
        [typeof(StageEntryNoAttackPending)] = NoAttackReason,
        [typeof(StageEntrySecondNoAttackPending)] = NoAttackReason,
        [typeof(StageEntryMovePending)] = StageEntryMoveReason,
        [typeof(StageEntrySecondMovePending)] = StageEntryMoveReason,
        [typeof(ThirdStageMovePending)] = StageEntryMoveReason,
    };

    public static TheoryData<string> Subtypes()
    {
        var data = new TheoryData<string>();
        foreach (var name in PendingFixtures.All.Keys.OrderBy(value => value.Name, StringComparer.Ordinal))
            data.Add(name.Name);
        return data;
    }

    // Tamper rows exist only for subtypes whose own retained record has a byte-level form
    // (CI rejects skipped rows, so unsupported subtypes are simply not generated). The
    // *CombatSelection / *Assault subtypes are excluded: their records are
    // CampaignNativeSecondCombatDecisionStateV47 / CampaignNativeRepeatAssaultRuntime.
    public static TheoryData<string> TamperableSubtypes()
    {
        var data = new TheoryData<string>();
        foreach (var type in PendingFixtures.All.Keys.OrderBy(value => value.Name, StringComparer.Ordinal))
            if (SessionTamper.TamperOwnRecord(Fixture(type.Name)).Tampered is not null)
                data.Add(type.Name);
        return data;
    }

    private static ExerciseSession Fixture(string typeName) =>
        PendingFixtures.All.Single(pair => pair.Key.Name == typeName).Value();

    [Fact]
    public void EveryPendingDecisionSubtypeHasAFixtureOrAnAllowListReason()
    {
        var all = typeof(PendingDecision).Assembly.GetTypes()
            .Where(type => typeof(PendingDecision).IsAssignableFrom(type) && !type.IsAbstract)
            .ToArray();
        Assert.NotEmpty(all);
        var missing = all.Where(type => !PendingFixtures.All.ContainsKey(type)
            && !UnreachableAllowList.ContainsKey(type)).Select(type => type.Name).Order().ToArray();
        Assert.True(missing.Length == 0,
            "PendingDecision subtypes with no Reconstruct fixture and no allow-list reason: "
            + string.Join(", ", missing));
        Assert.All(UnreachableAllowList, pair =>
        {
            Assert.False(string.IsNullOrWhiteSpace(pair.Value));
            Assert.DoesNotContain(pair.Key, PendingFixtures.All.Keys);
        });
        Assert.All(PendingFixtures.All.Keys, type => Assert.Contains(type, all));
    }

    [Theory]
    [MemberData(nameof(Subtypes))]
    public void ReconstructVerifiesEachSubtype(string typeName)
    {
        var session = Fixture(typeName);
        Assert.Equal(typeName, session.Pending!.GetType().Name);
        var result = CampaignExercises.Reconstruct(session);
        Assert.True(result.IsVerified, typeName + ": " + result.FailureReason);
    }

    [Theory]
    [MemberData(nameof(Subtypes))]
    public void QueryCheckpointDoesNotThrowForEachSubtype(string typeName)
    {
        var checkpoint = CampaignExercises.QueryCheckpoint(Fixture(typeName));
        Assert.False(string.IsNullOrEmpty(checkpoint.PositionId));
    }

    [Theory]
    [MemberData(nameof(TamperableSubtypes))]
    public void OneByteTamperReturnsInvalidHistoryForEachSubtype(string typeName)
    {
        var session = Fixture(typeName);
        var (tampered, property) = SessionTamper.TamperOwnRecord(session);
        Assert.NotNull(tampered);
        var result = CampaignExercises.Reconstruct(tampered);
        Assert.False(result.IsVerified, typeName + " tampered " + property + " still verified");
        Assert.Equal(ExerciseReconstructionFailureReason.InvalidHistory, result.FailureReason);
    }

    // Audit finding 4 (campaign-exercises-7250-end-audit.md): six replay arms
    // dereference predecessor.CurrentX! outside every catch filter. Each case blanks
    // that slot on the predecessor and expects InvalidHistory. A NullReferenceException
    // escaping Reconstruct is the production bug; those cases are skipped with the
    // reason (no production fix here) -- see the PR body.
    [Theory]
    [InlineData(nameof(InitiativeDeclarationPending), "CurrentSecondAssault")]
    [InlineData(nameof(StageEntryWeatherPending), "CurrentInitiativeDeclaration")]
    [InlineData(nameof(NoObligationOrganizationPending), "CurrentStageEntryWeather")]
    [InlineData(nameof(NoObligationNavalConvoyArrivalPending), "CurrentNoObligationOrganization")]
    [InlineData(nameof(NoObligationFleetAssignmentPending), "CurrentNoObligationNavalConvoyArrival")]
    [InlineData(nameof(NoObligationFleetRepairPending), "CurrentNoObligationFleetAssignment")]
    public void BlankedPredecessorSlotReturnsInvalidHistoryNotNullReference(string typeName, string slot)
    {
        var session = Fixture(typeName);
        var broken = SessionTamper.BlankPredecessorSlot(session, typeName, slot);
        ExerciseReconstructionResult result;
        try
        {
            result = CampaignExercises.Reconstruct(broken);
        }
        catch (NullReferenceException error)
        {
            Assert.Fail($"{typeName}: NullReferenceException escaped Reconstruct for blanked {slot}: {error.Message}");
            return;
        }
        Assert.Equal(ExerciseReconstructionFailureReason.InvalidHistory, result.FailureReason);
    }

    internal static string HashLabel(string label) =>
        "sha256:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(label)));
}

internal static class SessionTamper
{
    private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static ExerciseSession Clone(ExerciseSession session) =>
        (ExerciseSession)typeof(object).GetMethod("MemberwiseClone", Any)!.Invoke(session, null)!;

    private static void Set(ExerciseSession session, string property, object? value) =>
        (typeof(ExerciseSession).GetProperty(property, Any)
            ?? throw new MissingMemberException(nameof(ExerciseSession), property)).SetValue(session, value);

    // Flips one byte of the retained record named after the pending type (the step's own
    // event) and returns a clone holding the altered, still-deserializable record. Returns
    // null Tampered plus a reason when that record has no byte-level form (Assault runtimes,
    // CombatDecisionStateV47), because those cannot be one-byte tampered without typed construction.
    internal static (ExerciseSession? Tampered, string Property) TamperOwnRecord(ExerciseSession session)
    {
        var pendingName = session.Pending!.GetType().Name;
        var stem = pendingName[..^"Pending".Length].Replace("CombatSelection", "CombatDecision", StringComparison.Ordinal);
        var propertyName = "Current" + stem;
        var property = typeof(ExerciseSession).GetProperty(propertyName, Any)
            ?? throw new MissingMemberException(nameof(ExerciseSession), propertyName);
        var current = property.GetValue(session)
            ?? throw new InvalidOperationException(propertyName + " is null on its own fixture.");
        var serialize = current.GetType().GetMethod("SerializeCanonical", Any, Type.EmptyTypes);
        var deserialize = current.GetType().GetMethod("Deserialize", BindingFlags.Static | BindingFlags.Public
            | BindingFlags.NonPublic, [typeof(ReadOnlyMemory<byte>)]);
        if (serialize?.ReturnType != typeof(byte[]) || deserialize is null)
            return (null, propertyName + " (" + current.GetType().Name + " has no byte-level SerializeCanonical/Deserialize)");
        var bytes = (byte[])serialize.Invoke(current, null)!;
        for (var index = bytes.Length - 1; index >= 0; index--)
        {
            var altered = (byte[])bytes.Clone();
            altered[index] ^= 1;
            object? replacement;
            try { replacement = deserialize.Invoke(null, [new ReadOnlyMemory<byte>(altered)]); }
            catch (TargetInvocationException) { continue; }
            if (replacement is null || replacement.Equals(current)) continue;
            var clone = Clone(session);
            property.SetValue(clone, replacement);
            return (clone, propertyName);
        }
        throw new InvalidOperationException("No one-byte flip of " + propertyName + " still deserializes.");
    }

    // Clones the predecessor with `slot` nulled and re-points the head at the clone.
    internal static ExerciseSession BlankPredecessorSlot(ExerciseSession session, string pendingTypeName, string slot)
    {
        var predecessorProperty = pendingTypeName[..^"Pending".Length] + "Predecessor";
        var predecessor = (ExerciseSession)typeof(ExerciseSession).GetProperty(predecessorProperty, Any)!
            .GetValue(session)!;
        var blankedPredecessor = Clone(predecessor);
        Set(blankedPredecessor, slot, null);
        var head = Clone(session);
        Set(head, predecessorProperty, blankedPredecessor);
        return head;
    }
}

internal static class PendingFixtures
{
    // One line per subtype that the driven session below does NOT reach on its own,
    // e.g. [typeof(SomeStageThreePending)] = () => BuildSomeStageThreePending(),
    private static readonly IReadOnlyDictionary<Type, Func<ExerciseSession>> ExtraFixtures =
        new Dictionary<Type, Func<ExerciseSession>>();

    private static readonly Lazy<IReadOnlyDictionary<Type, Func<ExerciseSession>>> Lazy = new(Build);

    internal static IReadOnlyDictionary<Type, Func<ExerciseSession>> All => Lazy.Value;

    private static Dictionary<Type, Func<ExerciseSession>> Build()
    {
        var map = new Dictionary<Type, Func<ExerciseSession>>();
        foreach (var pair in new StageTwoDriver().Run())
        {
            var session = pair.Value;
            map[pair.Key] = () => session;
        }
        foreach (var pair in ExtraFixtures) map[pair.Key] = pair.Value;
        return map;
    }

    // Real Core dispatch through Operation Stage 1 (both sides) and Stage 2 (both
    // sides, preamble included); keeps the first session seen per Pending subtype.
    private sealed class StageTwoDriver
    {
        private readonly Dictionary<Type, ExerciseSession> seen = [];

        internal Dictionary<Type, ExerciseSession> Run()
        {
            var session = CampaignExercises.Begin(CreateRequest()).Session!;
            session = AdvanceFirstSideToCombatPositionDetermination(session);
            var stage1Order = session.CurrentSnapshot.OperationStageOrders.Single(value => value.OperationStage == 1);
            var firstAudience = stage1Order.FirstSide == LandSide.Axis
                ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
            var secondAudience = firstAudience == CampaignActionAudience.Axis
                ? CampaignActionAudience.Commonwealth : CampaignActionAudience.Axis;
            var firstElement = stage1Order.FirstSide == LandSide.Axis ? "axis-infantry" : "commonwealth-infantry";
            var secondElement = stage1Order.FirstSide == LandSide.Axis ? "commonwealth-infantry" : "axis-infantry";

            session = SubmitFinishWithoutAttack(session, firstAudience, firstElement,
                CampaignExercises.CompleteNativeCombatNoAttack);
            session = FinishCycleThroughPostCombatPasses(session, firstAudience);
            session = CloseOutSecondSideChain(session, secondAudience, secondElement);

            var gameTurn = session.CurrentSnapshot.Setup.InitialGameTurn;
            var holder = session.CurrentSnapshot.InitiativeHolder
                ?? throw new InvalidOperationException("Initiative holder is unresolved.");
            var holderAudience = holder == LandSide.Axis
                ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
            var otherAudience = holderAudience == CampaignActionAudience.Axis
                ? CampaignActionAudience.Commonwealth : CampaignActionAudience.Axis;
            var holderElement = holder == LandSide.Axis ? "axis-infantry" : "commonwealth-infantry";
            var otherElement = holder == LandSide.Axis ? "commonwealth-infantry" : "axis-infantry";

            var declared = CampaignExercises.SubmitStageEntryInitiativeDeclaration(
                session, holderAudience, PendingDecisionReconstructCoverageTests.HashLabel("initiative"), actFirst: true);
            Assert.True(declared.IsAccepted, declared.RejectionReason.ToString());
            session = Cap(declared.SuccessorSession!);

            var weather = CampaignNativeWeatherDeterminationV50.Create(session.CurrentInitiativeDeclaration!,
                session.CurrentSnapshot.RandomState, gameTurn, session.CurrentContacts.AuthorityVersion);
            session = Cap(session.WithStageEntryWeather(weather, session.CurrentContacts.AdvanceWeatherDetermination(weather)));
            var organization = CampaignNativeNoObligationOrganizationV51.Create(weather, weather.StateVersion);
            session = Cap(session.WithNoObligationOrganization(organization,
                session.CurrentContacts.AdvanceNoObligationOrganization(organization)));
            var convoy = CampaignNativeNoObligationNavalConvoyArrivalV52.Create(organization,
                session.Context, organization.StateVersion);
            session = Cap(session.WithNoObligationNavalConvoyArrival(convoy,
                session.CurrentContacts.AdvanceNoObligationNavalConvoyArrival(convoy)));
            var fleetAssignment = CampaignNativeNoObligationFleetAssignmentV53.Create(convoy, convoy.StateVersion);
            session = Cap(session.WithNoObligationFleetAssignment(fleetAssignment,
                session.CurrentContacts.AdvanceNoObligationFleetAssignment(fleetAssignment)));
            var fleetRepair = CampaignNativeNoObligationFleetRepairV54.Create(fleetAssignment, fleetAssignment.StateVersion);
            session = Cap(session.WithNoObligationFleetRepair(fleetRepair,
                session.CurrentContacts.AdvanceNoObligationFleetRepair(fleetRepair)));

            session = CloseOutStageEntrySide(session, holderAudience, holderElement, isSecondSide: false);
            session = CloseOutStageEntrySide(session, otherAudience, otherElement, isSecondSide: true);
            DriveThirdStagePreamble(session, holder, gameTurn, weather);
            return seen;
        }

        // Stage 3 preamble, same shape as CampaignStageThreeEntryVerificationTests.
        private void DriveThirdStagePreamble(ExerciseSession session, LandSide holder, int gameTurn,
            CampaignNativeWeatherDeterminationV50 stageTwoWeather)
        {
            var realPredecessor = session.CurrentStageEntrySecondAssault!.PostCombatPasses[^1];
            var declared = CampaignNativeInitiativeDeclarationV49.Create(
                realPredecessor, holder, actFirst: false, gameTurn, realPredecessor.StateVersion);
            session = Cap(session.WithThirdStageInitiativeDeclaration(declared,
                session.CurrentContacts.AdvanceInitiativeDeclaration(declared)));
            var initialRandom = session.CurrentSnapshot.RandomState;
            var random = new RandomStreamState(initialRandom.ContractVersion, initialRandom.AlgorithmId,
                initialRandom.Seed, stageTwoWeather.ResultRandomCursor);
            var weather = CampaignNativeWeatherDeterminationV50.Create(declared, random, gameTurn,
                session.CurrentContacts.AuthorityVersion);
            session = Cap(session.WithThirdStageWeather(weather,
                session.CurrentContacts.AdvanceWeatherDetermination(weather)));
            var organization = CampaignNativeNoObligationOrganizationV51.Create(weather, weather.StateVersion);
            session = Cap(session.WithThirdStageNoObligationOrganization(organization,
                session.CurrentContacts.AdvanceNoObligationOrganization(organization)));
            var arrival = CampaignNativeNoObligationNavalConvoyArrivalV52.Create(organization,
                session.Context, organization.StateVersion);
            session = Cap(session.WithThirdStageNoObligationNavalConvoyArrival(arrival,
                session.CurrentContacts.AdvanceNoObligationNavalConvoyArrival(arrival)));
            var assignment = CampaignNativeNoObligationFleetAssignmentV53.Create(arrival, arrival.StateVersion);
            session = Cap(session.WithThirdStageNoObligationFleetAssignment(assignment,
                session.CurrentContacts.AdvanceNoObligationFleetAssignment(assignment)));
            var repair = CampaignNativeNoObligationFleetRepairV54.Create(assignment, assignment.StateVersion);
            session = Cap(session.WithThirdStageNoObligationFleetRepair(repair,
                session.CurrentContacts.AdvanceNoObligationFleetRepair(repair)));
            // Stage 3 first side (S3-1..S3-3): Reserve, Movement Completion, Breakdown.
            var audience = repair.ToOrder().FirstSide == LandSide.Axis
                ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
            session = Ok(CampaignExercises.SubmitThirdStageReserve(session, audience, Hash("stage3-reserve"), elementId: null));
            session = Ok(CampaignExercises.CompleteThirdStageMovement(session, audience, Hash("stage3-movement-completion")));
            session = Ok(CampaignExercises.CompleteThirdStageBreakdown(session));
            // S3-4: first-side Combat Decision (finish without attack).
            var cut = CampaignExercises.QueryCheckpoint(session);
            var element = audience == CampaignActionAudience.Axis ? "axis-infantry" : "commonwealth-infantry";
            Ok(CampaignExercises.SubmitContactOwnerCombatSelection(session,
                new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion, cut.CampaignId,
                    cut.StateVersion, cut.PositionId, audience, new FinishCombatWithoutAttackAction(element).ActionId),
                element));
        }

        private ExerciseSession Cap(ExerciseSession session)
        {
            if (session.Pending is { } pending) seen.TryAdd(pending.GetType(), session);
            return session;
        }

        private ExerciseSession Ok(ExerciseStepResult result)
        {
            Assert.True(result.IsAccepted, result.RejectionReason.ToString());
            return Cap(result.SuccessorSession!);
        }

        private static CampaignCreationRequest CreateRequest()
        {
            var setup = CampaignSetupSnapshotV6.FromDefinition(Cna1979BreakdownSetupCatalog.Definitions
                .Single(value => value.SetupId == Cna1979BreakdownSetupCatalog.TruckSetupId));
            return new CampaignCreationRequest(1, "campaign-pending-reconstruct-coverage", Cna1979Ruleset.Manifest.Hash,
                12345, setup.SetupId, setup.SetupHash, setup.Content.Pack.PackId,
                setup.Content.Pack.Hash, setup.Content.ScenarioId);
        }

        private static string Hash(string label) => PendingDecisionReconstructCoverageTests.HashLabel(label);

        private ExerciseSession AdvanceFirstSideToCombatPositionDetermination(ExerciseSession session)
        {
            for (var index = 0; index < 80; index++)
            {
                var position = session.CurrentSnapshot.CurrentPosition.SequenceContext;
                if (position.SegmentId == LandSegmentIds.Combat && position.StepId == LandStepIds.PositionDetermination)
                    return session;
                var set = Enum.GetValues<CampaignActionAudience>()
                    .Select(audience => CampaignExercises.Query(session, audience).ActionSet)
                    .First(value => value?.Candidates.Count > 0)!;
                var action = set.Candidates.FirstOrDefault(value => value is not MoveElementAction) ?? set.Candidates[0];
                session = Cap(CampaignExercises.Submit(session, new CampaignActionSubmission(1, set.CampaignId,
                    set.StateVersion, set.PositionId, set.Audience, action.ActionId)).SuccessorSession!);
            }
            throw new InvalidOperationException("Combat position was not reached.");
        }

        private ExerciseSession FinishCycleThroughPostCombatPasses(ExerciseSession session,
            CampaignActionAudience audience)
        {
            session = Ok(CampaignExercises.SubmitNativeCycleControl(session, audience,
                CampaignNativeCycleControlChoiceV28.Finish, Hash("cycle-control-finish")));
            session = Ok(CampaignExercises.SubmitTruckConvoyPass(session, audience, Hash("truck-convoy-pass")));
            for (var index = 0; index < 4; index++)
                session = Ok(CampaignExercises.SubmitPostCombatPass(session, audience, Hash($"post-combat-pass-{index}")));
            return session;
        }

        private ExerciseSession CloseOutSecondSideChain(ExerciseSession session,
            CampaignActionAudience audience, string actingElementId)
        {
            session = Ok(CampaignExercises.SubmitSecondReserve(session, audience, Hash("second-reserve"), elementId: null));
            session = TryMoveSecondSide(session, audience);
            session = Ok(CampaignExercises.CompleteSecondMovement(session, audience, Hash("second-movement-completion")));
            session = Ok(CampaignExercises.CompleteSecondBreakdown(session));
            session = SubmitFinishWithoutAttack(session, audience, actingElementId,
                CampaignExercises.CompleteSecondCombatNoAttack);
            return FinishCycleThroughPostCombatPasses(session, audience);
        }

        // One real second-side move when the host owner draft offers any (reaches SecondMovePending).
        private ExerciseSession TryMoveSecondSide(ExerciseSession session, CampaignActionAudience audience)
        {
            foreach (var element in session.CurrentSnapshot.World.Elements.Select(value => value.ElementId)
                .Order(StringComparer.Ordinal))
            {
                string[] ids;
                try { ids = [.. Cna.Core.Hosting.CampaignHostAdapter.QuerySecondMovementOwnerDraft(session, audience, element).PrivateActionIds]; }
                catch (Exception error) when (error is InvalidOperationException or ArgumentException) { continue; }
                if (ids.Length == 0) continue;
                var moved = CampaignExercises.SubmitSecondMovement(session, audience, ids[0], element);
                if (moved.IsAccepted) return Cap(moved.SuccessorSession!);
            }
            return session;
        }

        private ExerciseSession CloseOutStageEntrySide(ExerciseSession session,
            CampaignActionAudience audience, string actingElementId, bool isSecondSide)
        {
            session = Ok(isSecondSide
                ? CampaignExercises.SubmitStageEntrySecondReserve(session, audience, Hash("stage2-reserve"), elementId: null)
                : CampaignExercises.SubmitStageEntryReserve(session, audience, Hash("stage2-reserve"), elementId: null));
            session = Ok(isSecondSide
                ? CampaignExercises.CompleteStageEntrySecondMovement(session, audience, Hash("stage2-movement-completion"))
                : CampaignExercises.CompleteStageEntryMovement(session, audience, Hash("stage2-movement-completion")));
            session = Ok(isSecondSide
                ? CampaignExercises.CompleteStageEntrySecondBreakdown(session)
                : CampaignExercises.CompleteStageEntryBreakdown(session));
            session = SubmitFinishWithoutAttack(session, audience, actingElementId,
                isSecondSide ? CampaignExercises.CompleteStageEntrySecondCombatNoAttack
                    : CampaignExercises.CompleteStageEntryCombatNoAttack);
            return FinishCycleThroughPostCombatPasses(session, audience);
        }

        private ExerciseSession SubmitFinishWithoutAttack(ExerciseSession session,
            CampaignActionAudience audience, string actingElementId,
            Func<ExerciseSession, ExerciseStepResult> completeNoAttack)
        {
            var checkpoint = CampaignExercises.QueryCheckpoint(session);
            var actionId = new FinishCombatWithoutAttackAction(actingElementId).ActionId;
            var submission = new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
                checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId, audience, actionId);
            session = Ok(CampaignExercises.SubmitContactOwnerCombatSelection(session, submission, actingElementId));
            session = Ok(completeNoAttack(session));
            return Ok(CampaignExercises.CompleteNativeReserveRelease(session));
        }
    }
}
