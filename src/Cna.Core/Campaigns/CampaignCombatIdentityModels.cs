using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>Current identity binding only; constructing this value grants no combat eligibility.</summary>
internal sealed class CampaignCombatParticipant
{
    internal CampaignCombatParticipant(CampaignCombatUnitKey unit, string representationId, string locationId,
        IReadOnlyList<string> componentIds)
    {
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(componentIds);
        var count = componentIds.Count;
        if (count is < 1 or > 512) throw new JsonException("Participant component count is outside bounds.");
        foreach (var id in new[] { unit.CreationBinding, unit.OriginalSide, unit.ElementId, representationId, locationId }) CheckId(id);
        var components = new string[count];
        for (var index = 0; index < components.Length; index++)
        {
            components[index] = componentIds[index];
            CheckId(components[index]);
        }
        if (components.Distinct(StringComparer.Ordinal).Count() != components.Length)
            throw new JsonException("Duplicate participant component binding.");
        Unit = unit; RepresentationId = representationId; LocationId = locationId;
        ComponentIds = Array.AsReadOnly(components);
        Components = Array.AsReadOnly(components.Select(id => new CampaignCombatComponentKey(unit, id)).ToArray());
    }
    public CampaignCombatUnitKey Unit { get; }
    public string RepresentationId { get; }
    public string LocationId { get; }
    public IReadOnlyList<string> ComponentIds { get; }
    public IReadOnlyList<CampaignCombatComponentKey> Components { get; }
    private static void CheckId(string value)
    {
        if (value is null || value.Length is < 1 or > 128) throw new JsonException("Participant ID exceeds bounds.");
        try { _ = ContentContractGuards.RequireStableId(value, nameof(value)); }
        catch (ArgumentException error) { throw new JsonException("Invalid participant ID.", error); }
    }
}

/// <summary>Frozen inherited-selection-v1 predicates at its supported empty G2 cuts.</summary>
internal sealed record CampaignCombatCandidateAssessment(CampaignCombatParticipant Acting, CampaignCombatParticipant Defending,
    bool NormalWeather, bool Adjacent, CapabilityPointAmount ActingCapabilityPointsExpended,
    CapabilityPointAmount DefendingCapabilityPointsExpended, bool ActingWithinVoluntaryCeiling, bool DefendingWithinVoluntaryCeiling)
{
    public IReadOnlyList<string> CandidateIds { get; } = Array.Empty<string>();
}

internal sealed class CampaignCombatAdmissionBoundary
{
    internal CampaignCombatAdmissionBoundary(CampaignCombatRetainedHistory history,
        CampaignCombatBreakdownCompletionState entry, CampaignCombatCandidateAssessment assessment)
    { History = history; Entry = entry; Assessment = assessment; }
    public CampaignCombatRetainedHistory History { get; }
    public CampaignCombatBreakdownCompletionState Entry { get; }
    public CampaignCombatCandidateAssessment Assessment { get; }
}
