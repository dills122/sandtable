using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Content;

// Content interchange only. This format is not an admitted campaign capability.
public static class ContentScheduledPackSerializer
{
    internal const string Format = "sandtable.content-scheduled.v1";
    internal const string SourceFormat = "sandtable.content-scheduled-source.v2";
    private const int MaximumBytes = 16 * 1024 * 1024;
    private sealed record WireReference(string SourceId, string Locator);
    private sealed record WireTrucks(int? Light, int? Medium, int? Heavy);
    private sealed record WireArrival(string GroupId, string SideId, int GameTurn, int OperationStage,
        string[] ElementIds, string? EntryAreaId, string OriginKind, WireReference[] References, WireTrucks? Trucks);
    private sealed record WireArea(string AreaId, string[] LocationIds, string OriginKind, WireReference[] References);
    private sealed record WireInitialGroup(string GroupId, string SideId, string LocationId, string[] ElementIds,
        string OriginKind, WireReference[] References, WireTrucks? Trucks);
    private sealed record WireSchedule(string ScenarioId, WireArrival[] Arrivals, WireArea[] EntryAreas,
        WireInitialGroup[] InitialGroups);
    private sealed record WirePack(string Format, byte[] BaseContent, WireSchedule[] Schedules);

    public static byte[] SerializeCanonical(ContentPackDefinition definition) => SerializeCore(definition, source: false);

    internal static byte[] SerializeSourceCanonical(ContentPackDefinition definition) => SerializeCore(definition, source: true);

