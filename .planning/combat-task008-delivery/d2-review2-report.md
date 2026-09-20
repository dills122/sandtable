# Independent review — Task008 D2

Review instance: 2 of 3. Target: `codex/combat-task008-reserve-completion`, HEAD/base `073423f`, explicit working-tree delta including four untracked primary files. Five source hashes match frozen manifest.

## Preliminary ledger — recorded before author packet

Blind first pass inspected canonical Reserve/cycle/successor requirements and oracle functions, implementation, tests, D1 predecessor, plan and documentation diff. No previous report opened. Required CCE recall returned brief historical review summaries; search also exposed short author/evidence snippets before ledger. Full author packet/evidence not yet read; this limits perfect blinding but does not supply review conclusions.

| Concern | Independent evidence | Preliminary result |
| --- | --- | --- |
| Authentic authority versus synthetic OpeningBase | `DeriveBase` calls full D1 replay; no base-taking admission API; request recovered from accepted creation | Satisfied |
| Completion identity framing | `CycleId` matches canonical `AUTH_FIELDS`, including S for rules/setup/content hashes and H only for prefix/config | Satisfied by source; fixture test execution pending |
| Order, state version and scope | Owner from predecessor order, result version checked +1, ordinal1, first-slot Movement5 with null activeSide | Satisfied |
| Receipt/base compatibility | Event order, receipt domain and OpeningBase fields match frozen oracle | Satisfied by source; sixteen-row parity pending execution |
| Forged/reordered/cross-history data | Whole-event recomputation comparison plus strict input reserialization, parent replay, malformed/actor/re-signed-cycle tests | Satisfied for codec boundary |
| D1 regression from shared writer extraction | Only WriteMembers extraction and WriteWorld accessibility changed; serialization order preserved | Focused D1 run pending |
| Plan versus whole Reserve requirement | D1 → D2 →019A→Movement explicitly retains terminal atomic application/retry/readback and parent/HOST-PUB-001 gates | Sound bounded refinement; D2 alone cannot close Reserve replay |
| Test limits | New tests cover bytes and retained predecessor; no terminal projection implemented or claimed | Appropriate; terminal cuts/retries remain019A |

No actionable defect established in preliminary source pass.

## Findings

No actionable findings. Scope is small and explicit: three new implementation files, one new test file and shared D1 writer extraction. Documentation/status changes align with that boundary; no fixture, transport contract, production dispatcher or historical codec change.

Adversarial assessment: independently recomputed canonical event equality prevents replacing cycle owner, ordinal, opening version/prefix, configuration, position, receipt or base identity even when attacker updates hashes. `Create` validates complete expected input after owner authorization. `ReadEvent` recovers only input from untrusted bytes, then reconstructs authority from full parents. Canonical input reserialization rejects duplicate/unknown/reordered keys and alternate JSON spelling; final event equality covers all remaining fields. Record size/depth limits apply before JSON materialization. Generated arrays/member counts are bounded by inherited closed profile, not attacker-supplied collections. No model/remote I/O, RNG advancement or World mutation occurs here.

## Plan Review

`docs/design/combat-cycle-implementation-plan.md` Task008 index preserves D1→D2→019A→E ordering. Original successor contract assigns008D codecs and019A first opening; refinement separates optional designation from completion encoding without creating another opening event or relaxing full-history provenance. D2 provides both Movement target and ordinal1 in one completion event, preserving atomicity requirement for019A to apply. No new requirement needs deferral beyond declared ownership.

019A remains responsible for result prefix, appended receipt, terminal ReserveState1 version11/12, combined optional-designation/completion replay, exact retry (including designation retry after completion), readback, and Movement handoff. Task008H must still prove retained-history restore with fresh admission disabled. Parent008, public activation and HOST-PUB-001 remain open in retained plan/docs. No dependency cycle, missing mandatory successor or premature parent closure found.

