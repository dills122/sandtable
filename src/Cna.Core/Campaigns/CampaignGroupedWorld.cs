using System.Buffers;
using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignGroupedComponent(string ComponentType, int Toe);

internal sealed record CampaignGroupedMember(string ElementId, string? ParentId, int? InfantryToe,
    int AssignedMotorPoints, bool HistoricalMotorPriority, string? ComponentType, int? ComponentToe,
    int? CapabilityPointAllowance, CapabilityPointAmount CapabilityPointsExpended, int CohesionLevel,
    IReadOnlyList<CampaignGroupedComponent>? AdditionalComponents = null);

internal sealed record CampaignDetachedGroupedMember(string GroupId, string SideId, string LocationId,
    int StackValue, CampaignGroupedMember Member);

internal sealed record CampaignGroupedExternalReservation(string GroupId, string ElementId,
    int InfantryToe, int AssignedMotorPoints);

internal sealed record CampaignWorldOccupant(CampaignMapRepresentationState? Independent,
    CampaignGroupedFormation? Formation, CampaignDetachedGroupedMember? Detached = null);

internal sealed class CampaignGroupedFormation
{
    internal CampaignGroupedFormation(string groupId, string sideId, string rootId, string locationId, int? stackValue,
        int motorPool, IEnumerable<CampaignGroupedMember> members, bool dispositionConfirmed = true)
    {
        GroupId = ContentContractGuards.RequireStableId(groupId, nameof(groupId));
        SideId = ContentContractGuards.RequireStableId(sideId, nameof(sideId));
        RootId = ContentContractGuards.RequireStableId(rootId, nameof(rootId));
        LocationId = ContentContractGuards.RequireStableId(locationId, nameof(locationId));
        if (stackValue < 0 || motorPool < 0) throw new ArgumentOutOfRangeException(nameof(stackValue));
        StackValue = stackValue;
        MotorPool = motorPool;
        DispositionConfirmed = dispositionConfirmed;
        var values = ContentContractGuards.CopyValues(members, nameof(members))
            .Select(value => value with
            {
                AdditionalComponents = value.AdditionalComponents is null ? null
                : Array.AsReadOnly(value.AdditionalComponents.OrderBy(component => component.ComponentType, StringComparer.Ordinal).ToArray())
            })
            .OrderBy(value => value.ElementId, StringComparer.Ordinal).ToArray();
        if (values.Length == 0 || values.Select(value => value.ElementId).Distinct(StringComparer.Ordinal).Count() != values.Length)
            throw new ArgumentException("Formation members must be unique.", nameof(members));
        var byId = values.ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        if (!byId.TryGetValue(rootId, out var root) || root.ParentId is not null)
            throw new ArgumentException("Formation must have one root.", nameof(members));
        foreach (var value in values)
        {
            ContentContractGuards.RequireStableId(value.ElementId, nameof(members));
            ArgumentNullException.ThrowIfNull(value.CapabilityPointsExpended);
            if (value.ComponentType is not null)
                ContentContractGuards.RequireStableId(value.ComponentType, nameof(members));
            if (value.AdditionalComponents is { } additional)
            {
                foreach (var component in additional) ContentContractGuards.RequireStableId(component.ComponentType, nameof(members));
                if (value.ComponentType is null || additional.Any(component => component.Toe < 0
                        || component.ComponentType == value.ComponentType)
                    || additional.Select(component => component.ComponentType).Distinct(StringComparer.Ordinal).Count() != additional.Count)
                    throw new ArgumentException("Additional formation components must have unique known types and nonnegative strength.", nameof(members));
            }
            if (value.InfantryToe < 0 || value.AssignedMotorPoints < 0
                || value.ComponentToe < 0 || value.CapabilityPointAllowance < 0 || value.CohesionLevel > 10
                || (value.CapabilityPointAllowance is int allowance && value.CapabilityPointsExpended > new CapabilityPointAmount(allowance, 1))
                || ((value.ComponentType is null) != (value.ComponentToe is null))
                || (value.InfantryToe is not null && value.InfantryToe != value.ComponentToe)
                || (value.AssignedMotorPoints > 0 && !value.HistoricalMotorPriority)
                || (value.AssignedMotorPoints > 0 && (value.InfantryToe is null || value.AssignedMotorPoints > value.InfantryToe)))
                throw new ArgumentException("Motor reservations require known infantry capacity.", nameof(members));
            var cursor = value;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            while (cursor.ParentId is not null)
            {
                if (!seen.Add(cursor.ElementId) || !byId.TryGetValue(cursor.ParentId, out var parent))
                    throw new ArgumentException("Formation hierarchy must be acyclic and complete.", nameof(members));
                cursor = parent;
            }
            if (cursor.ElementId != rootId) throw new ArgumentException("Member is outside the root hierarchy.", nameof(members));
        }
        if (values.Sum(value => value.AssignedMotorPoints) > motorPool)
            throw new ArgumentException("Motor reservations exceed their pool.", nameof(members));
        Members = Array.AsReadOnly(values);
    }

