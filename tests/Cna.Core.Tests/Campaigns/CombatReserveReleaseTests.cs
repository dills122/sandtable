using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatReserveReleaseTests
{
    [Fact]
    public void FirstReserveOpensAndReleasesWithoutRenewingBudget()
    {
        using var fixture = Load("Campaigns", "combat-reserve-release-v1.json");
        var recipe = fixture.RootElement.GetProperty("cases")[0];
        var (basis, request) = Build(recipe);
        var state = CampaignCombatReserveRelease.Replay(basis, request, [], []);
        var open = new CombatReleaseInput(new(1, "open", state.ReleaseId, state.StateVersion), CampaignOpeningPreambleActor.System, 2000);
        var opened = CampaignCombatReserveRelease.Apply(basis, request, [], [], open);
        Assert.Equal("open", opened.State.Status);
        var choose = new CombatReleaseInput(new(1, "choose", state.ReleaseId, opened.State.StateVersion, opened.State.DecisionId, basis.Members[0].Unit, "release-I"), CampaignOpeningPreambleActor.Axis, 2100);
        var released = CampaignCombatReserveRelease.Apply(basis, request, [open], [opened.EventBytes!], choose);
        Assert.Equal(CampaignElementReserveStatus.None, released.State.Members[0].Status);
        Assert.Equal(opened.State.Timing!.DeadlineUnixMilliseconds, released.State.Timing!.DeadlineUnixMilliseconds);
        Assert.Equal(released.ReceiptId, released.State.Members[0].History.ReleaseReceiptId);
        Assert.Equal("pending", released.State.Members[0].History.NextMovement!.Status);
        Assert.Equal(basis.Members[0].SpentCp, released.State.Members[0].SpentCp);
    }


    [Fact]
    public void FortyFourTracesMatchEveryFrozenEventStateAndTerminalLiteral()
    {
        using var fixture = Load("Campaigns", "combat-reserve-release-v1.json");
        var cases = fixture.RootElement.GetProperty("cases").EnumerateArray().ToArray();
        var traces = 0; var eventCount = 0; var stateCount = 0; var literals = 0; var excluded = 0;
        foreach (var golden in fixture.RootElement.GetProperty("goldens").EnumerateArray())
        {
            var recipe = cases.Single(c => Text(c, "name") == Text(golden, "case"));
            if (recipe.TryGetProperty("settledCase", out _)) { excluded++; continue; }
            var side = Text(golden, "side"); var (basis, request) = Build(recipe, side, Text(golden, "slot"));
            var trace = new Trace(basis, request); CheckFrame(0);
            var opened = (basis.AcceptedHighWater ?? 1000) + 1000;
            var available = !recipe.TryGetProperty("openingClock", out var clock) || clock.GetBoolean();
            trace.Send("open", now: available ? opened : null, available: available); CheckFrame(1);
            var actionIndex = 0;
            foreach (var actionValue in recipe.GetProperty("actions").EnumerateArray())
            {
                var action = actionValue.GetString()!; var now = opened + 100 * ++actionIndex;
                if (action is "expire" or "unavailable") trace.Send(action, now: action == "expire" ? trace.State.Timing?.DeadlineUnixMilliseconds ?? now : now);
                else if (action == "regressed-choice") trace.Send("choose", trace.State.Members.Single(m => m.Unit == trace.State.Pending[0]).Status == CampaignElementReserveStatus.ReserveI ? "release-I" : "release-II", opened);
                else if (action == "complete-release") trace.Send(action, now: now);
                else trace.Send("choose", action, now);
                CheckFrame(trace.Events.Count);
            }
            while (trace.State.Status != "completed")
            {
                trace.Send(trace.State.FallbackLocked || trace.State.OpeningClockFailure ? "fallback-step" : "complete");
                CheckFrame(trace.Events.Count);
            }
            Assert.Equal(golden.GetProperty("eventHashes").GetArrayLength(), trace.Events.Count);
            for (var i = 0; i < trace.Events.Count; i++) Assert.Equal(golden.GetProperty("eventHashes")[i].GetString(), Hash(trace.Events[i]));
            if (golden.TryGetProperty("terminalEvent", out var terminal)) { Assert.Equal(terminal.GetString(), Encoding.UTF8.GetString(trace.Events[^1])); literals++; }
            Assert.Equal(recipe.GetProperty("expectedStatuses").EnumerateArray().Select(x => x.GetString()), trace.State.Members.Select(m => m.Status switch { CampaignElementReserveStatus.None => "none", CampaignElementReserveStatus.ReserveI => "I", _ => "II" }));
            Assert.Equal(recipe.GetProperty("expectedReleased").EnumerateArray().Select(x => x.GetString()), trace.State.Members.Select(m => m.History.ReleasedType));
            Assert.Equal(recipe.GetProperty("expectedFallback").GetBoolean(), trace.State.FallbackLocked);
            Assert.Equal(basis.RetainedWorldHash, trace.State.RetainedWorldHash); Assert.Equal(basis.RandomState, trace.State.RandomState);
            Assert.Equal(basis.AttackHistory, trace.State.AttackHistory);
            foreach (var input in trace.Inputs)
            {
                var retry = trace.Apply(input with { AdmittedAt = 999999 });
                Assert.Equal(CombatStepsDisposition.Duplicate, retry.Disposition); Assert.Null(retry.EventBytes);
                Assert.Equal(trace.Bytes, CampaignCombatReserveReleaseCodec.SerializeState(retry.State));
            }
            eventCount += trace.Events.Count; traces++;
            void CheckFrame(int cut)
            {
                var frame = golden.GetProperty("frames")[cut]; var bytes = trace.Bytes;
                Assert.Equal(frame.GetProperty("bytes").GetInt32(), bytes.Length); Assert.Equal(Text(frame, "hash"), Hash(bytes));
                Assert.Equal(bytes, CampaignCombatReserveReleaseCodec.SerializeState(CampaignCombatReserveReleaseCodec.ReadState(bytes, basis, request, trace.Inputs, trace.Events)));
                stateCount++;
            }
        }
        Assert.Equal(44, traces); Assert.Equal(4, excluded); Assert.Equal(132, eventCount); Assert.Equal(176, stateCount); Assert.Equal(2, literals);
    }


    [Fact]
    public void OneDeadlineRejectsLateOwnerAndFallbackPreservesAcceptedChoice()
    {
        var t = Example("first-timeout-midway"); t.Send("open", now: 2000); var deadline = t.State.Timing!.DeadlineUnixMilliseconds;
        t.Send("choose", "release-I", 2100); var accepted = t.State.Members[0]; var before = t.Bytes;
        Assert.Equal(CombatStepsDisposition.NoOp, t.Apply(t.Input("expire", now: deadline - 1)).Disposition);
        Reject(t, t.Input("choose", "release-I", deadline));
        var stale = t.Input("expire", now: deadline); stale = stale with { Command = stale.Command with { DecisionId = "other.decision" } };
        Assert.Equal(CombatStepsDisposition.NoOp, t.Apply(stale).Disposition);
        Assert.Equal(before, t.Bytes);
        var fallback = t.Send("expire", now: deadline);
        Assert.True(t.State.FallbackLocked); Assert.Equal(accepted, t.State.Members[0]); Assert.Equal(CampaignElementReserveStatus.ReserveII, t.State.Members[1].Status);
        Assert.Equal(deadline, t.State.Timing!.DeadlineUnixMilliseconds);
        Reject(t, t.Input("choose", "release-II", deadline + 1) with { Command = t.Input("choose", "release-II").Command with { Unit = t.Basis.Members[1].Unit } });
        t.Send("fallback-step"); Assert.Equal("completed", t.State.Status);
        Assert.Equal(fallback.ReceiptId, t.Apply(t.Inputs[2]).ReceiptId);
        Assert.Equal(CombatStepsDisposition.Duplicate, t.Apply(t.Inputs[2]).Disposition);
        var newTimer = t.Input("expire", now: deadline + 1); newTimer = newTimer with { Command = newTimer.Command with { DecisionId = "old.decision" } };
        Assert.Equal(CombatStepsDisposition.NoOp, t.Apply(newTimer).Disposition);
    }

    [Fact]
    public void ClockLossKeepsOriginalActorAndLocksOrderedConversion()
    {
        foreach (var (now, available) in new (long?, bool)[] { (2099, true), (null, false), (2200, false) })
        {
            var t = Example("first-timeout-midway"); t.Send("open", now: 2000); t.Send("choose", "release-I", 2100);
            var first = t.State.Members[0]; var output = t.Send("choose", "release-I", now, available);
            using var e = JsonDocument.Parse(output.EventBytes!);
            Assert.Equal("system", Text(e.RootElement, "author")); Assert.Equal("axis", Text(e.RootElement.GetProperty("input"), "actor"));
            Assert.Equal(CampaignOpeningPreambleActor.Axis, t.State.Receipts[^1].Actor); Assert.Equal(first, t.State.Members[0]);
            Assert.Equal(2100, t.State.AcceptedHighWater); Assert.True(t.State.FallbackLocked);
        }
        var many = Many(3); many.Send("open", now: 2000); many.Send("unavailable", now: 2100);
        var duplicate = many.Apply(many.Inputs[^1]); Assert.Equal(CombatStepsDisposition.Duplicate, duplicate.Disposition); Assert.Equal(2, duplicate.State.Pending.Count);
        Reject(many, many.Input("choose", "release-I", 2200));
        while (many.State.Status != "completed") many.Send("fallback-step");
        Assert.All(many.State.Members, m => Assert.Equal(CampaignElementReserveStatus.ReserveII, m.Status));
        Assert.Equal(many.Basis.Members.Select(m => m.Unit), many.State.Dispositions.Select(d => d.Unit));
    }

    [Fact]
    public void OpeningClockFailureHasNoFabricatedTimingAndRecoveryDoesNotRenewBudget()
    {
        var example = Example("first-release"); var budget = example.Request.Context.Configuration.Windows.Single(w => w.Kind == "reserve-release").DecisionBudgetMilliseconds;
        foreach (var (now, available) in new (long?, bool)[] { (null, false), (999, true), (CampaignCombatSelectionSteps.UtcMaximum - budget + 1, true) })
        {
            var t = Example("first-release"); t.Send("open", now: now, available: available);
            Assert.Null(t.State.Timing); Assert.True(t.State.OpeningClockFailure); Assert.False(t.State.FallbackLocked);
            Assert.Equal(t.Basis.AcceptedHighWater, t.State.AcceptedHighWater);
            t.Send("choose", "release-I", 2200); Assert.True(t.State.FallbackLocked); Assert.Null(t.State.Timing);
            Assert.Equal(CampaignElementReserveStatus.ReserveII, t.State.Members[0].Status); t.Send("fallback-step");
        }
        foreach (var kind in new[] { "expire", "unavailable" })
        {
            var t = Example("first-release"); t.Send("open", now: 2000); t.Send("choose", "release-I", 2100);
            var members = t.State.Members; var dispositions = t.State.Dispositions; var deadline = t.State.Timing!.DeadlineUnixMilliseconds;
            var recovered = CampaignCombatReserveReleaseCodec.ReadState(t.Bytes, t.Basis, t.Request, t.Inputs, t.Events);
            Assert.Equal(deadline, recovered.Timing!.DeadlineUnixMilliseconds);
            Assert.Equal(CombatStepsDisposition.NoOp, t.Apply(t.Input("expire", now: deadline - 1)).Disposition);
            t.Send(kind, now: deadline); Assert.Equal("completed", t.State.Status); Assert.Equal(members, t.State.Members); Assert.Equal(dispositions, t.State.Dispositions);
            Assert.Single(t.State.Dispositions); Assert.True(t.State.FallbackLocked);
        }
    }

    [Fact]
    public void ActorShapeIdentityAndQueueChecksPrecedeRetryAndRejectInvalidChoices()
    {
        var t = Example("first-timeout-midway"); t.Send("open", now: 2000);
        Reject(t, t.Input("complete")); Reject(t, t.Input("complete-release", now: 2100)); Reject(t, t.Input("choose", "retain-II", 2100));
        var choose = t.Input("choose", "release-I", 2100);
        Reject(t, choose with { Command = choose.Command with { Unit = t.Basis.Members[1].Unit } });
        Reject(t, choose with { Actor = CampaignOpeningPreambleActor.Commonwealth });
        Reject(t, choose with { Command = choose.Command with { ReleaseId = "other.release" } });
        Reject(t, choose with { Command = choose.Command with { ExpectedPriorVersion = null } });
        Reject(t, choose with { Command = choose.Command with { Unit = null } });
        Reject(t, choose with { Command = choose.Command with { Choice = null } });
        Reject(t, choose with { Command = choose.Command with { ContractVersion = 2 } });
        Reject(t, choose with { Command = choose.Command with { Kind = "future" } });
        Reject(t, choose with { Actor = (CampaignOpeningPreambleActor)99 });
        t.Send("choose", "convert-to-II", 2100);
        Reject(t, choose with { Command = choose.Command with { Choice = "release-I" } }); // Changed stale command, not accepted conversion.
        var accepted = t.Inputs[^1];
        Reject(t, accepted with { Actor = CampaignOpeningPreambleActor.System });
        Reject(t, accepted with { Command = accepted.Command with { DecisionId = "foreign.decision" } });
        Reject(t, t.Input("choose", "release-II", 2200) with { Command = t.Input("choose", "release-II", 2200).Command with { Unit = t.Basis.Members[0].Unit } });
        Assert.Equal(CombatStepsDisposition.Duplicate, t.Apply(accepted with { AdmittedAt = 999999, ClockAvailable = false }).Disposition);
        t.Send("choose", "convert-to-II", 2200); t.Send("complete");
        Assert.Equal(CombatStepsDisposition.Duplicate, t.Apply(accepted).Disposition);
        Reject(t, accepted with { Command = accepted.Command with { Choice = "release-I" } });
    }

    [Fact]
    public void RecoveryRejectsForgedStateEventsAndTrustedInputSubstitution()
    {
        var t = Example("first-timeout-midway"); t.Send("open", now: 2000); t.Send("choose", "release-I", 2100); t.Send("expire", now: t.State.Timing!.DeadlineUnixMilliseconds); t.Send("fallback-step");
        foreach (var path in Leaves(JsonNode.Parse(t.Bytes)))
        {
            var node = JsonNode.Parse(t.Bytes)!; var parent = node;
            foreach (var key in path[..^1]) parent = parent is JsonArray a ? a[int.Parse(key, System.Globalization.CultureInfo.InvariantCulture)]! : parent[key]!;
            var last = path[^1]; var value = parent is JsonArray values ? values[int.Parse(last, System.Globalization.CultureInfo.InvariantCulture)] : parent[last];
            JsonNode? replacement = value?.GetValueKind() switch
            {
                JsonValueKind.Number => JsonValue.Create(value.GetValue<long>() + 1),
                JsonValueKind.True or JsonValueKind.False => JsonValue.Create(!value.GetValue<bool>()),
                JsonValueKind.String => JsonValue.Create(value.GetValue<string>() + "x"),
                _ => JsonValue.Create("forged")
            };
            if (parent is JsonArray items) items[int.Parse(last, System.Globalization.CultureInfo.InvariantCulture)] = replacement; else parent[last] = replacement;
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveReleaseCodec.ReadState(JsonSerializer.SerializeToUtf8Bytes(node), t.Basis, t.Request, t.Inputs, t.Events));
        }
        for (var i = 0; i < t.Events.Count; i++)
        {
            foreach (var field in new[] { "priorVersion", "stateVersion", "priorPrefix", "configurationHash", "baseHash", "cycleId", "receiptId", "author" })
            {
                var changed = JsonNode.Parse(t.Events[i])!;
                changed[field] = changed[field]!.GetValueKind() == JsonValueKind.Number ? JsonValue.Create(changed[field]!.GetValue<long>() + 1) : JsonValue.Create(field == "author" ? "commonwealth" : "forged");
                var events = t.Events.Select(e => e.ToArray()).ToArray(); events[i] = JsonSerializer.SerializeToUtf8Bytes(changed);
                Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveRelease.Replay(t.Basis, t.Request, t.Inputs, events));
            }
        }
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveReleaseCodec.ReadState(t.Bytes, t.Basis, t.Request, t.Inputs[..^1], t.Events[..^1]));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveRelease.Replay(t.Basis, t.Request, [.. t.Inputs, t.Inputs[^1]], [.. t.Events, t.Events[^1]]));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveRelease.Replay(t.Basis, t.Request, t.Inputs, t.Events.AsEnumerable().Reverse().ToArray()));
        var substituted = t.Inputs.ToArray(); substituted[1] = substituted[1] with { Actor = CampaignOpeningPreambleActor.Commonwealth };
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveRelease.Replay(t.Basis, t.Request, substituted, t.Events));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveRelease.Replay(t.Basis with { CombatCompletionReceiptId = "forged" }, t.Request, t.Inputs, t.Events));
    }

    [Fact]
    public void MalformedRawStateAndEventRejectBeforeTrustedContext()
    {
        var t = Example("first-release"); t.Send("open", now: 2000);
        foreach (var bytes in new[] { t.Bytes, t.Events[0] })
        {
            var text = Encoding.UTF8.GetString(bytes);
            var malformed = new List<byte[]>
            {
                Array.Empty<byte>(), new byte[1_048_577], bytes[..^1], new byte[] { 0xef, 0xbb, 0xbf }.Concat(bytes).ToArray(), Encoding.UTF8.GetBytes(text + "\n"),
                Encoding.UTF8.GetBytes(text.Replace("\"contractVersion\":1", "\"contractVersion\":1.0", StringComparison.Ordinal)),
                Encoding.UTF8.GetBytes(text.Replace("\"contractVersion\":1", "\"contractVersion\":true", StringComparison.Ordinal)),
                Encoding.UTF8.GetBytes(text.Replace("\"contractVersion\":1", "\"contractVersion\":1,\"contractVersion\":1", StringComparison.Ordinal)),
                Encoding.UTF8.GetBytes(text.Replace("\"contractVersion\":1,", "", StringComparison.Ordinal)),
                Encoding.UTF8.GetBytes(text.Replace("\"contractVersion\":1", "\"extra\":0,\"contractVersion\":1", StringComparison.Ordinal)),
                Encoding.UTF8.GetBytes(text.Replace("\"stateVersion\":101", "\"stateVersion\":9223372036854775808", StringComparison.Ordinal)),
                Encoding.UTF8.GetBytes(text.Replace("\"highWaterUnixMilliseconds\":2000", "\"highWaterUnixMilliseconds\":-1", StringComparison.Ordinal)),
                Encoding.UTF8.GetBytes(text.Replace("\"openingClockFailure\":false", "\"openingClockFailure\":0", StringComparison.Ordinal)),
                Encoding.UTF8.GetBytes(text.Replace("reserve-release", "reserve\\u002drelease", StringComparison.Ordinal)),
                Encoding.UTF8.GetBytes(new string('[', 33) + "0" + new string(']', 33))
            };
            foreach (var bad in malformed)
            {
                if (ReferenceEquals(bytes, t.Events[0])) Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveRelease.Replay(null!, null!, [t.Inputs[0]], [bad]));
                else Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveReleaseCodec.ReadState(bad, null!, null!, [], []));
            }
        }
        var badEffect = JsonNode.Parse(t.Events[0])!; badEffect["effect"]!["kind"] = 12;
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveRelease.Replay(null!, null!, [t.Inputs[0]], [JsonSerializer.SerializeToUtf8Bytes(badEffect)]));
    }

    [Fact]
    public void CapacityOverflowAndOwnedCollectionsHaveAtomicBoundaries()
    {
        var t = Many(32); t.Send("open", now: 2000); var deadline = t.State.Timing!.DeadlineUnixMilliseconds;
        t.Send("unavailable", now: 2100);
        while (t.State.Status != "completed") t.Send("fallback-step");
        Assert.Equal(34, t.Events.Count); Assert.Equal(32, t.State.Dispositions.Count); Assert.Equal(deadline, t.State.Timing!.DeadlineUnixMilliseconds);
        Assert.True(t.Bytes.Length < 1_048_576);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveRelease.Replay(t.Basis, t.Request, [.. t.Inputs, t.Inputs[^1]], [.. t.Events, t.Events[^1]]));
        var later = Example("later-release-retain"); var maxOrdinal = new Trace(later.Basis with { Cycle = later.Basis.Cycle with { Ordinal = int.MaxValue } }, later.Request);
        Reject(maxOrdinal, maxOrdinal.Input("open", now: 2000));
        var first = Example("first-release"); var maxVersion = new Trace(first.Basis with { PriorVersion = long.MaxValue }, first.Request);
        Reject(maxVersion, maxVersion.Input("open", now: 2000));
        var nearMax = new Trace(first.Basis with { PriorVersion = long.MaxValue - 1 }, first.Request); nearMax.Send("open", now: 2000);
        Reject(nearMax, nearMax.Input("choose", "release-I", 2100)); Assert.Equal(CombatStepsDisposition.Duplicate, nearMax.Apply(nearMax.Inputs[0]).Disposition);
        var members = t.State.Members.ToArray(); var clone = t.State with { Members = members }; var expected = CampaignCombatReserveReleaseCodec.SerializeState(clone);
        members[0] = members[0] with { BaseCpa = 999 }; Assert.Equal(expected, CampaignCombatReserveReleaseCodec.SerializeState(clone));
        Assert.Throws<NotSupportedException>(() => ((IList<CombatReleaseMember>)clone.Members)[0] = members[0]);
        Assert.Throws<NotSupportedException>(() => ((IList<CampaignOpeningPreambleReceipt>)clone.Receipts).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<CombatReleaseDisposition>)clone.Dispositions).Clear());
        var solo = Example("first-release"); var result = solo.Send("open", now: 2000); var eventBytes = result.EventBytes!; var pinned = result.EventBytes!;
        eventBytes[0] = 0; Assert.Equal(pinned, result.EventBytes);
    }

    [Fact]
    public void ReleasePreservesCumulativeExpenditureAndExistingOffensiveHistory()
    {
        var later = Example("later-release-retain"); later.Send("open", now: 2000); later.Send("choose", "release-II", 2100); later.Send("complete-release", now: 2200);
        Assert.Equal(9, later.State.Members[0].History.CpaBasis); Assert.Equal(4, later.State.Members[0].History.VoluntaryCeiling);
        Assert.Equal(later.Basis.Members[0].SpentCp, later.State.Members[0].SpentCp); Assert.Equal(later.Basis.Members[1], later.State.Members[1]);
        var old = Example("prior-release-retained"); var member = old.Basis.Members[0];
        var attack = new CombatRoundAttackHistory("probe.attack", CampaignCombatReserveCompletionCodec.CycleId(old.Basis.Cycle), "probe.segment", member.Unit,
            new(member.Unit.CreationBinding, "commonwealth", "probe.enemy"), "probe.target", 1, 1);
        var used = member with { History = member.History with { OffensiveCommitmentId = attack.CommitmentId }, SpentCp = new(99, 1) };
        var b = Copy(old.Basis, [used], [attack]); var retained = new Trace(b, old.Request);
        retained.Send("open", now: 2000); Assert.Null(retained.State.DecisionId); retained.Send("complete");
        Assert.Equal(used, retained.State.Members[0]); Assert.Equal(new[] { attack }, retained.State.AttackHistory);
    }

    private static void Reject(Trace t, CombatReleaseInput input)
    { var before = t.Bytes; Assert.ThrowsAny<JsonException>(() => t.Apply(input)); Assert.Equal(before, t.Bytes); }
    private static Trace Example(string name)
    {
        using var fixture = Load("Campaigns", "combat-reserve-release-v1.json");
        var (basis, request) = Build(fixture.RootElement.GetProperty("cases").EnumerateArray().Single(c => Text(c, "name") == name));
        return new(basis, request);
    }
    private static Trace Many(int count)
    {
        var t = Example("first-release"); var members = Enumerable.Range(0, count).Select(i => t.Basis.Members[0] with
        { Unit = new(t.Request.CreationBinding, "axis", $"probe.{i:D2}") }).ToArray();
        return new(Copy(t.Basis, members), t.Request);
    }
    private static CombatReleaseBase Copy(CombatReleaseBase b, IEnumerable<CombatReleaseMember> members, IEnumerable<CombatRoundAttackHistory>? attacks = null) =>
        new(b.ContractVersion, b.Profile, b.Cycle, b.FirstActingSide, b.PositionId, b.PriorVersion, b.PriorPrefix, b.CombatCompletionReceiptId,
            b.RetainedWorldHash, b.RandomState, b.AcceptedHighWater, members, attacks ?? b.AttackHistory);
    private static IEnumerable<string[]> Leaves(JsonNode? node)
    {
        if (node is JsonObject obj) foreach (var pair in obj) foreach (var tail in Leaves(pair.Value)) yield return [pair.Key, .. tail];
        else if (node is JsonArray array) for (var i = 0; i < array.Count; i++) foreach (var tail in Leaves(array[i])) yield return [i.ToString(System.Globalization.CultureInfo.InvariantCulture), .. tail];
        else yield return [];
    }

    private sealed class Trace(CombatReleaseBase basis, CampaignCombatCreationRequest request)
    {
        public CombatReleaseBase Basis { get; } = basis;
        public CampaignCombatCreationRequest Request { get; } = request;
        public List<CombatReleaseInput> Inputs { get; } = [];
        public List<byte[]> Events { get; } = [];
        public CombatReleaseState State { get; private set; } = CampaignCombatReserveRelease.Replay(basis, request, [], []);
        public byte[] Bytes => CampaignCombatReserveReleaseCodec.SerializeState(State);
        public CombatReleaseInput Input(string kind, string? choice = null, long? now = null, bool available = true) =>
            new(new(1, kind, State.ReleaseId, kind is "expire" or "unavailable" ? null : State.StateVersion,
                kind == "open" ? null : State.DecisionId, kind == "choose" && State.Pending.Count > 0 ? State.Pending[0] : null, choice),
                kind is "choose" or "complete-release" ? Basis.Cycle.ActingSide == LandSide.Axis ? CampaignOpeningPreambleActor.Axis : CampaignOpeningPreambleActor.Commonwealth : CampaignOpeningPreambleActor.System, now, available);
        public CombatReleaseResult Apply(CombatReleaseInput input) => CampaignCombatReserveRelease.Apply(Basis, Request, Inputs, Events, input);
        public CombatReleaseResult Send(string kind, string? choice = null, long? now = null, bool available = true)
        {
            var input = Input(kind, choice, now, available); var before = Bytes; var result = Apply(input);
            Assert.Equal(before, Bytes); Assert.Equal(CombatStepsDisposition.Accepted, result.Disposition);
            Inputs.Add(input); Events.Add(result.EventBytes!); State = result.State; return result;
        }
    }

    // Independent frozen base_for recipe: authority Created11 + Content7 + Result2 cycle template.
    // No expected base hash, event, or state is used to construct these isolated probe values.
    private static (CombatReleaseBase Base, CampaignCombatCreationRequest Request) Build(JsonElement recipe, string side = "axis", string slot = "first-acting-side")
    {
        using var authority = Load("Rules", "combat-authority-envelope-v1.json");
        using var created = JsonDocument.Parse(authority.RootElement.GetProperty("goldens").GetProperty("created").GetProperty("canonicalUtf8").GetString()!);
        var artifact = Cna1979CombatContentCatalog.Artifact; var scenario = Assert.Single(artifact.Definition.Scenarios);
        var setup = CampaignSetupV7Codec.Deserialize(Encoding.UTF8.GetBytes(created.RootElement.GetProperty("setup").GetRawText()), artifact, scenario);
        var config = CombatDecisionConfigurationCodec.Deserialize(Encoding.UTF8.GetBytes(created.RootElement.GetProperty("configuration").GetRawText()), Cna1979CombatRuleset.Manifest);
        var request = CampaignCombatCreationRequest.Create("rules-lab.combat-creation.1", 0, new(Cna1979CombatRuleset.Manifest, setup, artifact, scenario, config));
        using var result = Load("Campaigns", "combat-result-settlement-v2.json");
        using var resultBase = JsonDocument.Parse(result.RootElement.GetProperty("traces")[0].GetProperty("baseCanonicalUtf8").GetString()!);
        var c = resultBase.RootElement.GetProperty("boundary").GetProperty("cycle");
        var cycle = new CampaignCombatCycleAuthority(1, Text(c, "campaignId"), Text(c, "rulesetHash"), Text(c, "setupId"), Text(c, "setupHash"), Text(c, "contentPackId"), Text(c, "contentHash"), Text(c, "scenarioId"),
            c.GetProperty("gameTurn").GetInt32(), c.GetProperty("operationStage").GetInt32(), slot, Side(side), recipe.GetProperty("ordinal").GetInt32(), 20, Text(c, "openingPrefix"), Text(c, "admittedPolicyBundleDigest"));
        using var content = Load("Content", "combat-content-v7.canonical.json");
        var elementId = Text(content.RootElement.GetProperty("elements").EnumerateArray().First(e => Text(e, "sideId") == side), "elementId");
        var scope = new CombatReleaseScope(cycle.GameTurn, cycle.OperationStage, slot, cycle.ActingSide);
        var members = recipe.GetProperty("statuses").EnumerateArray().Select((s, i) =>
        {
            var status = s.GetString();
            var cpa = recipe.TryGetProperty("cpa", out var amounts) ? amounts[i].GetInt32() : 10;
            var spent = recipe.TryGetProperty("spent", out var spending) ? spending[i].GetInt64() : 0;
            var history = new CombatReleaseHistory(scope, status is "I" or "II" ? $"probe.designation.{i}" : null, status == "II" ? $"probe.conversion.{i}" : null);
            if (recipe.TryGetProperty("priorRelease", out _)) history = new(scope, "probe.designation.0", null, "I", "probe.release.0", 1, cpa, cpa, null, new(scope, 2, "expired", "probe.movement-completed"));
            return new CombatReleaseMember(new(request.CreationBinding, side, elementId + (i == 0 ? "" : $".probe-{i + 1}")), status switch { "I" => CampaignElementReserveStatus.ReserveI, "II" => CampaignElementReserveStatus.ReserveII, _ => CampaignElementReserveStatus.None }, cpa, new(spent, 1), history);
        }).ToArray();
        var first = slot == "first-acting-side" ? Side(side) : Side(side == "axis" ? "commonwealth" : "axis");
        var actor = slot == "first-acting-side" ? LandActorRole.FirstActingSide : LandActorRole.SecondActingSide;
        var position = Cna1979LandSequence.CreateTurn(cycle.GameTurn).Single(p => p.OperationStage == cycle.OperationStage && p.SegmentId == LandSegmentIds.ReserveRelease && p.ActorRole == actor);
        var prefix = Hash(JsonSerializer.SerializeToUtf8Bytes(new { probe = Text(recipe, "name"), side, slot }));
        return (new(1, "isolated-ledger", cycle, first, position.PositionId, 100, prefix, "probe.combat-completed", Hash("synthetic retained World for isolated ledger probe"u8.ToArray()), request.RandomState, 1000, members, []), request);
    }
    private static JsonDocument Load(string area, string name) => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, area, "Fixtures", name)));
    private static string Text(JsonElement element, string name) => element.GetProperty(name).GetString()!;
    private static string Hash(byte[] bytes) => CampaignOpeningPreambleCodec.Hash(bytes);
    private static LandSide Side(string side) => side == "axis" ? LandSide.Axis : LandSide.Commonwealth;
}
