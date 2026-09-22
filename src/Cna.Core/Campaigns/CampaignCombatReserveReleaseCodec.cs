using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>Canonical isolated probe codec; expected value is an explicit independent trust input.</summary>
internal static class CampaignCombatReserveReleaseCodec
{
    public static byte[] SerializeBase(CombatReleaseBase basis, CampaignCombatCreationRequest retainedRequest)
    {
        ArgumentNullException.ThrowIfNull(basis);
        Validate(basis, retainedRequest);
        using var stream = new MemoryStream();
        using (var w = new Utf8JsonWriter(stream))
        {
            w.WriteStartObject(); w.WriteNumber("contractVersion", basis.ContractVersion); w.WriteString("profile", basis.Profile);
            w.WritePropertyName("cycle"); CampaignCombatReserveCompletionCodec.WriteCycle(w, basis.Cycle);
            w.WriteString("firstActingSide", Side(basis.FirstActingSide)); w.WriteString("positionId", basis.PositionId);
            w.WriteNumber("priorVersion", basis.PriorVersion); w.WriteString("priorPrefix", basis.PriorPrefix);
            w.WriteString("combatCompletionReceiptId", basis.CombatCompletionReceiptId); w.WriteString("retainedWorldHash", basis.RetainedWorldHash);
            CampaignSnapshotSerializer.WriteRandomState(w, basis.RandomState); Number(w, "acceptedHighWater", basis.AcceptedHighWater);
            w.WriteStartArray("members");
            foreach (var m in basis.Members)
            {
                w.WriteStartObject(); w.WritePropertyName("unit"); Unit(w, m.Unit);
                w.WriteString("status", m.Status switch { CampaignElementReserveStatus.None => "none", CampaignElementReserveStatus.ReserveI => "I", CampaignElementReserveStatus.ReserveII => "II", _ => throw new JsonException("Invalid reserve status.") }); w.WriteNumber("baseCpa", m.BaseCpa);
                w.WriteStartObject("spentCp"); w.WriteNumber("numerator", m.SpentCp.Numerator); w.WriteNumber("denominator", m.SpentCp.Denominator); w.WriteEndObject();
                w.WritePropertyName("history"); History(w, m.History); w.WriteEndObject();
            }
            w.WriteEndArray(); w.WriteStartArray("attackHistory");
            foreach (var a in basis.AttackHistory)
            {
                w.WriteStartObject(); w.WriteString("commitmentId", a.CommitmentId); w.WriteString("cycleId", a.CycleId); w.WriteString("segmentId", a.SegmentId);
                w.WritePropertyName("attacker"); Unit(w, a.Attacker); w.WritePropertyName("defender"); Unit(w, a.Defender);
                w.WriteString("targetLocationId", a.TargetLocationId); w.WriteNumber("gameTurn", a.GameTurn); w.WriteNumber("operationStage", a.OperationStage); w.WriteEndObject();
            }
            w.WriteEndArray(); w.WriteEndObject();
        }
        var bytes = stream.ToArray();
        CheckRaw(bytes);
        return bytes;
    }

    /// <summary>Caller must retain expected base independently. Candidate-derived expected values do not authenticate anything.</summary>
    public static CombatReleaseBase ReadBase(ReadOnlySpan<byte> bytes, CombatReleaseBase independentlyExpectedBase,
        CampaignCombatCreationRequest retainedRequest)
    {
        CheckRaw(bytes); // Reject malformed raw input before touching trusted context.
        if (!bytes.SequenceEqual(SerializeBase(independentlyExpectedBase, retainedRequest)))
            throw new JsonException("Release base differs from independently expected canonical value.");
        return independentlyExpectedBase; // All nested values immutable; collections owned at construction.
    }

