using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// Task 021A1 (dormant, internal; no runtime caller). Closed Combat capability manifest for the native
// contact Combat path (Rules9 + Content V6), following the fog spike's "Capability admission" rule:
// every family reachable from the configuration must have a disclosure transition, an owner or
// System action and an adjudication handler; an unknown or uncovered family fails closure.
// Register sources and every disposition are listed in
// docs/work/plans/2026-11-20-task-021-capability-manifest-scoping.md §7. Activation and restore
// wiring belong to 021A2/021A3. Today's Combat-capable content correctly fails closure (owner
// ruling T021-Q1 option c): post-assault retreat and the repeat cycle have no frozen disclosure.

internal enum CampaignCombatCapabilityDisposition
{
    /// <summary>Reachable and served by a frozen audience stage, an action and a handler.</summary>
    Covered,
    /// <summary>Reachable (or not proven unreachable) without a frozen disclosure. Fails closure.</summary>
    Uncovered,
    /// <summary>Source-legal but excluded from the bounded profile by a named owner policy.</summary>
    PolicyExcluded,
    /// <summary>Unreachable because an authenticated content/admission condition is false.</summary>
    SourceConditionAbsent,
    /// <summary>Not a Combat-sequence family; governed by the content capability profile or deferred.</summary>
    OutsideCombatScope,
}

/// <summary>Coverage triple: frozen audience stage, owner or System action, adjudication step kind.</summary>
internal sealed record CampaignCombatCapabilityCoverage(string AudienceStage, string Action, string Handler);

internal sealed record CampaignCombatCapabilityFamily(string FamilyId,
    CampaignCombatCapabilityDisposition Disposition, string Authority, CampaignCombatCapabilityCoverage? Coverage);

internal sealed class CampaignCombatCapabilityManifest
{
    public const int CurrentContractVersion = 1;
    public const string ManifestId = "sandtable.combat-capability-manifest.v1";

    private readonly byte[] canonicalBytes;

