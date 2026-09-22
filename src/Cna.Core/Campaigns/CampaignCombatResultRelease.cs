using System.Text;
using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>Owned native lineage with an independently trusted synthetic pre-Combat boundary.</summary>
internal sealed class CombatResultReleaseSource
{
    private readonly byte[] created;
    private readonly byte[][] predecessorEvents;
    private readonly byte[][] roundEvents;
    private readonly byte[][] resultEvents;
    public CombatResultReleaseSource(string caseId, CampaignCombatCreationRequest request, byte[] created,
        CombatStepsBoundary boundary, IReadOnlyList<CombatStepsInput> predecessorInputs,
        IReadOnlyList<byte[]> predecessorEvents, IReadOnlyList<CombatRoundInput> roundInputs,
        IReadOnlyList<byte[]> roundEvents, IReadOnlyList<CombatResolutionInput> resultInputs,
        IReadOnlyList<byte[]> resultEvents)
    {
        CaseId = caseId; Request = request; this.created = created.ToArray(); Boundary = boundary;
        PredecessorInputs = Array.AsReadOnly(predecessorInputs.ToArray());
        RoundInputs = Array.AsReadOnly(roundInputs.ToArray()); ResultInputs = Array.AsReadOnly(resultInputs.ToArray());
        this.predecessorEvents = Copy(predecessorEvents); this.roundEvents = Copy(roundEvents); this.resultEvents = Copy(resultEvents);
    }
    public string CaseId { get; }
    public CampaignCombatCreationRequest Request { get; }
    public byte[] Created => created.ToArray();
    public CombatStepsBoundary Boundary { get; }
    public IReadOnlyList<CombatStepsInput> PredecessorInputs { get; }
    public IReadOnlyList<byte[]> PredecessorEvents => Array.AsReadOnly(Copy(predecessorEvents));
    public IReadOnlyList<CombatRoundInput> RoundInputs { get; }
    public IReadOnlyList<byte[]> RoundEvents => Array.AsReadOnly(Copy(roundEvents));
    public IReadOnlyList<CombatResolutionInput> ResultInputs { get; }
    public IReadOnlyList<byte[]> ResultEvents => Array.AsReadOnly(Copy(resultEvents));
    private static byte[][] Copy(IReadOnlyList<byte[]> values) => values.Select(value => value.ToArray()).ToArray();
}

internal sealed record CombatResultReleaseProjection(CombatResolutionState Result, CombatReleaseBase Basis, CombatReleaseState Release);

/// <summary>Bounded native Result2 to empty Release adapter. Does not advance cycle control.</summary>
internal static class CampaignCombatResultRelease
{
    public static CombatResultReleaseProjection Replay(CombatResultReleaseSource source,
        IReadOnlyList<CombatReleaseInput> releaseInputs, IReadOnlyList<byte[]> releaseEvents)
    {
        ValidateSuffix(releaseInputs, releaseEvents);
        var (result, basis) = Derive(source);
        return new(result, basis, CampaignCombatReserveRelease.Replay(basis, source.Request, releaseInputs, releaseEvents));
    }

    public static CombatReleaseResult Apply(CombatResultReleaseSource source, IReadOnlyList<CombatReleaseInput> releaseInputs,
        IReadOnlyList<byte[]> releaseEvents, CombatReleaseInput input)
    {
        ValidateSuffix(releaseInputs, releaseEvents);
        ValidateInput(input);
        var (_, basis) = Derive(source);
        return CampaignCombatReserveRelease.Apply(basis, source.Request, releaseInputs, releaseEvents, input);
    }

    public static CombatReleaseBase ReadBase(ReadOnlySpan<byte> bytes, CombatResultReleaseSource source)
    {
        CampaignCombatReserveReleaseCodec.ValidateBaseSyntax(bytes);
        var (_, basis) = Derive(source);
        return CampaignCombatReserveReleaseCodec.ReadBase(bytes, basis, source.Request);
    }

    public static CombatResultReleaseProjection ReadState(ReadOnlySpan<byte> bytes, CombatResultReleaseSource source,
        IReadOnlyList<CombatReleaseInput> releaseInputs, IReadOnlyList<byte[]> releaseEvents)
    {
        CampaignCombatReserveReleaseCodec.ValidateSyntax(bytes, "state");
        var projection = Replay(source, releaseInputs, releaseEvents);
        Require(bytes.SequenceEqual(CampaignCombatReserveReleaseCodec.SerializeState(projection.Release)),
            "Release state differs from authenticated Result2 continuation.");
        return projection;
    }