    private static void Validate(CombatReleaseBase b, CampaignCombatCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var c = b.Cycle; var setup = request.Context.Setup;
        Require(c is not null && b.RandomState is not null && b.ContractVersion == 1 && b.Profile is "isolated-ledger" or "settled-empty-release");
        Require(c!.ContractVersion == 1 && c.GameTurn is >= 1 and <= 111 && c.OperationStage is >= 1 and <= 3 && c.Ordinal >= 1 && c.OpenedAuthorityVersion >= 1);
        Require(c.CampaignId == request.CampaignId && c.RulesetHash == request.Context.RulesetHash && c.SetupId == setup.SetupId && c.SetupHash == setup.SetupHash &&
            c.ContentPackId == setup.Artifact.Identity.PackId && c.ContentHash == setup.Artifact.Identity.Hash && c.ScenarioId == setup.Scenario.ScenarioId && c.AdmittedPolicyBundleDigest == request.Context.Configuration.Hash);
        Require(Enum.IsDefined(b.FirstActingSide) && Enum.IsDefined(c.ActingSide) && c.PlayerPhaseSlot is "first-acting-side" or "second-acting-side");
        Require(c.ActingSide == (c.PlayerPhaseSlot == "first-acting-side" ? b.FirstActingSide : b.FirstActingSide == LandSide.Axis ? LandSide.Commonwealth : LandSide.Axis));
        var role = c.PlayerPhaseSlot == "first-acting-side" ? LandActorRole.FirstActingSide : LandActorRole.SecondActingSide;
        Require(Cna1979LandSequence.CreateTurn(c.GameTurn).Any(p => p.PositionId == b.PositionId && p.OperationStage == c.OperationStage && p.SegmentId == LandSegmentIds.ReserveRelease && p.ActorRole == role));
        Require(b.PriorVersion >= c.OpenedAuthorityVersion && b.Members.Count <= 32 && b.AttackHistory.Count <= 512);
        var scope = new CombatReleaseScope(c.GameTurn, c.OperationStage, c.PlayerPhaseSlot, c.ActingSide);
        if (b.Profile == "settled-empty-release")
        {
            Require(b.AcceptedHighWater is null && b.Members.Count == 1);
            var member = b.Members[0];
            Require(member is not null && member.Status == CampaignElementReserveStatus.None &&
                member.History == new CombatReleaseHistory(scope));
        }
        Require(b.AttackHistory.All(a => a is not null && a.Attacker is not null && a.Defender is not null));
        CampaignCombatUnitKey? previous = null;
        foreach (var m in b.Members)
        {
            Require(m is not null && m.Unit is not null && m.History is not null && m.SpentCp is not null);
            var u = m!.Unit; var h = m.History;
            Require(previous is null || Compare(previous, u) < 0); previous = u;
            Require(u.CreationBinding == request.CreationBinding && u.OriginalSide == Side(c.ActingSide) && m.BaseCpa >= 1 && h.Scope == scope);
            Require(Enum.IsDefined(m.Status) && (m.Status != CampaignElementReserveStatus.ReserveI || c.Ordinal == 1) && (m.Status != CampaignElementReserveStatus.ReserveII || c.Ordinal > 1));
            if (m.Status != CampaignElementReserveStatus.None) Require(h.DesignationReceiptId is not null);
            if (m.Status == CampaignElementReserveStatus.ReserveI) Require(h.ConversionReceiptId is null);
            if (m.Status == CampaignElementReserveStatus.ReserveII) Require(h.ConversionReceiptId is not null);
            if (h.ReleasedType is null)
            {
                Require(h.ReleaseReceiptId is null && h.ReleaseCycle is null && h.CpaBasis is null && h.VoluntaryCeiling is null && h.OffensiveCommitmentId is null && h.NextMovement is null);
                if (m.Status == CampaignElementReserveStatus.None) Require(h.DesignationReceiptId is null && h.ConversionReceiptId is null);
            }
            else
            {
                Require(m.Status == CampaignElementReserveStatus.None && h.ReleasedType is "I" or "II" && h.DesignationReceiptId is not null && h.ReleaseReceiptId is not null);
                Require(h.ReleaseCycle >= 1 && h.ReleaseCycle < c.Ordinal && h.CpaBasis == m.BaseCpa && h.VoluntaryCeiling == (h.ReleasedType == "I" ? m.BaseCpa : m.BaseCpa / 2));
                Require((h.ConversionReceiptId is not null) == (h.ReleasedType == "II") && (h.ReleasedType == "I" ? h.ReleaseCycle == 1 : h.ReleaseCycle > 1));
                Require(h.NextMovement is { Status: "expired", CompletionReceiptId: not null } ex && ex.Scope == scope && ex.Ordinal == h.ReleaseCycle + 1);
                if (h.OffensiveCommitmentId is not null)
                {
                    var matches = b.AttackHistory.Where(a => a.CommitmentId == h.OffensiveCommitmentId).ToArray();
                    Require(matches.Length == 1 && matches[0].Attacker == u && matches[0].GameTurn == c.GameTurn && matches[0].OperationStage == c.OperationStage);
                }
            }
        }
    }
    private static int Compare(CampaignCombatUnitKey a, CampaignCombatUnitKey b)
    {
        var compare = StringComparer.Ordinal.Compare(a.CreationBinding, b.CreationBinding);
        if (compare == 0) compare = StringComparer.Ordinal.Compare(a.OriginalSide, b.OriginalSide);
        return compare == 0 ? StringComparer.Ordinal.Compare(a.ElementId, b.ElementId) : compare;
    }
    private static void Scope(Utf8JsonWriter w, CombatReleaseScope s)
    {
        w.WriteStartObject(); w.WriteNumber("gameTurn", s.GameTurn); w.WriteNumber("operationStage", s.OperationStage);
        w.WriteString("playerPhaseSlot", s.PlayerPhaseSlot); w.WriteString("actingSide", Side(s.ActingSide)); w.WriteEndObject();
    }
    private static void History(Utf8JsonWriter w, CombatReleaseHistory h)
    {
        w.WriteStartObject(); w.WritePropertyName("scope"); Scope(w, h.Scope);
        w.WriteString("designationReceiptId", h.DesignationReceiptId); w.WriteString("conversionReceiptId", h.ConversionReceiptId);
        w.WriteString("releasedType", h.ReleasedType); w.WriteString("releaseReceiptId", h.ReleaseReceiptId);
        Number(w, "releaseCycle", h.ReleaseCycle); Number(w, "cpaBasis", h.CpaBasis); Number(w, "voluntaryCeiling", h.VoluntaryCeiling);
        w.WriteString("offensiveCommitmentId", h.OffensiveCommitmentId); w.WritePropertyName("nextMovement");
        if (h.NextMovement is not { } ex) w.WriteNullValue();
        else
        {
            w.WriteStartObject(); w.WritePropertyName("scope"); Scope(w, ex.Scope); w.WriteNumber("ordinal", ex.Ordinal);
            w.WriteString("status", ex.Status); w.WriteString("completionReceiptId", ex.CompletionReceiptId); w.WriteEndObject();
        }
        w.WriteEndObject();
    }
    private static void Unit(Utf8JsonWriter w, CampaignCombatUnitKey u)
    {
        w.WriteStartObject(); w.WriteString("creationBinding", u.CreationBinding); w.WriteString("originalSide", u.OriginalSide); w.WriteString("elementId", u.ElementId); w.WriteEndObject();
    }
    private static void Number(Utf8JsonWriter w, string name, long? value)
    { if (value is { } n) w.WriteNumber(name, n); else w.WriteNull(name); }
    private static string Side(LandSide side) => CampaignSnapshotSerializer.FormatSide(side);
    private static void Require([DoesNotReturnIf(false)] bool condition) { if (!condition) throw new JsonException("Invalid isolated Release base."); }

