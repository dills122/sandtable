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

    private static string CalculateId(string kind, int? operationStage) =>
        $"sha256:{Convert.ToHexStringLower(SHA256.HashData(WriteSemantics(kind, operationStage)))}";
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

public sealed record ActFirstAction : CampaignActionCandidate
{
    internal ActFirstAction(int operationStage) : base("act-first", operationStage) { }
}

public sealed record ActLastAction : CampaignActionCandidate
{
    internal ActLastAction(int operationStage) : base("act-last", operationStage) { }
}