    private static void ValidateSuffix(IReadOnlyList<CombatReleaseInput> inputs, IReadOnlyList<byte[]> events)
    {
        ArgumentNullException.ThrowIfNull(inputs); ArgumentNullException.ThrowIfNull(events);
        Require(events.Count <= 2 && inputs.Count == events.Count, "Empty Release suffix capacity/count mismatch.");
        foreach (var bytes in events) CampaignCombatReserveReleaseCodec.ValidateSyntax(bytes, "event");
        foreach (var input in inputs) ValidateInput(input);
    }

    private static void ValidateInput(CombatReleaseInput input)
    {
        _ = CampaignCombatReserveReleaseCodec.SerializeInput(input);
        Require(input.Actor == CampaignOpeningPreambleActor.System && input.Command.Kind is "open" or "complete" &&
            input.AdmittedAt is null && input.ClockAvailable && input.Command.DecisionId is null &&
            input.Command.Unit is null && input.Command.Choice is null,
            "Empty Result2 Release admits only untimed System open/complete.");
    }

    private static (CombatResolutionState Result, CombatReleaseBase Basis) Derive(CombatResultReleaseSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        Require(Supported.TryGetValue(source.CaseId, out var admission), "Unsupported Result2 Release source.");
        var resultEvents = source.ResultEvents;
        var result = CampaignCombatResolution.ReplayTrustedBoundary(source.Request, source.Created, source.Boundary,
            source.PredecessorInputs, source.PredecessorEvents, source.RoundInputs, source.RoundEvents,
            source.ResultInputs, resultEvents);
        // Typed closed upstream envelopes carry no ignored predecessor metadata. The replayed committed
        // hash binds canonical Base2, selection inputs/events and both seals through their receipt chains.
        // This compatibility catalogue admits the frozen synthetic sources, not arbitrary positive history.
        Require(result.Context.CommittedHash == admission!.CommittedHash &&
            Signature(source.ResultInputs) == admission.Signature, "Result2 source differs from supported lineage/choices.");
        Require(result.Closed && result.Status == "closed" && result.Window is null &&
            result.CaCompletionReceiptId is not null && result.RoundClosureReceiptId is not null &&
            result.World.Settlements.All(s => s.Disposition is not null && s.Losses is not null &&
                s.Retreat is not null && s.Relationships is not null), "Result2 immediate settlement is incomplete.");
        for (var index = 0; index < resultEvents.Count; index++)
        {
            using var document = JsonDocument.Parse(resultEvents[index]);
            var frame = document.RootElement; var effect = frame.GetProperty("effect");
            var reason = effect.TryGetProperty("reason", out var reasonValue) ? reasonValue.GetString() : null;
            Require(reason is null or "owner-choice" or "not-required", "Result2 fallback source is outside Release profile.");
            var input = source.ResultInputs[index];
            if (input.Command.Kind == "choose")
                Require(frame.GetProperty("author").GetString() == CampaignCombatSealedRoundCodec.Actor(input.Actor) &&
                    reason == "owner-choice" && effect.GetProperty("kind").GetString() is "disposition-recorded" or "custody-settled" &&
                    effect.GetProperty("payload").GetProperty("kind").GetString() == input.Command.Choice,
                    "Result2 choice must retain owner author and exact accepted payload.");
        }
        var committed = result.Context.Committed; var boundary = committed.Base.Boundary; var cycle = boundary.Cycle;
        var selected = committed.Base.Steps.Selection!.Attacker.Unit;
        var own = result.World.Elements.Single(e => e.ElementId == selected.ElementId);
        var unit = new CampaignCombatUnitKey(result.World.CreationBinding,
            source.Request.Context.Setup.Artifact.Definition.Elements.Single(e => e.ElementId == own.ElementId).SideId, own.ElementId);
        Require(unit == selected && own.ReserveStatus == CampaignElementReserveStatus.None,
            "Settled Release member differs from selected acting unit.");
        var role = cycle.PlayerPhaseSlot == "first-acting-side" ? LandActorRole.FirstActingSide : LandActorRole.SecondActingSide;
        var position = Cna1979LandSequence.CreateTurn(cycle.GameTurn).Single(p => p.OperationStage == cycle.OperationStage &&
            p.ActorRole == role && p.SegmentId == LandSegmentIds.ReserveRelease);
        using var stateDocument = JsonDocument.Parse(CampaignCombatResolutionCodec.SerializeState(result));
        var worldHash = CampaignOpeningPreambleCodec.Hash(Encoding.UTF8.GetBytes(stateDocument.RootElement.GetProperty("world").GetRawText()));
        var scope = new CombatReleaseScope(cycle.GameTurn, cycle.OperationStage, cycle.PlayerPhaseSlot, cycle.ActingSide);
        var member = new CombatReleaseMember(unit, own.ReserveStatus, 10,
            own.OperationalState.CapabilityPointsExpended, new(scope));
        var basis = new CombatReleaseBase(1, "settled-empty-release", cycle, boundary.FirstActingSide, position.PositionId,
            result.StateVersion, result.Prefix, result.CaCompletionReceiptId!, worldHash, result.RandomState, null,
            [member], committed.AttackHistory);
        _ = CampaignCombatReserveReleaseCodec.SerializeBase(basis, source.Request);
        return (result, basis);
    }