    private CampaignCombatCapabilityManifest(string rulesetHash, string setupId, string setupHash,
        ContentPackV6Identity pack, string capabilityProfileId, IReadOnlyList<CampaignCombatCapabilityFamily> families)
    {
        RulesetHash = rulesetHash;
        SetupId = setupId;
        SetupHash = setupHash;
        Pack = pack;
        CapabilityProfileId = capabilityProfileId;
        Families = families;
        canonicalBytes = CampaignCombatCapabilityManifestCodec.Serialize(this);
        Hash = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(canonicalBytes));
    }

    public string RulesetHash { get; }
    public string SetupId { get; }
    public string SetupHash { get; }
    public ContentPackV6Identity Pack { get; }
    /// <summary>The content's own capability profile; the manifest does not mint a Combat profile ID.</summary>
    public string CapabilityProfileId { get; }
    public IReadOnlyList<CampaignCombatCapabilityFamily> Families { get; }
    public string Hash { get; }
    public bool IsClosed => Families.All(value => value.Disposition != CampaignCombatCapabilityDisposition.Uncovered);
    public IReadOnlyList<string> UncoveredFamilyIds => Families
        .Where(value => value.Disposition == CampaignCombatCapabilityDisposition.Uncovered)
        .Select(value => value.FamilyId).ToArray();
    public byte[] SerializeCanonical() => canonicalBytes.ToArray();

    /// <summary>
    /// Derives the manifest for one creation configuration. Rejects (throws) a non-canonical ruleset,
    /// a setup that does not pin exactly this content, or content that fails V6 admission.
    /// </summary>
    public static CampaignCombatCapabilityManifest Derive(string rulesetHash, CampaignSetupSnapshotV6 setup,
        ContentPackV6Artifact artifact)
    {
        ArgumentNullException.ThrowIfNull(setup);
        ArgumentNullException.ThrowIfNull(artifact);
        if (!Cna1979Ruleset.IsCanonicalHash(rulesetHash))
            throw new ArgumentException("Unsupported ruleset for a Combat capability manifest.", nameof(rulesetHash));
        if (setup.Content.Pack != artifact.Identity
            || setup.CapabilityProfileId != artifact.Definition.CapabilityProfileId
            || !ContentPackV6Validator.Validate(artifact.Definition).IsValid)
            throw new ArgumentException("Setup and content do not form an admitted configuration.", nameof(setup));
        return new CampaignCombatCapabilityManifest(rulesetHash, setup.SetupId, setup.SetupHash, artifact.Identity,
            artifact.Definition.CapabilityProfileId, DeriveFamilies(artifact.Definition));
    }

    /// <summary>Restore revalidation primitive: retained bytes must equal a fresh derivation.</summary>
    public static bool Revalidate(ReadOnlySpan<byte> retained, string rulesetHash, CampaignSetupSnapshotV6 setup,
        ContentPackV6Artifact artifact)
    {
        try
        {
            CampaignCombatCapabilityManifestCodec.Read(retained);
            return retained.SequenceEqual(Derive(rulesetHash, setup, artifact).canonicalBytes);
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException)
        {
            return false;
        }
    }

    internal static CampaignCombatCapabilityManifest FromParts(string rulesetHash, string setupId, string setupHash,
        ContentPackV6Identity pack, string capabilityProfileId, IReadOnlyList<CampaignCombatCapabilityFamily> families) =>
        new(rulesetHash, setupId, setupHash, pack, capabilityProfileId, families);

    /// <summary>Family dispositions from content facts alone, in register order.</summary>
    internal static IReadOnlyList<CampaignCombatCapabilityFamily> DeriveFamilies(ContentPackV6Definition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var facts = definition.ElementCombatFacts.ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        var combatElements = definition.LegacyDefinition.Elements
            .Where(element => facts.TryGetValue(element.ElementId, out var value) && value.Components.Count > 0)
            .ToArray();
        // An assault can reach a result only when both sides field an element with a combat component.
        // A contact bound to a component-less unit is cancelled as force-unavailable (T020-Q8) before any
        // result, so it opens no post-assault decision.
        var assaultResultReachable = combatElements.Select(element => element.SideId)
            .Distinct(StringComparer.Ordinal).Count() >= 2;
        var nonInfantry = combatElements.SelectMany(element => facts[element.ElementId].Components)
            .Any(component => component.ComponentClassId != Cna1979Combat.InfantryComponentClassId);
        var motorizedCombat = combatElements.Any(element => element.MobilityId == Cna1979Movement.MotorizedMobilityId);
        return Register.Select(entry => entry.Resolve(assaultResultReachable, nonInfantry, motorizedCombat)).ToArray();
    }

    internal static IReadOnlyList<string> FamilyIds => Register.Select(value => value.FamilyId).ToArray();

    internal static CampaignCombatCapabilityCoverage? CoverageOf(string familyId) =>
        Register.Single(value => value.FamilyId == familyId).Coverage;

    private enum Reach { Always, AssaultResult, NonInfantryComponent, MotorizedCombatElement, Never }

    private sealed record RegisterEntry(string FamilyId, CampaignCombatCapabilityDisposition WhenReachable,
        Reach Reach, string Authority, CampaignCombatCapabilityCoverage? Coverage = null)
    {
        public CampaignCombatCapabilityFamily Resolve(bool assaultResult, bool nonInfantry, bool motorized)
        {
            var reachable = Reach switch
            {
                Reach.Always => true,
                Reach.AssaultResult => assaultResult,
                Reach.NonInfantryComponent => nonInfantry,
                Reach.MotorizedCombatElement => motorized,
                _ => false,
            };
            // Policy exclusions and out-of-scope families keep their disposition regardless of reach.
            var disposition = IsReachDependent && !reachable
                ? CampaignCombatCapabilityDisposition.SourceConditionAbsent
                : WhenReachable;
            return new(FamilyId, disposition, Authority,
                disposition == CampaignCombatCapabilityDisposition.Covered ? Coverage : null);
        }

        public bool IsReachDependent => WhenReachable is CampaignCombatCapabilityDisposition.Covered
            or CampaignCombatCapabilityDisposition.Uncovered;

        public bool Allows(CampaignCombatCapabilityDisposition disposition) => disposition == WhenReachable
            || IsReachDependent && Reach != Reach.Always
                && disposition == CampaignCombatCapabilityDisposition.SourceConditionAbsent;
    }

    internal static string AuthorityOf(string familyId) =>
        Register.Single(value => value.FamilyId == familyId).Authority;

    /// <summary>Whether the register can produce this disposition for this family at all.</summary>
    internal static bool Allows(string familyId, CampaignCombatCapabilityDisposition disposition) =>
        Register.Single(value => value.FamilyId == familyId).Allows(disposition);

    private static CampaignCombatCapabilityCoverage Frozen(string stage, string action, string handler) =>
        new(stage, action, handler);

    // Closed register, in canonical order. Decision families use the Host stage names from
    // combat-contact-side-projection-v2; capability families use the Task 004 handoff exclusion IDs
    // (verify-combat-authority-composition-v1.py) plus CMB-POL-009. Adding, removing or reordering an
    // entry is a new manifest contract version.
    private static readonly RegisterEntry[] Register =
    [
        new("combat-selection", CampaignCombatCapabilityDisposition.Covered, Reach.Always,
            "combat-audience-decision-v1",
            Frozen("combat-selection", "select-close-assault|finish-without-attack", "contact-combat-selection")),
        new("retreat-before-assault", CampaignCombatCapabilityDisposition.Covered, Reach.Always,
            "combat-audience-decision-v1; CMB-POL-001 decline-only",
            Frozen("rba-decline", "rba-decline", "contact-rba-decline")),
        new("force-assignment", CampaignCombatCapabilityDisposition.Covered, Reach.AssaultResult,
            "combat-audience-decision-v1",
            Frozen("force-assignment", "seal-full-toe", "contact-force-seal")),
        new("noncombat-contact-assault", CampaignCombatCapabilityDisposition.Covered, Reach.Always,
            "T020-Q8 option c",
            Frozen("combat-selection", "system-force-unavailable", "native-combat-force-cancellation")),
        new("post-assault-retreat", CampaignCombatCapabilityDisposition.Uncovered, Reach.AssaultResult,
            "no frozen audience stage (T021-Q1 option c)"),
        new("custody", CampaignCombatCapabilityDisposition.Covered, Reach.AssaultResult,
            "combat-audience-decision-v1; T020-Q5",
            Frozen("custody", "relocate-and-guard|leave-unguarded", "contact-combat-custody")),
        new("reserve-release", CampaignCombatCapabilityDisposition.Covered, Reach.Always,
            "combat-audience-decision-v1",
            Frozen("reserve-release", "release-choice", "contact-reserve-release-choice")),
        new("cycle-control", CampaignCombatCapabilityDisposition.Covered, Reach.Always,
            "combat-audience-decision-v1",
            Frozen("cycle-control", "repeat|finish", "contact-cycle-control")),
        new("repeat-movement", CampaignCombatCapabilityDisposition.Uncovered, Reach.AssaultResult,
            "no frozen audience stage (T021-Q1 option c); repeat needs a retained Combat result cursor"),
        new("repeat-combat-selection", CampaignCombatCapabilityDisposition.Uncovered, Reach.AssaultResult,
            "no frozen audience stage (T021-Q1 option c); repeat needs a retained Combat result cursor"),
        new("second-side-combat-selection", CampaignCombatCapabilityDisposition.Uncovered, Reach.Always,
            "no frozen audience stage; not proven unreachable (fail-closed)"),
        new("multiple-opportunity-reaction", CampaignCombatCapabilityDisposition.OutsideCombatScope, Reach.Never,
            "Task004 handoff post-B-extension; pre-Combat, content capability profile"),
        new("positive-vehicle-breakdown", CampaignCombatCapabilityDisposition.OutsideCombatScope, Reach.Never,
            "Task004 handoff post-B-extension; pre-Combat, content capability profile"),
        new("two-runtime-assaults", CampaignCombatCapabilityDisposition.Uncovered, Reach.AssaultResult,
            "Task004 handoff deferred; realized natively by the repeat cycle without frozen disclosure"),
        new("general-resupply", CampaignCombatCapabilityDisposition.OutsideCombatScope, Reach.Never,
            "Task004 handoff deferred: outside-combat-package"),
        new("real-rba-movement", CampaignCombatCapabilityDisposition.PolicyExcluded, Reach.Never,
            "CMB-POL-001; T020-Q7"),
        new("released-reserve-offensive-combat", CampaignCombatCapabilityDisposition.Uncovered, Reach.AssaultResult,
            "Task004 handoff deferred; realized natively by the repeat cycle without frozen disclosure"),
        new("motorized-retreat", CampaignCombatCapabilityDisposition.Uncovered, Reach.MotorizedCombatElement,
            "Task004 handoff deferred: infantry-profile-only"),
        new("gun-armor-combat", CampaignCombatCapabilityDisposition.Uncovered, Reach.NonInfantryComponent,
            "Task004 handoff deferred: infantry-profile-only; CMB-POL-001 empty gun/armor steps; "
            + "BRK-CERT-004 admits only the infantry component class"),
        new("mandatory-attacks", CampaignCombatCapabilityDisposition.SourceConditionAbsent, Reach.Never,
            "BRK-CERT-003 admission; T020-Q2"),
        new("new-phase-slot-execution", CampaignCombatCapabilityDisposition.OutsideCombatScope, Reach.Never,
            "Task004 handoff deferred; CMB-POL-008 terminal is same-slot Truck Convoy entry"),
        new("hosted-intelligence", CampaignCombatCapabilityDisposition.OutsideCombatScope, Reach.Never,
            "Task004 handoff deferred: in-process-contract-only"),
        new("voluntary-surrender", CampaignCombatCapabilityDisposition.PolicyExcluded, Reach.Never,
            "CMB-POL-009"),
    ];
}

