using System.Collections.ObjectModel;
using System.Security.Cryptography;

namespace Cna.Core.Content;

public sealed record ContentCombatComponentV2(
    string ComponentId,
    string ComponentClassId,
    int MaximumToe,
    int OffensiveCloseAssaultRating,
    int DefensiveCloseAssaultRating,
    ContentOrigin Origin);

public sealed record ContentFormationMorale(
    string FormationId,
    string SideId,
    string? ParentFormationId,
    string OrganizationId,
    int BasicMorale,
    ContentOrigin BasicMoraleOrigin,
    ContentOrigin Origin);

public sealed record ContentInitialAmmunition(int Points, ContentOrigin Origin);

public sealed record ContentInitialCombatReadiness(
    int GameTurn,
    int OperationStage,
    string WaterStatus,
    string StoresStatus,
    bool Pinned,
    ContentOrigin Origin);

public sealed record ContentRetreatSupplyAnchor(
    string SideId,
    string LocationId,
    string Kind,
    ContentOrigin Origin);

public sealed record ContentElementCombatFactsV2
{
    public ContentElementCombatFactsV2(
        string elementId,
        string sideId,
        string parentFormationId,
        string organizationId,
        string mobilityId,
        int baseCapabilityPointAllowance,
        ContentPlacementMode placementMode,
        string combatClassificationId,
        ContentOrigin combatOrigin,
        IEnumerable<ContentCombatComponentV2> components,
        ContentOrigin origin)
    {
        ElementId = elementId;
        SideId = sideId;
        ParentFormationId = parentFormationId;
        OrganizationId = organizationId;
        MobilityId = mobilityId;
        BaseCapabilityPointAllowance = baseCapabilityPointAllowance;
        PlacementMode = placementMode;
        CombatClassificationId = combatClassificationId;
        CombatOrigin = combatOrigin;
        Components = CopySorted(components, value => value.ComponentId);
        Origin = origin;
    }

    public string ElementId { get; }
    public string SideId { get; }
    public string ParentFormationId { get; }
    public string OrganizationId { get; }
    public string MobilityId { get; }
    public int BaseCapabilityPointAllowance { get; }
    public ContentPlacementMode PlacementMode { get; }
    public string CombatClassificationId { get; }
    public ContentOrigin CombatOrigin { get; }
    public IReadOnlyList<ContentCombatComponentV2> Components { get; }
    public ContentOrigin Origin { get; }

    public bool Equals(ContentElementCombatFactsV2? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && ElementId == other.ElementId
            && SideId == other.SideId
            && ParentFormationId == other.ParentFormationId
            && OrganizationId == other.OrganizationId
            && MobilityId == other.MobilityId
            && BaseCapabilityPointAllowance == other.BaseCapabilityPointAllowance
            && PlacementMode == other.PlacementMode
            && CombatClassificationId == other.CombatClassificationId
            && CombatOrigin == other.CombatOrigin
            && Components.SequenceEqual(other.Components)
            && Origin == other.Origin);

    public override int GetHashCode() => HashCode.Combine(ElementId, SideId, ParentFormationId);

    private static ReadOnlyCollection<T> CopySorted<T>(IEnumerable<T> values, Func<T, string> key)
        where T : class => Array.AsReadOnly(ContentContractGuards.CopyValues(values, nameof(values))
            .OrderBy(key, StringComparer.Ordinal).ToArray());
}

public sealed record ContentInitialPlacementCombatFactsV2
{
    public ContentInitialPlacementCombatFactsV2(
        string elementId,
        string locationId,
        IEnumerable<ContentInitialComponentToe> initialComponentToes,
        ContentInitialAmmunition initialAmmunition,
        ContentInitialCombatReadiness initialReadiness,
        ContentOrigin origin)
    {
        ElementId = elementId;
        LocationId = locationId;
        InitialComponentToes = Array.AsReadOnly(ContentContractGuards.CopyValues(
            initialComponentToes,
            nameof(initialComponentToes)).OrderBy(value => value.ComponentId, StringComparer.Ordinal).ToArray());
        InitialAmmunition = initialAmmunition;
        InitialReadiness = initialReadiness;
        Origin = origin;
    }

    public string ElementId { get; }
    public string LocationId { get; }
    public IReadOnlyList<ContentInitialComponentToe> InitialComponentToes { get; }
    public ContentInitialAmmunition InitialAmmunition { get; }
    public ContentInitialCombatReadiness InitialReadiness { get; }
    public ContentOrigin Origin { get; }

    public bool Equals(ContentInitialPlacementCombatFactsV2? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && ElementId == other.ElementId
            && LocationId == other.LocationId
            && InitialComponentToes.SequenceEqual(other.InitialComponentToes)
            && InitialAmmunition == other.InitialAmmunition
            && InitialReadiness == other.InitialReadiness
            && Origin == other.Origin);

