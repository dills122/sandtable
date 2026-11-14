using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Content;

// Content interchange; no runtime capability or historical approval is implied.
public static class ContentScheduledCombatPackSerializer
{
    internal const string Format = "sandtable.content-scheduled-combat.v1";
    internal const string SourceFormat = "sandtable.content-scheduled-combat-source.v2";
    private const int MaximumBytes = 16 * 1024 * 1024;
    private sealed record WireReference(string SourceId, string Locator);
    private sealed record WireOrigin(string Kind, WireReference[] References);
    private sealed record WireRating(string Kind, int? Value);
    private sealed record WireWeaponProfile(WireRating CapabilityPoints, WireRating AntiAir, WireRating AntiArmor,
        WireRating Vulnerability, WireRating ArmorProtection, WireRating FuelRate, string BreakdownMarker);
    private sealed record WireComponent(string ComponentId, string ComponentClassId, int MaximumToe,
        int DefensiveCloseAssaultRating, bool DefensiveRatingParenthesized, int? OffensiveCloseAssaultRating,
        bool OffensiveRatingParenthesized, int? BarrageRating, bool BarrageRatingParenthesized,
        WireWeaponProfile? WeaponSourceProfile, WireOrigin Origin);
    private sealed record WireComponentLimit(string LimitId, string[] ComponentIds, int MaximumToe, WireOrigin Origin);
    private sealed record WireFacts(string ElementId, string CombatClassificationId, WireComponent[] Components,
        int? MaximumCombinedToe, WireComponentLimit[] ComponentToeLimits, WireOrigin Origin);
    private sealed record WireToe(string ComponentId, int CurrentToe, WireOrigin Origin);
    private sealed record WireSeed(string ScenarioId, string ElementId, WireToe[] InitialComponentToes);
    private sealed record WirePack(string Format, byte[] ScheduledContent, WireFacts[] ElementCombatFacts, WireSeed[] Seeds);

    public static byte[] SerializeCanonical(ContentPackV5Definition definition) => SerializeCore(definition, source: false);

    internal static byte[] SerializeSourceCanonical(ContentPackV5Definition definition) => SerializeCore(definition, source: true);

