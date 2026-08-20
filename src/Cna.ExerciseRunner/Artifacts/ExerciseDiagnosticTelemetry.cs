namespace Cna.ExerciseRunner.Artifacts;

internal sealed record ExercisePhaseTiming(string Operation, long ElapsedMicroseconds);

internal sealed class ExerciseDiagnosticTelemetry
{
    private readonly List<ExercisePhaseTiming> phases = [];

    internal IReadOnlyList<ExercisePhaseTiming> Phases => phases.AsReadOnly();
    internal int? PayloadCountBeforeDiagnostics { get; private set; }
    internal long? LogicalBytesBeforeDiagnostics { get; private set; }

    internal void RecordPhase(string operation, long elapsedMicroseconds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentOutOfRangeException.ThrowIfNegative(elapsedMicroseconds);
        phases.Add(new ExercisePhaseTiming(operation, elapsedMicroseconds));
    }

    internal void RecordPreparedPayloads(int payloadCount, long logicalBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(payloadCount);
        ArgumentOutOfRangeException.ThrowIfNegative(logicalBytes);
        PayloadCountBeforeDiagnostics = payloadCount;
        LogicalBytesBeforeDiagnostics = logicalBytes;
    }
}
