using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignCombatWeatherCommand(int ContractVersion, string Kind,
    string CreationBinding, string CreationEventHash, long ExpectedPriorVersion, string ExpectedPositionId);

/// <summary>Actor is trusted submission metadata; recorded actor bytes do not authenticate a caller.</summary>
internal sealed record CampaignCombatWeatherInput(CampaignCombatWeatherCommand Command,
    CampaignOpeningPreambleActor Actor);

internal sealed record CampaignCombatWeatherEvent(CampaignCombatWeatherState Before,
    CampaignCombatWeatherInput Input, CampaignOperationStageWeather Weather, RandomStreamState RandomState,
    LandSequencePosition SequencePosition, IReadOnlyList<RuleReference> Sources)
{
    public string ReceiptId => "wth." + CampaignOpeningPreambleCodec.HashWithDomain(
        "sandtable.combat.weather-receipt.v1", CampaignCombatWeatherCodec.SerializeEvent(this, false))[7..];
}

/// <summary>Private creation-rooted Weather projection, not an independently admissible snapshot.</summary>
internal sealed class CampaignCombatWeatherState
{
    internal CampaignCombatWeatherState(CampaignOpeningPreambleState opening, long stateVersion,
        string prefix, LandSequencePosition position, RandomStreamState randomState,
        IEnumerable<CampaignOperationStageWeather> weather,
        IEnumerable<CampaignOpeningPreambleReceipt> receipts, CampaignCombatWeatherEvent? acceptedEvent)
    {
        Opening = opening;
        StateVersion = stateVersion;
        Prefix = prefix;
        SequencePosition = position;
        RandomState = randomState;
        Weather = Array.AsReadOnly(weather.ToArray());
        Receipts = Array.AsReadOnly(receipts.ToArray());
        AcceptedEvent = acceptedEvent;
    }
    public CampaignOpeningPreambleState Opening { get; }
    public long StateVersion { get; }
    public string Prefix { get; }
    public LandSequencePosition SequencePosition { get; }
    public RandomStreamState RandomState { get; }
    public IReadOnlyList<CampaignOperationStageWeather> Weather { get; }
    public IReadOnlyList<CampaignOpeningPreambleReceipt> Receipts { get; }
    internal CampaignCombatWeatherEvent? AcceptedEvent { get; }
}

internal sealed record CampaignCombatWeatherResult(CampaignCombatWeatherState State,
    byte[] EventBytes, bool Duplicate);
