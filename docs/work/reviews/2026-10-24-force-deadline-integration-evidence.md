# Force deadline validation integration evidence

Bounded follow-up to durable creation retry and Combat continuation evidence.
Independent review instance 2 approves the exact patch with non-blocking follow-ups;
see [review verdict](2026-10-24-force-deadline-independent-review.md).

## Reviewed behavior

An invalid Force proposal cannot publish clock cancellation before its retained
opening, current owner draft, action mapping and HMAC have been checked. Accepted
receipt recovery remains before clock handling. Genuine current proposals retain
existing deadline cancellation behavior. No timing schema or public contract changes.

## Pre-integration evidence

Reviewed patch SHA-256: `0a4b955ee5f8324a11e1c68ca180597a935dc3b46a5f0b840bcc03526801990e`.
Scratch regression: four failures before fix, four passes after fix. Existing
ForceRound compatibility: 8/8 passed, zero failed/skipped, 21.904s. These are
isolated scratch results recorded by independent review, not main-tree validation.

## Main-tree application and validation

Original packet committed as `6625f9f5` after its integrated gate passed. Applied
exact reviewed patch above without alterations. All commands below ran from main
worktree, with logs under `artifacts/rc-overnight/` and actual exit codes observed.

| Command | Result | Log |
| --- | --- | --- |
| `dotnet format Sandtable.slnx --verify-no-changes --no-restore` | Exit 0 | `force-deadline-main-format.log` |
| `dotnet build Sandtable.slnx --no-restore` | Exit 0; zero warnings/errors; 16.27s | `force-deadline-main-build.log` |
| `dotnet test --project tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj --no-build --filter-method '*InvalidForceProposalAtDeadlineCannotCancelCurrentRound*'` | Exit 0; 4/4, zero failed/skipped; 11.536s | `force-deadline-main-regression.log` |
| `dotnet test --project tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj --no-build --filter-method '*ForceRound*'` | Exit 0; 8/8, zero failed/skipped; 19.799s | `force-deadline-main-compatibility.log` |

Final-tree integrated gate, exact-head CI before merge, original source-pin
failures, public certification and playable RC obligations remain open. The two
original Python verifiers still report source-pin drift; no hashes were changed.
Source review approves bounded integration, not broader certificate completion.
