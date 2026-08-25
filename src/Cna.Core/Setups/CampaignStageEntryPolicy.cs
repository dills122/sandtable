using System.Collections.ObjectModel;
using Cna.Core.Rules;

namespace Cna.Core.Setups;

internal enum StageEntryObligationKind
{
    ExplicitNone = 1,
    HasObligations = 2,
}
internal sealed record CampaignStageEntryPolicy
{
    public const int CurrentContractVersion = 1;

    internal static RuleReference SourceReference { get; } = new(
        "sandtable-rules-lab",
        "stage-entry.no-obligations.v1");

    public CampaignStageEntryPolicy(
        int contractVersion,
        int gameTurn,
        int operationStage,
        StageEntryObligationKind organization,
        StageEntryObligationKind navalConvoyArrival,
        StageEntryObligationKind fleetAssignment,
        StageEntryObligationKind fleetRepair,
        IReadOnlyList<RuleReference> sources)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(
            contractVersion,
            CurrentContractVersion);
        ArgumentOutOfRangeException.ThrowIfLessThan(gameTurn, 1);
        ArgumentOutOfRangeException.ThrowIfNotEqual(operationStage, 1);
        RequireDefined(organization, nameof(organization));
        RequireDefined(navalConvoyArrival, nameof(navalConvoyArrival));
        RequireDefined(fleetAssignment, nameof(fleetAssignment));
        RequireDefined(fleetRepair, nameof(fleetRepair));

        ContractVersion = contractVersion;
        GameTurn = gameTurn;
        OperationStage = operationStage;
        Organization = organization;
        NavalConvoyArrival = navalConvoyArrival;
        FleetAssignment = fleetAssignment;
        FleetRepair = fleetRepair;
        Sources = CopySources(sources);
    }

    public int ContractVersion { get; }

    public int GameTurn { get; }

    public int OperationStage { get; }

    public StageEntryObligationKind Organization { get; }

    public StageEntryObligationKind NavalConvoyArrival { get; }

    public StageEntryObligationKind FleetAssignment { get; }

    public StageEntryObligationKind FleetRepair { get; }

    public IReadOnlyList<RuleReference> Sources { get; }

    public bool Equals(CampaignStageEntryPolicy? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && ContractVersion == other.ContractVersion
            && GameTurn == other.GameTurn
            && OperationStage == other.OperationStage
            && Organization == other.Organization
            && NavalConvoyArrival == other.NavalConvoyArrival
            && FleetAssignment == other.FleetAssignment
            && FleetRepair == other.FleetRepair
            && Sources.SequenceEqual(other.Sources));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ContractVersion);
        hash.Add(GameTurn);
        hash.Add(OperationStage);
        hash.Add(Organization);
        hash.Add(NavalConvoyArrival);
        hash.Add(FleetAssignment);
        hash.Add(FleetRepair);
        foreach (var source in Sources) hash.Add(source);
        return hash.ToHashCode();
    }

    private static void RequireDefined(
        StageEntryObligationKind kind,
        string parameterName)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static ReadOnlyCollection<RuleReference> CopySources(
        IReadOnlyList<RuleReference> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);
        var copy = sources.ToArray();

        if (copy.Length != 1 || copy[0] is null || copy[0] != SourceReference)
        {
            throw new ArgumentException(
                "The exact Stage Entry source reference is required.",
                nameof(sources));
        }

        return Array.AsReadOnly(copy);
    }
}
