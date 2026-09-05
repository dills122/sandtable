# BRK progress review through Task 005

Review instance: **3 of 3** for implementation flow. Research/design flow remains closed at 3 of 3.
Target: `2e9f9b8..3d5baa330d257a695bc8f9921200e5fa96195f86`, focused `d511ec7..3d5baa330d257a695bc8f9921200e5fa96195f86`.
Branch: `codex/breakdown-adjudication-design`.

## Preliminary ledger — before author explanation

Fresh reviewer inherited no implementation conversation. Scope and excluded user-owned dirt verified.
Required CCE recall exposed prior design and Task 002 verdicts, disregarded as evidence. Indexed status
snippets were stale; exact frozen files supplied authority. Author packet remained unread until this
ledger was sent to implementation task:

- 177 campaign Breakdown tests, freeze/outcome audits and diff check passed.
- No confirmed move/stop/check defect: immutable lots, exact RNG regeneration, six-state continuation,
  exhausted-reactor completion and state-scoped handles inspected.
- Provisional concern: Snapshot 11 validates local cohort/flow/world, weather context and RNG algorithm,
  but does not establish earlier initiative/weather/history provenance. Determine whether this is
  explicitly deferred restore validation or a completed-scope gap.
- Public activation, forgery/privacy and Runner readback remain pending. Collection tests do not imply
  positive-loss public Reaction.

Reviewer then read separate author packet and prior report. Bootstrap and explanation retained locally
under `.planning/2026-09-05-brk-task-005/`; this report retains scope, claims and resulting judgment.

## Findings

### P3 — Align remaining completion pointers with implemented Task 005

Frozen documentation contradicts completed stop/check implementation:

- `docs/README.md:46` identifies Task 005 stop/check authority as next.
- `docs/roadmap/pre-alpha-roadmap.md:901` identifies Task 005 stop/check authority and RNG replay as next.
- `docs/specs/breakdown-wire-contract-v1.md:3` says stop/completion codecs remain pending.

Task 005 factories, codecs, projectors and tests exist at reviewed HEAD. Canonical design correctly
identifies Task 006 as next. Readers following repository index or roadmap therefore receive
conflicting delivery status.

Smallest correction: mark dormant stop/check/completion implementation complete and public
activation/privacy pending under Task 006. Historical task evidence can retain its original checkpoint
wording.

Parent identified README/roadmap housekeeping during review; reviewer verified frozen lines and
independently found wire-contract status mismatch. No correction included in reviewed target.

**No other actionable findings. No confirmed implementation defect within completed dormant scope.**

## Plan Review

Task ordering remains sound: contracts and certification precede accounting, accounting precedes
stop/RNG transactions, and all remain dormant until coherent public activation.

Completed evidence supports:

- **Task 003:** Content 6, Setup 6, World 6, Snapshot 11, Created 10, finite flow records,
  sequence/catalog 4, Ruleset 9 composition and bounded Truck fixture.
- **Task 004:** exact BP accounting independent of CP; terrain/route/weather/directional additions;
  route identity; phasing forced stops; combat-only Reaction triggers; immutable move replay.
- **Task 005:** collection check transaction, authoritative rejection-sampled dice, no-roll precedence,
  zero-loss checked memory, persistent lots, explicit participant completion, active System closure,
  ordered continuation and first-side Combat checkpoint.

Replay reconstructs expected event bytes before projection. This checks evidence against prior
authority rather than merely verifying internally consistent totals. Tests exercise plausible
canonical forgeries, including changed dice yielding unchanged table outcome.

Review 2's exhausted-Reaction ambiguity is corrected consistently: final-CP Reaction move retains
active route; participant completion or System closure records stop. Pending reactor stop blocks
window closure and resolves before deferred phasing stop or route resumption.

Tasks 006–007 remain substantive obligations. Current tests do not satisfy public activation,
disclosure permutations, complete current-boundary identity matrix, Runner migration or two clean
public trajectories. Plan retains those distinctions.

No heavy architecture pivot required.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Consequence |
| --- | --- | --- | --- |
| Tasks 003–005 remain dormant | Cumulative production diff; separate internal successor factories/codecs; unchanged active dispatcher paths | Confirmed | Current public activation not claimed |
| Shared BP accounting preserves CP semantics | `CampaignBreakdownAccounting`, ordinary/Reaction factories, accounting and move tests | Confirmed | BP derives from admitted edge/weather inputs |
| Exact RNG, no-roll and zero-loss behavior | `CampaignBreakdownCheckResolver`, check tests, canonical wrong-dice replay test | Confirmed | Actual draws/cursors and checked memory rederive |
| Lots remain stationary and conserve points | World validator, resolution projector, survivor movement and checkpoint replay tests | Confirmed | Working/broken/lot balances preserved |
| Reaction stop resolves before phasing continuation | Lifecycle factory, snapshot validator, open/closed/deferred-stop replay tests | Confirmed | No duplicate costs or draws on resumption |
| Strict event/version dispatch and full reconstruction | Breakdown event serializer and move/lifecycle projectors | Confirmed | Structural readback alone grants no authority |
| Two private Reaction helpers became internal | Focused `d511ec7..HEAD` diff for `CampaignReactingElementMovedV2Factory` | Confirmed | Refers to successor factory's `MoveOptions` and `CreateWindowCapability` |
| Replay begins at certified Movement checkpoint | Integration test setup, author packet and retained campaign/stop evidence | Confirmed, bounded | Creation/preamble history and public restore wiring remain pending |
| Positive-cohort collection tests prove public motorized Reaction | Author explicitly disclaims this; profile permits non-cohort combat reactors | Correctly excluded | No unsupported profile widening |
| Final full gate: 1,509 tests, build/format pass | Retained Task 005 evidence and named binlogs | Not independently rerun | Reported as author evidence only |
| All current documentation reflects Task 005 completion | Three frozen status pointers above | Contradicted | Bounded documentation follow-up |