    internal static void ValidateBaseSyntax(ReadOnlySpan<byte> bytes) => CheckRaw(bytes);

    private static void CheckRaw(ReadOnlySpan<byte> bytes)
    {
        Require(bytes.Length is > 0 and <= 1_048_576);
        // Every permitted string is an ASCII atom/hash: escaping and whitespace have no canonical use.
        foreach (var item in bytes) Require(item is > 32 and < 127 && item != 92);
        using var document = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 32 });
        BaseShape(document.RootElement);
    }
    private static JsonElement[] Fields(JsonElement value, params string[] names)
    {
        Require(value.ValueKind == JsonValueKind.Object);
        var fields = value.EnumerateObject().ToArray();
        Require(fields.Length == names.Length);
        for (var i = 0; i < names.Length; i++) Require(fields[i].Name == names[i]);
        return fields.Select(f => f.Value).ToArray();
    }
    private static void BaseShape(JsonElement value)
    {
        var f = Fields(value, "contractVersion", "profile", "cycle", "firstActingSide", "positionId", "priorVersion", "priorPrefix", "combatCompletionReceiptId", "retainedWorldHash", "randomState", "acceptedHighWater", "members", "attackHistory");
        Int(f[0]); Id(f[1]); CycleShape(f[2]); SideShape(f[3]); Id(f[4]); Long(f[5]); Hash(f[6]); Id(f[7]); Hash(f[8]);
        var r = Fields(f[9], "contractVersion", "algorithmId", "seed", "nextByteCursor"); Int(r[0]); Id(r[1]); Ulong(r[2]); Ulong(r[3]);
        if (f[10].ValueKind != JsonValueKind.Null) { Long(f[10]); Require(f[10].GetInt64() is >= 0 and <= 253402300799999); }
        Require(f[11].ValueKind == JsonValueKind.Array && f[11].GetArrayLength() <= 32);
        foreach (var m in f[11].EnumerateArray())
        {
            var p = Fields(m, "unit", "status", "baseCpa", "spentCp", "history"); UnitShape(p[0]); Id(p[1]); Int(p[2]);
            var cp = Fields(p[3], "numerator", "denominator"); Long(cp[0]); Int(cp[1]);
            Require(cp[0].GetInt64() >= 0 && cp[1].GetInt32() > 0 && BigInteger.GreatestCommonDivisor(cp[0].GetInt64(), cp[1].GetInt32()) == 1);
            HistoryShape(p[4]);
        }
        Require(f[12].ValueKind == JsonValueKind.Array && f[12].GetArrayLength() <= 512);
        foreach (var a in f[12].EnumerateArray())
        {
            var p = Fields(a, "commitmentId", "cycleId", "segmentId", "attacker", "defender", "targetLocationId", "gameTurn", "operationStage");
            Id(p[0]); Hash(p[1]); Id(p[2]); UnitShape(p[3]); UnitShape(p[4]); Id(p[5]); Int(p[6]); Int(p[7]);
        }
    }
    private static void CycleShape(JsonElement value)
    {
        var f = Fields(value, "contractVersion", "campaignId", "rulesetHash", "setupId", "setupHash", "contentPackId", "contentHash", "scenarioId", "gameTurn", "operationStage", "playerPhaseSlot", "actingSide", "ordinal", "openedAuthorityVersion", "openingPrefix", "admittedPolicyBundleDigest");
        Int(f[0]); Id(f[1]); Hash(f[2], false); Id(f[3]); Hash(f[4]); Id(f[5]); Hash(f[6]); Id(f[7]); Int(f[8]); Int(f[9]); Id(f[10]); SideShape(f[11]); Int(f[12]); Long(f[13]); Hash(f[14]); Hash(f[15]);
    }
    private static void ScopeShape(JsonElement value)
    { var f = Fields(value, "gameTurn", "operationStage", "playerPhaseSlot", "actingSide"); Int(f[0]); Int(f[1]); Id(f[2]); SideShape(f[3]); }
    private static void UnitShape(JsonElement value)
    { var f = Fields(value, "creationBinding", "originalSide", "elementId"); Id(f[0]); SideShape(f[1]); Id(f[2]); }
    private static void HistoryShape(JsonElement value)
    {
        var f = Fields(value, "scope", "designationReceiptId", "conversionReceiptId", "releasedType", "releaseReceiptId", "releaseCycle", "cpaBasis", "voluntaryCeiling", "offensiveCommitmentId", "nextMovement");
        ScopeShape(f[0]); NullableId(f[1]); NullableId(f[2]); NullableId(f[3]); NullableId(f[4]);
        for (var i = 5; i <= 7; i++) if (f[i].ValueKind != JsonValueKind.Null) Int(f[i]);
        NullableId(f[8]);
        if (f[9].ValueKind != JsonValueKind.Null)
        { var ex = Fields(f[9], "scope", "ordinal", "status", "completionReceiptId"); ScopeShape(ex[0]); Int(ex[1]); Id(ex[2]); NullableId(ex[3]); }
    }
    private static void NullableId(JsonElement value) { if (value.ValueKind != JsonValueKind.Null) Id(value); }
    private static void Id(JsonElement value)
    {
        Require(value.ValueKind == JsonValueKind.String); var s = value.GetString()!;
        Require(s.Length is >= 1 and <= 128 && char.IsAsciiLetterOrDigit(s[0]) && s.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or ':' or '-'));
    }
    private static void Hash(JsonElement value, bool prefix = true)
    {
        Require(value.ValueKind == JsonValueKind.String); var s = value.GetString()!;
        Require(s.Length == (prefix ? 71 : 64) && (!prefix || s.StartsWith("sha256:", StringComparison.Ordinal)) && s[(prefix ? 7 : 0)..].All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f'));
    }
    private static void SideShape(JsonElement value) { Require(value.ValueKind == JsonValueKind.String && value.GetString() is "axis" or "commonwealth"); }
    private static void Int(JsonElement value) => Require(value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var n) && value.GetRawText() == n.ToString(System.Globalization.CultureInfo.InvariantCulture));
    private static void Long(JsonElement value) => Require(value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var n) && value.GetRawText() == n.ToString(System.Globalization.CultureInfo.InvariantCulture));
    private static void Ulong(JsonElement value) => Require(value.ValueKind == JsonValueKind.Number && value.TryGetUInt64(out var n) && value.GetRawText() == n.ToString(System.Globalization.CultureInfo.InvariantCulture));
    public static byte[] SerializeCommand(CombatReleaseCommand command) => Encode(w => Command(w, command), "command");
    public static byte[] SerializeInput(CombatReleaseInput input) => Encode(w => Input(w, input), "input");
    public static byte[] SerializeState(CombatReleaseState s) => Encode(w =>
    {
        w.WriteStartObject(); w.WriteNumber("contractVersion", 1); w.WriteString("baseHash", s.BaseHash); w.WriteString("releaseId", s.ReleaseId);
        w.WriteNumber("stateVersion", s.StateVersion); w.WriteString("prefix", s.Prefix); w.WriteString("status", s.Status);
        w.WriteString("openingReceiptId", s.OpeningReceiptId); w.WriteString("decisionId", s.DecisionId);
        w.WritePropertyName("timing"); Timing(w, s.Timing); w.WriteBoolean("openingClockFailure", s.OpeningClockFailure);
        Number(w, "acceptedHighWater", s.AcceptedHighWater); w.WriteBoolean("fallbackLocked", s.FallbackLocked); w.WriteString("fallbackReason", s.FallbackReason);
        w.WriteStartArray("members"); foreach (var member in s.Members) Member(w, member); w.WriteEndArray();
        Units(w, "pending", s.Pending); w.WriteStartArray("dispositions");
        foreach (var d in s.Dispositions)
        {
            w.WriteStartObject(); w.WriteString("receiptId", d.ReceiptId); w.WritePropertyName("unit"); Unit(w, d.Unit); w.WriteString("choice", d.Choice);
            w.WriteString("beforeStatus", Status(d.BeforeStatus)); w.WriteString("afterStatus", Status(d.AfterStatus)); w.WriteEndObject();
        }
        w.WriteEndArray(); w.WriteString("completionReceiptId", s.CompletionReceiptId); w.WriteString("retainedWorldHash", s.RetainedWorldHash);
        CampaignSnapshotSerializer.WriteRandomState(w, s.RandomState); w.WriteStartArray("attackHistory");
        foreach (var a in s.AttackHistory)
        {
            w.WriteStartObject(); w.WriteString("commitmentId", a.CommitmentId); w.WriteString("cycleId", a.CycleId); w.WriteString("segmentId", a.SegmentId);
            w.WritePropertyName("attacker"); Unit(w, a.Attacker); w.WritePropertyName("defender"); Unit(w, a.Defender);
            w.WriteString("targetLocationId", a.TargetLocationId); w.WriteNumber("gameTurn", a.GameTurn); w.WriteNumber("operationStage", a.OperationStage); w.WriteEndObject();
        }
        w.WriteEndArray(); w.WriteStartArray("receipts");
        foreach (var r in s.Receipts)
        {
            w.WriteStartObject(); w.WriteString("commandHash", r.CommandHash); w.WriteString("eventHash", r.EventHash); w.WriteString("receiptId", r.ReceiptId);
            w.WriteString("actor", Actor(r.Actor)); w.WriteNumber("stateVersion", r.StateVersion); w.WriteEndObject();
        }
        w.WriteEndArray(); w.WriteEndObject();
    }, "state");

    public static CombatReleaseState ReadState(ReadOnlySpan<byte> bytes, CombatReleaseBase independentlyExpectedBase,
        CampaignCombatCreationRequest request, IReadOnlyList<CombatReleaseInput> trustedInputs, IReadOnlyList<byte[]> events)
    {
        ValidateSyntax(bytes, "state");
        var expected = CampaignCombatReserveRelease.Replay(independentlyExpectedBase, request, trustedInputs, events);
        if (!bytes.SequenceEqual(SerializeState(expected))) throw new JsonException("Release state differs from independently trusted replay.");
        return expected;
    }

    internal static byte[] Event(CombatReleaseBase b, CombatReleaseState prior, CombatReleaseInput input,
        CampaignOpeningPreambleActor author, CombatReleaseEffect effect, string? receiptId) => Encode(w =>
    {
        w.WriteStartObject(); w.WriteNumber("contractVersion", 1);
        w.WriteString("eventType", effect.Kind switch { "release-opened" => "reserve-release-opened", "unit-disposition" => "reserve-unit-disposition-recorded", "release-completed" => "reserve-release-completed", _ => throw new JsonException("Invalid Release effect.") });
        w.WriteString("author", Actor(author)); w.WriteString("campaignId", b.Cycle.CampaignId); w.WriteString("rulesetHash", b.Cycle.RulesetHash);
        w.WriteString("configurationHash", b.Cycle.AdmittedPolicyBundleDigest); w.WriteString("cycleId", CampaignCombatReserveCompletionCodec.CycleId(b.Cycle));
        w.WriteString("positionId", b.PositionId); w.WriteString("baseHash", prior.BaseHash); w.WriteString("releaseId", prior.ReleaseId);
        w.WriteNumber("priorVersion", prior.StateVersion); w.WriteNumber("stateVersion", checked(prior.StateVersion + 1)); w.WriteString("priorPrefix", prior.Prefix);
        w.WritePropertyName("input"); Input(w, input); w.WritePropertyName("effect"); Effect(w, effect);
        if (receiptId is not null) w.WriteString("receiptId", receiptId); w.WriteEndObject();
    }, receiptId is null ? null : "event");

    internal static string ReleaseId(string baseHash, CombatReleaseBase basis) => "rel." + CampaignOpeningPreambleCodec.HashWithDomain(
        "sandtable.combat.reserve-release.v1", Encode(w =>
        {
            w.WriteStartObject(); w.WriteString("baseHash", baseHash); w.WriteString("cycleId", CampaignCombatReserveCompletionCodec.CycleId(basis.Cycle));
            w.WriteString("positionId", basis.PositionId); w.WriteEndObject();
        }, null))[7..];

    private static byte[] Encode(Action<Utf8JsonWriter> write, string? shape)
    {
        using var stream = new MemoryStream(); using (var writer = new Utf8JsonWriter(stream)) write(writer);
        var bytes = stream.ToArray(); if (shape is not null) ValidateSyntax(bytes, shape); return bytes;
    }
    private static void Command(Utf8JsonWriter w, CombatReleaseCommand c)
    {
        ArgumentNullException.ThrowIfNull(c);
        w.WriteStartObject(); w.WriteNumber("contractVersion", c.ContractVersion); w.WriteString("kind", c.Kind); w.WriteString("releaseId", c.ReleaseId);
        Number(w, "expectedPriorVersion", c.ExpectedPriorVersion); w.WriteString("decisionId", c.DecisionId);
        w.WritePropertyName("unit"); if (c.Unit is null) w.WriteNullValue(); else Unit(w, c.Unit); w.WriteString("choice", c.Choice); w.WriteEndObject();
    }
    private static void Input(Utf8JsonWriter w, CombatReleaseInput i)
    {
        ArgumentNullException.ThrowIfNull(i);
        w.WriteStartObject(); w.WritePropertyName("command"); Command(w, i.Command); w.WriteString("actor", Actor(i.Actor));
        Number(w, "admittedAt", i.AdmittedAt); w.WriteBoolean("clockAvailable", i.ClockAvailable); w.WriteEndObject();
    }
    private static void Timing(Utf8JsonWriter w, CombatStepsTiming? t)
    {
        if (t is null) { w.WriteNullValue(); return; }
        w.WriteStartObject(); w.WriteNumber("contractVersion", t.ContractVersion); w.WriteString("configHash", t.ConfigHash); w.WriteString("kind", t.Kind);
        w.WriteNumber("decisionBudgetMilliseconds", t.DecisionBudgetMilliseconds); w.WriteNumber("openedAtUnixMilliseconds", t.OpenedAtUnixMilliseconds);
        w.WriteNumber("deadlineUnixMilliseconds", t.DeadlineUnixMilliseconds); w.WriteNumber("highWaterUnixMilliseconds", t.HighWaterUnixMilliseconds); w.WriteEndObject();
    }
    private static void ExceptionValue(Utf8JsonWriter w, CombatReleaseMovementException? ex)
    {
        if (ex is null) { w.WriteNullValue(); return; }
        w.WriteStartObject(); w.WritePropertyName("scope"); Scope(w, ex.Scope); w.WriteNumber("ordinal", ex.Ordinal);
        w.WriteString("status", ex.Status); w.WriteString("completionReceiptId", ex.CompletionReceiptId); w.WriteEndObject();
    }
    private static void Member(Utf8JsonWriter w, CombatReleaseMember m)
    {
        w.WriteStartObject(); w.WritePropertyName("unit"); Unit(w, m.Unit); w.WriteString("status", Status(m.Status)); w.WriteNumber("baseCpa", m.BaseCpa);
        w.WriteStartObject("spentCp"); w.WriteNumber("numerator", m.SpentCp.Numerator); w.WriteNumber("denominator", m.SpentCp.Denominator); w.WriteEndObject();
        w.WritePropertyName("history"); History(w, m.History); w.WriteEndObject();
    }
    private static void Units(Utf8JsonWriter w, string name, IReadOnlyList<CampaignCombatUnitKey> units)
    { w.WriteStartArray(name); foreach (var unit in units) Unit(w, unit); w.WriteEndArray(); }
    private static void Effect(Utf8JsonWriter w, CombatReleaseEffect e)
    {
        w.WriteStartObject(); w.WriteString("kind", e.Kind);
        switch (e)
        {
            case CombatReleaseEffect.Open o:
                w.WriteString("decisionId", o.DecisionId); w.WritePropertyName("timing"); Timing(w, o.Timing);
                w.WriteBoolean("openingClockFailure", o.OpeningClockFailure); Units(w, "pending", o.Pending); break;
            case CombatReleaseEffect.UnitDisposition d:
                w.WritePropertyName("unit"); Unit(w, d.Unit); w.WriteString("choice", d.Choice); w.WriteString("beforeStatus", Status(d.BeforeStatus));
                w.WriteString("afterStatus", Status(d.AfterStatus)); w.WriteString("reason", d.Reason); w.WriteNumber("cpaBasis", d.CpaBasis);
                Number(w, "voluntaryCeiling", d.VoluntaryCeiling); w.WritePropertyName("nextMovement"); ExceptionValue(w, d.NextMovement);
                w.WritePropertyName("timing"); Timing(w, d.Timing); w.WriteBoolean("fallbackLocked", d.FallbackLocked); break;
            case CombatReleaseEffect.Complete c:
                w.WriteString("reason", c.Reason); Units(w, "retainedUnits", c.RetainedUnits); w.WriteStartArray("dispositionReceipts");
                foreach (var receipt in c.DispositionReceipts) w.WriteStringValue(receipt); w.WriteEndArray();
                w.WritePropertyName("timing"); Timing(w, c.Timing); w.WriteBoolean("fallbackLocked", c.FallbackLocked); break;
            default: throw new JsonException("Invalid Release effect.");
        }
        w.WriteEndObject();
    }
    private static string Status(CampaignElementReserveStatus s) => s switch
    { CampaignElementReserveStatus.None => "none", CampaignElementReserveStatus.ReserveI => "I", CampaignElementReserveStatus.ReserveII => "II", _ => throw new JsonException("Invalid Release status.") };
    private static string Actor(CampaignOpeningPreambleActor actor) => actor switch
    { CampaignOpeningPreambleActor.Axis => "axis", CampaignOpeningPreambleActor.Commonwealth => "commonwealth", CampaignOpeningPreambleActor.System => "system", _ => throw new JsonException("Invalid Release actor.") };

    // Raw shape validation runs before replay touches independently retained context. It is not authority validation.
    internal static void ValidateSyntax(ReadOnlySpan<byte> bytes, string shape)
    {
        Require(bytes.Length is > 0 and <= 1_048_576);
        foreach (var item in bytes) Require(item is > 32 and < 127 && item != 92);
        using var document = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 32 });
        Shape(document.RootElement, shape);
    }
    private static void Shape(JsonElement value, string shape)
    {
        var schema = shape switch
        {
            "command" => "contractVersion:int kind:id releaseId:id expectedPriorVersion:long? decisionId:id? unit:unit? choice:id?",
            "input" => "command:command actor:actor admittedAt:utc? clockAvailable:bool",
            "timing" => "contractVersion:int configHash:hash kind:id decisionBudgetMilliseconds:int openedAtUnixMilliseconds:utc deadlineUnixMilliseconds:utc highWaterUnixMilliseconds:utc",
            "event" => "contractVersion:int eventType:id author:actor campaignId:id rulesetHash:rawHash configurationHash:hash cycleId:hash positionId:id baseHash:hash releaseId:id priorVersion:long stateVersion:long priorPrefix:hash input:input effect:effect receiptId:id",
            "state" => "contractVersion:int baseHash:hash releaseId:id stateVersion:long prefix:hash status:id openingReceiptId:id? decisionId:id? timing:timing? openingClockFailure:bool acceptedHighWater:utc? fallbackLocked:bool fallbackReason:id? members:member[] pending:unit[] dispositions:disposition[] completionReceiptId:id? retainedWorldHash:hash randomState:random attackHistory:attack[] receipts:receipt[]",
            "member" => "unit:unit status:id baseCpa:int spentCp:cp history:history",
            "disposition" => "receiptId:id unit:unit choice:id beforeStatus:id afterStatus:id",
            "receipt" => "commandHash:hash eventHash:hash receiptId:id actor:actor stateVersion:long",
            "random" => "contractVersion:int algorithmId:id seed:ulong nextByteCursor:ulong",
            "attack" => "commitmentId:id cycleId:hash segmentId:id attacker:unit defender:unit targetLocationId:id gameTurn:int operationStage:int",
            "exception" => "scope:scope ordinal:int status:id completionReceiptId:id?",
            "openEffect" => "kind:id decisionId:id? timing:timing? openingClockFailure:bool pending:unit[]",
            "dispositionEffect" => "kind:id unit:unit choice:id beforeStatus:id afterStatus:id reason:id cpaBasis:int voluntaryCeiling:int? nextMovement:exception? timing:timing? fallbackLocked:bool",
            "completeEffect" => "kind:id reason:id retainedUnits:unit[] dispositionReceipts:id[] timing:timing? fallbackLocked:bool",
            _ => null
        };
        if (schema is not null)
        {
            var fields = schema.Split(' ').Select(f => f.Split(':')).ToArray();
            var values = Fields(value, fields.Select(f => f[0]).ToArray());
            for (var i = 0; i < fields.Length; i++) Shape(values[i], fields[i][1]);
            if (shape is "state" or "openEffect")
            {
                if (shape == "state") { Require(value.GetProperty("members").GetArrayLength() <= 32); Require(value.GetProperty("receipts").GetArrayLength() <= 34); Require(value.GetProperty("dispositions").GetArrayLength() <= 32); }
                Require(value.GetProperty("pending").GetArrayLength() <= 32);
            }
            return;
        }
        if (shape.EndsWith('?')) { if (value.ValueKind != JsonValueKind.Null) Shape(value, shape[..^1]); return; }
        if (shape.EndsWith("[]", StringComparison.Ordinal))
        { Require(value.ValueKind == JsonValueKind.Array && value.GetArrayLength() <= 512); foreach (var item in value.EnumerateArray()) Shape(item, shape[..^2]); return; }
        switch (shape)
        {
            case "int": Int(value); break;
            case "long": Long(value); break;
            case "ulong": Ulong(value); break;
            case "utc": Long(value); Require(value.GetInt64() is >= 0 and <= CampaignCombatSelectionSteps.UtcMaximum); break;
            case "id": Id(value); break;
            case "hash": Hash(value); break;
            case "rawHash": Hash(value, false); break;
            case "bool": Require(value.ValueKind is JsonValueKind.True or JsonValueKind.False); break;
            case "actor": Require(value.ValueKind == JsonValueKind.String && value.GetString() is "system" or "axis" or "commonwealth"); break;
            case "unit": UnitShape(value); break;
            case "scope": ScopeShape(value); break;
            case "history": HistoryShape(value); break;
            case "cp":
                var cp = Fields(value, "numerator", "denominator"); Long(cp[0]); Int(cp[1]);
                Require(cp[0].GetInt64() >= 0 && cp[1].GetInt32() > 0 && BigInteger.GreatestCommonDivisor(cp[0].GetInt64(), cp[1].GetInt32()) == 1); break;
            case "effect":
                Require(value.ValueKind == JsonValueKind.Object && value.TryGetProperty("kind", out _));
                Id(value.GetProperty("kind"));
                Shape(value, value.GetProperty("kind").GetString() switch
                { "release-opened" => "openEffect", "unit-disposition" => "dispositionEffect", "release-completed" => "completeEffect", _ => throw new JsonException("Unknown Release effect.") }); break;
            default: throw new JsonException("Unknown Release shape.");
        }
    }

}
