# Bounded runtime triage (unproven hypotheses)

Observed original failure: source80f633f, SDK10.0.401, Linuxx64 .NET10.0.12, exit139 after2103 passes. No native stack or signal capture. The SDK/runtime pairing matches the official release: https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.12/10.0.12.md .

Known reports inspected do not identify this failure:
- https://github.com/dotnet/runtime/issues/133449 : .NET10.0.12 OpenVZ Server GC initialization failure before startup. Our process completed2103 tests; no matching initialization error. Does not justify affinity/GC flags.
- https://github.com/dotnet/runtime/issues/134736 : .NET10.0.12 LINQ Skip/Concat managed ArgumentOutOfRangeException; no evidence matching silent process139. Does not justify changing test data or LINQ paths.
- https://github.com/dotnet/runtime/issues/128255 : Linuxx64 CompileMethod/LambdaCompiler native crash in a different prerelease runtime/large-thread service. No native stack or matching call chain here. A runtime fault remains a hypothesis, not attribution.
- https://github.com/dotnet/runtime/issues/115815 : old.NET8/9 Linuxx64 GC/JIT crash reportedly absent in.NET10 preview; no basis to apply its workaround to.NET10.0.12.
- Additional native ONNX/cgo/httpcloak reports require dependencies/process context absent from the bounded Core test context. They are not matching evidence.

Source reads showed bounded JSON parser depth, fixed stackalloc buffers, local/disposed hashing instances and locked ActualRoundEntry replay memo, with no confirmed unsafe or native call site in the inspected Core paths. Negative source searches are bounded and cannot rule out runtime/BCL faults. No native culprit, deterministic managed failure, or concurrency fault has been established. Existing active-row overlap is temporal, not causal.

Next discriminating evidence: exact80f control outcome; candidate natural Linux recurrence with triage dump/JSON native stack; native runtime frames versus managed test/codec frames; runner/runtime identity and memory observations if present. A green run does not establish remediation. Preserve any crash independently and use its stack before selecting a targeted fix.