## Verification Performed

Ground truth checked with Git: expected branch and exact HEAD; 76 cumulative changed paths and 21
focused changed paths. Only declared user-owned dirty paths present. No source/doc edits, staging,
commits or builds performed by reviewer.

Required CCE recall exposed prior design/Task 002 verdicts and test totals. Those were disregarded as
evidence. Author packet and retained review 2 were read only after preliminary ledger was sent to
parent. CCE retrieval was used for exploration; exact files supplied authoritative content where
indexed status snippets were stale.

Executed checks:

| Command | Outcome |
| --- | --- |
| `dotnet --version` | `10.0.400` |
| `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class 'Cna.Core.Tests.Campaigns.Breakdown*'` | **177 passed**, zero failed/skipped |
| `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class 'Cna.Core.Tests.Content.BreakdownContentTests' --filter-class 'Cna.Core.Tests.Rules.BreakdownSequenceTests'` | **44 passed**, zero failed/skipped |
| `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-trait 'Boundary=UserSpace'` | **48 passed**, zero failed/skipped |
| `python3 docs/research/verify-breakdown-contract-freeze.py` | **PASS:** 14 historical fixture hashes, 15 Reaction children, 15 inherited schema hashes, 10 requirements/12 mappings, 34 links |
| `python3 docs/research/verify-breakdown-outcomes.py` | **PASS:** 324 cells, complete unique ranges, monotone columns, nine source probes, 606 arithmetic combinations |
| `git diff --check 2e9f9b8..3d5baa330d257a695bc8f9921200e5fa96195f86` | **PASS**, including final repeat |

First sandboxed campaign test attempt failed before execution with named-pipe `SocketException (13):
Permission denied`, exit 134. Authorized local IPC escalation succeeded; all reported test results
came from escalated no-build runs. Existing artifacts used; bootstrap states they match frozen HEAD.

Run-tests/platform-detection instructions applied; `global.json`, project and central configuration
confirm SDK-style native MTP with xUnit.

## Open Questions And Residual Risks

Snapshot certification establishes local shape, identity, content, conservation and flow consistency.
It does **not** replay preceding initiative/weather/history or establish historical provenance of an
arbitrary checkpoint. This preliminary concern is explicitly documented in Task 003 evidence and
consistent with dormant checkpoint scope. Task 006 restore/admission must preserve that distinction.

Existing boundary tests prove current outward closure remains intact. They do not prove pending
Observation 7, disclosure manifest 2 or successor history privacy.

Actual certified Reaction histories contain empty cohort accounting/checks. Broader collection and
accounting tests establish reusable calculation behavior, without establishing public positive-loss
Reaction or positive ZOC.

Full solution gate, build and format were not independently rerun. Review relies on focused execution
plus retained full-gate evidence. Primary historical rule sources were not reread; numeric verification
used accepted retained transcription.

Successor factories duplicate some predecessor movement/Reaction logic. This is understandable version
isolation; coupled activation and future maintenance must guard against divergence.

## Verdict

**Ready with non-blocking follow-ups** for completed BRK progress through Task 005 at frozen HEAD.

Dormant implementation and ordered plan are supported by inspected code and executed checks. Three
documentation status pointers need alignment. This verdict does not approve public Breakdown
activation or claim Tasks 006–007 complete.

## Recommended Next Actions

1. Accept and correct P3 status mismatch during review retention.
2. Continue Task 006 with coherent activation, creation/restore history validation, exact action
   membership and privacy evidence.
3. Preserve Task 007 Runner and clean-run obligations.
4. Close review loop at **3 of 3**. No automatic fourth instance; further review requires explicit human
   direction.

## Implementation-task disposition

**Accept** P3. Review-retention change aligns `docs/README.md`, all current Breakdown progress pointers
in `docs/roadmap/pre-alpha-roadmap.md`, and wire-contract status with completed Task 005 and next Task
006. Historical Task 003/004 checkpoint evidence remains dated as originally verified.

No code/test or architecture change followed review. Parent SHA-256 audit confirmed all 15 Task 005
source/test paths match reviewed commit `3d5baa3`; closeout checks verify the cumulative reviewed
source/test set too. Documentation links, freeze audit and staged whitespace pass. This bounded status
correction does not require another implementation review. Last independent verdict remains as issued;
review budget is exhausted at 3 of 3, with no remaining actionable finding after accepted documentation
correction. Snapshot provenance, public privacy and Runner obligations above remain assigned to Tasks
006–007, not counted as completed.
