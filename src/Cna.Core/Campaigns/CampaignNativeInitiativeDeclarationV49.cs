using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// MTS-TASK-002 slice 1 (2026-11-22, docs/work/plans/2026-11-22-stage-entry-bridge-design-options.md):
// the first native-chain preamble event. Bridges from the prior stage's last
// side-block (CampaignNativePostCombatPassV42's second-acting-side Patrol
// pass) into the new stage's Initiative Declaration, mirroring
// CampaignNativePostCombatPassV42/CampaignNativeSecondReserveV43's own
// predecessor-chain shape. Unlike CampaignV11Preamble.Create's handling of
// the same decision (gated on a literal `prior.StateVersion == 4`, which only
// ever means "the 5th event this campaign accepted" -- not reusable once
// StateVersion is in the thousands), this validates against the real
// Cna1979LandSequenceV4 catalog position, the same way every other native
// event in this chain already does.
internal sealed record CampaignNativeInitiativeDeclarationV49(
    int ContractVersion, string CampaignId, long PriorStateVersion, string PriorEventHash,
    string PositionId, string NextPositionId, string WorldHash, int GameTurn, int OperationStage,
    LandSide DeclaringHolder, LandSide FirstSide, LandSide SecondSide)
{
    internal const int CurrentContractVersion = 49;
    internal long StateVersion => checked(PriorStateVersion + 1);

    // The holder is resolved once per Game Turn, before any Operation Stage. The
    // caller supplies the accepted holder from history (or the frozen initial
    // snapshot for the opening turn). This event only declares acting order.
    internal static CampaignNativeInitiativeDeclarationV49 Create(
        object predecessor, LandSide holder, bool actFirst, int gameTurn, long contactVersion)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!Enum.IsDefined(holder)) throw new ArgumentOutOfRangeException(nameof(holder));
        var (campaignId, version, positionId, worldHash, priorBytes) = predecessor switch
        {
            CampaignNativePostCombatPassV42 phase => (phase.CampaignId, phase.StateVersion,
                phase.NextPositionId, phase.WorldHash, phase.SerializeCanonical()),
            CampaignNativeNoObligationTurnPhaseV57 phase when phase.GameTurn == gameTurn
                && phase.PositionId == "land.position.naval-convoy.tactical-shipping" => (phase.CampaignId, phase.StateVersion,
                phase.NextPositionId, phase.WorldHash, phase.SerializeCanonical()),
            _ => throw new InvalidOperationException("Initiative Declaration requires an accepted predecessor."),
        };
        if (contactVersion != version)
            throw new InvalidOperationException("Initiative Declaration belongs to the current cut.");
        var position = Cna1979LandSequenceV4.CreateTurn(gameTurn).Single(value => value.PositionId == positionId);
        if (position.ActorRole != LandActorRole.InitiativeHolder
            || position.PhaseId != LandPhaseIds.InitiativeDeclaration)
            throw new InvalidOperationException("This phase does not admit an Initiative Declaration.");
        var next = Cna1979LandSequenceV4.GetNext(position);
        if (next.PhaseId != LandPhaseIds.WeatherDetermination
            || next.GameTurn != position.GameTurn || next.OperationStage != position.OperationStage)
            throw new InvalidOperationException("Initiative Declaration must advance to Weather Determination.");
        var firstSide = actFirst ? holder : Opposite(holder);
        var secondSide = Opposite(firstSide);
        // Validates the shape (distinct sides, defined values) the same way
        // CampaignOperationStageOrder's own constructor already guards --
        // reused here, not re-implemented, exactly as OpeningPreambleEvents's
        // InitiativeOrderDeclared does for the Stage 1 original.
        _ = new CampaignOperationStageOrder(CampaignOperationStageOrder.CurrentContractVersion,
            position.GameTurn, position.OperationStage, firstSide, secondSide);
        return new(CurrentContractVersion, campaignId, version,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(priorBytes)),
            position.PositionId, next.PositionId, worldHash, position.GameTurn, position.OperationStage,
            holder, firstSide, secondSide);
    }

    // The resolved order for this stage, read directly off this event's own
    // fields -- every mechanic later in the six-event chain (and, per the
    // owner's ruling, the Reserve Designation step that follows it) carries
    // this value forward by reading it off whichever native predecessor is
    // in hand, the same way every other native-chain value (WorldHash, Side)
    // is read directly off a concrete predecessor type rather than a
    // dedicated ExerciseSession field.
    internal CampaignOperationStageOrder ToOrder() => new(CampaignOperationStageOrder.CurrentContractVersion,
        GameTurn, OperationStage, FirstSide, SecondSide);

    internal byte[] SerializeCanonical()
    {
        if (ContractVersion != CurrentContractVersion || PriorStateVersion < 1 || !IsHash(PriorEventHash)
            || !IsHash(WorldHash) || GameTurn < 1 || OperationStage is < 1 or > 3
            || !Enum.IsDefined(DeclaringHolder) || !Enum.IsDefined(FirstSide)
            || !Enum.IsDefined(SecondSide) || FirstSide == SecondSide)
            throw new JsonException("Invalid Initiative Declaration.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "native-initiative-declared");
            writer.WriteString("campaignId", ContentContractGuards.RequireStableId(CampaignId, nameof(CampaignId)));
            writer.WriteNumber("priorStateVersion", PriorStateVersion);
            writer.WriteString("priorEventHash", PriorEventHash);
            writer.WriteString("positionId", ContentContractGuards.RequireStableId(PositionId, nameof(PositionId)));
            writer.WriteString("nextPositionId", ContentContractGuards.RequireStableId(NextPositionId, nameof(NextPositionId)));
            writer.WriteString("worldHash", WorldHash);
            writer.WriteNumber("gameTurn", GameTurn);
            writer.WriteNumber("operationStage", OperationStage);
            writer.WriteString("declaringHolder", CampaignSnapshotSerializer.FormatSide(DeclaringHolder));
            writer.WriteString("firstSide", CampaignSnapshotSerializer.FormatSide(FirstSide));
            writer.WriteString("secondSide", CampaignSnapshotSerializer.FormatSide(SecondSide));
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static CampaignNativeInitiativeDeclarationV49 Deserialize(ReadOnlyMemory<byte> bytes)
    {
        try
        {
            if (bytes.Length is < 1 or > 8192) throw new JsonException("Invalid event size.");
            using var document = JsonDocument.Parse(bytes);
            var root = document.RootElement;
            CampaignSnapshotSerializer.RequireProperties(root, "contractVersion", "eventType", "campaignId",
                "priorStateVersion", "priorEventHash", "positionId", "nextPositionId", "worldHash",
                "gameTurn", "operationStage", "declaringHolder", "firstSide", "secondSide");
            if (root.GetProperty("eventType").GetString() != "native-initiative-declared")
                throw new JsonException("Invalid event type.");
            LandSide ParseSide(string property) => root.GetProperty(property).GetString() switch
            {
                "axis" => LandSide.Axis,
                "commonwealth" => LandSide.Commonwealth,
                _ => throw new JsonException("Invalid side."),
            };
            var parsed = new CampaignNativeInitiativeDeclarationV49(root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("campaignId").GetString()!, root.GetProperty("priorStateVersion").GetInt64(),
                root.GetProperty("priorEventHash").GetString()!, root.GetProperty("positionId").GetString()!,
                root.GetProperty("nextPositionId").GetString()!, root.GetProperty("worldHash").GetString()!,
                root.GetProperty("gameTurn").GetInt32(), root.GetProperty("operationStage").GetInt32(),
                ParseSide("declaringHolder"), ParseSide("firstSide"), ParseSide("secondSide"));
            if (!bytes.Span.SequenceEqual(parsed.SerializeCanonical())) throw new JsonException("Noncanonical event.");
            return parsed;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException
            or KeyNotFoundException or FormatException or NullReferenceException)
        {
            throw new JsonException("Invalid Initiative Declaration.", error);
        }
    }

    private static LandSide Opposite(LandSide side) => side == LandSide.Axis ? LandSide.Commonwealth : LandSide.Axis;

    private static bool IsHash(string? value) => value is { Length: 71 }
        && value.StartsWith("sha256:", StringComparison.Ordinal)
        && value.AsSpan(7).ToString().All(digit => digit is (>= '0' and <= '9') or (>= 'a' and <= 'f'));
}
