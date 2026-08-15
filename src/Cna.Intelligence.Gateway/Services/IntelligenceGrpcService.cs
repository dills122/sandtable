using Cna.Intelligence.Contracts.V1;
using Grpc.Core;

namespace Cna.Intelligence.Gateway.Services;

internal sealed class IntelligenceGrpcService : IntelligenceService.IntelligenceServiceBase
{
    private static readonly Status ProviderUnavailable = new(
        StatusCode.Unavailable,
        "No intelligence provider is configured. Use the deterministic scripted fallback.");

    public override Task<DecisionResponse> ChoosePlan(
        DecisionRequest request,
        ServerCallContext context) =>
        Task.FromException<DecisionResponse>(new RpcException(ProviderUnavailable));

    public override Task GenerateNarrative(
        NarrativeRequest request,
        IServerStreamWriter<NarrativeChunk> responseStream,
        ServerCallContext context) =>
        Task.FromException(new RpcException(ProviderUnavailable));

    public override Task<CapabilitiesResponse> GetCapabilities(
        CapabilitiesRequest request,
        ServerCallContext context) =>
        Task.FromResult(new CapabilitiesResponse
        {
            DecisionProviderAvailable = false,
            NarrativeProviderAvailable = false,
        });
}