    private static byte[] SerializeCore(ContentPackV5Definition definition, bool source)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var validation = source ? ContentPackV5Validator.ValidateSourceScheduledStructure(definition, allowEmptyNoncombat: true)
            : ContentPackV5Validator.ValidateScheduledStructure(definition, allowEmptyNoncombat: true);
        if (!validation.IsValid) throw new InvalidContentPackException(validation.Issues);
        var facts = definition.ElementCombatFacts.Select(value => new WireFacts(value.ElementId,
            value.CombatClassificationId, value.Components.Select(component => new WireComponent(component.ComponentId,
                component.ComponentClassId, component.MaximumToe, component.DefensiveCloseAssaultRating,
                    component.DefensiveRatingParenthesized, component.OffensiveCloseAssaultRating,
                    component.OffensiveRatingParenthesized, component.BarrageRating, component.BarrageRatingParenthesized,
                    EncodeWeaponProfile(component.WeaponSourceProfile), EncodeOrigin(component.Origin))).ToArray(),
                value.MaximumCombinedToe, value.ComponentToeLimits.Select(limit => new WireComponentLimit(limit.LimitId,
                    limit.ComponentIds.ToArray(), limit.MaximumToe, EncodeOrigin(limit.Origin))).ToArray(),
                EncodeOrigin(value.Origin))).ToArray();
        var seeds = definition.InitialPlacementCombatFacts.Select(value => new WireSeed(value.ScenarioId,
            value.ElementId, value.InitialComponentToes.Select(toe => new WireToe(toe.ComponentId,
                toe.CurrentToe, EncodeOrigin(toe.Origin))).ToArray())).ToArray();
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new WirePack(source ? SourceFormat : Format,
            source ? ContentScheduledPackSerializer.SerializeSourceCanonical(definition.LegacyDefinition)
                : ContentScheduledPackSerializer.SerializeCanonical(definition.LegacyDefinition), facts, seeds));
        if (bytes.Length > MaximumBytes) throw new InvalidOperationException("Scheduled combat content exceeds its size limit.");
        return bytes;
    }

    public static ContentPackV5Definition Deserialize(ReadOnlySpan<byte> bytes) => DeserializeCore(bytes, source: false);

    internal static ContentPackV5Definition DeserializeSource(ReadOnlySpan<byte> bytes) => DeserializeCore(bytes, source: true);

    private static ContentPackV5Definition DeserializeCore(ReadOnlySpan<byte> bytes, bool source)
    {
        if (bytes.Length is 0 or > MaximumBytes) throw new JsonException("Invalid scheduled combat content size.");
        try
        {
            var wire = JsonSerializer.Deserialize<WirePack>(bytes);
            if (wire is null || wire.Format != (source ? SourceFormat : Format) || wire.ScheduledContent is null
                || wire.ElementCombatFacts is null || wire.Seeds is null)
                throw new JsonException("Invalid scheduled combat content envelope.");
            var legacy = source ? ContentScheduledPackSerializer.DeserializeSource(wire.ScheduledContent)
                : ContentScheduledPackSerializer.Deserialize(wire.ScheduledContent);
            var facts = wire.ElementCombatFacts.Select(value =>
            {
                if (value is null || value.Components is null || value.ComponentToeLimits is null)
                    throw new JsonException("Invalid combat facts.");
                var components = value.Components.Select(component => component is null
                    ? throw new JsonException("Invalid combat component.")
                    : new ContentCombatComponent(component.ComponentId, component.ComponentClassId, component.MaximumToe,
                        component.DefensiveCloseAssaultRating, DecodeOrigin(component.Origin), component.DefensiveRatingParenthesized,
                        component.OffensiveCloseAssaultRating, component.OffensiveRatingParenthesized,
                        component.BarrageRating, component.BarrageRatingParenthesized,
                        DecodeWeaponProfile(component.WeaponSourceProfile)));
                var origin = DecodeOrigin(value.Origin);
                var limits = value.ComponentToeLimits.Select(limit => limit is null || limit.ComponentIds is null
                    ? throw new JsonException("Invalid component TOE limit.")
                    : new ContentComponentToeLimit(limit.LimitId, limit.ComponentIds, limit.MaximumToe, DecodeOrigin(limit.Origin)));
                return ContentElementCombatFacts.CreateScheduled(value.ElementId, value.CombatClassificationId,
                    components, origin, value.MaximumCombinedToe, limits);
            }).ToArray();
            var seeds = wire.Seeds.Select(value =>
            {
                if (value is null || value.InitialComponentToes is null) throw new JsonException("Invalid TOE seed.");
                return new ContentInitialPlacementCombatFacts(value.ScenarioId, value.ElementId,
                    value.InitialComponentToes.Select(toe => toe is null
                        ? throw new JsonException("Invalid TOE component.")
                        : new ContentInitialComponentToe(toe.ComponentId, toe.CurrentToe, DecodeOrigin(toe.Origin))));
            }).ToArray();
            var definition = new ContentPackV5Definition(legacy, facts, seeds);
            if (!bytes.SequenceEqual(SerializeCore(definition, source)))
                throw new JsonException("Scheduled combat content is not canonical.");
            return definition;
        }
        catch (JsonException) { throw; }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException
            or FormatException or OverflowException or InvalidContentPackException)
        { throw new JsonException("Invalid scheduled combat content.", exception); }
    }

    public static string CalculateHash(ContentPackV5Definition definition) =>
        "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical(definition)));

    private static WireRating EncodeRating(ContentPrintedRating rating) => new(rating.Kind switch
    {
        ContentPrintedRatingKind.Unknown => "unknown",
        ContentPrintedRatingKind.Number => "number",
        ContentPrintedRatingKind.Parenthesized => "parenthesized",
        ContentPrintedRatingKind.Dash => "dash",
        _ => throw new InvalidOperationException("Unknown printed-rating kind.")
    }, rating.Value);

    private static ContentPrintedRating DecodeRating(WireRating rating)
    {
        if (rating is null) throw new JsonException("Missing printed rating.");
        var kind = rating.Kind switch
        {
            "unknown" => ContentPrintedRatingKind.Unknown,
            "number" => ContentPrintedRatingKind.Number,
            "parenthesized" => ContentPrintedRatingKind.Parenthesized,
            "dash" => ContentPrintedRatingKind.Dash,
            _ => throw new JsonException("Unknown printed-rating kind.")
        };
        return new ContentPrintedRating(kind, rating.Value);
    }

    private static WireWeaponProfile? EncodeWeaponProfile(ContentWeaponSourceProfile? profile) => profile is null ? null :
        new WireWeaponProfile(EncodeRating(profile.CapabilityPoints), EncodeRating(profile.AntiAir), EncodeRating(profile.AntiArmor),
            EncodeRating(profile.Vulnerability), EncodeRating(profile.ArmorProtection), EncodeRating(profile.FuelRate), profile.BreakdownMarker);

    private static ContentWeaponSourceProfile? DecodeWeaponProfile(WireWeaponProfile? profile) => profile is null ? null :
        new(DecodeRating(profile.CapabilityPoints), DecodeRating(profile.AntiAir), DecodeRating(profile.AntiArmor),
            DecodeRating(profile.Vulnerability), DecodeRating(profile.ArmorProtection), DecodeRating(profile.FuelRate), profile.BreakdownMarker);

    private static WireOrigin EncodeOrigin(ContentOrigin origin) => new(origin.Kind switch
    {
        ContentOriginKind.SourceDerived => "source-derived",
        ContentOriginKind.Synthetic => "synthetic",
        _ => throw new InvalidOperationException("Unknown content origin.")
    }, origin.References.Select(value => new WireReference(value.SourceId, value.Locator)).ToArray());

    private static ContentOrigin DecodeOrigin(WireOrigin value)
    {
        if (value is null || value.References is null || value.References.Any(reference => reference is null))
            throw new JsonException("Invalid content origin.");
        var kind = value.Kind switch
        {
            "source-derived" => ContentOriginKind.SourceDerived,
            "synthetic" => ContentOriginKind.Synthetic,
            _ => throw new JsonException("Unknown content origin kind.")
        };
        return new ContentOrigin(kind, value.References.Select(reference => new RuleReference(reference.SourceId, reference.Locator)));
    }
}
