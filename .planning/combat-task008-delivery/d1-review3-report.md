# Independent review — Task008 D1

Review instance: 3 of 3.

## Preliminary ledger — recorded before author explanation

- Verified branch `codex/combat-task008-reserve-designation`, base/HEAD `3ded1ebd8906b04969a9289a8b1b8068b0b8f75c`, seven tracked changed files plus four untracked source/test files. Scope matches bootstrap.
- Inspected canonical designation spec/schema/oracle, stage-entry spec, completion/cycle requirements, new tests before implementation, all three new source files, and plan/navigation diff. Canonical designation fixtures/oracle/spec and completion/cycle sources unchanged against base.
- Replay derives state10 from complete accepted predecessor history; own member derives retained order. Exact-byte event recomputation, actor-before-retry checks, one-event precompletion limit, original-event retry and readback checks align with D1 slice.
- Typed World mutation retains all unrelated fields. Writer compares complete World against history-derived expected World before emitting bounded schema. Tests cover both owners, all four Weather outcomes, canonical hashes, resources, forged events/caches and buffer independence.
- D1/D2/019A split preserves same-event atomic completion and explicitly defers terminal state11/12, post-completion retry, and Movement handoff; no parent closure or public activation claimed.
- No actionable defect identified in preliminary pass. Still to run focused no-build checks and reconcile author claims.
- Independence limitation: required CCE session recall exposed summaries about unrelated historical reviews; initial context search unsolicitedly returned opening scope fragment from `d1-review2-report.md`. No prior report opened, findings/verdict not consulted. Author explanation not yet read. Low-quality retrieval prompted targeted reads of named canonical/source files.

## Findings

No actionable findings. No heavy pivot required.

`CampaignCombatReserveDesignation.Replay`, `Initial`, `Authorize`, `Emit` and `ReadState` preserve causal admission: complete predecessor replay, resolved first-side ownership, exact owner command, byte-recomputed event and independently replayed cache comparison. Retry authenticates actor before accepted-input comparison. No supplied projection becomes admission authority.

`CampaignCombatReserveCodec.WriteWorld` (lines221–303) derives expected initial/designated World and rejects unequal typed World before emitting bounded fields. `CampaignWorldSnapshotV7.Equals` includes every World collection. `DesignatedWorld` retains operational state, TOE/components, ammo/readiness, locations, representation and remaining World collections. Initial-only codec remains unchanged. Raw input and event readers reject malformed or noncanonical bytes through parsing plus exact serialization comparison; cached state is compared directly with replay output.

## Plan Review

Ready for D1 acceptance. Canonical plan lines801–817 retain D1 → D2 → 019A → E ordering and original contract obligations. D2 owns internally history-derived OpeningBase and exact completion2 bytes, including ordinal1 identity. 019A applies that same event atomically, restores terminal state11/12 and full retry/readback, then supplies Movement handoff. No second opening event, synthetic authority, closed parent task or activation implied. This refines existing codec/projector ownership while preserving `combat-reserve-designation-v1.md` and `combat-cycle-sequence-v1.md` atomic completion requirements.

Five primary implementation/test files respect bounded delivery scope. README, design rationale, naming and roadmap agree on precompletion capability and pending gates. Existing publication, general Snapshot12 restore, public ingress and Runner gates remain explicit.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Consequence |
| --- | --- | --- | --- |
| Real state10 and optional own none→I designation | Replay/Initial/Emit, canonical oracle initial/_emit, frozen-cut tests | Confirmed | D1 success criteria met |
| Both owners and all Weather outcomes preserved | Sixteen fixture rows; request/Created/predecessor/state hashes; owner/resource assertions | Confirmed | C# evidence covers 24 precompletion cuts, not 40 complete-contract cuts |
| Exact receipt/history, original-event retry, strict caches | Event hashing, accepted input comparison, readback and forgery tests | Confirmed | No cached-state authority substitution |
| Bounded typed World writer leaves initial reader strict | ExpectedWorld equality guard, reconstructed immutable World, typed-forgery and old-reader tests | Confirmed | Scope duplication acceptable for this closed profile |
| Focused23/full1968/build success | Independently ran focused23; inspected retained suite/build logs and source fingerprint manifest | Confirmed with distinction | Full suite/build are retained author evidence, not independent reruns |
| Completion and post-completion retry deferred | Runtime rejects completion; plan assigns unchanged event to D2/019A | Confirmed | D1 readiness does not close parent D or Task008 |

## Verification Performed

- `git status --short`, `git branch --show-current`, `git rev-parse HEAD`, `git diff --stat`: scope/base confirmed.
- Canonical spec/schema/fixture/oracle and inherited completion/cycle sources checked with `git diff --exit-code 3ded1eb -- <named canonical paths>`: unchanged, exit0.
- `shasum -a 256 -c .planning/combat-task008-delivery/d1-source.sha256`: all five implementation/test fingerprints OK.
- `git diff --check`: exit0.
- `dotnet --version`: 10.0.400. `global.json`, test project and package configuration confirm SDK-style native MTP/xUnit.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatReserveDesignationTests' '-bl:/tmp/d1-review3-{}.binlog'`: first sandbox attempt aborted exit134 on denied local named-pipe bind. Authorized elevated retry passed23, failed0, skipped0 in3s690ms. No build performed.
- Inspected `/tmp/d-reserve-baseline.log`: retained canonical oracle reports16 traces,40 cuts,2342 leaf mutations,733 raw rejections,1578 boundary/retry checks,4 frozen-kernel cases,14 source pins. Canonical source unchanged; no long oracle rerun warranted or claimed.
- Inspected `/tmp/d1-build.log` (zero warnings/errors) and `/tmp/d1-suite.log` (1968 passed, zero failed/skipped). These remain retained lead verification.

## Open Questions And Residual Risks

No unresolved D1 blocker. Bounded World serialization duplicates closed-profile layout; future World expansion must extend validation and preserve golden parity. Internal typed projections/serializers are not general validators or trusted ingress; admission continues to require full replay. No independent build/full-suite/long-oracle run undertaken, per review constraints. CCE contamination limited to scope fragment, disclosed above; author rationale read only after saved preliminary ledger.

## Verdict

**Ready** — D1 implementation and D1/D2/019A plan refinement. No approval for terminal Reserve, generic restoration, production activation or publication inferred.

## Recommended Next Actions

Lead may accept D1 and continue D2, retaining full terminal/retry/Movement evidence for019A. Review instance3of3 complete; no further reviewer started or requested. No code, tests, plan or commits changed by this review; only this report written.