    private static byte[] SerializeCore(ContentPackDefinition definition, bool source)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var issues = source ? ValidateSource(definition) : Validate(definition);
        if (!issues.IsValid) throw new InvalidContentPackException(issues.Issues);
        var baseDefinition = CopyWithScenarios(definition, definition.Scenarios.Select(WithoutSchedule));
        var schedules = definition.Scenarios.Where(value => value.ScheduledArrivals.Count != 0 || value.EntryAreas.Count != 0
                || value.InitialGroups.Count != 0)
            .Select(scenario => new WireSchedule(scenario.ScenarioId,
                scenario.ScheduledArrivals.Select(arrival => new WireArrival(arrival.GroupId, arrival.SideId,
                    arrival.Arrival.GameTurn, arrival.Arrival.OperationStage, arrival.ElementIds.ToArray(),
                    arrival.EntryAreaId, arrival.Origin.Kind switch
                    {
                        ContentOriginKind.SourceDerived => "source-derived",
                        ContentOriginKind.Synthetic => "synthetic",
                        _ => throw new InvalidOperationException("Unknown scheduled origin kind.")
                    }, arrival.Origin.References
                        .Select(value => new WireReference(value.SourceId, value.Locator)).ToArray(),
                    arrival.Trucks is null ? null : new WireTrucks(arrival.Trucks.Light, arrival.Trucks.Medium,
                        arrival.Trucks.Heavy))).ToArray(),
                scenario.EntryAreas.Select(area => new WireArea(area.AreaId, area.LocationIds.ToArray(),
                    EncodeOriginKind(area.Origin.Kind), area.Origin.References.Select(value =>
                        new WireReference(value.SourceId, value.Locator)).ToArray())).ToArray(),
                scenario.InitialGroups.Select(group => new WireInitialGroup(group.GroupId, group.SideId,
                    group.LocationId, group.ElementIds.ToArray(), EncodeOriginKind(group.Origin.Kind),
                    group.Origin.References.Select(value => new WireReference(value.SourceId, value.Locator)).ToArray(),
                    group.Trucks is null ? null : new WireTrucks(group.Trucks.Light, group.Trucks.Medium,
                        group.Trucks.Heavy))).ToArray())).ToArray();
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new WirePack(source ? SourceFormat : Format,
            ContentPackSerializer.SerializeDraftDefinition(baseDefinition), schedules));
        if (bytes.Length > MaximumBytes) throw new InvalidOperationException("Scheduled content exceeds its size limit.");
        return bytes;
    }

    public static ContentValidationResult Validate(ContentPackDefinition definition) => ValidateCore(definition, source: false);

    internal static ContentValidationResult ValidateSource(ContentPackDefinition definition) => ValidateCore(definition, source: true);

    private static ContentValidationResult ValidateCore(ContentPackDefinition definition, bool source)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var scheduled = definition.Scenarios.SelectMany(scenario => scenario.ScheduledArrivals
                .SelectMany(arrival => arrival.ElementIds.Select(id => (scenario.ScenarioId, ElementId: id))))
            .Select(value => $"/scenarios/{value.ScenarioId}/initialPlacements/{value.ElementId}")
            .ToHashSet(StringComparer.Ordinal);
        var grouped = definition.Scenarios.SelectMany(scenario => scenario.InitialGroups
                .SelectMany(group => group.ElementIds.Select(id => (scenario.ScenarioId, ElementId: id))))
            .Select(value => $"/scenarios/{value.ScenarioId}/initialPlacements/{value.ElementId}")
            .ToHashSet(StringComparer.Ordinal);
        var cityPaths = definition.Locations.Where(location => location.TerrainId == Cna1979SourceContentVocabulary.MajorCityTerrainId
                && location.Origin.Kind == ContentOriginKind.SourceDerived)
            .Select(location => $"/locations/{location.LocationId}").ToHashSet(StringComparer.Ordinal);
        static bool SourceOrganization(string id) => id is Cna1979SourceContentVocabulary.CompanyOrganizationId
            or Cna1979SourceContentVocabulary.BrigadeOrganizationId or Cna1979SourceContentVocabulary.DivisionOrganizationId
            or Cna1979SourceContentVocabulary.RegimentOrganizationId;
        var sourceOrganizationPaths = definition.Formations
            .Where(value => value.Origin.Kind == ContentOriginKind.SourceDerived && SourceOrganization(value.OrganizationId))
            .Select(value => $"/formations/{value.FormationId}/organizationId")
            .Concat(definition.Elements.Where(value => value.Origin.Kind == ContentOriginKind.SourceDerived
                    && SourceOrganization(value.OrganizationId))
                .Select(value => $"/elements/{value.ElementId}/organizationId"))
            .ToHashSet(StringComparer.Ordinal);
        var unresolvedMobilityPaths = definition.Elements.Where(value => value.Origin.Kind == ContentOriginKind.SourceDerived
                && value.MobilityId == Cna1979SourceContentVocabulary.UnresolvedMobilityId)
            .Select(value => $"/elements/{value.ElementId}/mobilityId").ToHashSet(StringComparer.Ordinal);
        var compatibility = Cna1979ContentCompatibilityValidator.Validate(definition).Issues.Where(issue =>
            !source || !(issue.Code == "vocabulary.unknown-id" && issue.Path.EndsWith("/terrainId", StringComparison.Ordinal)
                && cityPaths.Contains(issue.Path[..^"/terrainId".Length])
                || issue.Code == "content.terrain-mobility-unsupported" && cityPaths.Contains(issue.Path)
                || issue.Code == "vocabulary.unknown-id" && (sourceOrganizationPaths.Contains(issue.Path)
                    || unresolvedMobilityPaths.Contains(issue.Path))));
        var issues = ContentPackValidator.Validate(definition).Issues.Where(issue =>
            issue.Code != "scenario.scheduled-arrivals-unsupported"
            && issue.Code != "scenario.entry-areas-unsupported"
            && issue.Code != "scenario.initial-groups-unsupported"
            // Source initialization may retain known hexes before their connecting map is transcribed.
            && !(source && issue.Code == "topology.disconnected")
            && !(issue.Code == "placement.missing-element" && (scheduled.Contains(issue.Path) || grouped.Contains(issue.Path))))
            .Concat(compatibility).ToList();
        if (source && definition.SourceIndex.Any(value => value.Kind == ContentSourceKind.RepositorySynthetic))
            issues.Add(new ContentValidationIssue("source.synthetic-not-allowed", "/sourceIndex",
                "Source content cannot contain synthetic source records."));
        if (source && definition.Locations.Any(value => value.TerrainId == Cna1979SourceContentVocabulary.MajorCityTerrainId
                && value.Origin.Kind != ContentOriginKind.SourceDerived))
            issues.Add(new ContentValidationIssue("source.city-origin-required", "/locations",
                "Major-city terrain requires a source-derived origin."));
        if (scheduled.Count == 0 && grouped.Count == 0)
            issues.Add(new ContentValidationIssue("arrival.schedule-empty", "/scenarios", "Successor content requires an arrival or initial group."));
        return new ContentValidationResult(issues);
    }

    public static ContentPackDefinition Deserialize(ReadOnlySpan<byte> bytes) => DeserializeCore(bytes, source: false);

    internal static ContentPackDefinition DeserializeSource(ReadOnlySpan<byte> bytes) => DeserializeCore(bytes, source: true);

    private static ContentPackDefinition DeserializeCore(ReadOnlySpan<byte> bytes, bool source)
    {
        if (bytes.Length is 0 or > MaximumBytes) throw new JsonException("Invalid scheduled content size.");
        try
        {
            var wire = JsonSerializer.Deserialize<WirePack>(bytes);
            if (wire is null || wire.Format != (source ? SourceFormat : Format) || wire.BaseContent is null || wire.Schedules is null
                || wire.Schedules.Length == 0 || wire.Schedules.Any(value => value is null))
                throw new JsonException("Invalid scheduled content envelope.");
            var parsed = ContentPackSerializer.Deserialize(wire.BaseContent);
            var baseDefinition = parsed.Definition ?? throw new JsonException("Invalid base content.");
            var byScenario = new Dictionary<string, WireSchedule>(StringComparer.Ordinal);
            foreach (var schedule in wire.Schedules)
            {
                if (schedule.ScenarioId is null || schedule.Arrivals is null || schedule.EntryAreas is null || schedule.InitialGroups is null
                    || (schedule.Arrivals.Length == 0 && schedule.EntryAreas.Length == 0 && schedule.InitialGroups.Length == 0)
                    || !byScenario.TryAdd(schedule.ScenarioId, schedule))
                    throw new JsonException("Invalid scenario schedule.");
            }
            if (byScenario.Keys.Any(id => baseDefinition.Scenarios.All(value => value.ScenarioId != id)))
                throw new JsonException("Schedule references an unknown scenario.");
            var scenarios = baseDefinition.Scenarios.Select(scenario =>
            {
                if (!byScenario.TryGetValue(scenario.ScenarioId, out var schedule)) return scenario;
                var arrivals = schedule.Arrivals.Select(value =>
                {
                    if (value is null || value.ElementIds is null || value.References is null
                        || value.References.Any(reference => reference is null))
                        throw new JsonException("Invalid scheduled arrival.");
                    var kind = value.OriginKind switch
                    {
                        "source-derived" => ContentOriginKind.SourceDerived,
                        "synthetic" => ContentOriginKind.Synthetic,
                        _ => throw new JsonException("Unknown scheduled origin kind.")
                    };
                    var origin = new ContentOrigin(kind,
                        value.References.Select(reference => new RuleReference(reference.SourceId, reference.Locator)));
                    return new ContentScheduledArrival(value.GroupId, value.SideId,
                        new ContentScenarioBoundary(value.GameTurn, value.OperationStage), value.ElementIds,
                        value.EntryAreaId, origin, value.Trucks is null ? null : new ContentSourceTruckQuantities(
                            value.Trucks.Light, value.Trucks.Medium, value.Trucks.Heavy));
                });
                var areas = schedule.EntryAreas.Select(value =>
                {
                    if (value is null || value.LocationIds is null || value.References is null
                        || value.References.Any(reference => reference is null))
                        throw new JsonException("Invalid entry area.");
                    var origin = new ContentOrigin(DecodeOriginKind(value.OriginKind),
                        value.References.Select(reference => new RuleReference(reference.SourceId, reference.Locator)));
                    return new ContentLocationArea(value.AreaId, value.LocationIds, origin);
                });
                var groups = schedule.InitialGroups.Select(value =>
                {
                    if (value is null || value.ElementIds is null || value.References is null
                        || value.References.Any(reference => reference is null))
                        throw new JsonException("Invalid initial group.");
                    var origin = new ContentOrigin(DecodeOriginKind(value.OriginKind),
                        value.References.Select(reference => new RuleReference(reference.SourceId, reference.Locator)));
                    return new ContentInitialGroup(value.GroupId, value.SideId, value.LocationId, value.ElementIds, origin,
                        value.Trucks is null ? null : new ContentSourceTruckQuantities(
                            value.Trucks.Light, value.Trucks.Medium, value.Trucks.Heavy));
                });
                return new ContentScenario(scenario.ScenarioId, scenario.Start, scenario.End,
                    scenario.InitialPlacements, arrivals, areas, groups, scenario.Origin);
            }).ToArray();
            var definition = CopyWithScenarios(baseDefinition, scenarios);
            if (!bytes.SequenceEqual(SerializeCore(definition, source)))
                throw new JsonException("Scheduled content is not canonical.");
            return definition;
        }
        catch (JsonException) { throw; }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException
            or FormatException or OverflowException or InvalidContentPackException)
        { throw new JsonException("Invalid scheduled content.", exception); }
    }

    public static string CalculateHash(ContentPackDefinition definition) =>
        "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical(definition)));

    private static ContentScenario WithoutSchedule(ContentScenario value) =>
        new(value.ScenarioId, value.Start, value.End, value.InitialPlacements, value.Origin);

    private static string EncodeOriginKind(ContentOriginKind kind) => kind switch
    {
        ContentOriginKind.SourceDerived => "source-derived",
        ContentOriginKind.Synthetic => "synthetic",
        _ => throw new InvalidOperationException("Unknown origin kind.")
    };

    private static ContentOriginKind DecodeOriginKind(string kind) => kind switch
    {
        "source-derived" => ContentOriginKind.SourceDerived,
        "synthetic" => ContentOriginKind.Synthetic,
        _ => throw new JsonException("Unknown origin kind.")
    };

    private static ContentPackDefinition CopyWithScenarios(ContentPackDefinition source, IEnumerable<ContentScenario> scenarios) =>
        new(source.SchemaVersion, source.FormatId, source.PackId, source.RulesetId, source.Capabilities,
            source.SourceIndex, source.Locations, source.WeatherAreaAssignments, source.Edges,
            source.Formations, source.Elements, scenarios);
}
