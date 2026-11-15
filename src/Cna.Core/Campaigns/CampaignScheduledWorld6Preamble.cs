using System.Security.Cryptography;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Campaigns;

internal sealed record CampaignScheduledWorld6PreambleStep(long ResultVersion, string PositionId,
    string ReferenceEventHash, string NativeEventBase64);

internal sealed class CampaignScheduledWorld6PreambleCheckpoint
{
    internal CampaignScheduledWorld6PreambleCheckpoint(long stateVersion, LandSequencePosition position,
        CampaignWorldSnapshotV6 pendingWorld, RandomStreamState randomState, LandSide? initiativeHolder,
        IEnumerable<CampaignOperationStageOrder> orders, IEnumerable<CampaignOperationStageWeather> weather)
    {
        StateVersion = stateVersion;
        Position = position;
        World = pendingWorld;
        RandomState = randomState;
        InitiativeHolder = initiativeHolder;
        Orders = Array.AsReadOnly(orders.ToArray());
        Weather = Array.AsReadOnly(weather.ToArray());
    }

    public long StateVersion { get; }
    public LandSequencePosition Position { get; }
    public CampaignWorldSnapshotV6 World { get; }
    public RandomStreamState RandomState { get; }
    public LandSide? InitiativeHolder { get; }
    public IReadOnlyList<CampaignOperationStageOrder> Orders { get; }
    public IReadOnlyList<CampaignOperationStageWeather> Weather { get; }
}

internal sealed record CampaignScheduledWorld6PreambleResult(
    CampaignScheduledWorld6Preamble.Cut Cut)
{
    public RandomStreamState RandomState => Cut.RandomState;
    public LandSide? InitiativeHolder => Cut.InitiativeHolder;
    public IReadOnlyList<CampaignOperationStageOrder> Orders => Cut.Orders;
    public IReadOnlyList<CampaignOperationStageWeather> Weather => Cut.Weather;
    public IReadOnlyList<CampaignScheduledWorld6PreambleStep> Trace => Cut.Trace;
}

// Recomputes the pinned preamble directly from authoritative rules and the successor creation.
internal static class CampaignScheduledWorld6Preamble
{
    internal sealed class Cut
    {
        private Cut(CampaignCreationRequest request, CampaignScheduledWorld6Created creation,
            long version, LandSequencePosition position, RandomStreamState randomState, LandSide? initiativeHolder,
            IEnumerable<CampaignOperationStageOrder> orders, IEnumerable<CampaignOperationStageWeather> weather,
            IEnumerable<CampaignScheduledWorld6PreambleStep> trace,
            IEnumerable<CampaignScheduledWorld6PreambleCheckpoint> checkpoints)
        {
            Request = request;
            Creation = creation;
            Version = version;
            Position = position;
            RandomState = randomState;
            InitiativeHolder = initiativeHolder;
            Orders = Array.AsReadOnly(orders.ToArray());
            Weather = Array.AsReadOnly(weather.ToArray());
            Trace = Array.AsReadOnly(trace.ToArray());
            Checkpoints = Array.AsReadOnly(checkpoints.ToArray());
        }

        internal CampaignCreationRequest Request { get; }
        internal CampaignScheduledWorld6Created Creation { get; }
        internal long Version { get; }
        internal LandSequencePosition Position { get; }
        internal RandomStreamState RandomState { get; }
        internal LandSide? InitiativeHolder { get; }
        internal IReadOnlyList<CampaignOperationStageOrder> Orders { get; }
        internal IReadOnlyList<CampaignOperationStageWeather> Weather { get; }
        internal IReadOnlyList<CampaignScheduledWorld6PreambleStep> Trace { get; }
        internal IReadOnlyList<CampaignScheduledWorld6PreambleCheckpoint> Checkpoints { get; }

