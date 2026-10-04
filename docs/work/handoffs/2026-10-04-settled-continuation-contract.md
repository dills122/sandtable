# Handoff: Result2 settled continuation contract — 2026-10-04

## Objective and boundary

CMB-019D0 executable contract implemented and locally verified; independent review1 of3 **Ready**,
no actionable findings. Draft PR149 is published; metadata follows below. Parents017–019 stay open;
actual positive entry/repeat, later-II/consumed, Snapshot/publication and public activation are separate.
Coordinator: `01a0c9dc-00bc-78a3-800d-3cb36859e422` (local).

## Canonical sources and retained work

[Dispatch acceptance](../../design/combat-cycle-post-movement-dispatch.md#frozen-next-dispatch-cmb-019d0-result2-settled-continuation-contract-bridge),
[new contract](../../specs/combat-settled-continuation-v1.md),
[canonical plan](../../design/combat-cycle-implementation-plan.md#task019d0--native-result2-settled-continuation-executable-contract).
Exactly five primary files: spec, ordered schema, new fixture, Python oracle and canonical plan.
Review bootstrap/author/report and this handoff are administrative artifacts. No C# source/test,
old fixture, instruction/config, primary checkout or unrelated work changed. Existing module
architecture/names/setup are unchanged; README/tech-design/naming rationale therefore needs no rewrite.

Bridge independently replays Request/Created11, complete selection/RBA, Round2/Result2 and empty
Release1. Full source/clock/actor bytes then match independent synthetic catalogue admission.
Progress binds actual accepted combat-attack-committed receipt/hash, distinct from commitment ID.
Earlier Movement is separately pinned synthetic-pre-combat evidence, never current/retreat-derived.
Immutable canonical proof retains full World/RNG/history/duties and pure source-specific witnesses.
No spend/repeat/exception/catalogue advance/public action is granted. All32 owner/seal contexts are
explicitly supported, with4 retained synthetic distance/exclusion vectors. About5.7MB new fixture
retains literal packet/proof bytes; every executable value is bounded to1MiB. Future native consumer
is not implemented by this Python packet.

## Verification and evidence

Meaningful semantic RED before implementation: direct
`python3 -B docs/specs/verify-combat-settled-continuation-v1.py`, exit1, two semantic failures:
`test_engaged_and_contact_literals: zero-engaged.axis.attacker` and
`test_commitment_receipt_hash`. The stub imported and ran; no syntax/import failure substituted.
Log `/private/tmp/cmb019d0-red.log`. Assertions remain; setup was wired to new packet after implementation.
Initial two-group GREEN passed, then expanded final suite passed. New vectors were frozen only after
literal arithmetic, actual progress identity and obligation assertions passed.

| Exact command/check | Result |
| --- | --- |
| `python3 -B docs/specs/verify-combat-settled-continuation-v1.py` | Exit0;9 semantic groups,32 canonical immutable proofs,4 retained descriptor vectors,29 pins;8 original-proof comparisons,6 independent arithmetic probes,31 source rejects,1 valid re-signed upstream clock/admission rejection,22 Release/proof rejects,11 canonical/bounds rejects. |
| `python3 -B docs/specs/verify-combat-result-settlement-v2.py` | Exit0;10 groups,32 traces,304 cuts,3,728 mutations,1,360 raw rejects,384 timing checks,200 same-owner clock-isolation comparisons. |
| `python3 -B docs/specs/verify-combat-sealed-round-v2.py` | Exit0;12 groups,10 traces,68 cuts,610 replay mutations,340 raw rejects,288 clock comparisons/retries,480 lifecycle retries,30 invalid proposals. |
| `python3 -B docs/specs/verify-combat-reserve-release-v1.py` | Exit0;13 cases/48 side-slot traces,188 cuts,2,368 mutations,840 raw rejects,20 timing/27 boundary checks. |
| `python3 -B docs/specs/verify-combat-ordinary-movement-v1.py` | Exit0;8 cases/both sides,36 cuts,486 mutations,120 raw rejects,630 arithmetic coordinates,14 atomic/overflow guards. |
| `python3 -B docs/specs/verify-combat-cycle-control-v1.py` | Exit0;19 cases/64 traces,164 cuts,1,748 mutations,700 raw rejects,43 boundary checks,216 cost coordinates. |
| `python3 -B docs/specs/verify-combat-inherited-reserve-movement-completion-v1.py` | Exit0;2 traces/6 events,16 readbacks,6 retries,1,626 mutations,60 raw/58 boundary rejects. |
| `PATH="/private/tmp/cmb019d0-tools:$PATH" just check` | Exit0; restore, format, build0 warnings/errors, Boundary81 and full2,475 pass, zero failed/skipped. Full duration7m50.189s. |
| Python AST + both new JSON parsers; local-link script; source-byte comparison; `git diff --check` and untracked primary no-index whitespace checks | Pass;12 scoped links/anchors;28 pre-existing pinned files byte-identical to base,1 new schema pin. |

Logs: `/private/tmp/cmb019d0-green-final.log`, `/private/tmp/cmb019d0-oracles.log`,
`/private/tmp/cmb019d0-just-check.log`, `/private/tmp/cmb019d0-static-checks.log`.
The seven-oracle batch's first new-oracle snapshot was eight groups; separate final nine-group run
follows the added admission-trust test. All six unchanged dependency runs remain valid.
Reviewer independently reran all seven final oracles. .NET results are repository regression
checks and do not prove a .NET implementation of this contract.

Temporary wrapper only adds a unique `/bl:` per restore/build/test; it changes no repository recipe.
Actual invoked commands, native .NET10 MTP/xUnit SDK-style mode:

```text
dotnet restore Sandtable.slnx /bl:/private/tmp/cmb019d0-restore-cba63cec-9216-42c2-ad4a-80e7859db84f.binlog
dotnet format Sandtable.slnx --verify-no-changes --no-restore
dotnet build Sandtable.slnx --no-restore /bl:/private/tmp/cmb019d0-build-99e2267c-8d1a-4241-bf29-0cad10231c25.binlog
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-trait Boundary=UserSpace /bl:/private/tmp/cmb019d0-test-fae3aa6a-7652-4e34-9f10-42021b76a0d1.binlog
dotnet test --solution Sandtable.slnx --no-build /bl:/private/tmp/cmb019d0-test-ef8115da-9dde-47a5-bec7-af06df17377d.binlog
```

Restore/build binlogs exist; MTP emits corresponding `*-dotnet-test.binlog` files for both tests.
No test result failure occurred. Broad264-link scan found one **unchanged baseline** missing
side-projection anchor (`known-blocker-private-seal-changes-clock-regression-outcome`); new scoped
links pass. Serena/tool hook read-counter rejections were retried; they were not gate failures.
No old fixture regeneration, runtime change or profile expansion was used to obtain GREEN.

## Independent review

[Retained report](../reviews/2026-10-04-settled-continuation-review.md): fresh GPT-6.1-sol medium,
review1 of3, read-only `fork_turns=none`, neutral bootstrap then separate author explanation;
preliminary concerns recorded first. Ready, no findings. Reviewer independently executed all seven
oracles, syntax/JSON/links/whitespace, inspected native017B mapping and observed author .NET logs.
Author accepts report; no fixes or further review instance required. Reviewed contract/schema/
fixture/oracle hashes remain unchanged; subsequent plan changes only reconcile passing gates/status.
Report retains exact reviewed SHA-256 values.

## Decisions, trust and limitations

Exact admitted byte catalogue preserves native Result2 clock/progress provenance without promoting
synthetic earlier history. Full semantic replay precedes equality/admission; valid re-signed variants
cannot self-authenticate. Earlier proof membership/receipt/scope/exclusions remain independently
pinned, never caller/current-world-derived. Full immutable-byte cache is not a digest trust cache.
Spent9→15/DP5, rejected16 and overflow/strict ceilings remain independent arithmetic probes,
not admitted campaign histories. All supported sources differ in witness/duty/CP outcomes.

Serena exact worktree activated, manual read and C# Replay retrieval verified. Python specs are
ignored by Serena; direct focused reads supplied source evidence. Unique Codebase Memory project
`sandtable-cmb019d0`, full generation2026-10-04T22:36:29Z,25,667 nodes/137,060 edges;
original cited paths metadata-matched, new untracked paths/changed plan covered by direct reads.
Best-effort graph signals do not authenticate gameplay. Untracked `.serena/` excluded from publication.

## Immediate next action

Coordinator: inspect this contract and draft PR, reconcile scoped acceptance, then freeze native
settled-proof adapter through017B replay and019A assessment retaining synthetic trust. Settled
control and actual positive campaign-entry/repeat histories follow separately. Do not close017–019,
claim reachable creation-rooted settled admission, or dispatch later-II/consumed/public expansion
from this packet alone.

## Delivery metadata

Worktree `/Users/dsteele/.codex/worktrees/settled-continuation-contract/sandtable`.
Branch `codex/settled-continuation-contract`; base `ec0db5319d9598313372ab239253ac1bde351594`.
Implementation head: `34498122330429be823a771fe0e0a99421505fa7`.
Draft PR: [149 — Freeze native Result2 settled continuation contract](https://github.com/dills122/sandtable/pull/149),
base `main`, branch `codex/settled-continuation-contract`. A following metadata-only commit records
this PR/head; resolve final branch head through `git rev-parse HEAD` or the PR. Both commits are
retained on this PR. Only excluded untracked `.serena/` tooling remains. PR is attached to this chat;
no merge was performed. Hosted CI starts asynchronously and is not claimed passed by local gates.

Human authorization directly verified in coordinator turn `01a108e4-42ed-7720-90a7-bb8406e1a2f5`:
sub-chats, GPT-6.1 medium, TDD, independent review before PR, Keychain skill and handoff back for
core sync. Latest human continuation `01a1090d-3b68-7e43-bb42-02f36acd9cf3` confirms merged work
and continuing the workflow. Publication uses the explicitly requested
[github-keychain-auth skill](/Users/dsteele/.ai-central/templates/skills/first-party/github-keychain-auth/SKILL.md)
outside sandbox, unsetting GH_TOKEN/GITHUB_TOKEN for gh, without extracting credentials. No merge.
