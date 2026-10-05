# Independent review: CMB-019D3 native settled control

Verdict: **Ready. No actionable findings.**

Review instance: **N1/CMB-019D3, set1/pass1, total1 of9; maximum3 per set.**
Fresh GPT-6.1-sol medium reviewer dispatched by coordinator. Read-only review; no
edits, commits, fixes or reviewer dispatch. Report delivered by coordinator
01a0c9dc-00bc-78a3-800d-3cb36859e422 and retained here without changing its verdict.

## Findings

The preliminary ledger was recorded before reading the separate author explanation.
It contained no actionable findings and held readiness pending final-gate verification.

The implementation reproduces the accepted settled-control-v1 contract:

- Admission replays the complete packet through native019D1 and compares the regenerated proof before retaining owned bytes.
- Actor/control/cycle validation precedes duplicate lookup. Retries return original canonical event bytes, including after closure and with changed admission clocks.
- Replay regenerates accepted events; state readback and caller caches require complete byte equality.
- Owner, deadline equality, early/stale timers, missing/regressed clocks and deterministic System fallback follow the frozen policy.
- Repeat establishes the next ordinal with the pre-repeat prefix and resulting authority version. It resets target uses and new-cycle progress while preserving World/resources/RNG/members/history/duties.
- Finish enters Truck Convoy without executing it. Neither transition executes successor Movement or charges resources.

## Plan Review

Branch `codex/native-settled-control`, base/head
`907f41403ed159d65024f193f3e1f730a23b9bbb`, and explicit dirty scope match the bootstrap.
The five primary files and administrative documentation remain within the accepted
manifest. All204 existing spec files are byte-identical to the base. No shared runtime
refactor, public/Snapshot/transport activation, actual positive-history admission,
later-II/consumed lineage or parent017–019 closure was introduced.

## Author-Claim Reconciliation

| Claim | Evidence | Status |
| --- | --- | --- |
| Full upstream provenance; fixtures are not authority | Admission factory,019D1 ReadProof/Bridge, re-signed-source rejection test | Confirmed |
|32+4 sources,80 traces,236 cuts,464 retries | Native parity test and independent focused execution | Confirmed |
|1067 control state/event leaf mutations | Mutation test; independently counted fixture leaves | Confirmed |
| Ordered closed grammar and immutable outputs | Codec, ownership tests; all eight inventory shapes compared exactly | Confirmed |
| Final full gate was pending | Final log now contains complete passing results | Superseded by completed evidence |

## Verification Performed

Independently executed:

- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatSettledControlTests'`:16 passed,0 failed/skipped,21.895s.
- `python3 -B docs/specs/verify-combat-settled-control-v1.py`:10 semantic groups passed,80 traces,32 source pins;3676 replay forgeries,341 source attacks, re-signed-source attack and93 canonical/capacity cases.
- `git diff --check`: passed.
- Compared all204 existing spec files against HEAD: unchanged.
- Verified all ten frozen target hashes against the bootstrap: match.

The first focused-test attempt failed because the sandbox prevented .NET's IPC pipe.
The authorized outside-sandbox rerun passed.

Inspected final author gate evidence: `/private/tmp/cmb019d3-final-check.log` contains
completed restore, format, build with0 warnings/errors,81 Boundary tests and2512
solution tests, all passing with0 skipped. The run completed after the final retry-test
changes; compiled test artifacts postdate those changes. Frozen implementation hashes
remained unchanged. Earlier pre-retry evidence was not substituted. Seven unchanged
predecessor/policy oracle logs also report passing results. No duplicate full suite
was run by the reviewer.

## Frozen Hashes

| Primary file | SHA-256 |
| --- | --- |
| Engine/models | `8b167d88c07c8506e0b43e571c81117209747e0c2d7721b5135e0c91265793d1` |
| Codec | `792707d1bf72214d209ab8a609e384ec02e123e9aeceaeae907246c9df827832` |
| Tests | `799c02435c69c760b20d42243a31f54d33363b56ce2e6de6e481c45b4a5827e6` |
| Test project | `37fa69bd8e923e356b27d5024c1c3769e185f086ea52aae1de71b95f1d6e926b` |
| Canonical plan | `08b70421d8998ba20694b99d5444878def27a02d236ebcc291c9aec25bbee774` |

Final full-gate log SHA-256:
`593b01773adf44a8f2cdea7e53cddfc8ef94e12c98872258350bcb6fd79b2d33`.

## Open Questions And Residual Risks

Earlier Movement provenance remains synthetic-pre-combat. This approval covers the
bounded private adapter; it does not approve successor gameplay or public activation.
Serena was activated on the exact worktree. Codebase Memory generation03:24:45Z
reported stale test metadata and incomplete structural results; exact source inspection
supplied the evidence.

## Verdict And Recommended Next Actions

Ready, no actionable findings. Reconcile administrative gate-running notes with
completion, retain this report, and proceed through scoped commit/publication and
exact-final-head CI. Merge remains subject to the coordinator's required CI gate.

## Author Response

Accept. No findings require fixes or deferral. Behavior/test/project bytes remain
identical to reviewed hashes. Subsequent changes only record completed gate/review
and publication metadata. No new review instance or full gate is started.
