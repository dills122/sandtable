using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
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

internal enum CampaignContactObserverKind
{
    Formation,
    IndependentCounter,
}

// Core emits these transitions from adjudicated sight, combat, and sharing events.
// The bound representation is private authority data; only CampaignContactView leaves Core.
internal sealed record CampaignContactTransition(
    int ContractVersion,
    string CampaignId,
    LandSide Side,
    long AuthorityVersion,
    string SourceEventId,
    string SourceRuleId,
    CampaignContactTransitionKind Kind,
    string ContactHandle,
    string BoundRepresentationId,
    string ObserverFormationId,
    string ApparentLocationId,
    CampaignContactIdentification Identification,
    CampaignContactAccess Access,
    CampaignContactObserverKind ObserverKind = CampaignContactObserverKind.Formation);

internal sealed record CampaignContact(
    string Handle,
    string BoundRepresentationId,
    string ObserverFormationId,
    string ApparentLocationId,
    CampaignContactIdentification Identification,
    CampaignContactAccess Access,
    bool IsCurrent,
    CampaignContactObserverKind ObserverKind = CampaignContactObserverKind.Formation);

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

internal sealed record CampaignIndependentContactView(string Handle, string ObserverCounterId,
    string ApparentLocationId, CampaignContactIdentification Identification, bool IsCurrent);

internal sealed class CampaignContactKnowledge
{
    public const int CurrentContractVersion = 1;
    public const int IndependentObserverContractVersion = 2;

    private ImmutableDictionary<string, CampaignContact> contacts =
        ImmutableDictionary.Create<string, CampaignContact>(StringComparer.Ordinal);
    private ImmutableHashSet<(string SourceEventId, string ContactHandle)> appliedSources =
        ImmutableHashSet<(string SourceEventId, string ContactHandle)>.Empty;
    private ImmutableDictionary<(CampaignContactObserverKind Kind, string Id), long> observerRevisions =
        ImmutableDictionary<(CampaignContactObserverKind Kind, string Id), long>.Empty;
    private long sideSharedRevision;

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

    public string NextHandle(string observerFormationId)
    {
        RequireId(observerFormationId);
        var scope = SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{CampaignId}\0{Side}\0{observerFormationId}"));
        var sequence = contacts.Values.Count(value =>
            value.ObserverKind == CampaignContactObserverKind.Formation
            && string.Equals(value.ObserverFormationId, observerFormationId, StringComparison.Ordinal)) + 1;
        return $"c-{Convert.ToHexString(scope.AsSpan(0, 6)).ToLowerInvariant()}-{sequence:D8}";
    }

    public long AudienceRevisionFor(string actingFormationId)
    {
        RequireId(actingFormationId);
        observerRevisions.TryGetValue((CampaignContactObserverKind.Formation, actingFormationId), out var local);
        return checked(sideSharedRevision + local);
    }

    internal string NextIndependentCounterHandle(string counterId)
    {
        RequireId(counterId);
        var scope = SHA256.HashData(Encoding.UTF8.GetBytes($"{CampaignId}\0{Side}\0independent-counter\0{counterId}"));
        var sequence = contacts.Values.Count(value => value.ObserverKind == CampaignContactObserverKind.IndependentCounter
            && value.ObserverFormationId == counterId) + 1;
        return $"c-{Convert.ToHexString(scope.AsSpan(0, 6)).ToLowerInvariant()}-{sequence:D8}";
    }

    internal long AudienceRevisionForCounter(string counterId)
    {
        RequireId(counterId);
        observerRevisions.TryGetValue((CampaignContactObserverKind.IndependentCounter, counterId), out var local);
        // Independent source observers do not yet consume side-shared contacts.
        return local;
    }

    internal IReadOnlyList<CampaignIndependentContactView> ObserveForCounter(string counterId)
    {
        RequireId(counterId);
        return contacts.Values.Where(value => value.ObserverKind == CampaignContactObserverKind.IndependentCounter
                && value.ObserverFormationId == counterId)
            .OrderBy(value => value.Handle, StringComparer.Ordinal)
            .Select(value => new CampaignIndependentContactView(value.Handle, counterId,
                value.ApparentLocationId, value.Identification, value.IsCurrent)).ToArray();
    }

    internal CampaignContactKnowledge Copy()
    {
        // Each writable cut owns its roots; immutable entries and unchanged branches are shared.
        return new CampaignContactKnowledge(CampaignId, Side)
        {
            AudienceRevision = AudienceRevision,
            LastAuthorityVersion = LastAuthorityVersion,
            contacts = contacts,
            appliedSources = appliedSources,
            observerRevisions = observerRevisions,
            sideSharedRevision = sideSharedRevision,
        };
    }

