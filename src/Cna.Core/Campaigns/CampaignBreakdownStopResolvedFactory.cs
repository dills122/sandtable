using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal static class CampaignBreakdownStopResolvedFactory
{
    public static BreakdownStopResolved Create(CampaignSnapshotV11 prior, ContentPackV6Artifact artifact,
        ContentScenario scenario, string actionId)
    {
        if (!CampaignSnapshotV11Validator.IsValid(prior, artifact, scenario)
            || prior.CurrentPosition.Kind != CampaignPositionV11Kind.BreakdownStop
            || actionId != CampaignBreakdownLifecycleFactory.CreateResolveActionId(prior))
            throw new InvalidOperationException("invalid-breakdown-authority");
        var stop = prior.BreakdownFlow switch
        {
            CampaignBreakdownFlow.PhasingStop phasing => phasing.Stop,
            CampaignBreakdownFlow.ReactorStopOpen open => open.Stop,
            CampaignBreakdownFlow.ReactorStopClosed closed => closed.Stop,
            _ => throw new InvalidOperationException("invalid-breakdown-authority"),
        };
        var ledgers = prior.World.Elements.Where(x => x.OperationalState.VehicleBreakdownState is not null)
            .Select(x => x.OperationalState.VehicleBreakdownState!).ToDictionary(x => x.CohortId, StringComparer.Ordinal);
        var broken = stop.CohortInputs.ToDictionary(x => x.CohortId, x => ledgers[x.CohortId].BrokenPointCount, StringComparer.Ordinal);
        var batch = CampaignBreakdownCheckResolver.Resolve(prior.CampaignId, prior.RulesetHash, stop,
            broken, prior.RandomState, checked(prior.StateVersion + 1));
        CampaignBreakdownFlow flow = prior.BreakdownFlow switch
        {
            CampaignBreakdownFlow.PhasingStop => new CampaignBreakdownFlow.Idle(),
            CampaignBreakdownFlow.ReactorStopOpen open => new CampaignBreakdownFlow.Reacting(open.PhasingContinuation, null),
            CampaignBreakdownFlow.ReactorStopClosed closed => Resume(closed.PhasingContinuation),
            _ => throw new InvalidOperationException("invalid-breakdown-authority"),
        };
        var sources = batch.Checks.SelectMany(x => x.Sources)
            .Concat(Cna1979BreakdownAdjudication.CreateRulings().Single(x => x.RulingId == "land.breakdown.ruling.stop-reaction-precedence").Sources)
            .Concat(batch.CreatedLots.SelectMany(x => x.Sources)).Distinct()
            .OrderBy(x => x.SourceId, StringComparer.Ordinal).ThenBy(x => x.Locator, StringComparer.Ordinal).ToArray();
        return new BreakdownStopResolved(prior.CampaignId, checked(prior.StateVersion + 1), prior.StateVersion,
            prior.RulesetHash, Cna1979LandSequenceV4.BreakdownStopPositionId, actionId, stop,
            prior.RandomState, batch.Checks, batch.CreatedLots, batch.RandomStateAfter, flow, sources);
    }
    private static CampaignBreakdownFlow Resume(CampaignPhasingContinuation continuation) => continuation switch
    {
        CampaignPhasingContinuation.ResumeRoute resume => new CampaignBreakdownFlow.Moving(resume.Route),
        CampaignPhasingContinuation.ResolveStop resolve => new CampaignBreakdownFlow.PhasingStop(resolve.Stop),
        _ => throw new InvalidOperationException("invalid-breakdown-authority"),
    };
}
