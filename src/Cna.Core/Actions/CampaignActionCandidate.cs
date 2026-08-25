using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Actions;

public abstract record CampaignActionCandidate
{
    public const int CurrentContractVersion = 1;

    internal CampaignActionCandidate(string kind, int? operationStage = null)
    {
        Kind = ContentContractGuards.RequireStableId(kind, nameof(kind));
        if (operationStage is not null and not 1)
        {
            throw new ArgumentOutOfRangeException(nameof(operationStage));
        }
        ContractVersion = CurrentContractVersion;
        OperationStage = operationStage;
        ActionId = CalculateId(kind, operationStage);
    }

    internal CampaignActionCandidate(string kind, string elementId)
    {
        Kind = ContentContractGuards.RequireStableId(kind, nameof(kind));
        ContractVersion = CurrentContractVersion;
        ActionId = CalculateId(WriteSubjectSemantics(kind, elementId));
    }

    public int ContractVersion { get; }
    public string ActionId { get; }
    public string Kind { get; }
    public int? OperationStage { get; }

    internal static byte[] WriteSemantics(string kind, int? operationStage)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CurrentContractVersion);
            writer.WriteString("kind", kind);
            if (operationStage is not null) writer.WriteNumber("operationStage", operationStage.Value);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static byte[] WriteSubjectSemantics(string kind, string elementId)
    {
        _ = ContentContractGuards.RequireStableId(kind, nameof(kind));
        var stableElementId = ContentContractGuards.RequireStableId(
            elementId,
            nameof(elementId));
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CurrentContractVersion);
            writer.WriteString("kind", kind);
            writer.WriteString("elementId", stableElementId);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    private static string CalculateId(string kind, int? operationStage) =>
        CalculateId(WriteSemantics(kind, operationStage));

    private static string CalculateId(byte[] semantics) =>
        $"sha256:{Convert.ToHexStringLower(SHA256.HashData(semantics))}";
}

public sealed record ResolveInitiativeAction : CampaignActionCandidate
{
    internal ResolveInitiativeAction() : base("resolve-initiative") { }
}

public sealed record ResolveNoObligationNavalConvoyScheduleAction : CampaignActionCandidate
{
    internal ResolveNoObligationNavalConvoyScheduleAction()
        : base("resolve-no-obligation-naval-convoy-schedule") { }
}

public sealed record ResolveNoObligationTacticalShippingAction : CampaignActionCandidate
{
    internal ResolveNoObligationTacticalShippingAction()
        : base("resolve-no-obligation-tactical-shipping") { }
}

public sealed record ResolveWeatherAction : CampaignActionCandidate
{
    internal ResolveWeatherAction() : base("resolve-weather") { }
}

public sealed record ResolveNoObligationOrganizationAction : CampaignActionCandidate
{
    internal ResolveNoObligationOrganizationAction()
        : base("resolve-no-obligation-organization") { }
}

public sealed record ResolveNoObligationNavalConvoyArrivalAction : CampaignActionCandidate
{
    internal ResolveNoObligationNavalConvoyArrivalAction()
        : base("resolve-no-obligation-naval-convoy-arrival") { }
}

public sealed record ResolveNoObligationFleetAssignmentAction : CampaignActionCandidate
{
    internal ResolveNoObligationFleetAssignmentAction()
        : base("resolve-no-obligation-fleet-assignment") { }
}

public sealed record ResolveNoObligationFleetRepairAction : CampaignActionCandidate
{
    internal ResolveNoObligationFleetRepairAction()
        : base("resolve-no-obligation-fleet-repair") { }
}

public sealed record ActFirstAction : CampaignActionCandidate
{
    internal ActFirstAction(int operationStage) : base("act-first", operationStage) { }
}

public sealed record ActLastAction : CampaignActionCandidate
{
    internal ActLastAction(int operationStage) : base("act-last", operationStage) { }
}

public sealed record DesignateReserveAction : CampaignActionCandidate
{
    internal DesignateReserveAction(string elementId)
        : base("designate-reserve", elementId)
    {
        ElementId = ContentContractGuards.RequireStableId(elementId, nameof(elementId));
    }

    public string ElementId { get; }
}

public sealed record CompleteReserveDesignationAction : CampaignActionCandidate
{
    internal CompleteReserveDesignationAction()
        : base("complete-reserve-designation") { }
}
