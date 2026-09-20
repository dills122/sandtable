# B2 stage-entry independent review

Review instance: 2 of 3.

## Preliminary ledger — before author explanation

- Scope verified: HEAD `a0babdb157c28d048601e3b3a1cceaa28cfe849c`, branch `codex/combat-task008-stage-entry`; three untracked implementation files and one untracked test file included, alongside seven tracked metadata/project changes.
- Read canonical stage-entry specification, oracle kernel and test suite before implementation rationale. CCE search was unhelpful and exposed only a brief evidence-file snippet; no author explanation or prior reports read. Session recall exposed earlier accepted contract decisions, not current review findings.
- No actionable implementation finding identified. Replay derives Weather authority; four explicit-none checks precede predecessor replay; Emit enforces causal occurrence and System actor. ReadState compares complete canonical replay bytes. Retry matches complete accepted typed input and returns current state.
- Tests cover twelve frozen traces, every cut, all Weather outcomes, both order choices, source/prefix/receipt bytes, null Reserve side, unchanged World/RNG, old-reader rejection and semantic/history/raw negatives.
- Plan keeps B2 within four stage entries; Reserve execution, noninitial Snapshot12 and publication remain separately owned. B parent appropriately stays open during review.
- Pending verification: focused existing-binary test run, author-claim reconciliation, inherited schema/authority context checks.

## Findings

No actionable findings. Frozen schema, oracle and C# implementation agree on command/event versions, field order, four edges, source references, receipt domain and replay admission. Canonical byte equality rejects malformed event fields beyond extracted input; state reader never admits a supplied cache as authority.

## Plan Review

Task008 B2 acceptance covered: complete Created11/opening/Weather provenance, versions6–10, four explicit-none gates, all Weather kinds, chronological receipts5–9 and first-acting-side/null-side Reserve handoff. `docs/design/combat-cycle-implementation-plan.md` execution ordering A2→B1→C→B2→D matches actual dependencies. No artificial Weather predecessor or premature generic Snapshot12 admission found.

Implementation limited to three internal production files, one test file and fixture project wiring. README, technical design, naming and roadmap explain delivered private capability while preserving later gates. Full restore/H, Reserve/D, first opening/019A, positive obligations and trusted publication remain valid exclusions, not omitted B2 requirements. B/B2 completion bookkeeping should occur after required review sequence, as currently planned.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Complete predecessor replay and policy-first admission | `CampaignCombatStageEntry.Replay`, `RequirePolicy`; frozen oracle `initial` | Confirmed | No cache authority substitution |
| System authorization precedes retry lookup; retry returns current state | `Apply`, `Authorize`, `Emit`; every-cut retry tests | Confirmed | Commonwealth fleet position cannot authorize side actor |
| Exact twelve chains/sixty cuts/228 fingerprints | `Cases`, `FrozenTracesMatchEveryCutAndPreserveWeatherAuthority`; fixture linkage and independent focused run | Confirmed | Literal expected fingerprint evidence independent of new serializer |
| Weather/World/order/RNG unchanged; nine receipts and null Reserve side | `SerializeState`, `Emit`; immutable-field and final-order assertions | Confirmed | Valid handoff to separately owned Reserve work |
| Sources/catalog and legacy artifacts unchanged | Working-tree scope, schema, oracle source checks, fixture and old-reader tests | Confirmed | No historical byte or registry activation changes |
| Retained full build/suite evidence | `/tmp/b2-build.log`, `/tmp/b2-suite.log`, `b2-source.sha256` | Confirmed as retained evidence | Build zero warnings/errors; full1,945 pass; reviewer did not rerun full gate |

## Verification Performed

- `git status --short`, `git rev-parse HEAD`, `git branch --show-current`, `git diff --stat`, `git diff`, `git diff --check`: target verified; whitespace check exit0.
- `shasum -a 256 -c .planning/combat-task008-delivery/b2-source.sha256`: all five primary files OK.
- `dotnet --version`:10.0.400; repository global.json native MTP, SDK-style net10.0/xUnit MTP project confirmed.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatStageEntryTests' '-bl:/tmp/b2-review2-{}.binlog'`: initial sandbox attempt exited134 because local named-pipe bind denied. Same command outside sandbox succeeded:17 passed,0 failed/skipped,5s399ms. Successful binlog `/tmp/b2-review2-20260919-222825--56627--+pjppY-dotnet-test.binlog` exists.
- Inspected unchanged Python oracle source and `/tmp/b2-stage-baseline.log`: retained PASS12 traces/60cuts/11052 leaf mutations/1299 raw rejections/3552 boundary-retry checks. Oracle not rerun in this review.
- Read canonical stage-entry specification/schema, predecessor Weather/opening requirements, creation authority and sequence catalog requirements; read tests before author explanation and recorded preliminary ledger first.

## Open Questions And Residual Risks

No unresolved B2 question. Existing-binary focused run paired with retained source hash/build/full-suite evidence; no independent rebuild, as instructed. Public authentication, trusted archive head, actual persistence and complete campaign capacity remain later gates. No prior review reports read; author evidence exposed earlier round verdict only after preliminary review had been recorded.

## Verdict

**Ready** for bounded B2 slice. Both implementation and plan supported by inspected canonical evidence and focused execution. This verdict does not close parent008 or activate gameplay.

## Recommended Next Actions

Return report to lead; complete prescribed third review, then reconcile B/B2 completion status and proceed to D only after acceptance. No fixes or extra workstreams requested.