        internal static CampaignScheduledWorld6PreambleResult RunVerified(CampaignCreationRequest request, CampaignScheduledWorld6Created creation)
        {
            var validated = CampaignScheduledWorld6Creation.Replay(request, creation);
            if (!Cna1979BreakdownSetupCatalog.TryGet("rules-lab.breakdown.truck.v1", out var definition) || definition is null)
                throw new InvalidOperationException("Certified preamble setup is unavailable.");
            var setup = CampaignSetupSnapshotV6.FromDefinition(definition);
            if (setup.SetupHash != CampaignScheduledWorld6Phase.ReferenceSetupHash)
                throw new InvalidOperationException("Certified preamble setup changed.");
            var position = validated.Position;
            var version = validated.StateVersion;
            var random = validated.RandomState;
            LandSide? holder = null;
            var orders = new List<CampaignOperationStageOrder>();
            var weather = new List<CampaignOperationStageWeather>();
            var trace = new List<CampaignScheduledWorld6PreambleStep>();
            var checkpoints = new List<CampaignScheduledWorld6PreambleCheckpoint>
            {
                new(version, position, validated.World, random, holder, orders, weather),
            };
            RuleReference[] convoySources = [Cna1979LandSequence.SourceReference, Cna1979SetupCatalog.OpeningPreambleSourceReference];
            RuleReference[] declarationSources = [Cna1979LandSequence.SourceReference, Cna1979LandSequence.InitiativeSideSourceReference,
                Cna1979LandSequence.StageChoiceSourceReference, new("spi-1979-land-rules", "7.16")];
            for (var index = 0; index < 6; index++)
            {
                var next = Cna1979LandSequenceV4.GetNext(position);
                var resultVersion = checked(version + 1);
                CampaignEvent value;
                switch (index)
                {
                    case 0:
                        var initiative = InitiativeResolver.Resolve(position.GameTurn, setup.InitialInitiative, random, setup.Sources);
                        value = new InitiativeDetermined(validated.CampaignId, resultVersion, position.PositionId, initiative.Outcome,
                            random.AlgorithmId, random.NextByteCursor, initiative.RandomState.NextByteCursor, next, initiative.Sources);
                        random = initiative.RandomState;
                        holder = initiative.Outcome.Holder;
                        break;
                    case 1:
                        value = new NoObligationNavalConvoyScheduleResolved(validated.CampaignId, resultVersion, position.PositionId, next, convoySources);
                        break;
                    case 2:
                        value = new NoObligationTacticalShippingResolved(validated.CampaignId, resultVersion, position.PositionId, next, convoySources);
                        break;
                    case 3:
                        if (holder != LandSide.Axis) throw new InvalidOperationException("Synthetic initiative order requires Axis ownership.");
                        value = new InitiativeOrderDeclared(validated.CampaignId, resultVersion, position.PositionId, next, 1,
                            holder.Value, LandSide.Axis, LandSide.Commonwealth, declarationSources);
                        orders.Add(new CampaignOperationStageOrder(CampaignOperationStageOrder.CurrentContractVersion,
                            next.GameTurn, 1, LandSide.Axis, LandSide.Commonwealth));
                        break;
                    case 4:
                        var resolved = Cna1979Weather.Resolve(position.GameTurn, random);
                        var weatherEvent = new WeatherDetermined(validated.CampaignId, resultVersion, position.PositionId,
                            position.GameTurn, 1, holder!.Value, resolved.Season, resolved.FirstDie, resolved.SecondDie,
                            resolved.Kind, resolved.Scope, resolved.LocationDie, resolved.AffectedAreas, 0, 0, 0,
                            resolved.RandomState.NextByteCursor, next, WeatherEventFactory.GetSources(resolved.Kind));
                        value = weatherEvent;
                        weather.Add(weatherEvent.ToState());
                        random = resolved.RandomState;
                        break;
                    default:
                        value = new NoObligationOrganizationResolved(validated.CampaignId, resultVersion, position.PositionId,
                            position.GameTurn, 1, next, NoObligationOrganizationResolved.RequiredSources);
                        break;
                }
                var nativeBytes = CampaignV11PreambleCodec.Serialize(value);
                var eventHash = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(nativeBytes));
                version = resultVersion;
                position = next;
                trace.Add(new CampaignScheduledWorld6PreambleStep(version, position.PositionId, eventHash, Convert.ToBase64String(nativeBytes)));
                checkpoints.Add(new CampaignScheduledWorld6PreambleCheckpoint(version, position, validated.World, random, holder, orders, weather));
            }
            if (trace.Count != 6 || checkpoints.Count != 7 || version != 7 || position != CampaignScheduledWorld6.ArrivalPosition())
                throw new InvalidOperationException("Certified preamble did not reach the expected arrival cut.");
            var cut = new Cut(request, validated, version, position, random, holder, orders, weather, trace, checkpoints);
            return new CampaignScheduledWorld6PreambleResult(cut);
        }
    }

    public static CampaignScheduledWorld6PreambleResult Run(CampaignCreationRequest request, CampaignScheduledWorld6Created creation) =>
        Cut.RunVerified(request, creation);
}