    internal string GroupId { get; }
    internal string SideId { get; }
    internal string RootId { get; }
    internal string LocationId { get; }
    internal int? StackValue { get; }
    internal int MotorPool { get; }
    internal bool DispositionConfirmed { get; }
    internal IReadOnlyList<CampaignGroupedMember> Members { get; }
}

/// <summary>Versioned authoritative world with grouped counters and retained scheduled world.</summary>
internal sealed class CampaignGroupedWorld
{
    private static readonly SearchValues<char> HashCharacters = SearchValues.Create("0123456789abcdef");
    private CampaignGroupedWorld(CampaignWorldSnapshotV8 prior, string contentHash, IEnumerable<CampaignGroupedFormation> groups,
        IEnumerable<CampaignDetachedGroupedMember>? detached = null,
        IEnumerable<CampaignGroupedExternalReservation>? externalReservations = null)
    {
        Prior = prior ?? throw new ArgumentNullException(nameof(prior));
        if (contentHash is null || !contentHash.StartsWith("sha256:", StringComparison.Ordinal) || contentHash.Length != 71
            || contentHash.AsSpan(7).ContainsAnyExcept(HashCharacters))
            throw new ArgumentException("A source content hash is required.", nameof(contentHash));
        ContentHash = contentHash;
        var values = ContentContractGuards.CopyValues(groups, nameof(groups)).OrderBy(value => value.GroupId, StringComparer.Ordinal).ToArray();
        var ids = values.SelectMany(value => value.Members.Select(member => member.ElementId)).ToArray();
        if (values.Select(value => value.GroupId).Distinct(StringComparer.Ordinal).Count() != values.Length
            || ids.Distinct(StringComparer.Ordinal).Count() != ids.Length
            || ids.Intersect(prior.World.Elements.Select(value => value.ElementId), StringComparer.Ordinal).Any()
            || ids.Intersect(prior.PendingArrivals.SelectMany(value => value.ElementIds), StringComparer.Ordinal).Any())
            throw new ArgumentException("Grouped members must be unique and absent from the retained world and arrivals.", nameof(groups));
        Groups = Array.AsReadOnly(values);
        var independent = ContentContractGuards.CopyValues(detached ?? [], nameof(detached))
            .Select(value => value with
            {
                Member = value.Member with
                {
                    AdditionalComponents = value.Member.AdditionalComponents is null ? null
                : Array.AsReadOnly(value.Member.AdditionalComponents.OrderBy(component => component.ComponentType, StringComparer.Ordinal).ToArray())
                }
            })
            .OrderBy(value => value.Member.ElementId, StringComparer.Ordinal).ToArray();
        var independentIds = independent.Select(value => value.Member.ElementId).ToArray();
        if (independentIds.Distinct(StringComparer.Ordinal).Count() != independentIds.Length
            || independentIds.Intersect(ids, StringComparer.Ordinal).Any()
            || independentIds.Intersect(prior.World.Elements.Select(value => value.ElementId), StringComparer.Ordinal).Any()
            || independentIds.Intersect(prior.PendingArrivals.SelectMany(value => value.ElementIds), StringComparer.Ordinal).Any()
            || independent.Any(value => value.StackValue < 0 || value.Member.ParentId is not null
                || !values.Any(group => group.GroupId == value.GroupId
                    && group.SideId == value.SideId && group.LocationId == value.LocationId)))
            throw new ArgumentException("Detached members must be unique independent occupants of their source formation.", nameof(detached));
        foreach (var value in independent)
        {
            var member = value.Member;
            ContentContractGuards.RequireStableId(member.ElementId, nameof(detached));
            ArgumentNullException.ThrowIfNull(member.CapabilityPointsExpended);
            if (member.ComponentType is not null) ContentContractGuards.RequireStableId(member.ComponentType, nameof(detached));
            if (member.AdditionalComponents is { } additional)
            {
                foreach (var component in additional) ContentContractGuards.RequireStableId(component.ComponentType, nameof(detached));
                if (member.ComponentType is null || additional.Any(component => component.Toe < 0 || component.ComponentType == member.ComponentType)
                    || additional.Select(component => component.ComponentType).Distinct(StringComparer.Ordinal).Count() != additional.Count)
                    throw new ArgumentException("Detached additional components are invalid.", nameof(detached));
            }
            if (member.InfantryToe < 0 || member.ComponentToe < 0 || member.CapabilityPointAllowance < 0
                || member.CohesionLevel > 10 || (member.CapabilityPointAllowance is int allowance
                    && member.CapabilityPointsExpended > new CapabilityPointAmount(allowance, 1))
                || ((member.ComponentType is null) != (member.ComponentToe is null))
                || (member.InfantryToe is not null && member.InfantryToe != member.ComponentToe)
                || member.AssignedMotorPoints < 0 || (member.AssignedMotorPoints > 0
                    && (!member.HistoricalMotorPriority || member.InfantryToe is null
                        || member.AssignedMotorPoints > member.InfantryToe)))
                throw new ArgumentException("Detached member operational state is invalid.", nameof(detached));
        }
        Detached = Array.AsReadOnly(independent);
        var external = ContentContractGuards.CopyValues(externalReservations ?? [], nameof(externalReservations))
            .OrderBy(value => value.ElementId, StringComparer.Ordinal).ToArray();
        if (external.Select(value => value.ElementId).Distinct(StringComparer.Ordinal).Count() != external.Length
            || external.Any(value => value.InfantryToe < 0 || value.AssignedMotorPoints < 0
                || value.AssignedMotorPoints > value.InfantryToe || !values.Any(group => group.GroupId == value.GroupId
                    && prior.World.Elements.Count(element => element.ElementId == value.ElementId && element.CurrentLocationId == group.LocationId
                        && element.Components.Count == 1 && element.Components[0].ComponentId == value.ElementId + ".infantry"
                        && element.Components[0].CurrentToe == value.InfantryToe) == 1
                    && prior.World.Representations.Count(representation => representation.CurrentLocationId == group.LocationId
                        && representation.BindingKind == CampaignMapRepresentationBindingKind.IndependentElement
                        && representation.BoundElementIds.SequenceEqual([value.ElementId])) == 1)))
            throw new ArgumentException("External reservations require unique active independent units and known capacity.", nameof(externalReservations));
        ExternalReservations = Array.AsReadOnly(external);
        foreach (var group in values)
        {
            var reserved = checked(group.Members.Sum(value => value.AssignedMotorPoints)
                + independent.Where(value => value.GroupId == group.GroupId).Sum(value => value.Member.AssignedMotorPoints)
                + external.Where(value => value.GroupId == group.GroupId).Sum(value => value.AssignedMotorPoints));
            if (reserved > group.MotorPool) throw new ArgumentException("Formation motor pool is oversubscribed.", nameof(groups));
        }
    }

