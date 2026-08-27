using Cna.Intelligence.Contracts.V1;
using Google.Protobuf;

namespace Cna.Intelligence.Contracts.Tests;

public sealed class ModelTraceContractTests
{
    [Fact]
    public void ModelTraceCarriesProviderAndTokenAccountingCoordinates()
    {
        var trace = new ModelTrace
        {
            Provider = "provider-1",
            Model = "model-1",
            PromptTemplateVersion = "prompt-v1",
            PersonaVersion = "persona-v1",
            LatencyMs = 1234,
            InputTokens = 100,
            OutputTokens = 200,
            FallbackReason = "none",
            ResponseHash = "hash-1",
        };

        var roundTripped = ModelTrace.Parser.ParseFrom(trace.ToByteArray());

        Assert.Equal(1, ModelTrace.Descriptor.FindFieldByName("provider").FieldNumber);
        Assert.Equal(2, ModelTrace.Descriptor.FindFieldByName("model").FieldNumber);
        Assert.Equal(
            3, ModelTrace.Descriptor.FindFieldByName("prompt_template_version").FieldNumber);
        Assert.Equal(4, ModelTrace.Descriptor.FindFieldByName("persona_version").FieldNumber);
        Assert.Equal(5, ModelTrace.Descriptor.FindFieldByName("latency_ms").FieldNumber);
        Assert.Equal(6, ModelTrace.Descriptor.FindFieldByName("input_tokens").FieldNumber);
        Assert.Equal(7, ModelTrace.Descriptor.FindFieldByName("output_tokens").FieldNumber);
        Assert.Equal(8, ModelTrace.Descriptor.FindFieldByName("fallback_reason").FieldNumber);
        Assert.Equal(9, ModelTrace.Descriptor.FindFieldByName("response_hash").FieldNumber);

        Assert.Equal("provider-1", roundTripped.Provider);
        Assert.Equal("model-1", roundTripped.Model);
        Assert.Equal("prompt-v1", roundTripped.PromptTemplateVersion);
        Assert.Equal("persona-v1", roundTripped.PersonaVersion);
        Assert.Equal(1234, roundTripped.LatencyMs);
        Assert.Equal(100, roundTripped.InputTokens);
        Assert.Equal(200, roundTripped.OutputTokens);
        Assert.Equal("none", roundTripped.FallbackReason);
        Assert.Equal("hash-1", roundTripped.ResponseHash);
    }
}
