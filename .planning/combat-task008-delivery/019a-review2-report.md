# Independent review — Task019A

Review instance: 2 of 3

## Preliminary ledger — before author packet

- Scope verified: branch `codex/combat-task019a-first-opening`, HEAD/base `fa5e7236aa1f314ac94a370d43878ac95143ac51`; five primary source/test hashes match frozen manifest. Four new files plus one visibility-only codec edit; retained documentation delta matches bootstrap.
- Canonical Reserve designation/completion, inherited-successor and cycle-sequence contracts inspected before author packet; source and tests inspected directly after CCE located entry points. CCE search inadvertently exposed three opening sentences of author intent; full author rationale/evidence and prior review reports were not read. No implementation conversation inherited.
- Atomic projection appears sound: completion reuses D2 `ReadEvent` and canonical bytes; projection holds precompletion authority and completion together, extending prefix and receipts exactly once. Terminal state11/12, Movement position and ordinal1 derive from same event.
- Retry ordering appears sound: typed input validation and actor authorization precede accepted-occurrence lookup; same-kind equality covers complete records, cross-kind prior-version reuse rejects; duplicate returns fully replayed current state.
- Full-history admission appears sound: no supplied base/cache admission; preamble/Weather/stage/designation reconstruction remains delegated to existing readers. Exact state-byte comparison rejects forged caches. Optional designation plus completion grammar rejects duplicate/reordered/extra records.
- Tests cover 16 literal fixture cases/40 cuts and terminal retry, actor, cross-family, cache, history, raw/scalar and buffer-mutation negatives. No actionable defect identified in preliminary inspection. Remaining check: execute focused no-build suite and reconcile author claims with observed evidence.
- Plan preserves B2 → D1 → D2 → 019A → E dependency, five-primary-file boundary, dormant scope and open HOST-PUB-001; no parent Task008 completion claim.

## Findings

No actionable findings. No code or plan correction required for this bounded slice.

`CampaignCombatReserveOpening.Replay` (lines 9–30) admits only optional designation followed by completion, reusing D1 reconstruction or D2 history-derived canonical readback. `Apply` (lines 33–72) validates shape/actor before lookup, checks complete accepted-input equality and returns current replay state for both retry types. Same-prior-version cross-family collisions reject before fresh-command handling. `ReadState` (lines 75–83) compares complete canonical projection against full retained history. No caller-supplied OpeningBase or state establishes authority.

`CampaignCombatReserveOpeningState` constructor derives terminal prefix/receipt from same validated completion used for version, position and cycle. Codec preserves predecessor World/members, Weather/RNG/order/holder and exact schema order. Keeping genuine D1 predecessor avoids weakening D1's precompletion World checks. Existing completion codec changes only `WriteCycle` visibility, preserving frozen encoding and identity framing.

## Plan Review

Plan matches implementation and canonical acceptance criteria: terminal empty/I histories reach state11/state12, ten/eleven receipts, ordinal1 and exact first relative slot Movement position. Full sixteen-case Weather/order/selection matrix covers forty cuts and all 152 frozen artifact fingerprints. D1 remains precompletion; no second opening command/event, resource reset or remote work introduced.