    internal CampaignWorldSnapshotV8 Prior { get; }
    internal string ContentHash { get; }
    internal IReadOnlyList<CampaignGroupedFormation> Groups { get; }
    internal IReadOnlyList<CampaignDetachedGroupedMember> Detached { get; }
    internal IReadOnlyList<CampaignGroupedExternalReservation> ExternalReservations { get; }
    internal static CampaignGroupedWorld Create(CampaignWorldSnapshotV8 prior, string contentHash,
        IEnumerable<CampaignGroupedFormation> groups, IEnumerable<CampaignDetachedGroupedMember>? detached = null,
        IEnumerable<CampaignGroupedExternalReservation>? externalReservations = null) => new(prior, contentHash, groups, detached, externalReservations);

    internal CampaignGroupedWorld AddFormation(string successorContentHash, CampaignGroupedFormation formation,
        CampaignDetachedGroupedMember? detached = null, CampaignGroupedExternalReservation? externalReservation = null) =>
        new(Prior, successorContentHash, Groups.Append(formation), detached is null ? Detached : Detached.Append(detached),
            externalReservation is null ? ExternalReservations : ExternalReservations.Append(externalReservation));

    internal CampaignGroupedWorld DetachMember(string groupId, string elementId, int stackValue, int? remainingGroupStackValue)
    {
        var group = Groups.Single(value => value.GroupId == groupId);
        var member = group.Members.Single(value => value.ElementId == elementId);
        if (!group.DispositionConfirmed || member.ParentId is null || member.AssignedMotorPoints != 0
            || group.Members.Any(value => value.ParentId == elementId))
            throw new InvalidOperationException("Detachment requires a confirmed, unreserved leaf member.");
        var replacement = new CampaignGroupedFormation(group.GroupId, group.SideId, group.RootId, group.LocationId,
            remainingGroupStackValue, group.MotorPool, group.Members.Where(value => value.ElementId != elementId), true);
        var individual = new CampaignDetachedGroupedMember(groupId, group.SideId, group.LocationId, stackValue,
            member with { ParentId = null });
        return new(Prior, ContentHash, Groups.Select(value => value.GroupId == groupId ? replacement : value),
            Detached.Append(individual), ExternalReservations);
    }

