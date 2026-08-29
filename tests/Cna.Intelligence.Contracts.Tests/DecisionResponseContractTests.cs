using Cna.Intelligence.Contracts.V1;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace Cna.Intelligence.Contracts.Tests;

public sealed class DecisionResponseContractTests
{
    [Fact]
    public void DecisionResponseCarriesProposalValidationCoordinates()
    {
        var response = new DecisionResponse
        {
            DecisionId = "decision-1",
            BasedOnStateVersion = 42,
            RulesetHash = "rules-v1",
            SelectedPlanId = "plan-a",
            Parameters =
            {
                ["axis"] = "north",
                ["tempo"] = "deliberate",
            },
            CommanderCommentary = "Advance on the northern axis.",
            Trace = new ModelTrace
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
            },
        };

        var roundTripped = DecisionResponse.Parser.ParseFrom(response.ToByteArray());

        AssertField("decision_id", 1, FieldType.String);
        AssertField("based_on_state_version", 2, FieldType.Int64);
        AssertField("selected_plan_id", 3, FieldType.String);
        AssertField("parameters", 4, FieldType.Message, isRepeated: true, isMap: true);
        AssertField("commander_commentary", 5, FieldType.String);
        AssertField("trace", 6, FieldType.Message);
        AssertField("ruleset_hash", 7, FieldType.String);

        Assert.Equal("decision-1", roundTripped.DecisionId);
        Assert.Equal(42, roundTripped.BasedOnStateVersion);
        Assert.Equal("rules-v1", roundTripped.RulesetHash);
        Assert.Equal("plan-a", roundTripped.SelectedPlanId);
        Assert.Equal("north", roundTripped.Parameters["axis"]);
        Assert.Equal("deliberate", roundTripped.Parameters["tempo"]);
        Assert.Equal("Advance on the northern axis.", roundTripped.CommanderCommentary);
        Assert.Equal(response.Trace, roundTripped.Trace);
    }

    private static void AssertField(
        string name,
        int number,
        FieldType type,
        bool isRepeated = false,
        bool isMap = false)
    {
        var field = DecisionResponse.Descriptor.FindFieldByName(name);

        Assert.NotNull(field);
        Assert.Equal(number, field.FieldNumber);
        Assert.Equal(type, field.FieldType);
        Assert.Equal(isRepeated, field.IsRepeated);
        Assert.Equal(isMap, field.IsMap);
    }
}
