# Independent review — Task008 D1

Review instance: 2 of 3.

## Preliminary ledger (before author explanation)

- Verified branch `codex/combat-task008-reserve-designation`, HEAD/base `3ded1ebd8906b04969a9289a8b1b8068b0b8f75c`, seven tracked modified paths and four untracked implementation/test files. Scope matches bootstrap.
- Inspected canonical designation/stage specs, frozen oracle initial/emission/replay/retry semantics, implementation and tests. D1 intentionally accepts only zero/one designation record and state10/11; terminal completion explicitly belongs to D2 then019A. No scope blocker found.
- Full predecessor replay establishes owner through retained order. Input authorization precedes accepted-input retry lookup; canonical recomputation rejects forged events/caches. Typed World mutation preserves unrelated fields; serialization compares complete derived World before writing bounded fields.
- Tests compare retained hashes/lengths for all16 fixture cases and derive predecessor records through real C# readers. Negatives cover actor/identity/conflict, predecessor omission/reordering, resigned forgery, cache mutation, caller buffers and typed World changes.
- Preliminary concerns to verify: no-build artifact freshness; actual focused execution; source pin/fixture preservation; author scope claims. No actionable defect established.

## Findings

No actionable findings. Reviewed full D1 source, tests and tracked documentation delta against canonical designation/stage-entry contracts and designation/release schemas. Frozen oracle designation/retry logic matches implemented D1 subset. Canonical initial-World reader remains unchanged.

## Plan Review

Ready for D1 scope. Execution index at `docs/design/combat-cycle-implementation-plan.md:801–816` preserves original responsibility while refining delivery order to B2 → D1 → D2 →019A → E. D2 must derive internal OpeningBase from accepted history and encode frozen completion2, including ordinal1 identity.019A must apply that same event atomically, with no extra command/event. This satisfies atomic-entry requirement in `combat-cycle-sequence-v1.md` and existing008/019 ownership in `combat-inherited-successors-v1.md`.

Terminal Reserve state11/12, full replay/readback, designation retry after completion and Movement handoff remain explicitly open until019A. Parent008, noninitial Snapshot12 restore, public activation and HOST-PUB-001 remain open. No dropped acceptance criterion or dependency cycle found. README, architecture and naming updates accurately describe precompletion scope. Five primary source/test/project files fit stated cap.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Consequence |
| --- | --- | --- | --- |
| Full predecessor provenance; order-derived owner | `CampaignCombatReserveDesignation.Replay`, `Initial`, all16 golden cases | Confirmed | No synthetic prefix/cache admission; ActLast owner resolves correctly despite Axis Initiative holder |
| Only own none→I changes World | `DesignatedWorld`, `ExpectedWorld`, bounded `WriteWorld`; resource/opponent forgery test | Confirmed | Typed World equality protects hardcoded absent fields; initial-only codec unchanged |
| Actual designation receipt/history and chronological ledger | `Emit`, canonical event/state schema, independent test receipt/prefix hashes | Confirmed | Event and history bound to accepted input and real prefix |
| Authorization before exact retry lookup | `Apply`, `Authorize`, actor/conflict test for empty and accepted history | Confirmed | Exact precompletion retry returns original event and current replayed state |
|16 fixture rows,24 precompletion cuts,8 designation events | `Cases`, frozen precompletion test and fixture | Confirmed | Future terminal cuts not counted as D1 runtime evidence |
|23 focused tests pass | Independent no-build execution; `/tmp/d1-final.log` | Confirmed | Zero failures/skips |
| Frozen oracle covers broader future contract | Unchanged oracle source,14 source pins, `/tmp/d-reserve-baseline.log` | Confirmed retained result | Broader40-cut Python result does not imply C# completion readiness |
| Full-suite/build/format results | Author evidence names logs; reviewer did not rerun these | Not independently verified | Integration lead retains full gate ownership |

Author evidence file contained prior-round status metadata; no prior review report was opened and no prior verdict was used as evidence. Preliminary ledger above predates author packet read.

## Verification Performed

- `git status --short`, `git branch --show-current`, `git rev-parse HEAD`, `git diff --stat`: expected D1 scope and base confirmed.
- `shasum -a 256 -c .planning/combat-task008-delivery/d1-source.sha256`: all five entries OK.
- `git diff --exit-code -- docs/specs`: exit0; canonical oracle/spec/fixture files unchanged.
- Read frozen Python oracle initial/designation/event/replay/retry/source-pin routines and retained baseline:16 traces,40 cuts,2342 leaf mutations,733 raw rejections,1578 boundary/retry checks,4 frozen-kernel cases. Did not rerun oracle per bootstrap.
- Independently recomputed all14 SHA-256 source pins from retained fixture: match.
- Runner configuration: SDK-style net10.0, native MTP, xUnit v3; `dotnet --version` returns10.0.400. Core/test DLL modification times are newer than all four D1 source/test files.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatReserveDesignationTests' '-bl:/tmp/d1-review2-{}.binlog'`: initial sandbox attempt exit134 before tests due local named-pipe bind permission. Approved escalation rerun exit0;23 passed,0 failed,0 skipped,3.863 seconds. Successful binlog: `/tmp/d1-review2-20260919-224912--59122--8evoXQ-dotnet-test.binlog`.
- No build, source edit, commit or delegation performed. Only this review report written.

## Open Questions And Residual Risks

No blocking open questions. Bounded World serializer duplicates closed-profile field layout; exact golden hashes and complete derived-World equality provide focused protection, but future profile expansion must update validation and serialization together. This review does not certify terminal completion, generic restore, public ingress, durable publication or full campaign capacity. No independent full-solution build/format/suite rerun performed.

## Verdict

**Ready** — Task008 D1 implementation and D1/D2/019A execution refinement, within explicit precompletion scope.

## Recommended Next Actions

Lead retains integration gate and required third sequential independent round. After D1 acceptance, continue D2 completion codec and019A same-event atomic projector in dependency order; preserve full terminal/retry/readback obligations before Movement admission.
