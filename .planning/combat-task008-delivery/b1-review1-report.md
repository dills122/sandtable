# B1 Independent Review

Review instance: 1 of 3.

## Preliminary ledger (before author explanation)

Scope verified: branch codex/combat-task008-opening-preamble, HEAD b67b4f9bffccd7e5114ef0b39cf1ef3b1c3f3e92; three untracked runtime files plus test, fixture project inclusion and documented plan/navigation changes. No other reviewer reports read. CCE searched first; terse returned snippets required exact-file inspection. Session recall exposed past accepted dependency decisions but no B1 author explanation.

- Replay/Apply regenerate exact canonical events from independently validated Created11 and fixed Setup/configuration context. No caller prefix/state admitted. Retry authenticates first and returns original bytes plus current state.
- Golden tests cover six traces, all five state cuts and all four event cuts; explicit World/RNG preservation and old-reader negatives. Negative tests exercise cross-seed/history and both legal declaration branches.
- Plan split A2 → B1 → C → B2 → D matches canonical opening/Weather/stage dependency chain. Parent B and Task008 remain open. Weather/publication/public activation exclusions justified.
- Investigating primitive bounds and canonical input behavior; no confirmed actionable defect so far. Need executed focused checks and author-claim reconciliation before verdict.

## Findings

No actionable findings. Primitive-bound concern cleared: `ContentContractGuards.RequireSourceAtom` enforces 1–128 safe ASCII characters; expected creation/position identities additionally compare to replay-derived commands. Input shape/type validation plus complete regenerated-byte comparison rejects unknown, duplicate, reordered, oversized or forged records before state is returned. Semantically invalid input combinations may be representable by the private input codec but cannot pass `Emit`; this is not an authority bypass.

Read-only scope maintained; only this report written. Lead corrected formatting during review; no semantic target change observed.

## Plan Review

Ready for this bounded B1 slice. `docs/design/combat-cycle-implementation-plan.md` Task008 index preserves original B ownership through B1 and B2, changes C dependency to B1, and requires C before B2. This matches `combat-opening-preamble-v1.md` terminal state5 and `combat-stage-entry-v1.md` requirement for real Weather/state6 history. D now requires B2/state10, preventing a fabricated stage predecessor. H/noninitial Snapshot12, HOST-PUB-001 publication evidence and public/Runner gates remain explicitly open. README, technical design, naming and roadmap consistently describe dormant opening capability; no gameplay-completion claim.

Implementation uses three small internal source files and one focused test file, plus fixture project inclusion. Contract bytes, old readers, public registration and provider behavior unchanged. Replay has a hard four-event bound; recursive event ancestry retains at most four prior states and creates no unbounded campaign cache. Position/source derivation agrees with frozen golden hashes for all covered positions. Future adapters must consume independently retained creation/event history, as their specs already require.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Consequence |
| --- | --- | --- | --- |
| Four creation-rooted transitions end at Weather | `CampaignOpeningPreamble.Replay`/`Emit`, golden test, canonical opening spec | Confirmed | B1 scope met |
| Exact retries authenticate first, retain original bytes and return current state | `Apply`, retry loop at every golden cut and wrong-actor assertions | Confirmed | No stale projection or extra event on retry |
| Fixed certified context, independent Created11 validation | Setup/config hash guard, `CampaignCreationSnapshotV12.Create`, foreign-context and creation negatives | Confirmed | No self-certified cached authority |
| State, World and RNG remain stable except declared fields | immutable projection/list copies, buffer-isolation test, World/RNG assertions at every cut | Confirmed | No resource or RNG reset introduced |
| 60 frozen fingerprints and 30 state cuts | Six fixture cases × creation/four events/five states; focused run 12/12 | Confirmed | Literal length/hash parity against frozen external oracle |
| Actor metadata is trusted ingress, publication not proven | internal types/XML summary; unchanged public dispatch; plan exclusions | Confirmed | No production authentication/durability claim |
| Worker RED then GREEN, build zero warnings/errors | author/evidence packet, no independent RED/build execution | Unverified by reviewer | Historical claim not used as sole correctness evidence |
| Full gate pending | evidence packet and observed suite log | Confirmed at review time | Lead owns final full-suite/format evidence |

## Verification Performed

- `git branch --show-current` and `git rev-parse HEAD`: exact bootstrap branch/base verified.
- `git status --short`: reviewed explicit dirty/untracked boundary; no unrelated tracked modifications identified.
- `dotnet --version`: 10.0.400. `global.json` selects native MTP, SDK-style net10.0 test project selects xUnit v3 MTP.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatOpeningPreambleTests' '-bl:/tmp/b1-review1-{}.binlog'`: initial sandbox attempt failed before tests with local IPC SocketException/permission denied; same command with approved escalation passed **12/12, 0 failed, 0 skipped**, duration 2.589s. No reviewer build.
- `PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-opening-preamble-v1.py`: exit0; **6 traces, 30 cuts, 1,050 event mutations, 3,348 state mutations, 192 raw rejections, 204 boundary/retry checks**. Oracle remains contract evidence; focused .NET run establishes implementation evidence.
- `git diff --check`: exit0.
- Read lead `/tmp/b1-suite.log`: Core and Intelligence.Contracts projects reported pass; full suite had not yet completed. No independent full-suite or format claim.

## Open Questions And Residual Risks

No blocking open question. Fixed Setup/configuration hashes intentionally support only selected first-turn Axis/no-obligation context. This is a closed adapter, not generic initiative handling. Hash/fingerprint parity does not authenticate publication; trusted ingress and archive head verification remain external obligations. No Weather, later-stage, general Snapshot12, public dispatch, provider or durable recovery behavior verified here.

## Verdict

**Ready** for B1's dormant opening-replay scope. Findings-free implementation and plan review plus independently executed focused tests/oracle support this verdict. Lead must retain final integration gate results before delivery; this verdict does not close parent B, Task008 or publication gates.

## Recommended Next Actions

Retain final full-suite/format evidence. Continue user-required sequential review rounds within existing count; proceed to Weather only after B1 acceptance. Do not widen B1 to stage entry with synthetic Weather history.