    internal CampaignContact? FindPrivateContact(string boundRepresentationId, string observerFormationId) =>
        FindPrivateContact(boundRepresentationId, observerFormationId, CampaignContactObserverKind.Formation);

    internal CampaignContact? FindPrivateContact(string boundRepresentationId, string observerId, CampaignContactObserverKind kind) =>
        contacts.Values.SingleOrDefault(contact =>
            string.Equals(contact.BoundRepresentationId, boundRepresentationId, StringComparison.Ordinal)
            && contact.ObserverKind == kind && string.Equals(contact.ObserverFormationId, observerId, StringComparison.Ordinal));

    public IReadOnlyList<CampaignContactView> ObserveFor(string actingFormationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actingFormationId);
        return contacts.Values
            .Where(contact => contact.ObserverKind == CampaignContactObserverKind.Formation
                && (contact.Access == CampaignContactAccess.Side
                    || string.Equals(contact.ObserverFormationId, actingFormationId, StringComparison.Ordinal)))
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
            || contact.ObserverKind != CampaignContactObserverKind.Formation
            || (contact.Access != CampaignContactAccess.Side
                && !string.Equals(contact.ObserverFormationId, actingFormationId, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("Contact is unavailable to this formation.");
        }

        return contact.BoundRepresentationId;
    }

    public void Apply(CampaignContactTransition transition)
    {
        ArgumentNullException.ThrowIfNull(transition);
        if ((transition.ObserverKind == CampaignContactObserverKind.Formation && transition.ContractVersion != CurrentContractVersion)
            || (transition.ObserverKind == CampaignContactObserverKind.IndependentCounter
                && (transition.ContractVersion != IndependentObserverContractVersion || transition.Access != CampaignContactAccess.Observer))
            || !Enum.IsDefined(transition.ObserverKind)
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
        RequireId(transition.BoundRepresentationId);
        RequireId(transition.ObserverFormationId);
        RequireId(transition.ApparentLocationId);
        var sourceKey = (transition.SourceEventId, transition.ContactHandle);
        if (appliedSources.Contains(sourceKey))
            throw new ArgumentException("Contact source event was already applied for this contact.", nameof(transition));

        var exists = contacts.TryGetValue(transition.ContactHandle, out var prior);
        if (transition.Kind == CampaignContactTransitionKind.Observed)
        {
            if (exists || !string.Equals(transition.ContactHandle,
                transition.ObserverKind == CampaignContactObserverKind.Formation
                    ? NextHandle(transition.ObserverFormationId) : NextIndependentCounterHandle(transition.ObserverFormationId), StringComparison.Ordinal))
                throw new ArgumentException("New contact must use next side-scoped handle.", nameof(transition));
            if (FindPrivateContact(transition.BoundRepresentationId, transition.ObserverFormationId, transition.ObserverKind) is not null)
                throw new ArgumentException("Observer already has a handle for this private contact.", nameof(transition));
        }
        else if (!exists
            || !string.Equals(prior!.BoundRepresentationId, transition.BoundRepresentationId, StringComparison.Ordinal)
            || !string.Equals(prior.ObserverFormationId, transition.ObserverFormationId, StringComparison.Ordinal)
            || prior.ObserverKind != transition.ObserverKind)
        {
            throw new ArgumentException("Contact transition does not match its private binding and observer.", nameof(transition));
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
            _ => new CampaignContact(transition.ContactHandle, transition.BoundRepresentationId,
                transition.ObserverFormationId, transition.ApparentLocationId,
                transition.Identification, transition.Access, true, transition.ObserverKind),
        };
        if (prior is null || Visible(prior) != Visible(next))
        {
            AudienceRevision = checked(AudienceRevision + 1);
            if (prior?.Access == CampaignContactAccess.Side || next.Access == CampaignContactAccess.Side)
                sideSharedRevision = checked(sideSharedRevision + 1);
            else
            {
                IncrementObserver(next.ObserverKind, next.ObserverFormationId);
                if (prior is not null && prior.ObserverFormationId != next.ObserverFormationId)
                    IncrementObserver(prior.ObserverKind, prior.ObserverFormationId);
            }
        }
        contacts = contacts.SetItem(transition.ContactHandle, next);
        appliedSources = appliedSources.Add(sourceKey);
        LastAuthorityVersion = transition.AuthorityVersion;
    }

    private static CampaignContactView Visible(CampaignContact contact) => new(
        contact.Handle, contact.ObserverFormationId, contact.ApparentLocationId,
        contact.Identification, contact.Access, contact.IsCurrent);

    private void IncrementObserver(CampaignContactObserverKind kind, string observerId)
    {
        observerRevisions.TryGetValue((kind, observerId), out var prior);
        observerRevisions = observerRevisions.SetItem((kind, observerId), checked(prior + 1));
    }

    private static void RequireId(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Contact identity must be nonempty.");
    }
}