    public override int GetHashCode() => HashCode.Combine(ElementId, LocationId);
}

public sealed record ContentCombatScenario
{
    public ContentCombatScenario(
        string scenarioId,
        ContentScenarioBoundary start,
        ContentScenarioBoundary end,
        IEnumerable<ContentInitialPlacementCombatFactsV2> initialPlacements,
        IEnumerable<ContentRetreatSupplyAnchor> retreatSupplyAnchors,
        ContentOrigin origin)
    {
        ScenarioId = scenarioId;
        Start = start;
        End = end;
        InitialPlacements = Array.AsReadOnly(ContentContractGuards.CopyValues(
            initialPlacements,
            nameof(initialPlacements)).OrderBy(value => value.ElementId, StringComparer.Ordinal)
            .ThenBy(value => value.LocationId, StringComparer.Ordinal).ToArray());
        RetreatSupplyAnchors = Array.AsReadOnly(ContentContractGuards.CopyValues(
            retreatSupplyAnchors,
            nameof(retreatSupplyAnchors)).OrderBy(value => value.SideId, StringComparer.Ordinal).ToArray());
        Origin = origin;
    }

    public string ScenarioId { get; }
    public ContentScenarioBoundary Start { get; }
    public ContentScenarioBoundary End { get; }
    public IReadOnlyList<ContentInitialPlacementCombatFactsV2> InitialPlacements { get; }
    public IReadOnlyList<ContentRetreatSupplyAnchor> RetreatSupplyAnchors { get; }
    public ContentOrigin Origin { get; }

    public bool Equals(ContentCombatScenario? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && ScenarioId == other.ScenarioId
            && Start == other.Start
            && End == other.End
            && InitialPlacements.SequenceEqual(other.InitialPlacements)
            && RetreatSupplyAnchors.SequenceEqual(other.RetreatSupplyAnchors)
            && Origin == other.Origin);

    public override int GetHashCode() => ScenarioId.GetHashCode(StringComparison.Ordinal);
}

public sealed record ContentPackV7Definition
{
    public const int SchemaVersion = 7;
    public const string CanonicalFormatId = "sandtable.content-json.v6";
    public const string SupportedPackId = "rules-lab.content.close-assault.v1";
    public const string SupportedRulesetId = "cna-1979.1";
    public const string SupportedCapabilityProfileId = "sandtable.capability.combat-cycle-infantry.v1";

    public static readonly IReadOnlyList<string> RequiredCapabilities = Array.AsReadOnly(new[]
    {
        "land.close-assault-inputs",
        "land.combat-components",
        "land.element-mobility",
        "land.formations",
        "land.hex-topology",
        "land.initial-deployment",
        "land.weather-areas",
    });

    public ContentPackV7Definition(
        string packId,
        string rulesetId,
        IEnumerable<string> capabilities,
        string capabilityProfileId,
        IEnumerable<ContentSourceIndexEntry> sourceIndex,
        IEnumerable<ContentHex> locations,
        IEnumerable<ContentWeatherAreaAssignment> weatherAreaAssignments,
        IEnumerable<ContentHexEdge> edges,
        IEnumerable<ContentFormationMorale> formations,
        IEnumerable<ContentElementCombatFactsV2> elements,
        IEnumerable<ContentCombatScenario> scenarios)
    {
        PackId = packId;
        RulesetId = rulesetId;
        Capabilities = CopyStrings(capabilities);
        CapabilityProfileId = capabilityProfileId;
        SourceIndex = CopySorted(sourceIndex, value => value.SourceId);
        Locations = CopySorted(locations, value => value.LocationId);
        WeatherAreaAssignments = CopySorted(weatherAreaAssignments, value => value.LocationId);
        Edges = Array.AsReadOnly(ContentContractGuards.CopyValues(edges, nameof(edges))
            .OrderBy(value => value.FirstLocationId, StringComparer.Ordinal)
            .ThenBy(value => value.SecondLocationId, StringComparer.Ordinal).ToArray());
        Formations = CopySorted(formations, value => value.FormationId);
        Elements = CopySorted(elements, value => value.ElementId);
        Scenarios = CopySorted(scenarios, value => value.ScenarioId);
    }

    public string PackId { get; }
    public string RulesetId { get; }
    public IReadOnlyList<string> Capabilities { get; }
    public string CapabilityProfileId { get; }
    public IReadOnlyList<ContentSourceIndexEntry> SourceIndex { get; }
    public IReadOnlyList<ContentHex> Locations { get; }
    public IReadOnlyList<ContentWeatherAreaAssignment> WeatherAreaAssignments { get; }
    public IReadOnlyList<ContentHexEdge> Edges { get; }
    public IReadOnlyList<ContentFormationMorale> Formations { get; }
    public IReadOnlyList<ContentElementCombatFactsV2> Elements { get; }
    public IReadOnlyList<ContentCombatScenario> Scenarios { get; }

