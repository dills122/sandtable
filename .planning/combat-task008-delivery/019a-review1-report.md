# Task019A independent review

Review instance: 1 of 3

## Preliminary ledger — recorded before author packet

Scope verified: branch codex/combat-task019a-first-opening, HEAD fa5e7236aa1f314ac94a370d43878ac95143ac51. Five frozen primary-file hashes match. Four new primary files are untracked; completion codec changes only cycle-writer visibility. Documentation changes reviewed separately.

CCE search inadvertently returned opening paragraph from 019a-author.md before independent pass; full author packet/evidence not read. No prior review report read. Canonical spec, tests, full implementation and predecessor completion code inspected directly after CCE returned signatures rather than sufficient source.

- No actionable defect found in preliminary source inspection.
- Atomic projection: single completion reconstructs trusted predecessor, adds exactly one prefix/receipt, projects existing completion's version/position/cycle, and preserves predecessor World/members/weather/RNG.
- Retry: input shape and resolved actor checked before occurrence lookup; exact designation retry after completion returns original event and terminal state; changed and cross-family occurrences reject.
- Authority: all admission methods replay complete Created/preamble/weather/stage history; no standalone state/evidence admission. Event parser dispatch cannot bypass canonical byte comparison in predecessor readers.
- Tests cover 16 fixture traces, 40 state cuts, both actors/selections, current-state retry, leaf/raw cache mutations and coherently re-signed cycle forgery. Need independently execute focused tests and reconcile final evidence.
- Plan correctly keeps Movement, general Snapshot12, production publication and Task008 parent open. Current 008D/019A status is provisional pending review/integration.

## Findings

No actionable findings. Review covers frozen working-tree delta, not production publication or later gameplay.

## Plan Review

Plan ordering D1 → D2 → 019A → inherited Movement is sound. Task019A closes the missing terminal projection rather than weakening D1's precompletion World validation. Wrapper retains genuine D1 predecessor; existing D2 completion event remains sole transition. No contract, public API, host registration, persistence provider, migration or rollback requirement introduced by this dormant internal addition.

Task008 parent, 008E–H, general Snapshot12, authenticated ingress and HOST-PUB-001 remain explicitly open. README, technical design, naming overview and roadmap agree on implemented private first opening and deferred Movement. Canonical plan's 019A in-progress status is appropriately provisional until lead reconciles review/integration; update that status during closeout. No plan pivot required.

## Author-Claim Reconciliation

| Claim | Inspected evidence | Status | Consequence |
| --- | --- | --- | --- |
| Same completion event atomically projects terminal state | Opening.Replay; OpeningState constructor; D2 ReadEvent/Generate; codec output | Confirmed | One version/position/cycle transition and one prefix/receipt append |
| All 16 histories, 40 cuts and 152 artifacts covered | CompleteFrozenTracesAtomicallyOpenCycleAndRetryAtEveryLaterCut; canonical fixture calls for request, creation, predecessor records, each state/input/event and opening base | Confirmed by source and passing focused suite | Positive authority and exact canonical bytes demonstrated across both choices/selections and Weather cases |
| Exact retries return original event/current state | Both Apply overloads; every-later-cut golden trace assertions | Confirmed | Designation retry after completion retains terminal state and original designation receipt |
| Actor/shape precede accepted occurrence lookup | SerializeInput then Authorize before both lookup branches; actor and conflict tests | Confirmed | Wrong actor and changed/cross-family occurrence cannot obtain duplicate response |
| Supplied cache or forged completion cannot establish authority | Full-history D1/D2 reconstruction; ReadState exact comparison; coherently re-signed ordinal/cache negative | Confirmed | Self-consistency alone insufficient |
| World, members, receipts, Weather, RNG and order preserved | Predecessor delegation, append-only receipts, terminal assertions and golden states | Confirmed | No resource reset or hidden second transition |
| Buffer ownership stable | CopyEvents, serialization into new arrays, mutation test | Confirmed | Caller byte-buffer mutation does not alter accepted projection |
| General persistence/publication remains deferred | Internal-only classes, no host/action diff, docs boundaries | Confirmed | Ready verdict limited to dormant first-opening slice |
| Root build/format/full suite | Author evidence states build/format passed and full suite running | Not independently rerun | Lead owns final integration evidence; no full-suite claim from this reviewer |

## Verification Performed

- Read AGENTS.md and independent-review skill; established branch/HEAD/status and working-tree boundary.
- `shasum -a 256 -c .planning/combat-task008-delivery/019a-source.sha256`: all five files OK at start and end.
- `git diff --check`: exit 0.
- Read canonical Reserve spec/schema/oracle replay/apply/emit/readback, inherited-successor opening/authority requirements, changed plan/docs, all five primary source/test files, and relevant unchanged D1/D2 context.
- Read `/tmp/d-reserve-baseline.log`: retained canonical oracle PASS (16 traces, 40 cuts, 2342 leaf mutations, 733 raw rejections, 1578 boundary/retry checks, 4 frozen-kernel parity cases, 14 pins). Did not rerun unchanged Python oracle; no canonical change or discrepancy warranted it.
- Native MTP/xUnit v3 mode confirmed from global.json, SDK-style test project, Directory.Build.props and Directory.Packages.props.
- Executed exactly:

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatReserveOpeningTests' --filter-class '*CombatReserveCompletionTests' --filter-class '*CombatReserveDesignationTests' '-bl:/tmp/019a-review1-{}.binlog'
```

Initial sandbox execution failed before test run: local named-pipe SocketException (13), permission denied, exit 134. Same no-build command rerun outside sandbox with approved escalation: exit 0, 67 passed, 0 failed, 0 skipped, 7.454 seconds. No build, code edit or commit performed. Only this report written intentionally.

## Open Questions And Residual Risks

No unresolved correctness question within closed profile. Repeated full predecessor replay and duplicate private-state serialization are bounded costs; profile has at most two Reserve records and one own member. No general-capacity claim follows. Internal constructors remain callable by trusted assembly code, so future Movement implementation must continue admitting full history rather than accepting fabricated wrapper objects. Production trusted-head comparison, authenticated actor mapping, durable atomic commit and ambiguity/recovery remain explicitly separate gates.

Independent pass was not perfectly blind: CCE search leaked author packet's first paragraph before preliminary ledger. Full explanation/evidence read only after ledger; no prior review reports consulted.

## Verdict

**Ready** for scoped dormant Task019A first-cycle opening, supported by canonical inspection, frozen artifact parity and independently passing 67-test focused suite. This verdict does not close Task008, Movement, Snapshot12 restore or HOST-PUB-001.

## Recommended Next Actions

Lead should reconcile full solution/build/format results, complete configured remaining review rounds, and update provisional task status before delivery. No corrective implementation work requested by this review. No extra review instance or experiment started.
