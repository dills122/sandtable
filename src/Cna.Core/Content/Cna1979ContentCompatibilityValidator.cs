using Cna.Core.Rules;

namespace Cna.Core.Content;

public static class Cna1979ContentCompatibilityValidator
{
    public static ContentValidationResult Validate(ContentPackDefinition pack)
    {
        ArgumentNullException.ThrowIfNull(pack);
        var issues = new List<ContentValidationIssue>();

        if (!string.Equals(pack.RulesetId, Cna1979Ruleset.RulesetId, StringComparison.Ordinal))
        {
            AddUnknown(
                issues,
                "/rulesetId",
                $"Ruleset '{pack.RulesetId}' is not compatible with '{Cna1979Ruleset.RulesetId}'.");
        }

        foreach (var location in pack.Locations)
        {
            Check(
                issues,
                ContentVocabularyKind.Terrain,
                location.TerrainId,
                $"/locations/{location.LocationId}/terrainId");
        }

        foreach (var edge in pack.Edges)
        {
            var edgePath = $"/edges/{edge.FirstLocationId}|{edge.SecondLocationId}";

            foreach (var feature in edge.Features)
            {
                var featurePath = $"{edgePath}/features/{feature.FeatureId}";

                if (!Cna1979ContentVocabulary.Contains(
                    ContentVocabularyKind.EdgeFeature,
                    feature.FeatureId))
                {
                    AddUnknown(
                        issues,
                        $"{featurePath}/featureId",
                        $"Unknown edge feature ID '{feature.FeatureId}'.");
                    continue;
                }

                var vocabulary = Cna1979ContentVocabulary.Get(
                    ContentVocabularyKind.EdgeFeature,
                    feature.FeatureId);
                var validDirection = vocabulary.DirectionPolicy switch
                {
                    ContentDirectionPolicy.Required =>
                        feature.DirectionFromLocationId is not null
                        && (string.Equals(
                                feature.DirectionFromLocationId,
                                edge.FirstLocationId,
                                StringComparison.Ordinal)
                            || string.Equals(
                                feature.DirectionFromLocationId,
                                edge.SecondLocationId,
                                StringComparison.Ordinal)),
                    ContentDirectionPolicy.Forbidden => feature.DirectionFromLocationId is null,
                    _ => false,
                };

                if (!validDirection)
                {
                    issues.Add(new ContentValidationIssue(
                        "topology.invalid-direction",
                        $"{featurePath}/directionFromLocationId",
                        $"Feature '{feature.FeatureId}' violates direction policy '{vocabulary.DirectionPolicy}'."));
                }
            }
        }

        foreach (var formation in pack.Formations)
        {
            Check(
                issues,
                ContentVocabularyKind.Side,
                formation.SideId,
                $"/formations/{formation.FormationId}/sideId");
            Check(
                issues,
                ContentVocabularyKind.Organization,
                formation.OrganizationId,
                $"/formations/{formation.FormationId}/organizationId");
        }

        foreach (var element in pack.Elements)
        {
            Check(
                issues,
                ContentVocabularyKind.Side,
                element.SideId,
                $"/elements/{element.ElementId}/sideId");
            Check(
                issues,
                ContentVocabularyKind.Organization,
                element.OrganizationId,
                $"/elements/{element.ElementId}/organizationId");
            if (!Cna1979Movement.IsSupportedMobilityId(element.MobilityId))
            {
                AddUnknown(
                    issues,
                    $"/elements/{element.ElementId}/mobilityId",
                    $"Unknown Movement mobility ID '{element.MobilityId}'.");
            }

            if (element.BreakdownVehicleCohort is { } cohort)
            {
                if (!Cna1979Breakdown.IsSupportedVehicleTypeId(cohort.VehicleTypeId))
                {
                    AddUnknown(
                        issues,
                        $"/elements/{element.ElementId}/breakdownVehicleCohort/vehicleTypeId",
                        $"Unknown Breakdown vehicle type ID '{cohort.VehicleTypeId}'.");
                }

                if (!Cna1979Breakdown.IsSupportedProfileId(cohort.ProfileId))
                {
                    AddUnknown(
                        issues,
                        $"/elements/{element.ElementId}/breakdownVehicleCohort/profileId",
                        $"Unknown Breakdown profile ID '{cohort.ProfileId}'.");
                }


                if (!Cna1979Breakdown.IsSupportedVehicleProfile(
                    cohort.VehicleTypeId,
                    cohort.ProfileId))
                {
                    issues.Add(new ContentValidationIssue(
                        "content.breakdown-cohort.profile-mismatch",
                        $"/elements/{element.ElementId}/breakdownVehicleCohort/profileId",
                        $"Breakdown vehicle type '{cohort.VehicleTypeId}' does not use profile '{cohort.ProfileId}'."));
                }
            }
        }

        return new ContentValidationResult(issues);
    }

    private static void Check(
        ICollection<ContentValidationIssue> issues,
        ContentVocabularyKind kind,
        string id,
        string path)
    {
        if (!Cna1979ContentVocabulary.Contains(kind, id))
        {
            AddUnknown(issues, path, $"Unknown {kind} ID '{id}'.");
        }
    }

    private static void AddUnknown(
        ICollection<ContentValidationIssue> issues,
        string path,
        string message) => issues.Add(new ContentValidationIssue(
            "vocabulary.unknown-id",
            path,
            message));
}
