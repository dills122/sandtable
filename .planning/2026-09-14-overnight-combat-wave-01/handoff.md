# Handoff: overnight Combat wave01 — HARD STOP

## Objective And Boundary

Required outcome **incomplete**: CMB-TASK-004/checkpoint B not complete; CMB-TASK-005 not begun.
CMB-TASK-006 deferred. Work started2026-09-15 02:49:17 UTC; eight-hour deadline10:49:17 UTC.
Hard stop independently confirmed03:19:38 UTC (~30minutes): accepted clock/privacy policy conflict.
Only preservation, status reconciliation and authorized draft delivery followed. No automatic resume
or merge. Owner policy disposition is first safe next action.

## Canonical Sources

- AGENTS.md
- docs/design/combat-cycle-implementation-plan.md (current task/dependency/status authority)
- docs/roadmap/pre-alpha-roadmap.md
- docs/specs/combat-authority-composition-v1.md (exact unchanged Task003 handoff)
- docs/design/combat-sealed-decision-protocol-v1.md:124–138,155–163,274
- docs/design/combat-cycle-policy-reconciliation.md:25–27
- docs/specs/combat-sealed-round-v1.md:103–106

## Completed Work And Evidence

Completed setup, baseline verification, attested execution plan and source audits. Retained one
**unaccepted**004A1 candidate in four owned files with semantic RED/GREEN history. No accepted
implementation story or full-CON005 freeze. Canonical status navigation now states blocked.

| Check | Exact command | Result |
| --- | --- | --- |
| Baseline and candidate integration | `just check` (outside sandbox) | Both exit0; format/build0warnings/errors;81/81 boundary;1670/1670 full tests;0skipped. Full durations3m13.322s baseline,3m32.443s candidate |
| Task003 baseline | `python3 -B docs/specs/verify-combat-authority-composition-v1.py` | exit0;28traces/9families/31pins/225readbacks/203mutations/6raw/8boundaries |
| A1 candidate vectors | `python3 -B docs/specs/verify-combat-side-projection-v1.py` | writer and fresh reviewer exit0;14semantic groups;18traces/146cuts/63submissions/297mutations/180raw/21bindings |
| Selection predecessor | `python3 -B docs/specs/verify-combat-selection-steps-v1.py` | writer exit0;5traces/41cuts/246mutations/164raw |
| Round predecessor | `python3 -B docs/specs/verify-combat-sealed-round-v1.py` | writer exit0;4traces/23cuts/276mutations/138raw |
| Cycle predecessor | `python3 -B docs/specs/verify-combat-cycle-sequence-v1.py` | writer exit0;112positions/1interrupt/6edges/38mutations/3996identities/896actor mappings |
| Privacy acceptance counterexample | `python3 -B .planning/2026-09-14-overnight-combat-wave-01/evidence/clock-high-water-counterexample.py` | **brain exit1**; both genuine seal orders violate equal semantic outcomes |
| Static checks | `git diff --check`; AST/JSON/whitespace; local Markdown target script | pass;491 local targets in10files before final status edits; final link evidence in progress.md. Lychee absent; fallback does not check external URLs |

Exact root logs and failing diagnostic/output are in this directory's `evidence/`.
Source agent inspected20pins/146cuts/61literal observations; all hashes/lengths matched.
Fixture207039bytes; largest outward observation3686bytes (limit65536).

## Blocked Work And Required Decision

P1: same waiting-audience observation bytes and submission at trusted time3500:

| Genuine retained history | Private clock high-water | Predecessor effect | Outward result |
| --- | ---: | --- | --- |
| Opened at3000; no opposite seal |3000| own choice-sealed |accepted|
| Same open; hidden opposite seal at4000 |4000| System round-cancelled, clock-unavailable |rejected|

Both attacker-first/Commonwealth and defender-first/Axis reproduce. Deadline33000 stays equal.
POL004 requires shared high-water regression fallback; POL006/PRO-AC010 require equal accept/reject
under equal authorized histories. No explicit clock-regression exception found. Timing/traffic
privacy deferral does not waive semantic equality. Cannot silently restrict comparison to times
valid in both private histories, publish private high-water, relabel cancellation as acceptance or
change frozen authority. Owner must reconcile policy and authorize explicit compatible/versioned
contract treatment before A1 can be accepted. Passing limited tests do not override this failure.