    internal CampaignGroupedWorld Rebind(CampaignWorldSnapshotV8 successor, int ledgerGameTurn, int ledgerOperationStage)
    {
        ArgumentNullException.ThrowIfNull(successor);
        ArgumentOutOfRangeException.ThrowIfLessThan(ledgerGameTurn, 1);
        if (ledgerOperationStage is < 1 or > 3) throw new ArgumentOutOfRangeException(nameof(ledgerOperationStage));
        var newRepresentations = successor.World.Representations.Except(Prior.World.Representations).ToArray();
        var promoted = new HashSet<string>(StringComparer.Ordinal);
        foreach (var representation in newRepresentations.Where(value => Groups.Any(group => group.LocationId == value.CurrentLocationId)))
        {
            if (representation.BindingKind != CampaignMapRepresentationBindingKind.IndependentElement
                || representation.BoundElementIds.Count != 1)
                throw new InvalidOperationException("An added group-location counter must represent a detached member.");
            var id = representation.BoundElementIds[0];
            var detached = Detached.SingleOrDefault(value => value.Member.ElementId == id
                && value.LocationId == representation.CurrentLocationId)
                ?? throw new InvalidOperationException("An added group-location counter is not a retained detached member.");
            var element = successor.World.Elements.SingleOrDefault(value => value.ElementId == id)
                ?? throw new InvalidOperationException("Detached member state is missing.");
            if (!promoted.Add(id) || element.CurrentLocationId != detached.LocationId
                || element.ReserveStatus != Prior.Initialization.ReserveStatus
                || element.OperationalState.LedgerGameTurn != ledgerGameTurn
                || element.OperationalState.LedgerOperationStage != ledgerOperationStage
                || element.OperationalState.CapabilityPointsExpended != detached.Member.CapabilityPointsExpended
                || element.OperationalState.CohesionLevel != detached.Member.CohesionLevel
                || element.OperationalState.VehicleBreakdownState is not null
                || element.OperationalState.MovementEnded is not null
                || detached.Member.ComponentType != "infantry" || detached.Member.ComponentToe is not int toe
                || detached.Member.AdditionalComponents is { Count: > 0 }
                || element.Components.Count != 1 || element.Components[0].ComponentId != id + ".infantry"
                || element.Components[0].CurrentToe != toe)
                throw new InvalidOperationException("Promoted detached member differs from retained operational state.");
        }
        if (successor.ScenarioId != Prior.ScenarioId || successor.Initialization != Prior.Initialization
            || !Prior.World.Elements.All(successor.World.Elements.Contains)
            || !Prior.World.Representations.All(successor.World.Representations.Contains)
            || !Prior.World.BrokenVehicleLots.SequenceEqual(successor.World.BrokenVehicleLots)
            || !Prior.PendingArrivals.SequenceEqual(successor.PendingArrivals)
            || successor.World.Elements.Except(Prior.World.Elements)
                .Any(value => Groups.Any(group => group.LocationId == value.CurrentLocationId)
                    && !promoted.Contains(value.ElementId))
            || Detached.Any(value => successor.World.Elements.Any(element => element.ElementId == value.Member.ElementId)
                && !promoted.Contains(value.Member.ElementId)))
            throw new InvalidOperationException("Grouped world rebind must preserve its predecessor and occupied group locations.");
        var newExternal = Detached.Where(value => promoted.Contains(value.Member.ElementId) && value.Member.HistoricalMotorPriority)
            .Select(value => new CampaignGroupedExternalReservation(value.GroupId, value.Member.ElementId,
                value.Member.InfantryToe ?? throw new InvalidOperationException("Promoted priority infantry capacity is unknown."),
                value.Member.AssignedMotorPoints));
        return new(successor, ContentHash, Groups, Detached.Where(value => !promoted.Contains(value.Member.ElementId)),
            ExternalReservations.Concat(newExternal));
    }

