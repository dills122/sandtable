using System.Collections.ObjectModel;

namespace Cna.Core.Randomness;

public sealed record RandomProcedureCondition
{
    public RandomProcedureCondition(string label, IEnumerable<string> whenKindIn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentNullException.ThrowIfNull(whenKindIn);
        var kinds = whenKindIn.ToArray();
        if (kinds.Length == 0 || kinds.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("At least one condition kind is required.", nameof(whenKindIn));
        }

        Label = label;
        WhenKindIn = Array.AsReadOnly(kinds);
    }

    public string Label { get; }
    public IReadOnlyList<string> WhenKindIn { get; }
}

public sealed record RandomProcedureStep
{
    public RandomProcedureStep(
        string procedureId,
        IEnumerable<string> acceptedD6Order,
        string repeat,
        RandomProcedureCondition? conditionalAcceptedD6)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(procedureId);
        ArgumentNullException.ThrowIfNull(acceptedD6Order);
        ArgumentException.ThrowIfNullOrWhiteSpace(repeat);
        var order = acceptedD6Order.ToArray();
        if (order.Length == 0 || order.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("At least one accepted d6 label is required.", nameof(acceptedD6Order));
        }

        ProcedureId = procedureId;
        AcceptedD6Order = Array.AsReadOnly(order);
        Repeat = repeat;
        ConditionalAcceptedD6 = conditionalAcceptedD6;
    }

    public string ProcedureId { get; }
    public IReadOnlyList<string> AcceptedD6Order { get; }
    public string Repeat { get; }
    public RandomProcedureCondition? ConditionalAcceptedD6 { get; }
}