Five-primary-file cap respected. Documentation updates identify D2 checkpoint and active 019A work without closing inherited Movement, shared Snapshot12 restore, public activation, parent Task008 or actual publication evidence. B2 → D1 → D2 → 019A → E remains acyclic. No missing rollout/migration step: code remains internal and dormant; HOST-PUB-001 explicitly owns future durable publication proof. Status stays in progress pending lead acceptance, which is appropriate during review.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Same completion2 event atomically opens ordinal1 and advances position | Opening replay/model; unchanged D2 generator; fixture checks and explicit terminal assertions in `CompleteFrozenTracesAtomicallyOpenCycleAndRetryAtEveryLaterCut` | Confirmed | No separate authority event or partial externally returned transition |
| All 16 histories, 40 cuts, 152 artifact fingerprints covered | `Cases`, `CheckGolden` calls for request/creation/predecessor records, states, inputs, events and OpeningBase; focused execution | Confirmed | Literal fixtures independently constrain projection and canonical encoding |
| Exact retry returns original event/current state; actor checked first | Both `Apply` overloads; per-cut retry and wrong-actor assertions | Confirmed | Designation retry after completion preserves terminal cycle/ledger |
| Changed and cross-kind consumed occurrences reject | Prior-version checks and full record equality; `ClosedTerminalStateRejectsChangedRetriesAndCrossFamilyOccurrenceReuse` | Confirmed | No stale fallback permits occurrence rebinding |
| Full history required; cache and coherently re-signed cycle cannot supply authority | D1/D2 replay delegation; `ReadState`; missing/reordered/foreign history and re-signed event/cache tests | Confirmed | Handoff cannot trust standalone wrapper/cache |
| World/resources/history preserved and buffers cannot mutate state | Guarded D1 World writer; terminal predecessor World/member equality; per-cut golden fingerprints; buffer mutation test | Confirmed | Wrapper does not broaden World admission |
| Build, format and full suite pass | Retained build/format logs, refreshed evidence and `/tmp/019a-suite.log`; independently rerun focused 67 tests | Confirmed with provenance distinction | Build/full suite are lead runs inspected by reviewer, not independent reruns |
| Worker RED missing implementation | Author evidence summary only; original RED log not inspected | Unverified historical claim | No bearing on current readiness; current implementation/tests independently inspected |

## Verification Performed

- `git status --short`, `git branch --show-current`, `git rev-parse HEAD`, tracked diff and untracked primary files inspected. Frozen target matches `fa5e7236aa1f314ac94a370d43878ac95143ac51` plus declared working-tree delta.
- `shasum -a 256 -c .planning/combat-task008-delivery/019a-source.sha256`: all five files match before and after review.
- `git diff --check`: exit 0.
- SDK/platform checked: `dotnet --version` → `10.0.400`; global native MTP mode, SDK-style net10.0 xUnit v3 MTP project and central configuration verified.
- Independently executed:

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatReserveOpeningTests' --filter-class '*CombatReserveCompletionTests' --filter-class '*CombatReserveDesignationTests' '-bl:/tmp/019a-review2-{}.binlog'
```

Initial sandbox attempt exited 134 because MTP local IPC socket bind was denied; no tests executed in that attempt. Same command with approved escalation passed: 67 succeeded, 0 failed, 0 skipped, 6.830 seconds. Successful-run binlog exists at `/tmp/019a-review2-20260919-232447--64010--Q5C0Cd-dotnet-test.binlog`.

Retained lead logs inspected: `/tmp/019a-build.log` reports successful solution build, zero warnings/errors; `/tmp/019a-format.log` is empty, with exit 0 reported in evidence; `/tmp/019a-suite.log` reports 2,012 succeeded, zero failed/skipped. Reviewer did not build, rerun format, or rerun full suite.

Canonical Reserve schema and oracle replay/apply/projection inspected; `/tmp/d-reserve-baseline.log` reports 16 traces, 40 cuts, 2,342 leaf mutations, 733 raw rejections, 1,578 boundary/retry checks, four frozen-kernel parity cases and fourteen source pins. Python oracle not rerun; no changed contract/oracle artifact or new concern justified repetition. Its counts remain baseline contract evidence, distinct from executed C# tests.

## Open Questions And Residual Risks

No blocking open question. Closed initial profile establishes neither general campaign capacity nor later-cycle behavior. Internal constructors are trusted assembly mechanisms, not restore admission. Future Movement integration must continue to require complete accepted records and trusted published-head/authenticated-ingress checks. Durable publication and process recovery remain HOST-PUB-001 obligations. Finite repeated replay cost accepted within at most two Reserve records; no broader performance claim.

Review independence limited only by CCE's unsolicited three-sentence author-intent snippet before preliminary ledger; full author packet remained unread until ledger was written, and no prior reports or implementation conversation were consulted.

## Verdict

**Ready** for bounded Task019A first-opening slice. Source, canonical contracts, plan and focused execution agree; no actionable finding or heavy pivot required. This verdict does not close Task008, public activation, general restore or HOST-PUB-001.

## Recommended Next Actions

Lead records acceptance and proceeds with already scheduled final review round under existing three-round limit. On acceptance, retain exact test evidence and update 019A status before checkpoint/PR. No extra experiment or new workstream warranted by this review.
