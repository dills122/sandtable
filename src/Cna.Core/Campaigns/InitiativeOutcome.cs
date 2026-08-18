using System.Collections.ObjectModel;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record InitiativeRollRound
{
    public InitiativeRollRound(
        int round,
        int axisDie,
        int axisRating,
        int axisTotal,
        int commonwealthDie,
        int commonwealthRating,
        int commonwealthTotal)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(round, 1);
        RequireDie(axisDie, nameof(axisDie));
        ArgumentOutOfRangeException.ThrowIfLessThan(axisRating, 1);
        RequireDie(commonwealthDie, nameof(commonwealthDie));
        ArgumentOutOfRangeException.ThrowIfLessThan(commonwealthRating, 1);

        if (axisTotal != checked(axisDie + axisRating))
        {
            throw new ArgumentException("The Axis total is inconsistent.", nameof(axisTotal));
        }

        if (commonwealthTotal != checked(commonwealthDie + commonwealthRating))
        {
            throw new ArgumentException(
                "The Commonwealth total is inconsistent.",
                nameof(commonwealthTotal));
        }

        Round = round;
        AxisDie = axisDie;
        AxisRating = axisRating;
        AxisTotal = axisTotal;
        CommonwealthDie = commonwealthDie;
        CommonwealthRating = commonwealthRating;
        CommonwealthTotal = commonwealthTotal;
    }

    public int Round { get; }
    public int AxisDie { get; }
    public int AxisRating { get; }
    public int AxisTotal { get; }
    public int CommonwealthDie { get; }
    public int CommonwealthRating { get; }
    public int CommonwealthTotal { get; }

    private static void RequireDie(int value, string parameterName)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1, parameterName);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 6, parameterName);
    }
}

internal abstract record InitiativeOutcome
{
    public abstract LandSide Holder { get; }
}

internal sealed record PredeterminedInitiativeOutcome : InitiativeOutcome
{
    public PredeterminedInitiativeOutcome(LandSide holder)
    {
        if (!Enum.IsDefined(holder))
        {
            throw new ArgumentOutOfRangeException(nameof(holder));
        }

        Holder = holder;
    }

    public override LandSide Holder { get; }
}

internal sealed record ContestedInitiativeOutcome : InitiativeOutcome
{
    public ContestedInitiativeOutcome(
        AxisInitiativeSourceFacts axisFacts,
        AxisInitiativePresence axisPresence,
        IReadOnlyList<InitiativeRollRound> rounds,
        LandSide holder)
    {
        ArgumentNullException.ThrowIfNull(axisFacts);

        if (!Enum.IsDefined(axisPresence))
        {
            throw new ArgumentOutOfRangeException(nameof(axisPresence));
        }

        if (Cna1979InitiativeRatings.ClassifyAxisPresence(axisFacts) != axisPresence)
        {
            throw new ArgumentException(
                "The Axis presence does not match its source facts.",
                nameof(axisPresence));
        }

        if (!Enum.IsDefined(holder))
        {
            throw new ArgumentOutOfRangeException(nameof(holder));
        }

        ArgumentNullException.ThrowIfNull(rounds);
        var roundCopy = rounds.ToArray();

        if (roundCopy.Length == 0 || roundCopy.Any(round => round is null))
        {
            throw new ArgumentException(
                "At least one non-null Initiative roll round is required.",
                nameof(rounds));
        }

        for (var index = 0; index < roundCopy.Length; index++)
        {
            var round = roundCopy[index];

            if (round.Round != index + 1)
            {
                throw new ArgumentException("Initiative rounds must be contiguous.", nameof(rounds));
            }

            var isTie = round.AxisTotal == round.CommonwealthTotal;

            if (index < roundCopy.Length - 1 && !isTie)
            {
                throw new ArgumentException(
                    "Every non-final Initiative round must be tied.",
                    nameof(rounds));
            }

            if (index == roundCopy.Length - 1 && isTie)
            {
                throw new ArgumentException(
                    "The final Initiative round must select a holder.",
                    nameof(rounds));
            }
        }

        var final = roundCopy[^1];
        var expectedHolder = final.AxisTotal > final.CommonwealthTotal
            ? LandSide.Axis
            : LandSide.Commonwealth;

        if (holder != expectedHolder)
        {
            throw new ArgumentException(
                "The Initiative holder does not match the final round.",
                nameof(holder));
        }

        AxisFacts = axisFacts;
        AxisPresence = axisPresence;
        Rounds = Array.AsReadOnly(roundCopy);
        Holder = holder;
    }

    public AxisInitiativeSourceFacts AxisFacts { get; }
    public AxisInitiativePresence AxisPresence { get; }
    public IReadOnlyList<InitiativeRollRound> Rounds { get; }
    public override LandSide Holder { get; }

    public bool Equals(ContestedInitiativeOutcome? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && AxisFacts == other.AxisFacts
            && AxisPresence == other.AxisPresence
            && Rounds.SequenceEqual(other.Rounds)
            && Holder == other.Holder);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(AxisFacts);
        hash.Add(AxisPresence);

        foreach (var round in Rounds)
        {
            hash.Add(round);
        }

        hash.Add(Holder);
        return hash.ToHashCode();
    }
}
