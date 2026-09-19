# B1 Independent Review

Review instance: 3 of 3.

## Preliminary ledger — recorded before author explanation

Scope verified: branch `codex/combat-task008-opening-preamble`, HEAD/base `b67b4f9bffccd7e5114ef0b39cf1ef3b1c3f3e92`; working tree contains three new production files, one test file, test fixture inclusion, synchronized docs and execution ledger. No prior review reports read. CCE search used first; exact local files used for full review because search supplied abbreviated chunks. Session recall returned unrelated historical decisions, none used as review evidence.

- Replay derives initial authority using existing Created11/Snapshot12 validator, admits four bounded transitions, recomputes complete event bytes, and returns immutable projection; no independently supplied prefix becomes authority.
- Tests inspected before codec details: six frozen vectors at five cuts, retries at each later cut, actor rejection, changed command rejection, source/hash/cache forgery, context rejection, malformed bytes, unchanged World/RNG and caller-buffer ownership.
- Exact fixed setup/configuration hashes enforce selected profile; inherited creation context owns remaining Rules/Content validation. Check downstream handoff remains explicit and dormant.
- Candidate concern: standalone input codec validates primitive shape but not command-specific nullable values. Apply/Replay do compare with expected command before accepting state. Compare oracle boundary before deciding whether actionable.
- Plan split B1 → C → B2 matches frozen Weather/stage-entry dependencies; generic Snapshot12, public activation and actual persistence remain visibly open.
- No confirmed correctness blocker from initial inspection. Focused runtime/oracle checks pending; author claims not yet read.

## Findings

No actionable findings. Preliminary input-codec concern closed: frozen oracle `parse`/`typed` similarly handles shape independently; `_emit` validates command-specific values against causal occurrence. C# `Emit` and exact retry comparison reject those values before authority changes. No broader standalone semantic-reader contract is introduced.

## Plan Review

Task008 execution-index refinement is correct: B1 ends at state5 Weather entry; C reconstructs accepted opening history and reaches state6; B2 requires actual Weather history before four stage transitions reach state10; D follows B2. Frozen opening, Weather and stage-entry contracts directly support this ordering. B parent and H restore stay open. Reviewed README, technical design, naming guide, roadmap and execution index consistently describe dormant capability and exclude publication/full restore. No hidden current B1 requirement found in excluded Weather, public activation, persistence or generic Snapshot12 work.

Three new source files plus test and csproj fixture link fit bounded primary-file scope. Old codec/source behavior remains untouched. No provider/model I/O or registration introduced. Core owns authoritative reconstruction throughout.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Four creation-rooted transitions with exact canonical authority | `CampaignOpeningPreamble.Replay`, `Emit`, `CampaignCreationSnapshotV12.Create`, frozen-vector tests | Confirmed | Imported event fields cannot supply authority independently |
| Actor authorization precedes receipt lookup; retry returns original event/current state | `Apply`, `Authorize`, six trace tests at every later cut | Confirmed | No retry bypass or double prefix advance found |
| Selected fixed context only; caller campaign/seed retained | Setup/config guards, `CampaignCombatCreationContext`, request codec, context rejection tests | Confirmed | Intentional scope restriction matches fixed oracle registry |
| World/RNG preserved; caller buffers cannot alter projection | Shared validated creation state; owned read-only lists; isolation test | Confirmed | No resource reset or RNG consumption |
| Sixty frozen fingerprints/thirty state cuts | Six-case fixture test loop and independently executed focused suite | Confirmed | Hash/length comparison proves fixture parity without storing regenerated expectations |
| Full suite 1,886 passed; successful format | `/tmp/b1-suite.log` contains 1,886 passed/zero failed/skipped; empty format log and retained evidence | Full-suite result confirmed; format exit retained author evidence | Reviewer did not rerun full suite or format |
| Initial worker RED and zero-warning build | Retained author evidence; not rerun in read-only review | Unverified independently | No stronger claim made |
| No publication, general Snapshot12, Weather or active gameplay claim | Production scope and synchronized plan/docs | Confirmed | Readiness applies only to B1 |

## Verification Performed

- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatOpeningPreambleTests' '-bl:/tmp/b1-review3-{}.binlog'`: initial sandbox run exited 134 before tests because local named-pipe binding was denied. Same command with approved escalation passed **12/12**, zero failures/skips, duration 2s310ms. Binlog `/tmp/b1-review3-20260919-214836--51894--0D4Co7-dotnet-test.binlog` exists.
- `PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-opening-preamble-v1.py`: passed six traces, thirty cuts, 1,050 event mutations, 3,348 state mutations, 192 raw rejections, 204 boundary/retry checks. Oracle output correctly labels this contract-only evidence.
- `shasum -a 256 -c .planning/combat-task008-delivery/b1-source.sha256`: all five source/test/project fingerprints match.
- `git diff --check`: passed.
- Inspected retained `/tmp/b1-suite.log`: full suite 1,886 passed, zero failures/skips, 3m15s313ms. No build or source modification performed by reviewer.

## Open Questions And Residual Risks

No unresolved B1 blocker. Tests cover bounded frozen profile; they do not certify later family composition, all future campaign IDs/profiles, public command admission, trusted published archive heads or provider durability. Current four-event bound makes replay cost acceptable; downstream adapters must preserve complete causal evidence rather than promote private projections to independent authority. Focused verification reused existing build; retained source fingerprints and source review tie this pass to frozen worktree, while lead owns full build evidence.

## Verdict

**Ready** for bounded B1 opening-preamble slice.

## Recommended Next Actions

Lead may accept B1 and advance to Weather C while retaining B2 and full restore/publication gates. Review instance 3 of 3 complete; no further review initiated. Conditional fourth-review exception is unnecessary because no remaining blockers found.
