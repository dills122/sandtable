; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
CNA001 | Cna.Determinism | Warning | Bare Single()/First()/SingleOrDefault()/FirstOrDefault() on a stage-order collection assumes it never holds more than one entry
CNA002 | Cna.Determinism | Warning | Non-deterministic API (DateTime.Now/UtcNow, Random.Shared, unseeded new Random(), Guid.NewGuid()) used where DET-001/DET-002 forbid it
