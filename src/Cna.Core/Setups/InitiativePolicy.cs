using Cna.Core.Rules;

namespace Cna.Core.Setups;

public abstract record InitiativePolicy;

public sealed record PredeterminedInitiative : InitiativePolicy
{
    public PredeterminedInitiative(LandSide holder)
    {
        if (!Enum.IsDefined(holder))
        {
            throw new ArgumentOutOfRangeException(nameof(holder));
        }

        Holder = holder;
    }

    public LandSide Holder { get; }
}

public sealed record ContestedInitiative : InitiativePolicy
{
    public ContestedInitiative(AxisInitiativeSourceFacts axisFacts)
    {
        ArgumentNullException.ThrowIfNull(axisFacts);
        AxisFacts = axisFacts;
    }

    public AxisInitiativeSourceFacts AxisFacts { get; }
}
