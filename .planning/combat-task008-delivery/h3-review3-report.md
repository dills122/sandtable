# H3 independent review

Review instance: 3 of 3. Base `fdd2a4f`; reviewed head `c5f8db3954a868ed47bd172e87242e5429d034ad`.

## Preliminary blind ledger

Recorded before author/check packets and session recall. Separate bootstrap received; no prior reports or aggregate verdicts consulted.

- Scope verified: expected feature branch, clean initial Git status, four primary hashes match frozen manifest. Supporting documentation diff stays within H3 status and H4 deferral.
- Router admits F1 only after strict ordinary Movement predecessor; non-null window hint selects strict trigger replay. Closure and second-move tags select readers whose emitted bytes must equal supplied history. No supplied family/state cache grants authority.
- All remaining fork bytes are passed to bounded strict readers: F3 at most one, F2 at most four, F4 at most two, F5 exactly selected second event, F6 at most three. Internal gaps and extra tails cannot be silently dropped.
- Tests independently anchor generated traces against frozen event/state digests and H0 root fields, inventory 50 F1–F6 rows / 34 distinct histories, cover both owners, all intermediate cuts, buffer ownership, wrong actor/effect re-signing, malformed hints and fork splicing.
- Plan dependency ordering and H3/H4 boundary coherent. Full literal-root codec, disabled-admission restore, publication and later gameplay remain explicitly deferred.
- No actionable source finding identified. Remaining check: focused executable regression suite after root grants runner clearance; reconcile author claims afterward.

## Findings

No actionable findings identified in four frozen primary paths or supporting H3 plan/documentation changes.

## Plan Review

H3 meets its bounded sequencing: accepted H2 establishes ordinary prefix; F1 validates trigger; F2 proves first participant before F4/F5; F5 proves second move before F6. Reader reuse preserves canonical event effects, receipt ledger, RNG and position semantics without introducing authority outside Core. Four primary files remain within assigned scope. H0 contract requires strict single-stream routing; `ReplayReaction` supplies it while full literal Snapshot12 serialization/restore remains H4. README, tech-design, naming-overview and execution index consistently retain pending parent restore/publication gates. No scope pivot or plan correction needed.

## Author-Claim Reconciliation

| Claim | Inspected evidence | Status / consequence |
| --- | --- | --- |
| Hints cannot authorize effects | HistoryReplay movement loop and ReplayReaction; six predecessor Replay methods each recompute and compare canonical bytes | Confirmed; no family cache or swallowed reader error |
| Entire remaining suffix validated | Closure/lifecycle/fallback/completion calls pass `history.Count - cursor`; readers enforce bounded counts and sequential Emit | Confirmed; excess tails and missing internal transitions fail |
| Both owners and all selected fork cuts covered | Cases, Trace, CheckH0, inventory assertion and frozen H0 roots | Confirmed statically; independent Python inventory gives 50 rows, 34 identities, 2 shared ordinary, 32 new; all 50 root receipt counts/versions match exact history lengths |
| Full selected H0 inventory totals 368 rows / 286 histories | Independent fixture inspection | Confirmed fixture totals; runtime coverage additionally depends on retained H1/H2 tests in focused regression run |
| Owned buffers cannot alter accepted projections | RetainedHistory.Capture/copying, immutable predecessor collections, mutation tests | Confirmed source design and dedicated assertions |
| Typed forgeries test causal validation | Forgeries constructs changed effect/actor via existing serializers; verifies readable input and changed receipt before rejection | Confirmed; stronger than malformed JSON alone |
| Full-suite/build/format gates pass | h3-checks packet; root message; inspected `/tmp/h3-suite.log` and `/tmp/h3-boundary.log` tails | Full suite 2233 and boundary81 confirmed from logs; build/format reported by root, not independently rerun |
| Full restore and archive authenticity unimplemented | H0 trust contract, plan, docs, changed path scope | Confirmed honest boundary; no readiness claim for H4/HOST-PUB-001 |

Required session recall performed only after blind ledger. Returned H0/creation context treated as navigation context, not independent verification or prior-review endorsement.

## Verification Performed

- Initial branch/head/status and exact diff checked; four `h3-source.sha256` entries pass `shasum -a 256 -c`.
- `git diff --check fdd2a4f c5f8db3` passes.
- Independent Python read-only fixture inventory: F1=4, F2=10, F3=12, F4=12, F5=4, F6=8; 50 rows / 34 distinct histories; 2 shared ordinary / 32 new; complete fixture 368 rows / 286 identities.
- Native MTP/xUnit runner verified through global.json, SDK-style test project and shared props/packages. Installed SDK `10.0.400` satisfies `latestFeature` roll-forward from pinned10.0.302.
- Root cleared shared runner only after its full suite and boundary processes closed. Independent command `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*HistoryReplayTests' '-bl:/tmp/h3-review3-{}.binlog'` passed129/129, zero failures/skips,1m55s837, exit0. Used login:false and escalated local MTP IPC. Log `/tmp/h3-review3.log`; verified binlog `/tmp/h3-review3-20260920-054122--3721--Lv7JeX-dotnet-test.binlog`. Process23842 finished before report return.
- Final four frozen hashes rechecked unchanged. Shared root-owned evidence/check files changed during review; no runtime/test/plan drift observed.

## Open Questions And Residual Risks

Bounded selected history profile only. Repeated predecessor replay is acceptable here but is not campaign-long scalability evidence. Trusted commitment remains caller/archive responsibility. H4 literal root and fresh-admission-disabled restore remain mandatory future work. Remote CI remains root-owned and pending at review time; reviewer does not certify remote status.

## Verdict

Ready for bounded H3. Code and plan requirements satisfied by source audit, frozen fixture comparison and independently executed H1–H3 regression suite. No actionable findings or heavy pivot. Parent Task008/H4 and publication gates remain open.

## Recommended Next Actions

Lead reconciles root-owned remote CI and retained evidence, then continues agreed delivery sequence. Review3of3 complete; no additional review instance created or proposed. Reviewer modified only this report, made no Git mutations or fixes, and left no running processes.
