using Cna.Core.Rules;

namespace Cna.Core.Visibility;

internal enum CampaignContactTransitionKind
{
    Observed,
    Updated,
    Lost,
    Rediscovered,
}

internal enum CampaignContactIdentification
{
    Presence,
    FormationType,
    Identified,
}

internal enum CampaignContactAccess
{
    Observer,
    Side,
}

// Core emits these transitions from adjudicated sight, combat, and sharing events.
// The bound element is private authority data; only CampaignContactView leaves Core.
internal sealed record CampaignContactTransition(
    int ContractVersion,
    string CampaignId,
    LandSide Side,
    long AuthorityVersion,
    string SourceEventId,
    string SourceRuleId,
    CampaignContactTransitionKind Kind,
    string ContactHandle,
    string BoundElementId,
    string ObserverFormationId,
    string ApparentLocationId,
    CampaignContactIdentification Identification,
    CampaignContactAccess Access);

internal sealed record CampaignContact(
    string Handle,
    string BoundElementId,
    string ObserverFormationId,
    string ApparentLocationId,
    CampaignContactIdentification Identification,
    CampaignContactAccess Access,
    bool IsCurrent);

internal sealed record CampaignContactView(
    string Handle,
    string ObserverFormationId,
    string ApparentLocationId,
    CampaignContactIdentification Identification,
    CampaignContactAccess Access,
    bool IsCurrent);

internal sealed record CampaignContactTargetCandidate(
    string ActingFormationId,
    string ContactHandle,
    string ApparentLocationId);

internal sealed class CampaignContactKnowledge
{
    public const int CurrentContractVersion = 1;

    private readonly Dictionary<string, CampaignContact> contacts = new(StringComparer.Ordinal);
    private readonly HashSet<(string SourceEventId, string ContactHandle)> appliedSources = [];