    public ContentPackV7Definition WithCollections(
        IEnumerable<ContentSourceIndexEntry> sourceIndex,
        IEnumerable<ContentHex> locations,
        IEnumerable<ContentWeatherAreaAssignment> weatherAreaAssignments,
        IEnumerable<ContentHexEdge> edges,
        IEnumerable<ContentFormationMorale> formations,
        IEnumerable<ContentElementCombatFactsV2> elements,
        IEnumerable<ContentCombatScenario> scenarios) => new(
            PackId,
            RulesetId,
            Capabilities,
            CapabilityProfileId,
            sourceIndex,
            locations,
            weatherAreaAssignments,
            edges,
            formations,
            elements,
            scenarios);

    public bool Equals(ContentPackV7Definition? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && PackId == other.PackId
            && RulesetId == other.RulesetId
            && Capabilities.SequenceEqual(other.Capabilities)
            && CapabilityProfileId == other.CapabilityProfileId
            && SourceIndex.SequenceEqual(other.SourceIndex)
            && Locations.SequenceEqual(other.Locations)
            && WeatherAreaAssignments.SequenceEqual(other.WeatherAreaAssignments)
            && Edges.SequenceEqual(other.Edges)
            && Formations.SequenceEqual(other.Formations)
            && Elements.SequenceEqual(other.Elements)
            && Scenarios.SequenceEqual(other.Scenarios));

    public override int GetHashCode() => HashCode.Combine(PackId, RulesetId, CapabilityProfileId);

    private static ReadOnlyCollection<string> CopyStrings(IEnumerable<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var copy = values.ToArray();
        if (copy.Any(value => value is null))
            throw new ArgumentException("Null collection entries are not allowed.", nameof(values));
        return Array.AsReadOnly(copy.Order(StringComparer.Ordinal).ToArray());
    }

    private static ReadOnlyCollection<T> CopySorted<T>(IEnumerable<T> values, Func<T, string> key)
        where T : class => Array.AsReadOnly(ContentContractGuards.CopyValues(values, nameof(values))
            .OrderBy(key, StringComparer.Ordinal).ToArray());
}

public sealed record ContentPackV7Identity
{
    public ContentPackV7Identity(
        int schemaVersion,
        string formatId,
        string packId,
        string rulesetId,
        string hash)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(schemaVersion, ContentPackV7Definition.SchemaVersion);
        if (formatId != ContentPackV7Definition.CanonicalFormatId)
            throw new ArgumentException("Unsupported Content7 format.", nameof(formatId));
        SchemaVersion = schemaVersion;
        FormatId = formatId;
        PackId = ContentContractGuards.RequireStableId(packId, nameof(packId));
        RulesetId = ContentContractGuards.RequireStableId(rulesetId, nameof(rulesetId));
        Hash = ContentContractGuards.RequireSha256(hash, nameof(hash));
    }

    public int SchemaVersion { get; }
    public string FormatId { get; }
    public string PackId { get; }
    public string RulesetId { get; }
    public string Hash { get; }
}

public sealed class ContentPackV7Artifact
{
    private readonly byte[] canonicalBytes;

    private ContentPackV7Artifact(ContentPackV7Definition definition, byte[] canonicalBytes)
    {
        Definition = definition;
        this.canonicalBytes = canonicalBytes.ToArray();
        Identity = new ContentPackV7Identity(
            ContentPackV7Definition.SchemaVersion,
            ContentPackV7Definition.CanonicalFormatId,
            definition.PackId,
            definition.RulesetId,
            $"sha256:{Convert.ToHexString(SHA256.HashData(this.canonicalBytes)).ToLowerInvariant()}");
    }

    public ContentPackV7Definition Definition { get; }
    public ContentPackV7Identity Identity { get; }
    public int CanonicalByteCount => canonicalBytes.Length;

    public static ContentPackV7Artifact Create(ContentPackV7Definition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return new ContentPackV7Artifact(definition, ContentPackV7Serializer.SerializeCanonical(definition));
    }

    public byte[] GetCanonicalBytes() => canonicalBytes.ToArray();
}

public sealed class ContentPackV7ParseResult
{
    private ContentPackV7ParseResult(
        ContentPackV7Definition? definition,
        string? errorCode,
        string? errorPath,
        string? message)
    {
        Definition = definition;
        ErrorCode = errorCode;
        ErrorPath = errorPath;
        Message = message;
    }

    public bool IsSuccess => Definition is not null;
    public ContentPackV7Definition? Definition { get; }
    public string? ErrorCode { get; }
    public string? ErrorPath { get; }
    public string? Message { get; }

    internal static ContentPackV7ParseResult Success(ContentPackV7Definition definition) =>
        new(definition, null, null, null);

    internal static ContentPackV7ParseResult Failure(string code, string path, string message) =>
        new(null, code, path, message);
}
