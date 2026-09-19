# Independent review — Task008 D2

Review instance: 3 of 3. Branch `codex/combat-task008-reserve-completion`; HEAD/base `073423f35c8be6bdb43e4b1579a705285408b934`; explicit working-tree delta including four untracked source/test files. All five source SHA-256 pins match manifest.

## Preliminary ledger — before author packet

- Read canonical Reserve specification/schema, inherited-successor specification/schema, cycle identity oracle framing, source changes and all completion tests. No actionable defect established in first pass.
- Full history feeds D1 replay before base construction; retained replay-produced request avoids trusting caller context independently. Event validation recomputes bytes and compares entire record.
- Authority binary field order/types match canonical `AUTH_FIELDS`: ruleset/setup/content hash values are strings; openingPrefix/policy digest are raw hashes. Completion receipt excludes only receiptId; openingPrefix is predecessor prefix.
- Tests compare 16 retained fixture rows using byte length and SHA-256; direct assertions cover ordinal, resolved owner, state version, position, RNG preservation and legacy rejection. Forge, history and caller-buffer tests are present.
- D1 remains precompletion only. D2 exposes evidence plus predecessor, not terminal state. D/019A/E dependency refinement explicitly retains atomic same-event projection, terminal replay/retry/readback, and Movement handoff with 019A. No parent completion or HOST-PUB-001 closure claimed.
- Pending: verify canonical fixture/source provenance, inspect retained integration logs, reconcile author claims and any evidence gaps.
- Independence limitation: initial CCE search accidentally returned short author/evidence introductions and prior-report headings/first ledger fragments. No prior report was opened; author packet remains unread at this ledger. Thus first pass was mostly blind but not wholly blind.

## Findings

No actionable findings. Exact frozen completion bytes and authority framing are consistent with canonical requirements for this bounded codec slice.

## Plan Review

Original Reserve specification requires one completion event that advances Movement and opens ordinal1 atomically. `docs/design/combat-cycle-implementation-plan.md`, Task008 execution index and D execution refinement, preserve that requirement through B2 → D1 → D2 → 019A → E. D2 generates that exact event; it does not falsely expose a completed Reserve projection. `CampaignCombatReserveDesignation.Replay` still rejects completion records and `CampaignCombatReserveCodec.SerializeState` retains null terminal fields. Tests explicitly establish this boundary.

019A remains responsible for applying the same event to state11/12, resulting prefix, complete receipts, terminal readback, exact retry (including designation retry after completion), and Movement handoff. No separate event or command is proposed. Parent008D, Task008 restore, public activation and HOST-PUB-001 remain open. Five-primary-file split is coherent, preserves original ownership and does not turn incomplete terminal behavior into completed acceptance. README, technical design, naming guide and roadmap agree on this limitation.

## Author-Claim Reconciliation

| Claim | Evidence | Status / consequence |
| --- | --- | --- |
| Genuine history alone derives base | `CampaignCombatReserveCompletion.DeriveBase` calls D1 Replay and retains replay-produced creation request; D1 composes stage/Weather/preamble readers | Confirmed; no caller base/cache admission |
| Frozen base/input/event bytes and ordinal1 match | All 16 fixture cases, `CheckGolden`, canonical schema and Reserve oracle `_base`/`_completion_event` | Confirmed by source inspection plus retained 44-pass execution log |
| Cycle framing distinguishes string hashes from raw digests | Codec `CycleId`; canonical `verify-combat-cycle-sequence-v1.py` `AUTH_FIELDS` and `tuple_bytes`; independent test `Identity` | Confirmed; U32/U64 big-endian fields and NUL domain match |
| Receipt and event readback reject coherently re-signed forks | `ReceiptId`, `SerializeEvent(false)`, `ReadEvent` full canonical equality; mutation tests re-sign cycle and receipt | Confirmed |
| World/member/RNG remain unchanged | Base serializes replay predecessor; guarded D1 World serializer and unchanged extracted member writer; goldens and unchanged-state assertions | Confirmed for closed first-side profile |
| Caller buffers cannot mutate retained evidence | `ReadEvent` copies bytes, replay builds typed state, EventBytes produces fresh arrays; buffer mutation test | Confirmed for tested buffers; internal records remain trusted assembly objects |
| 44 focused / 1,989 full tests; build clean | `/tmp/d2-final.log`, `/tmp/d2-suite.log`, `/tmp/d2-build.log` | Confirmed as retained lead execution evidence, not independently rerun here |
| Format passed | Empty `/tmp/d2-format.log` and evidence declaration | No contradiction, but empty log alone cannot prove process exit status |
| Terminal semantics remain future work | Models XML summaries, D1 rejection test, plan/README/technical design | Confirmed; no 019A runtime proof inferred from broader Python oracle |

## Verification Performed

- `git status --short`, `git branch --show-current`, `git rev-parse HEAD`: expected branch/base and explicit working-tree scope; no unexpected primary path.
- `shasum -a 256 -c .planning/combat-task008-delivery/d2-source.sha256`: all five pins passed at initial inspection and final verification.
- `git diff --check`: passed.
- `git diff --exit-code --` canonical Reserve spec/schema/fixture/oracle and cycle spec/schema/oracle paths: passed; historical contracts unchanged.
- Read-only Python SHA-256 comparison of every fixture `sourceHashes` entry: all 14 passed; fixture contains 16 cases.
- Inspected canonical Reserve oracle `_base`, `_completion_event`, `_emit`, source audit, cycle `AUTH_FIELDS`/`tuple_bytes`, inherited schemas and frozen compatibility requirements.
- Retained `/tmp/d-reserve-baseline.log`: PASS, 16 traces, 40 cuts, 2,342 leaf mutations, 733 raw rejections, 1,578 boundary/retry checks, four parity cases and 14 source pins. Not rerun because no canonical drift or concern justified repeating it.
- Retained focused command: `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatReserveCompletionTests' --filter-class '*CombatReserveDesignationTests' '-bl:/tmp/d2-final-{}.binlog'`; log reports 44 passed, zero failed/skipped.
- Retained full command: `dotnet test --solution Sandtable.slnx --no-build '-bl:/tmp/d2-suite-{}.binlog'`; log reports 1,989 passed, zero failed/skipped, 3m21s916ms. Build log reports zero warnings/errors.
- No build, test rerun, code mutation, commit or delegation performed. Only this report written.

## Open Questions And Residual Risks

No blocking question. Evidence is finite first-side/first-stage coverage, not general campaign capacity. Runtime actor authentication, durable publication and trusted published-head comparison remain external gates. Internal evidence constructors do not provide a security capability; 019A must validate complete accepted history instead of trusting arbitrary internal record construction. Repeated bounded replay is acceptable at this scope; no measured general-scale performance claim.

## Verdict

**Ready** for Task008 D2 codec slice. This does not close full Reserve replay,019A, Task008 parent restore, or HOST-PUB-001.

## Recommended Next Actions

Lead may record D2 acceptance and proceed through normal branch/PR integration. Preserve019A same-event projection and terminal retry/readback requirements before Movement. Review instance limit reached (3 of3); no further review or experiment initiated, and no blocker requires conditional experiment.
