using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Content;

// Diagnostic only: this result cannot create or admit a content artifact.
internal sealed record CampaignScheduledArrivalContentDraftResult(
    ContentValidationResult LegacyValidation,
    ContentValidationResult DraftValidation,
    ContentValidationResult UnexplainedLegacyValidation,
    IReadOnlyList<string> ExplainedLegacyMissingPlacementPaths);

internal static class CampaignScheduledArrivalContentDraft
{
    public static CampaignScheduledArrivalContentDraftResult Analyze(
        ContentPackDefinition pack,
        string scenarioId,
        IEnumerable<ScheduledArrivalGroup> groups)
    {
        ArgumentNullException.ThrowIfNull(pack);
        ContentContractGuards.RequireStableId(scenarioId, nameof(scenarioId));
        ArgumentNullException.ThrowIfNull(groups);
        var arrivals = groups.ToArray();
        if (arrivals.Any(group => group is null))
            throw new ArgumentException("Arrival groups cannot contain null.", nameof(groups));

        var legacy = ContentPackValidator.Validate(pack);
        var issues = new List<ContentValidationIssue>();
        var explained = new List<string>();
        var scenarios = pack.Scenarios.Where(value => value.ScenarioId == scenarioId).ToArray();
        if (scenarios.Length != 1)
        {
            issues.Add(new ContentValidationIssue("arrival.scenario", "/scenarios/" + scenarioId,
                "The selected scenario must exist exactly once."));
            return Result();
        }

        var scenario = scenarios[0];
        var elements = pack.Elements.GroupBy(value => value.ElementId, StringComparer.Ordinal)
            .ToDictionary(value => value.Key, value => value.ToArray(), StringComparer.Ordinal);
        var sourceCounts = pack.SourceIndex.GroupBy(value => value.SourceId, StringComparer.Ordinal)
            .ToDictionary(value => value.Key, value => value.Count(), StringComparer.Ordinal);
        var groupCounts = arrivals.GroupBy(value => value.GroupId, StringComparer.Ordinal)
            .ToDictionary(value => value.Key, value => value.Count(), StringComparer.Ordinal);
        var unitCounts = arrivals.SelectMany(value => value.UnitIds)
            .GroupBy(value => value, StringComparer.Ordinal)
            .ToDictionary(value => value.Key, value => value.Count(), StringComparer.Ordinal);
        var initiallyPlaced = scenario.InitialPlacements.Select(value => value.ElementId)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var group in arrivals)
        {
            var path = "/scenarios/" + scenarioId + "/scheduledArrivals/" + group.GroupId;
            var valid = true;
            void Reject(string code, string message)
            {
                issues.Add(new ContentValidationIssue(code, path, message));
                valid = false;
            }

            if (groupCounts[group.GroupId] != 1)
                Reject("arrival.duplicate-group", "The group ID must be unique.");
            if (Compare(group.GameTurn, group.OperationStage, scenario.Start) < 0
                || Compare(group.GameTurn, group.OperationStage, scenario.End) > 0)
                Reject("arrival.outside-scenario", "The arrival slot is outside the scenario.");
            if (group.Sources.Any(source => !sourceCounts.TryGetValue(source.SourceId, out var count) || count != 1))
                Reject("arrival.source", "Every source must occur exactly once in the content index.");

            var expectedSide = group.Owner == LandSide.Axis ? "axis" : "commonwealth";
            foreach (var id in group.UnitIds)
            {
                if (unitCounts[id] != 1)
                    Reject("arrival.duplicate-element", "An element may be scheduled only once.");
                if (initiallyPlaced.Contains(id))
                    Reject("arrival.initially-placed", "An arriving element cannot already be placed.");
                if (!elements.TryGetValue(id, out var matches) || matches.Length != 1)
                    Reject("arrival.element", "Each arriving element must exist exactly once.");
                else if (matches[0].PlacementMode != ContentPlacementMode.Independent
                    || !string.Equals(matches[0].SideId, expectedSide, StringComparison.Ordinal))
                    Reject("arrival.element-owner-or-placement", "An arriving element must be independent and owned by its group.");
            }

            if (!valid) continue;
            foreach (var id in group.UnitIds)
            {
                var missingPath = "/scenarios/" + scenarioId + "/initialPlacements/" + id;
                if (legacy.Issues.Any(issue => issue.Code == "placement.missing-element"
                    && issue.Path == missingPath))
                    explained.Add(missingPath);
            }
        }

        return Result();

        CampaignScheduledArrivalContentDraftResult Result()
        {
            var paths = explained.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
            var explainedPaths = paths.ToHashSet(StringComparer.Ordinal);
            return new CampaignScheduledArrivalContentDraftResult(
                legacy,
                new ContentValidationResult(issues),
                new ContentValidationResult(legacy.Issues.Where(issue =>
                    issue.Code != "placement.missing-element" || !explainedPaths.Contains(issue.Path))),
                Array.AsReadOnly(paths));
        }
    }

    private static int Compare(int turn, int stage, ContentScenarioBoundary boundary) =>
        turn != boundary.GameTurn ? turn.CompareTo(boundary.GameTurn) : stage.CompareTo(boundary.OperationStage);
}