    internal CampaignGroupedWorld ConfirmDisposition(string groupId, int? confirmedStackValue)
    {
        var group = Groups.Single(value => value.GroupId == groupId);
        if (group.DispositionConfirmed) throw new InvalidOperationException("Formation disposition already confirmed.");
        var confirmed = new CampaignGroupedFormation(group.GroupId, group.SideId, group.RootId, group.LocationId,
            confirmedStackValue, group.MotorPool, group.Members, true);
        return new(Prior, ContentHash, Groups.Select(value => value.GroupId == groupId ? confirmed : value), Detached, ExternalReservations);
    }

    internal int RemainingMotorPoints(string groupId)
    {
        var group = Groups.Single(value => value.GroupId == groupId);
        return checked(group.MotorPool - group.Members.Sum(value => value.AssignedMotorPoints)
            - Detached.Where(value => value.GroupId == groupId).Sum(value => value.Member.AssignedMotorPoints)
            - ExternalReservations.Where(value => value.GroupId == groupId).Sum(value => value.AssignedMotorPoints));
    }

    internal CampaignGroupedWorld AssignMotorization(string groupId, string elementId, int delta)
    {
        ArgumentOutOfRangeException.ThrowIfZero(delta);
        var group = Groups.Single(value => value.GroupId == groupId);
        if (!group.DispositionConfirmed) throw new InvalidOperationException("Formation disposition requires owner confirmation.");
        var member = group.Members.SingleOrDefault(value => value.ElementId == elementId);
        if (member is null)
        {
            var detached = Detached.SingleOrDefault(value => value.GroupId == groupId && value.Member.ElementId == elementId);
            if (detached is not null)
            {
                if (delta > 0 && !detached.Member.HistoricalMotorPriority)
                    throw new InvalidOperationException("Global historical motorization priority remains unresolved.");
                var updatedDetached = detached with { Member = detached.Member with { AssignedMotorPoints = checked(detached.Member.AssignedMotorPoints + delta) } };
                return new(Prior, ContentHash, Groups, Detached.Select(value => value == detached ? updatedDetached : value), ExternalReservations);
            }
            var external = ExternalReservations.Single(value => value.GroupId == groupId && value.ElementId == elementId);
            var updatedExternal = external with { AssignedMotorPoints = checked(external.AssignedMotorPoints + delta) };
            return new(Prior, ContentHash, Groups, Detached, ExternalReservations.Select(value => value == external ? updatedExternal : value));
        }
        if (delta > 0 && !member.HistoricalMotorPriority)
            throw new InvalidOperationException("Global historical motorization priority remains unresolved.");
        var updated = member with { AssignedMotorPoints = checked(member.AssignedMotorPoints + delta) };
        var replacement = new CampaignGroupedFormation(group.GroupId, group.SideId, group.RootId, group.LocationId,
            group.StackValue, group.MotorPool, group.Members.Select(value => value.ElementId == elementId ? updated : value), group.DispositionConfirmed);
        return new(Prior, ContentHash, Groups.Select(value => value.GroupId == groupId ? replacement : value), Detached, ExternalReservations);
    }

