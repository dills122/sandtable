using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>Frozen3j armed witness from authenticated inherited Release; no repeat or Combat execution.</summary>
internal static class CampaignCombatArmedContinuation
{
    internal sealed record Proof(CampaignCombatCreationRequest Request, CampaignCombatInheritedReserveRelease.Projection Source,
        CampaignCombatCandidate Candidate, string PredecessorHash);

    public static Proof Derive(CombatInheritedReserveReleaseSource source, IReadOnlyList<CombatReleaseInput> inputs,
        IReadOnlyList<byte[]> events)
    {
        ArgumentNullException.ThrowIfNull(source);
        var p = CampaignCombatInheritedReserveRelease.Replay(source, inputs, events);
        var release = p.Release; var predecessor = p.Base.Predecessor;
        var cycle = p.Base.ReleaseBase.Cycle; var member = release.Members.Single(); var h = member.History;
        var hash = CampaignOpeningPreambleCodec.Hash(CampaignCombatInheritedReserveRelease.SerializeControl(p, source.Request));
        // Literal compatibility bounds supplement full replay; a digest alone never authenticates a terminal.
        var expected = cycle.ActingSide == LandSide.Axis
            ? "sha256:fa85df7519f622ea05d6af25be4a8bcc23164132a0b0a48fac5e2eff81c4c091"
            : "sha256:7dec0cd054a18ee9403df2431c06445e66f6a8c699679b7472f0ea9e51bcd0ae";
        Require(hash == expected && p.Closed && release.StateVersion == 25 && release.CompletionReceiptId is not null &&
            release.Pending.Count == 0 && cycle.Ordinal == 1 && cycle.PlayerPhaseSlot == "first-acting-side",
            "Unsupported armed continuation terminal.");
        var scope = new CombatReleaseScope(cycle.GameTurn, cycle.OperationStage, cycle.PlayerPhaseSlot, cycle.ActingSide);
        Require(member.Status == CampaignElementReserveStatus.None && member.BaseCpa == 10 && member.SpentCp == new CapabilityPointAmount(0, 1) &&
            h.Scope == scope && h.ReleasedType == "I" && h.ReleaseCycle == 1 && h.CpaBasis == 10 && h.VoluntaryCeiling == 10 &&
            h.OffensiveCommitmentId is null && h.NextMovement == new CombatReleaseMovementException(scope, 2, "pending", null),
            "Armed continuation requires unused released-I rights.");
        Require(p.Progress.Count == 1 && p.Progress[0].EventType == "reserve-unit-disposition-recorded" &&
            p.Progress[0].ReceiptId == h.ReleaseReceiptId && release.Dispositions.Single() is
            { Choice: "release-I", BeforeStatus: CampaignElementReserveStatus.ReserveI, AfterStatus: CampaignElementReserveStatus.None },
            "Armed continuation requires actual release-I material progress.");
        var end = predecessor.MovementEnd;
        Require(end is not null && end.Ordinal == 1 && end.Scope == new CampaignCombatReserveScope(scope.GameTurn,
            scope.OperationStage, scope.PlayerPhaseSlot, scope.ActingSide) && end.ExcludedBefore.Count == 0 &&
            end.EndLocations.Count == p.World.Elements.Count && end.EndLocations.All(l =>
                l.Unit.CreationBinding == p.World.CreationBinding && p.World.Elements.Any(e => e.ElementId == l.Unit.ElementId && e.CurrentLocationId == l.LocationId)),
            "Armed continuation requires retained unchanged Movement-end locations.");
        Require(release.AttackHistory.Count == 0 && p.World.BrokenVehicleLots.Count == 0 && p.World.CustodyLots.Count == 0 &&
            p.World.Guards.Count == 0 && p.World.FutureObligations.Count == 0 && p.World.Settlements.Count == 0 &&
            p.World.Relationships.Count == 0 && p.World.ReplacementEntitlements.Count == 0,
            "Armed continuation has unsupported prior work.");
        var weather = predecessor.Base.Predecessor.Stage.Weather.Weather;
        Require(weather.Count > 0 && weather.All(w => w.Kind == WeatherKind.Normal), "Unsupported armed Weather.");
        Require(p.World.Elements.All(e => e.OperationalState.CapabilityPointsExpended == new CapabilityPointAmount(0, 1) &&
            e.Components.Sum(c => c.CurrentToe) == 10 && e.Ammunition.Points == 10 && !e.Readiness.Pinned), "Unsupported armed resources.");
        // Regenerated Created11 is identical to the independently authenticated source creation.
        // Certification compares the full current World with initial facts and proves native result support.
        var created = CampaignCreatedV11Serializer.Serialize(CampaignCreatedV11.Create(source.Request));
        var candidate = CampaignCombatCertification.CertifyInitialProfileFacts(source.Request, created, p.World,
            cycle, p.Base.ReleaseBase.FirstActingSide, WeatherKind.Normal, WeatherKind.Normal);
        Require(candidate is not null && candidate.Attacker.Unit == member.Unit, "No supported released-I candidate.");
        var proof = new Proof(source.Request, p, candidate!, hash);
        _ = CampaignCombatArmedContinuationCodec.Serialize(proof);
        return proof;
    }
    private static void Require(bool condition, string message) { if (!condition) throw new JsonException(message); }
}
