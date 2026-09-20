# Task017A1 independent review

Review instance: 1 of 3.
Candidate `77ef166f03674a7e5875acb8c1ebfde36b0d406a`; base `afdcd9e`.
Clean detached clone `/tmp/sandtable-task017a1-review1-1921`. No source changes, commits, subagents, CCE, cross-session memory, or earlier reviews used. Preliminary ledger persisted before author explanation. Original repository untouched; integration writes limited to this report and preliminary ledger.

## Findings

No actionable findings in bounded A1 implementation or plan.

`CampaignCombatReserveReleaseCodec.ReadBase` checks raw inventory, primitive grammar and canonical spellings before accessing expected base/context; complete canonical byte comparison prevents substitutions, dropped history and reordered arrays. Serializer validates retained creation/cycle/topology, member ordering and ownership, first/later status constraints, coherent release/conversion/ceiling/expired-exception history and linked offensive commitments. Model constructors copy both collections; nested cycle/history/member/unit/CP/RNG/attack values are immutable. Returning retained expected value is sound under explicitly documented caller trust contract.

Full diff also includes project-map/design/roadmap/site and planning metadata; four runtime/test paths match bootstrap scope. Ancillary public documentation consistently leaves Release execution and public campaign admission gated. Frozen Release fixture/schema/oracle are unchanged.

## Plan Review

Task017 refinement at `docs/design/combat-cycle-implementation-plan.md:941` bounds A1 to immutable isolated values and expected-base codec. Six tests meet that scope: exactly44 isolated base hashes, four explicit historical exclusions, contextual/raw/history negatives and ownership/capacity checks. Independent expected typed bases derive from retained creation/content/cycle templates and recipe inputs, without using golden hash as construction input.

A2 lifecycle/event/state work, native empty adapter, positive predecessor/bridge and actual later-II/consumed lineage remain separate dependencies. No lifecycle, future-duty execution, campaign provenance, public admission, Snapshot successor, durable publication or parent017 acceptance inferred. Deferred work is explicit and appropriately sequenced; A1 adds no migration or active runtime path.

## Author-Claim Reconciliation

| Author claim | Independent evidence | Status / consequence |
| --- | --- | --- |
| Exactly44 isolated hashes, four historical exclusions | Fixture inventory, test builder, independently executed six C# tests | Confirmed; bounded base parity only |
| Raw shape/canonical rejection precedes trusted access | `CheckRaw`, `BaseShape`, primitive checks, null-context malformed tests | Confirmed |
| Full context and expected-byte comparison | `Validate`, `ReadBase`, field-forgery test | Confirmed; caller must independently retain expected base |
| Owned immutable values | Copying constructor, immutable nested types, collection/output mutation test | Confirmed |
| Legal retained history/ceilings/offensive links | Release oracle `validate_base`, codec validation, focused tests | Confirmed |
| No lifecycle or actual World provenance | Source paths, internal profile, plan/docs | Confirmed; exclusions preserved |
| Worker20 and development RED evidence | Not independently reconstructed; worker/check/dev records deliberately not read | Unverified as historical claims; own checks below support current candidate |

## Verification Performed

All commands ran with `login:false` in clean candidate clone unless stated otherwise.

- `git rev-parse HEAD`: exact candidate; `git status --short`: clean before and after checks.
- `git diff --check afdcd9e 77ef166`: passed.
- `python3 docs/specs/verify-combat-reserve-release-v1.py`: exit0;13 literal cases/48 side-slot traces,188 cuts,2368 mutations,840 raw rejects,20 timing and27 boundary checks. These are frozen Python contract checks, not C# lifecycle proof.
- Initial `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-class Cna.Core.Tests.Campaigns.CombatReserveReleaseBaseTests -p:UseSharedCompilation=false -nodeReuse:false`: exit134, native runner NamedPipeServer sandbox IPC denial. Log `/tmp/task017a1-review1-dotnet.log`.
- Same command with approved IPC/network access: exit5, zero tests selected. Not counted as passing evidence. Log `/tmp/task017a1-review1-dotnet-approved.log`. Built assembly independently listed expected class using direct executable `--xunit-list classes`; direct executable `--help` inspected runner options.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatReserveReleaseBaseTests'` with approved IPC: exit0;6 passed,0 failed,0 skipped,1.762s. Log `/tmp/task017a1-review1-focused.log`.
- Repository-native Boundary command `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-trait 'Boundary=UserSpace'` with approved IPC: exit0;81 passed,0 failed,0 skipped,10.582s. Log `/tmp/task017a1-review1-boundary.log`.

No full-solution build/test, formatter, CI, or standalone new C# harness run claimed. Review uses own execution of candidate tests plus independently inspected frozen oracle and source. No generated or source edits made. All owned execution sessions completed. Approved process inventory showed no remaining process referencing review clone/log prefix; no global build-server shutdown used.

## Open Questions And Residual Risks

Expected base is trusted input, not derived authority: candidate-derived expected values would invalidate authentication premise. This internal API documents that requirement. Frozen hashes prove isolated bytes, not actual retained World membership or campaign history. Future lifecycle/adapters need their own replay/provenance evidence. Full root gates and other required independent instances remain initiating task responsibilities; this review does not claim their completion.

## Verdict

**Ready** for bounded Task017A1 only. Implementation and stated child plan supported by inspected contracts, six independently run C# facts,81 Boundary checks and frozen oracle. Parent017 remains open.

## Recommended Next Actions

Retain exact candidate freeze and report; finish required remaining independent reviews/root gates before acceptance. Do not dispatch later children or widen provenance claims based on this review alone.