Blocked:004A1 acceptance,004A/004/B and required005. Deferred:004A2 settlement,004A3 Reserve/cycle,
004B CON006 freeze,004C72-AC map, optional006. CON006 preparation found versioned terminal/dual-slot/
continuation seams; source-audit-004b.md proposes bounded future packets but starts no implementation.

## Child Statuses

| Child | Final status |
| --- | --- |
| task004_writer | stopped;004A1 candidate blocked/unaccepted; only spec status correction after stop; no commit/push |
| task004a1_reviewer | ordinary fresh-context five-axis review complete; Request changes/P1; independent counterexample; no writes |
| task004_evidence | source audits and candidate coverage reconciliation complete; independently confirmed policy conflict; no writes |
| brain | reproduced failure, reconciled gate/source claims, halted implementation, owns blocked draft/handoff |

No formal independent-review skill/pass invoked; exhausted budget preserved. Maximum four active
agents including brain; sole writer and protected historical paths maintained.

## Files Retained

Candidate primary packet:

- docs/specs/combat-side-projection-v1.md
- docs/specs/combat-side-projection-v1.schema.json
- docs/specs/fixtures/combat-side-projection-v1.json
- docs/specs/verify-combat-side-projection-v1.py

Requirements/navigation (brain):

- docs/design/combat-cycle-implementation-plan.md
- docs/roadmap/pre-alpha-roadmap.md
- README.md

Durable planning: this directory's task_plan.md/.attestation, findings.md, progress.md, handoff.md,
source-audit-004a.md, source-audit-004b.md, evidence logs/candidate hashes/failing probe, and PR metadata.
Exact final staged/committed file list is retained as evidence/delivery-files.txt.
No src/tests/scenarios, existing contract bytes, protobuf fields or runtime registration changes.
Architecture/naming docs need no authority/name change; existing architecture retained.

## Assumptions And Limitations

A1 uses nine exact synthetic C3 histories, both audiences, not full creation-rooted gameplay.
Hidden mutations are declassifier-only probes; arbitrary valid unseen history admission unproved.
Task00328-trace packet pinned and unchanged; full Task004 handoff composition not done.
No hosting timing/privacy, runtime publication, migrations, new version registration, .NET Combat
implementation or complete72-AC evidence map. Policy conflict precludes current completion claims.
Skill sources at `/Users/dsteele/repos/sandtable/.codex/skills/`; worktree lacks local symlinks.
Broad skill lookup harmless/cancelled; sandbox restore stalled, replacement outside sandbox passed.
No dependency upgrades, destructive Git operations, user deletions or newer-main conflicts.

## Current Repository State And Delivery

Worktree: `/Users/dsteele/.codex/worktrees/0b59/sandtable`.
Branch: `codex/overnight-combat-wave-01`.
Base/latest-main: `ff6b5f60219afbb12d5e06bf836f06f94de32006`, refreshed after hard stop.
PR113 merge471d17de7837c48c29f5b8184e1ee5e8b21034f8 and PR114 mergeff6b5f6 verified.
Retained commits/draft PR: publication pending; updated below after creation.
Dirty state before publication: seven candidate/navigation files plus ignored planning packet;
explicit force-add needed for this requested durable evidence. No accepted-story commit exists.

## First Safe Next Action

Owner decides how accepted clock-regression behavior and equal-outcome privacy must reconcile.
Then record policy/contract disposition, preserve historical bytes through explicit versioning if
needed, write failing regression first, fix only authorized successor scope, rerun focused and full
story gates, obtain ordinary fresh-context quality approval, and only then accept004A1/continue.
Do not start005 until004 and checkpoint B are complete. Do not merge this blocked draft.

GitHub operations use Keychain outside sandbox with `env -u GH_TOKEN -u GITHUB_TOKEN gh ...`.
