namespace Cna.Intelligence.Gateway.Services;

/// <summary>
/// Single source of truth for whether a model-backed intelligence provider is
/// configured for this gateway. Consulted both by the root status endpoint and by
/// <see cref="IntelligenceGrpcService.GetCapabilities"/> so the two never disagree.
/// </summary>
public interface IIntelligenceProviderStatus
{
    bool DecisionProviderAvailable { get; }

    bool NarrativeProviderAvailable { get; }

    /// <summary>True if any provider is configured, decision or narrative.</summary>
    bool AnyProviderAvailable => DecisionProviderAvailable || NarrativeProviderAvailable;
}

/// <summary>
/// No model-backed provider is wired up yet; the gateway is scaffolding that always
/// falls back to the deterministic scripted behavior in <see cref="IntelligenceGrpcService"/>.
/// </summary>
internal sealed class NoIntelligenceProviderStatus : IIntelligenceProviderStatus
{
    public bool DecisionProviderAvailable => false;

    public bool NarrativeProviderAvailable => false;
}
