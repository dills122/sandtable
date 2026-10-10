# Fresh Review Bootstrap — corrected actual prepared-round contract

Review instance:2of9,set1/pass2. Prior review1of9 returned Not ready, one bounded P2,
accepted by coordinator Brain. Research recoveries0of2; no heavy pivot/reset. GPT-6.1 medium.

## Review Objective

Independently assess the actual prepared-round entry executable contract and canonical plan,
including the accepted error-order correction. Reconstruct implementation and plan evidence;
record preliminary findings before reading separate author testimony. Prior reviewer report
and the correction author's rationale are not a readiness verdict.

## Repository And Worktree

`/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable`. Branch `codex/actual-round-entry-contract`.
Draft PR https://github.com/dills122/sandtable/pull/167 targets `codex/actual-round-entry-research`;
research PR166 remains separate. Preserve local `.serena`, outside review/publication scope.
Read applicable AGENTS.md; structural graph docs are excluded, use focused source reads.

## Base, Head, Branch, And Dirty State

Accepted research base0db4745a5ec631269c7751a244e1f49d84a81d6f.
Previous published implementation75b308d7c4ceb9c84894e356f7cb9e41837046be.
Corrected immutable implementation `6ac709bcdca1f01e4f894bc372bb1f85ec5e81c6`; follow-up administrative commit packages
this bootstrap, author, handoff and review reconciliation only. Brain supplies exact published
head; verify Git and five primary hashes below. The correction diff over75b308d changes only
new RoundEffect validation/tests and canonical plan. Full contract scope is the five primary
paths from research base through corrected implementation.

## In-Scope Commits And Paths

Five primary paths and SHA256:

| Path | SHA256 |
| --- | --- |
| `docs/specs/combat-actual-round-entry-v1.md` | `ed4fd69b4f599acae7c4850270557363a546df36d0b984107c50c08c29304548` |
| `docs/specs/combat-actual-round-entry-v1.schema.json` | `677b1c671bebe5f23cf4b80f03078c55000c60e45416701757e2552cbd05edb8` |
| `docs/specs/fixtures/combat-actual-round-entry-v1.json` | `fc17cda07618bdd539a56b8c296037bbf36f37c6eb2ce6258c2b789b765f640b` |
| `docs/specs/verify-combat-actual-round-entry-v1.py` | `2596531cb50ddbae7f1e606284f3d845b224d4f1fc10f478b717843d1062990c` |
| `docs/design/combat-cycle-implementation-plan.md` | `d7aa53239cc1b330356e136b140656239a8700b9e15b16b9b607372bcd730265` |

Administrative packets: original WIP, dated author/bootstrap/handoff and review1 response.
Read implementation before separate author/response rationale; do not treat historical author
freeze at0db or75b as the corrected target.

## Canonical Requirements And Plan

All eight groups in `docs/research/combat-actual-round-entry-feasibility.md`; actual-round-entry
spec/schema and Task019F2/REL-AUD-02B in canonical Combat plan. Original actual-selection-v1,
positive-entry, step design and sealed-round-v2 compatibility; dependency disposition remains
unchanged. Two original owners, selected20 -> opening21 -> seals22/23 -> FA24 -> empty AA25
Prepared CA;30 cancellations end no-attack Release25/26. Separate independent ledgers, ownership,
clock, replay, retries, canonical capacity/error order, privacy and resource/RNG conservation.
All16 original plus41 additional dependencies unchanged. New RoundEffect shape/type001 before
unknown string tag003; inherited Effect grammar unchanged. All accepted literal bytes unchanged.

## Explicit Exclusions

No native/public/host/UI/AI/transport or production seat/clock/store authentication, paid/result,
settlement/fullSnapshot/Archives restart/Runner/repeat/later-II/consumed/parent017–019 closure.
Four pre-existing oracle failures remain separate; no pin update or waiver. No merge or fixes.

## Verification Commands Available To Reviewer

- `git status --short`, branch/HEAD/log, scoped diff and `git diff --check`.
- `python3 -B docs/specs/verify-combat-actual-round-entry-v1.py --semantic`.
- Full normal `python3 -B docs/specs/verify-combat-actual-round-entry-v1.py` is costly;
  exact author run metadata and stdout are retained under `/private/tmp/actual-round-entry-gates`.
- Public regression `python3 -B /private/tmp/actual-round-effect-regression.py` and embedded
  `round_effect_checks({})` after loading the verifier; public source/control/proof readbacks.
- Proportionate read-only literal, privacy, precedence and dependency checks; do not repeat
  unchanged original sweeps without a concrete concern. No unrelated .NET suite required.

## Author Explanation Location Or Delivery Step

Separate `docs/work/reviews/2026-10-10-actual-round-entry-contract-author.md`; complete accepted
finding/author reconciliation `docs/work/reviews/2026-10-10-actual-round-entry-contract-review-1-response.md`.
Read after initial independent inspection and preliminary ledger; handoff remains evidence.

Use $independent-review in reviewer mode. This is review instance2of9,set1/pass2. Work from
Fresh Review Bootstrap first and record preliminary review before Author Explanation. Verify
claims against repository; review implementation and plan, run proportionate non-mutating
checks, return evidence-backed verdict. Do not fix, spawn further reviewers or split workstreams.