internal static class CampaignCombatCapabilityManifestCodec
{
    private const int MaxBytes = 65_536;

    private static readonly string[] TopFields =
        ["contractVersion", "manifestId", "rulesetHash", "setupId", "setupHash", "content", "families", "closed"];
    private static readonly string[] ContentFields = ["schemaVersion", "formatId", "packId", "rulesetId", "hash",
        "capabilityProfileId"];
    private static readonly string[] FamilyFields = ["familyId", "disposition", "authority", "coverage"];
    private static readonly string[] CoverageFields = ["audienceStage", "action", "handler"];

    public static string DispositionName(CampaignCombatCapabilityDisposition value) => value switch
    {
        CampaignCombatCapabilityDisposition.Covered => "covered",
        CampaignCombatCapabilityDisposition.Uncovered => "uncovered",
        CampaignCombatCapabilityDisposition.PolicyExcluded => "policy-excluded",
        CampaignCombatCapabilityDisposition.SourceConditionAbsent => "source-condition-absent",
        CampaignCombatCapabilityDisposition.OutsideCombatScope => "outside-combat-scope",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    public static byte[] Serialize(CampaignCombatCapabilityManifest manifest)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CampaignCombatCapabilityManifest.CurrentContractVersion);
            writer.WriteString("manifestId", CampaignCombatCapabilityManifest.ManifestId);
            writer.WriteString("rulesetHash", manifest.RulesetHash);
            writer.WriteString("setupId", manifest.SetupId);
            writer.WriteString("setupHash", manifest.SetupHash);
            writer.WriteStartObject("content");
            writer.WriteNumber("schemaVersion", manifest.Pack.SchemaVersion);
            writer.WriteString("formatId", manifest.Pack.FormatId);
            writer.WriteString("packId", manifest.Pack.PackId);
            writer.WriteString("rulesetId", manifest.Pack.RulesetId);
            writer.WriteString("hash", manifest.Pack.Hash);
            writer.WriteString("capabilityProfileId", manifest.CapabilityProfileId);
            writer.WriteEndObject();
            writer.WriteStartArray("families");
            foreach (var family in manifest.Families)
            {
                writer.WriteStartObject();
                writer.WriteString("familyId", family.FamilyId);
                writer.WriteString("disposition", DispositionName(family.Disposition));
                writer.WriteString("authority", family.Authority);
                if (family.Coverage is { } coverage)
                {
                    writer.WriteStartObject("coverage");
                    writer.WriteString("audienceStage", coverage.AudienceStage);
                    writer.WriteString("action", coverage.Action);
                    writer.WriteString("handler", coverage.Handler);
                    writer.WriteEndObject();
                }
                else writer.WriteNull("coverage");
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteBoolean("closed", manifest.IsClosed);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    /// <summary>
    /// Strict reader. Rejects unknown, missing, duplicated or reordered families, unknown dispositions,
    /// coverage that differs from the register, a stale closure bit and any non-canonical encoding.
    /// It checks the register only; per-campaign dispositions are checked by Revalidate.
    /// </summary>
    public static CampaignCombatCapabilityManifest Read(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length is 0 or > MaxBytes) throw new JsonException("Combat capability manifest size is invalid.");
        try
        {
            using var document = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 8 });
            var root = document.RootElement;
            RequireFields(root, TopFields);
            if (root.GetProperty("contractVersion").GetInt32() != CampaignCombatCapabilityManifest.CurrentContractVersion
                || root.GetProperty("manifestId").GetString() != CampaignCombatCapabilityManifest.ManifestId)
                throw new JsonException("Unsupported Combat capability manifest version.");
            var content = root.GetProperty("content");
            RequireFields(content, ContentFields);
            var pack = new ContentPackV6Identity(content.GetProperty("schemaVersion").GetInt32(),
                content.GetProperty("formatId").GetString()!, content.GetProperty("packId").GetString()!,
                content.GetProperty("rulesetId").GetString()!, content.GetProperty("hash").GetString()!);
            var families = root.GetProperty("families").EnumerateArray().Select(ReadFamily).ToArray();
            if (!families.Select(value => value.FamilyId).SequenceEqual(CampaignCombatCapabilityManifest.FamilyIds,
                    StringComparer.Ordinal))
                throw new JsonException("Combat capability manifest family set is not the closed register.");
            foreach (var family in families)
            {
                if (!CampaignCombatCapabilityManifest.Allows(family.FamilyId, family.Disposition)
                    || family.Authority != CampaignCombatCapabilityManifest.AuthorityOf(family.FamilyId))
                    throw new JsonException($"Combat capability family '{family.FamilyId}' has an unregistered disposition.");
                var expected = family.Disposition == CampaignCombatCapabilityDisposition.Covered
                    ? CampaignCombatCapabilityManifest.CoverageOf(family.FamilyId) : null;
                if (family.Coverage != expected || family.Disposition == CampaignCombatCapabilityDisposition.Covered
                        && expected is null)
                    throw new JsonException($"Combat capability family '{family.FamilyId}' has unregistered coverage.");
            }
            var manifest = CampaignCombatCapabilityManifest.FromParts(root.GetProperty("rulesetHash").GetString()!,
                root.GetProperty("setupId").GetString()!, root.GetProperty("setupHash").GetString()!, pack,
                content.GetProperty("capabilityProfileId").GetString()!, families);
            if (!bytes.SequenceEqual(manifest.SerializeCanonical()))
                throw new JsonException("Combat capability manifest is not canonical.");
            return manifest;
        }
        catch (Exception exception) when (exception is InvalidOperationException or KeyNotFoundException
            or FormatException or ArgumentException)
        {
            throw new JsonException("Invalid Combat capability manifest.", exception);
        }
    }

    private static CampaignCombatCapabilityFamily ReadFamily(JsonElement element)
    {
        RequireFields(element, FamilyFields);
        var name = element.GetProperty("disposition").GetString();
        var disposition = Enum.GetValues<CampaignCombatCapabilityDisposition>()
            .Cast<CampaignCombatCapabilityDisposition?>()
            .SingleOrDefault(value => DispositionName(value!.Value) == name)
            ?? throw new JsonException($"Unknown Combat capability disposition '{name}'.");
        var coverageElement = element.GetProperty("coverage");
        CampaignCombatCapabilityCoverage? coverage = null;
        if (coverageElement.ValueKind != JsonValueKind.Null)
        {
            RequireFields(coverageElement, CoverageFields);
            coverage = new(coverageElement.GetProperty("audienceStage").GetString()!,
                coverageElement.GetProperty("action").GetString()!, coverageElement.GetProperty("handler").GetString()!);
        }
        return new(element.GetProperty("familyId").GetString()!, disposition,
            element.GetProperty("authority").GetString()!, coverage);
    }

    private static void RequireFields(JsonElement element, string[] fields)
    {
        if (element.ValueKind != JsonValueKind.Object
            || !element.EnumerateObject().Select(value => value.Name).SequenceEqual(fields, StringComparer.Ordinal))
            throw new JsonException("Combat capability manifest fields are not the exact ordered set.");
    }
}