README, technical design, naming overview and roadmap consistently describe codec completion separately from terminal projection. Execution index appropriately remains in progress during review. Later integration should update final review/test status after remaining scheduled checks, without representing this report as completed019A.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Full-history-only authority | `CampaignCombatReserveCompletion.DeriveBase`; D1 `Replay` and `Initial`; missing/foreign/reordered-parent tests | Confirmed | No synthetic OpeningBase admission |
| Exact frozen base/input/event parity for16 traces | `AllCompletionBytesAndCycleOneIdentitiesMatchHistoryDerivedGoldens`; canonical fixture, `_base`/`_completion_event` oracle and44-test run | Confirmed |48 length/hash fingerprints exercise both owners, both selections and four Weather kinds |
| Correct cycle framing and receipt | `CycleId`, `ReceiptId`; canonical sequence `AUTH_FIELDS`; test-side `Identity`/`Receipt` and intentionally wrong framing comparison | Confirmed | No string-hash/raw-digest substitution or recursive receipt |
| Actual advanced RNG/World/member provenance preserved | `SerializeBase`, retained D1 predecessor equality, frozen base fingerprints and caller-buffer test | Confirmed | Codec introduces no resource reset or random draw |
| Existing D1 byte behavior unchanged | `git diff` writer extraction;23 D1 tests included in44 successful tests | Confirmed | Precompletion cuts, typed World rejection and historical fingerprints retained |
| Strict raw/actor/re-signed/foreign rejection | Named negative tests and complete canonical comparison | Confirmed | Tests cover boundary semantics, not merely serializer round trips |
| Worker red-to-green/full lead checks | Worker red narrative not independently rerun; build log inspected (0 warnings/errors); format log empty; full suite pending in inspected evidence | Partly observed | No invented red/full-suite pass claim; focused independent run supplies local proof |
| No terminal projection or public/durable admission | Exposed API, model summary, unchanged dispatchers and documentation | Confirmed |019A/H/publication gates retain ownership |

## Verification Performed

- `git status --short`, `git branch --show-current`, `git rev-parse --short HEAD`: target matches bootstrap; no hidden committed implementation delta. Four new primary files present alongside modified D1 codec.
- `shasum -a 256 -c .planning/combat-task008-delivery/d2-source.sha256`: all five entries pass before review and after test run.
- `git diff --check`: pass.
- SDK detected10.0.400; repository native MTP and xUnit configuration inspected.
- Exact focused command: `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatReserveCompletionTests' --filter-class '*CombatReserveDesignationTests' '-bl:/tmp/d2-review2-{}.binlog'`. Initial sandbox attempt failed before tests with local IPC `SocketException (13): Permission denied` (exit134). Same authorized command rerun outside sandbox: **44 passed,0 failed,0 skipped**,4.622 seconds;21 D2 +23 D1. No build performed.
- Successful test binlog exists: `/tmp/d2-review2-20260919-230800--61732--I5XLGO-dotnet-test.binlog`.
- Canonical Reserve baseline `/tmp/d-reserve-baseline.log` inspected:16 traces,40 cuts,2342 leaf mutations,733 raw rejections,1578 boundary/retry checks,4 frozen-kernel parity cases,14 source pins. Canonical oracle/schema/source comparison raised no need to rerun unchanged oracle. These are baseline contract results, not reviewer-executed C# terminal-state coverage.
- Lead `/tmp/d2-build.log` inspected: build succeeded,0 warnings/errors. `/tmp/d2-format.log` empty, consistent with reported success but alone not proof of exit status. No full-suite rerun or independent build/format claim.

## Open Questions And Residual Risks

No blocking open question. Bounded replay repeatedly reconstructs predecessors; acceptable at fixed4/1/4 plus zero-or-one designation counts. Internal evidence/model constructors are not a capability boundary against other trusted assembly code;019A must continue full-history validation rather than treating arbitrarily constructed internal records as published authority. Trusted actor metadata still needs authenticated external ingress in later scope. No test here proves atomic durable publication, terminal retry/readback, general restore, later cycle ordinals or Movement adjudication.

CCE retrieval exposed brief author/evidence snippets before preliminary ledger and historical memory summaries; no prior review report was read. Full author packet was read only after ledger creation. No delegated reviews, implementation edits, fixture regeneration, staging or commits performed. Only this report was authored.

## Verdict

**Ready** for Task008 D2 codec scope. Implementation, frozen compatibility, predecessor preservation, negative boundaries and plan separation supported by inspected source plus independent44-test pass. This verdict does not close019A, Task008 parent or HOST-PUB-001.

## Recommended Next Actions

Return report to lead; finish already scheduled bounded review/integration checks. Proceed to019A only under retained full terminal projection/replay/retry acceptance criteria. No corrective code change requested by this review.