    internal IReadOnlyList<CampaignWorldOccupant> OccupantsAt(string locationId) => Array.AsReadOnly(
        Prior.World.Representations.Where(value => value.CurrentLocationId == locationId)
            .Select(value => new CampaignWorldOccupant(value, null))
            .Concat(Groups.Where(value => value.LocationId == locationId)
                .Select(value => new CampaignWorldOccupant(null, value)))
            .Concat(Detached.Where(value => value.LocationId == locationId)
                .Select(value => new CampaignWorldOccupant(null, null, value)))
            .ToArray());

    // The callback must derive existing counter values from retained authoritative content.
    // Unknown stack values propagate rather than being treated as zero.
    internal int? StackAt(string sideId, string locationId, Func<CampaignMapRepresentationState, int?> trustedExistingStack)
    {
        ArgumentNullException.ThrowIfNull(trustedExistingStack);
        var total = 0;
        foreach (var occupant in OccupantsAt(locationId))
        {
            var value = occupant.Independent is { } representation
                ? trustedExistingStack(representation)
                : occupant.Formation is { } formation ? formation.SideId == sideId ? formation.StackValue : 0
                : occupant.Detached!.SideId == sideId ? occupant.Detached.StackValue : 0;
            if (value is null) return null;
            if (value < 0) throw new InvalidOperationException("Stack values cannot be negative.");
            total = checked(total + value.Value);
        }
        return total;
    }

    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 9); writer.WriteString("contentHash", ContentHash);
            writer.WriteString("priorContentHash", Prior.ContentIdentity.Hash); writer.WriteString("scenarioId", Prior.ScenarioId);
            writer.WriteStartObject("initialization");
            writer.WriteNumber("expendedNumerator", Prior.Initialization.CapabilityPointsExpended.Numerator);
            writer.WriteNumber("expendedDenominator", Prior.Initialization.CapabilityPointsExpended.Denominator);
            writer.WriteNumber("cohesion", Prior.Initialization.CohesionLevel);
            writer.WriteString("reserveStatus", Prior.Initialization.ReserveStatus.ToString()); writer.WriteEndObject();
            CampaignV11CanonicalCodec.WriteWorld(writer, "world", Prior.World);
            writer.WriteStartArray("pendingArrivals");
            foreach (var arrival in Prior.PendingArrivals)
            {
                writer.WriteStartObject(); writer.WriteString("groupId", arrival.GroupId); writer.WriteString("sideId", arrival.SideId);
                writer.WriteNumber("gameTurn", arrival.Arrival.GameTurn); writer.WriteNumber("operationStage", arrival.Arrival.OperationStage);
                writer.WriteString("entryAreaId", arrival.EntryAreaId); writer.WriteStartArray("elementIds");
                foreach (var id in arrival.ElementIds) writer.WriteStringValue(id);
                writer.WriteEndArray(); writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteStartArray("groups");
            foreach (var group in Groups)
            {
                writer.WriteStartObject(); writer.WriteString("groupId", group.GroupId); writer.WriteString("sideId", group.SideId); writer.WriteString("rootId", group.RootId);
                writer.WriteString("locationId", group.LocationId); if (group.StackValue is int stack) writer.WriteNumber("stackValue", stack); else writer.WriteNull("stackValue");
                writer.WriteNumber("motorPool", group.MotorPool); writer.WriteBoolean("dispositionConfirmed", group.DispositionConfirmed); writer.WriteStartArray("members");
                foreach (var member in group.Members)
                {
                    writer.WriteStartObject(); writer.WriteString("elementId", member.ElementId);
                    if (member.ParentId is null) writer.WriteNull("parentId"); else writer.WriteString("parentId", member.ParentId);
                    if (member.InfantryToe is int toe) writer.WriteNumber("infantryToe", toe); else writer.WriteNull("infantryToe");
                    writer.WriteNumber("assignedMotorPoints", member.AssignedMotorPoints);
                    writer.WriteBoolean("historicalMotorPriority", member.HistoricalMotorPriority);
                    if (member.ComponentType is null) writer.WriteNull("componentType"); else writer.WriteString("componentType", member.ComponentType);
                    if (member.ComponentToe is int componentToe) writer.WriteNumber("componentToe", componentToe); else writer.WriteNull("componentToe");
                    WriteAdditionalComponents(writer, member.AdditionalComponents);
                    if (member.CapabilityPointAllowance is int allowance) writer.WriteNumber("capabilityPointAllowance", allowance); else writer.WriteNull("capabilityPointAllowance");
                    writer.WriteNumber("expendedNumerator", member.CapabilityPointsExpended.Numerator);
                    writer.WriteNumber("expendedDenominator", member.CapabilityPointsExpended.Denominator);
                    writer.WriteNumber("cohesion", member.CohesionLevel); writer.WriteEndObject();
                }
                writer.WriteEndArray(); writer.WriteEndObject();
            }
            writer.WriteEndArray();
            if (Detached.Count != 0)
            {
                writer.WriteStartArray("detached");
                foreach (var detached in Detached)
                {
                    var member = detached.Member;
                    writer.WriteStartObject(); writer.WriteString("groupId", detached.GroupId); writer.WriteString("sideId", detached.SideId);
                    writer.WriteString("locationId", detached.LocationId); writer.WriteNumber("stackValue", detached.StackValue);
                    writer.WriteString("elementId", member.ElementId);
                    if (member.InfantryToe is int toe) writer.WriteNumber("infantryToe", toe); else writer.WriteNull("infantryToe");
                    writer.WriteNumber("assignedMotorPoints", member.AssignedMotorPoints);
                    writer.WriteBoolean("historicalMotorPriority", member.HistoricalMotorPriority);
                    if (member.ComponentType is null) writer.WriteNull("componentType"); else writer.WriteString("componentType", member.ComponentType);
                    if (member.ComponentToe is int componentToe) writer.WriteNumber("componentToe", componentToe); else writer.WriteNull("componentToe");
                    WriteAdditionalComponents(writer, member.AdditionalComponents);
                    if (member.CapabilityPointAllowance is int allowance) writer.WriteNumber("capabilityPointAllowance", allowance); else writer.WriteNull("capabilityPointAllowance");
                    writer.WriteNumber("expendedNumerator", member.CapabilityPointsExpended.Numerator);
                    writer.WriteNumber("expendedDenominator", member.CapabilityPointsExpended.Denominator);
                    writer.WriteNumber("cohesion", member.CohesionLevel); writer.WriteEndObject();
                }
                writer.WriteEndArray();
            }
            if (ExternalReservations.Count != 0)
            {
                writer.WriteStartArray("externalReservations");
                foreach (var reservation in ExternalReservations)
                {
                    writer.WriteStartObject(); writer.WriteString("groupId", reservation.GroupId);
                    writer.WriteString("elementId", reservation.ElementId); writer.WriteNumber("infantryToe", reservation.InfantryToe);
                    writer.WriteNumber("assignedMotorPoints", reservation.AssignedMotorPoints); writer.WriteEndObject();
                }
                writer.WriteEndArray();
            }
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    private static void WriteAdditionalComponents(Utf8JsonWriter writer, IReadOnlyList<CampaignGroupedComponent>? components)
    {
        if (components is not { Count: > 0 }) return;
        writer.WriteStartArray("additionalComponents");
        foreach (var component in components)
        {
            writer.WriteStartObject(); writer.WriteString("componentType", component.ComponentType);
            writer.WriteNumber("toe", component.Toe); writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }
    internal string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
}
