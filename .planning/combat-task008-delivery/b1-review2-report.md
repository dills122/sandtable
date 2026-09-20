# B1 independent review

Review instance: 2 of 3.

## Preliminary ledger — recorded before author explanation

Scope verified: branch `codex/combat-task008-opening-preamble`, HEAD/base `b67b4f9bffccd7e5114ef0b39cf1ef3b1c3f3e92`; three untracked implementation files, one untracked test file and seven tracked documentation/project changes match bootstrap. No prior review report read. CCE search incidentally returned introductory B1 evidence snippet, and required memory recall exposed execution decisions; neither supplied substantive author rationale or review findings.

- Canonical opening specification, schema and Python transition/readback code agree with four-edge implementation: versions1–5, frozen Axis context, unchanged World/RNG, full creation-rooted replay and exact byte comparison.
- Tests inspect all six golden traces and all five cuts; historical exact retry returns current replay state, authorization precedes lookup, opposite declaration branch cache rejects.
- Standalone input codec does not enforce all occurrence-specific fields; Apply compares complete input to replay-derived command, consistent with oracle separation. No admitted invalid transition identified.
- Plan B1→C→B2→D matches Weather and stage-entry contracts. Parent B, full Snapshot12 restore and actual publication remain explicitly open.
- Remaining verification: execute frozen oracle and focused tests; reconcile author claims. No actionable defect identified in preliminary pass.

## Findings

No actionable findings. Reviewed complete B1 implementation and tests, canonical opening specification/schema/oracle, dependency contracts, creation validation seam and tracked diff. Frozen source fingerprints matched review boundary.

`Replay` validates Created11 through A2 before deriving authority, re-emits each accepted input and compares entire canonical event bytes. This rejects both malformed fields and well-formed forged authority. `Apply` authorizes before historical receipt lookup and rejects altered reuse; terminal historical retries preserve current state. `ReadState` compares full reconstruction, so cached prefix/order/world cannot certify itself. No old-reader or public runtime registration changed.

## Plan Review

B1 scope contains four opening successors only. Revised A2→B1→C→B2→D dependencies are required by canonical Weather's complete four-event predecessor and stage entry's genuine Weather event. Refinement removes an artificial dependency cycle without dropping stage-entry scope: parent B remains open until B1 and B2 complete. H still requires all inherited families and 019A; HOST-PUB-001 still owns actual persistence/publication proof. README, technical design, naming guide and roadmap agree on dormant Weather-entry capability. No rollout or migration needed for unregistered internal adapter; no parent completion inferred.

## Author-Claim Reconciliation

| Claim | Evidence | Status / consequence |
| --- | --- | --- |
| Six traces, 60 fingerprints and 30 state cuts match | `SixFrozenTracesMatchEveryCreationEventAndStateCut`, focused run, frozen oracle | Confirmed; fingerprint parity, not literal-byte fixture storage |
| Complete trusted replay and canonical import rejection | `Replay`, A2 `Create`, codec input/event reconstruction; mutation tests | Confirmed |
| Authorization before retry; original bytes/current state | `Apply` and retry loop at every retained cut | Confirmed |
| World/RNG remain unchanged; buffers do not mutate state | `SerializeState`, immutable creation projection, buffer-isolation and six-trace assertions | Confirmed |
| Fixed selected context; no public activation | Setup/config guards, trusted creation context, scoped diff | Confirmed |
| Full suite 1,886 passes | Existing `/tmp/b1-suite.log` summary inspected | Confirmed retained evidence; not independently rerun |
| Build and format clean | Author evidence; empty retained format log | Reported evidence only; not independently rerun |
| Worker RED before implementation | Author account | Not independently verified; no readiness consequence given current executed checks |

## Verification Performed

- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatOpeningPreambleTests' '-bl:/tmp/b1-review2-{}.binlog'`: initial sandbox attempt exited134, local named-pipe bind denied before tests. Same command with approved escalation passed12, failed0, skipped0, duration2s418ms. Successful binlog `/tmp/b1-review2-20260919-214541--51463--mheEby-dotnet-test.binlog` exists.
- `PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-opening-preamble-v1.py`: exit0; six traces,30 cuts,1050 event mutations,3348 state mutations,192 raw rejections,204 boundary/retry checks.
- `shasum -a 256 -c .planning/combat-task008-delivery/b1-source.sha256`: all five source/project fingerprints OK.
- `git diff --check`: exit0.
- Inspected existing full-suite log:1886 passed,0 failed,0 skipped. No builds or implementation edits performed.

## Open Questions And Residual Risks

No unresolved B1 blocker. Tests run from existing binaries under authorized no-build review boundary. Fixed hashes intentionally couple adapter to certified profile; future profile expansion must not bypass frozen-context restrictions. Actor metadata remains trusted internal input, not remote authentication. B1 cannot establish durable publication, resolve Weather, restore noninitial Snapshot12 or admit later gameplay; retained plan assigns each obligation. Full-suite/build/format were not independently repeated.

## Verdict

**Ready** for bounded B1 slice. Implementation and revised dependency plan satisfy inspected requirements; no approval of parent Task008 completion or public activation implied.

## Recommended Next Actions

Return report to lead; proceed under configured sequential review policy. After B1 acceptance, implement C from complete creation-rooted opening history, then B2. Reviewer starts no additional review instance.