    public CampaignContactKnowledge(string campaignId, LandSide side)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(campaignId);
        if (!Enum.IsDefined(side)) throw new ArgumentOutOfRangeException(nameof(side));
        CampaignId = campaignId;
        Side = side;
    }

    public string CampaignId { get; }
    public LandSide Side { get; }
    public long AudienceRevision { get; private set; }
    public long LastAuthorityVersion { get; private set; }

    public string NextHandle() => $"c-{contacts.Count + 1:D8}";

    internal CampaignContactKnowledge Copy()
    {
        var copy = new CampaignContactKnowledge(CampaignId, Side)
        {
            AudienceRevision = AudienceRevision,
            LastAuthorityVersion = LastAuthorityVersion,
        };
        foreach (var (handle, contact) in contacts) copy.contacts.Add(handle, contact);
        foreach (var source in appliedSources) copy.appliedSources.Add(source);
        return copy;
    }

    internal CampaignContact? FindPrivateContact(string boundElementId, string observerFormationId) =>
        contacts.Values.SingleOrDefault(contact =>
            string.Equals(contact.BoundElementId, boundElementId, StringComparison.Ordinal)
            && string.Equals(contact.ObserverFormationId, observerFormationId, StringComparison.Ordinal));

    public IReadOnlyList<CampaignContactView> ObserveFor(string actingFormationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actingFormationId);
        return contacts.Values
            .Where(contact => contact.Access == CampaignContactAccess.Side
                || string.Equals(contact.ObserverFormationId, actingFormationId, StringComparison.Ordinal))
            .OrderBy(contact => contact.Handle, StringComparer.Ordinal)
            .Select(contact => new CampaignContactView(contact.Handle, contact.ObserverFormationId,
                contact.ApparentLocationId, contact.Identification, contact.Access, contact.IsCurrent))
            .ToArray();
    }

    // These are visibility-qualified targets. Combat resource and obligation rules
    // still decide which of them become legal player actions.
    public IReadOnlyList<CampaignContactTargetCandidate> CurrentTargetsFor(string actingFormationId) =>
        ObserveFor(actingFormationId)
            .Where(contact => contact.IsCurrent)
            .Select(contact => new CampaignContactTargetCandidate(
                actingFormationId, contact.Handle, contact.ApparentLocationId))
            .ToArray();

    public string ResolvePrivateTarget(string actingFormationId, string contactHandle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actingFormationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(contactHandle);
        if (!contacts.TryGetValue(contactHandle, out var contact)
            || !contact.IsCurrent
            || (contact.Access != CampaignContactAccess.Side
                && !string.Equals(contact.ObserverFormationId, actingFormationId, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("Contact is unavailable to this formation.");
        }

        return contact.BoundElementId;
    }

    public void Apply(CampaignContactTransition transition)
    {
        ArgumentNullException.ThrowIfNull(transition);
        if (transition.ContractVersion != CurrentContractVersion
            || !string.Equals(transition.CampaignId, CampaignId, StringComparison.Ordinal)
            || transition.Side != Side
            || transition.AuthorityVersion < 1
            || transition.AuthorityVersion < LastAuthorityVersion
            || !Enum.IsDefined(transition.Kind)
            || !Enum.IsDefined(transition.Identification)
            || !Enum.IsDefined(transition.Access))
        {
            throw new ArgumentException("Invalid contact transition authority.", nameof(transition));
        }

        RequireId(transition.SourceEventId);
        RequireId(transition.SourceRuleId);
        RequireId(transition.ContactHandle);
        RequireId(transition.BoundElementId);
        RequireId(transition.ObserverFormationId);
        RequireId(transition.ApparentLocationId);
        var sourceKey = (transition.SourceEventId, transition.ContactHandle);
        if (appliedSources.Contains(sourceKey))
            throw new ArgumentException("Contact source event was already applied for this contact.", nameof(transition));

        var exists = contacts.TryGetValue(transition.ContactHandle, out var prior);
        if (transition.Kind == CampaignContactTransitionKind.Observed)
        {
            if (exists || !string.Equals(transition.ContactHandle, NextHandle(), StringComparison.Ordinal))
                throw new ArgumentException("New contact must use next side-scoped handle.", nameof(transition));
            if (FindPrivateContact(transition.BoundElementId, transition.ObserverFormationId) is not null)
                throw new ArgumentException("Observer already has a handle for this private contact.", nameof(transition));
        }
        else if (!exists || !string.Equals(prior!.BoundElementId, transition.BoundElementId, StringComparison.Ordinal))
        {
            throw new ArgumentException("Contact transition does not match its private binding.", nameof(transition));
        }

        if (transition.Kind == CampaignContactTransitionKind.Lost && !prior!.IsCurrent)
            throw new ArgumentException("Contact is already stale.", nameof(transition));
        if (transition.Kind == CampaignContactTransitionKind.Updated && !prior!.IsCurrent)
            throw new ArgumentException("Stale contact requires a new observation.", nameof(transition));
        if (transition.Kind == CampaignContactTransitionKind.Rediscovered && prior!.IsCurrent)
            throw new ArgumentException("Only a stale contact can be rediscovered.", nameof(transition));
        if (transition.Kind == CampaignContactTransitionKind.Lost
            && (!string.Equals(transition.ObserverFormationId, prior!.ObserverFormationId, StringComparison.Ordinal)
                || !string.Equals(transition.ApparentLocationId, prior.ApparentLocationId, StringComparison.Ordinal)
                || transition.Identification != prior.Identification
                || transition.Access != prior.Access))
            throw new ArgumentException("Loss must retain the last visible contact facts.", nameof(transition));

        var next = transition.Kind switch
        {
            CampaignContactTransitionKind.Lost => prior! with { IsCurrent = false },
            _ => new CampaignContact(transition.ContactHandle, transition.BoundElementId,
                transition.ObserverFormationId, transition.ApparentLocationId,
                transition.Identification, transition.Access, true),
        };
        if (prior is null || Visible(prior) != Visible(next))
            AudienceRevision = checked(AudienceRevision + 1);
        contacts[transition.ContactHandle] = next;
        appliedSources.Add(sourceKey);
        LastAuthorityVersion = transition.AuthorityVersion;
    }

    private static CampaignContactView Visible(CampaignContact contact) => new(
        contact.Handle, contact.ObserverFormationId, contact.ApparentLocationId,
        contact.Identification, contact.Access, contact.IsCurrent);

    private static void RequireId(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Contact identity must be nonempty.");
    }
}