    private static string Signature(IReadOnlyList<CombatResolutionInput> inputs) => string.Join(" ", inputs.Select(input =>
        input.Command.Kind + ":" + (input.Command.Choice ?? "-") + ":" + CampaignCombatSealedRoundCodec.Actor(input.Actor)));
    private static void Require(bool condition, string message) { if (!condition) throw new JsonException(message); }

    private sealed record Admission(string CommittedHash, string Signature);
    // Frozen combat-result-settlement-v2.json: 32 named sources, 28 distinct committed hashes.
    // Literal compatibility data only; production performs full native replay and loads no fixture.
    private static readonly Dictionary<string, Admission> Supported = new Dictionary<string, Admission>(StringComparer.Ordinal)
    {
        ["ordinary.axis.attacker"] = new("sha256:79a7b09d5a128c1e1b8b64f6ab498971b9f0f45f7c747d671b4999acebe0a6b7",
            "resolve:-:system advance:-:system advance:-:system advance:-:system advance:-:system advance:-:system advance:-:system"),
        ["ordinary.axis.defender"] = new("sha256:d6c2b8ca1136d66c79dc84cb9469dc3213347e4c43faeeafc77c5bdd18ddfade",
            "resolve:-:system advance:-:system advance:-:system advance:-:system advance:-:system advance:-:system advance:-:system"),
        ["ordinary.commonwealth.attacker"] = new("sha256:44297ba388dd0ce48c45b7444bdcb62d2070dff77da03d9c8ae309cdd8bf1e5f",
            "resolve:-:system advance:-:system advance:-:system advance:-:system advance:-:system advance:-:system advance:-:system"),
        ["ordinary.commonwealth.defender"] = new("sha256:4774c2e075a3f57faea8fde15e16cc12708bbe1d670ddbdd3b4d061934742259",
            "resolve:-:system advance:-:system advance:-:system advance:-:system advance:-:system advance:-:system advance:-:system"),
        ["zero-retreat.axis.attacker"] = new("sha256:5bf7c7b05d6ada120bc4aeb72fa9f091aa8d275f725f05b8704bd70b91f20cb9",
            "resolve:-:system advance:-:system choose:retreat:commonwealth advance:-:system advance:-:system advance:-:system advance:-:system advance:-:system"),
        ["zero-retreat.axis.defender"] = new("sha256:a3c64c52ded6d157e204bac4b999c8b551138a39478193c683672a9a0e63cf7d",
            "resolve:-:system advance:-:system choose:retreat:commonwealth advance:-:system advance:-:system advance:-:system advance:-:system advance:-:system"),
        ["zero-retreat.commonwealth.attacker"] = new("sha256:6c649a20b62299c4e4575faf3ccd5a0d3432c048995c4e63902563ebf4882290",
            "resolve:-:system advance:-:system choose:retreat:axis advance:-:system advance:-:system advance:-:system advance:-:system advance:-:system"),
        ["zero-retreat.commonwealth.defender"] = new("sha256:d4dd27a065be17c5a6b4cd244677331d6282735a371071be5749f6637a9a15b4",
            "resolve:-:system advance:-:system choose:retreat:axis advance:-:system advance:-:system advance:-:system advance:-:system advance:-:system"),
        ["refusal-loss-dp.axis.attacker"] = new("sha256:4ca0d54a582dff491e382c841f77a699d1478396a9f6e1c0d75fea668f7ecfc3",
            "resolve:-:system advance:-:system choose:refuse-retreat:commonwealth advance:-:system advance:-:system advance:-:system advance:-:system advance:-:system"),
        ["refusal-loss-dp.axis.defender"] = new("sha256:8a2d4bdb409cad370fa18c612227a1b1ce1f67c61a2b66ed599bb19e9c49b750",
            "resolve:-:system advance:-:system choose:refuse-retreat:commonwealth advance:-:system advance:-:system advance:-:system advance:-:system advance:-:system"),
        ["refusal-loss-dp.commonwealth.attacker"] = new("sha256:bb62e8759abaa5dc31094392e7eb1eacaf4625923f0edb83b4251943ecc173b9",
            "resolve:-:system advance:-:system choose:refuse-retreat:axis advance:-:system advance:-:system advance:-:system advance:-:system advance:-:system"),
        ["refusal-loss-dp.commonwealth.defender"] = new("sha256:46ccc29406c79d805c1813ac5f2158959253e3c209c0f04a67cebd25108350f0",
            "resolve:-:system advance:-:system choose:refuse-retreat:axis advance:-:system advance:-:system advance:-:system advance:-:system advance:-:system"),
        ["zero-engaged.axis.attacker"] = new("sha256:194df22f6eebb005bf60c7c07c397549a7ce0242cbea24f415ffd34aba0a08ee",
            "resolve:-:system advance:-:system advance:-:system advance:-:system advance:-:system advance:-:system advance:-:system"),
        ["zero-engaged.axis.defender"] = new("sha256:2eb7cbb8ca43e6f9177b884881968ee1dfae5dd7f76a8c06e8453a29d9ed15b1",
            "resolve:-:system advance:-:system advance:-:system advance:-:system advance:-:system advance:-:system advance:-:system"),
        ["zero-engaged.commonwealth.attacker"] = new("sha256:cf6c19e40926f1578ab840138e2f992c3f346789cc7139dc7f1df56d741c85f6",
            "resolve:-:system advance:-:system advance:-:system advance:-:system advance:-:system advance:-:system advance:-:system"),
        ["zero-engaged.commonwealth.defender"] = new("sha256:50a256c325341e46e44df0f3c9de2b627f919063c12390de59ff325a2406db3d",
            "resolve:-:system advance:-:system advance:-:system advance:-:system advance:-:system advance:-:system advance:-:system"),
        ["defender-capture-guard.axis.attacker"] = new("sha256:23849072b479609ca6a9a4053edaddeac616cad3a892e916faaf812604f773f8",
            "resolve:-:system advance:-:system advance:-:system advance:-:system advance:-:system choose:relocate-and-guard:axis advance:-:system advance:-:system advance:-:system"),
        ["defender-capture-guard.axis.defender"] = new("sha256:30b41d662fc8c7610c58412ca761937e462cb5de2e72da7f7955e01895329d41",
            "resolve:-:system advance:-:system advance:-:system advance:-:system advance:-:system choose:relocate-and-guard:axis advance:-:system advance:-:system advance:-:system"),
        ["defender-capture-guard.commonwealth.attacker"] = new("sha256:6b10a39ec7f53e2acbd859b2e3521c6c5cf5b3b6bd9649b8c9e6b5f3e680879a",
            "resolve:-:system advance:-:system advance:-:system advance:-:system advance:-:system choose:relocate-and-guard:commonwealth advance:-:system advance:-:system advance:-:system"),
        ["defender-capture-guard.commonwealth.defender"] = new("sha256:80fd7c00ec2ea8dd2bd54fa09c5ea85cb7b1d0da9b34df3407527f52e8738b5c",
            "resolve:-:system advance:-:system advance:-:system advance:-:system advance:-:system choose:relocate-and-guard:commonwealth advance:-:system advance:-:system advance:-:system"),
        ["defender-capture-escape.axis.attacker"] = new("sha256:23849072b479609ca6a9a4053edaddeac616cad3a892e916faaf812604f773f8",
            "resolve:-:system advance:-:system advance:-:system advance:-:system advance:-:system choose:leave-unguarded:axis advance:-:system advance:-:system advance:-:system"),
        ["defender-capture-escape.axis.defender"] = new("sha256:30b41d662fc8c7610c58412ca761937e462cb5de2e72da7f7955e01895329d41",
            "resolve:-:system advance:-:system advance:-:system advance:-:system advance:-:system choose:leave-unguarded:axis advance:-:system advance:-:system advance:-:system"),
        ["defender-capture-escape.commonwealth.attacker"] = new("sha256:6b10a39ec7f53e2acbd859b2e3521c6c5cf5b3b6bd9649b8c9e6b5f3e680879a",
            "resolve:-:system advance:-:system advance:-:system advance:-:system advance:-:system choose:leave-unguarded:commonwealth advance:-:system advance:-:system advance:-:system"),
        ["defender-capture-escape.commonwealth.defender"] = new("sha256:80fd7c00ec2ea8dd2bd54fa09c5ea85cb7b1d0da9b34df3407527f52e8738b5c",
            "resolve:-:system advance:-:system advance:-:system advance:-:system advance:-:system choose:leave-unguarded:commonwealth advance:-:system advance:-:system advance:-:system"),
        ["attacker-capture-guard-cp-limit.axis.attacker"] = new("sha256:3a6acdfbfd239202e50fc8d922ee87bf808d692da586c835436bb40afed2f414",
            "resolve:-:system advance:-:system choose:retreat:commonwealth advance:-:system advance:-:system advance:-:system choose:relocate-and-guard:commonwealth advance:-:system advance:-:system advance:-:system"),
        ["attacker-capture-guard-cp-limit.axis.defender"] = new("sha256:d44225809fae4d728894b2c1593198b8f0b0d063510971550452a3cd4860e770",
            "resolve:-:system advance:-:system choose:retreat:commonwealth advance:-:system advance:-:system advance:-:system choose:relocate-and-guard:commonwealth advance:-:system advance:-:system advance:-:system"),
        ["attacker-capture-guard-cp-limit.commonwealth.attacker"] = new("sha256:58d1741fc75731424462da336065dfb8cb8f639d83336fab3ee3c6180ebf9a71",
            "resolve:-:system advance:-:system choose:retreat:axis advance:-:system advance:-:system advance:-:system choose:relocate-and-guard:axis advance:-:system advance:-:system advance:-:system"),
        ["attacker-capture-guard-cp-limit.commonwealth.defender"] = new("sha256:cb68641cd03e915b20c9aee14ea07a766cedc9e4f5227de41561c92d426e7a06",
            "resolve:-:system advance:-:system choose:retreat:axis advance:-:system advance:-:system advance:-:system choose:relocate-and-guard:axis advance:-:system advance:-:system advance:-:system"),
        ["attacker-capture-escape.axis.attacker"] = new("sha256:b415da2684b29f748c3eb103df6bc1f6e56f7bd8c80496dd77dc04182993fc46",
            "resolve:-:system advance:-:system choose:retreat:commonwealth advance:-:system advance:-:system advance:-:system choose:leave-unguarded:commonwealth advance:-:system advance:-:system advance:-:system"),
        ["attacker-capture-escape.axis.defender"] = new("sha256:b6d9b6af10d0e2a30b5ab097265207ed1e9da6cd65bfe7bf7298906b24e172fb",
            "resolve:-:system advance:-:system choose:retreat:commonwealth advance:-:system advance:-:system advance:-:system choose:leave-unguarded:commonwealth advance:-:system advance:-:system advance:-:system"),
        ["attacker-capture-escape.commonwealth.attacker"] = new("sha256:3a1612bc64b28e2b7cc6acc9317f485f78c638d3b6d97dbe3f246db5359614f6",
            "resolve:-:system advance:-:system choose:retreat:axis advance:-:system advance:-:system advance:-:system choose:leave-unguarded:axis advance:-:system advance:-:system advance:-:system"),
        ["attacker-capture-escape.commonwealth.defender"] = new("sha256:0dd512efd430ee05979bbe5afd03499fed57f29790cf22888b4179e3818f4a29",
            "resolve:-:system advance:-:system choose:retreat:axis advance:-:system advance:-:system advance:-:system choose:leave-unguarded:axis advance:-:system advance:-:system advance:-:system"),
    };
}
