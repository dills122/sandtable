# Progress

## Setup — 2026-09-15

- Durable goal created at02:49:17 UTC; deadline10:49:17 UTC.
- Initial detached worktree clean. `git fetch origin main` exit0;
  `git merge-base --is-ancestor ff6b5f6 origin/main` exit0.
- `env -u GH_TOKEN -u GITHUB_TOKEN gh auth status --hostname github.com` exit0 outside sandbox.
- `env -u GH_TOKEN -u GITHUB_TOKEN gh pr view 113 --repo dills122/sandtable --json number,state,mergeCommit,mergedAt`
  and matching114 command exit0, both MERGED.
- `git switch -c codex/overnight-combat-wave-01 origin/main` exit0.
- Required skills read. Broad lookup cancelled with exit130; no material changes or auth failure.
- Four planning files created before implementation. No story implementation or test gate yet.

## Task004 scoping

- Plan attested; canonical004A/B/C decomposition recorded before edits.
- Writer task004_writer owns exactly four new side packet files; task004_evidence read-only.
-004A re-split before edits into004A1 codec/selection/RBA/seal,004A2 settlement,004A3 Reserve/cycle
  and handoff. Parent004A/004/B remain open until complete integrated evidence.
- Baseline `just check` running to /private/tmp/sandtable-wave01-baseline-check.log.
- Baseline `python3 -B docs/specs/verify-combat-authority-composition-v1.py` running to
  /private/tmp/sandtable-wave01-baseline-authority.log.
- Sandbox `ps` denied; read-only outside-sandbox process check succeeded. No gate failure yet.
- Baseline authority oracle exit0:28 traces/nine families/31pins/225readbacks/203mutations/6raw/8boundaries.
- Sandbox `just check` stuck at restore; interrupt ineffective. Terminated own PIDs75418/75401 only.
  Replacement `just check` outside sandbox running to /private/tmp/sandtable-wave01-baseline-check-outside.log;
  restore succeeded, formatting underway. Environment issue, not behavior failure or dependency upgrade.
- Fresh read-only reviewer task004a1_reviewer assigned canonical-source checklist before candidate.
- Outside-sandbox baseline build/format pass,0 warnings/errors and81/81 boundary tests. Full suite running.
- `command -v lychee` returned1: pinned checker absent from PATH. No installation or dependency
  change attempted; local-link fallback needed if checker remains unavailable.
- Planning files are ignored by repository `.gitignore:87`; retain explicit force-add on story
  commit so draft PR includes durable plan/handoff. Session-catchup script exit0, no unsynced output.
- Baseline `just check` outside sandbox exit0: format pass; build0warnings/errors;81/81 boundary
  tests;1670/1670 full tests,0skipped, duration3m13.322s. Exact log evidence/baseline-just-check.log.
  Baseline authority exact output retained evidence/baseline-authority.log.
- Optional TestResults directory lookup absent; test process exit/result log remain authoritative.
- README/roadmap/plan status synchronized to004 in progress/B open. Separate navigation ownership.
- Source agent final004A audit reconciled and retained source-audit-004a.md; reused for read-only
  CON006 requirements preparation while004A1 implementation proceeds.
-004A1 writer RED: `python3 -B docs/specs/verify-combat-side-projection-v1.py` exit1, assertions
  `test_codec invalid side value admitted`, `test_selection selection needs every authenticated cut`,
  `test_seals round needs every authenticated cut`, `test_submission foreign audience admitted`.
  Writer GREEN after shared codec/replay/submission implementation: same command exit0,
  `PASS: 004A1 semantic contract tests`. Further canonical/mutation/privacy coverage in progress;
  brain has not accepted candidate or independently run new oracle.
- Local Markdown target check on8 changed/planning files passed476 targets/0errors;
  external URLs and anchors excluded, not equivalent to pinned Lychee.
- Brain pre-freeze check found missing explicit submission campaign/rules/config/round/slot fields
  and cycle-domain mismatch. Writer added RED assertions `test_explicit_submission_context explicit
  protocol bindings missing` and `test_canonical_cycle_reference cycle reference bypassed frozen
  Public tuple codec`, focused command exit1. Fields corrected; cycle mapping paused then resolved
  by exact canonical sources/reviewer: use original full Rules10 hash in frozen Public codec.
  No new policy ruling or historical-byte change. Candidate not yet accepted.
- CON006 source preparation complete/read-only, retained source-audit-004b.md; recommends future
  three bounded packets terminal/continuation/schedule, strict child evidence, parent/paired evidence.
-004A1 extended writer matrix (not final acceptance):18 audience traces/146cuts,63submission and
  deadline/foreign checks,192mutations,180raw rejects,14receipt recoveries. New semantic RED
  `test_round_continues_side_history round reset authorized revision/receipt/history` exposed
  C3a→round join reset; writer preserving exact prior projection/own receipt chain before final goldens.
- Read-only source agent reused for004A1 behavioral-coverage reconciliation; no additional .NET run.
-004A1 draft source/test review complete; positive behavior/privacy/current submission/receipt/frozen
  cycle coverage present. Gaps sent to writer: explicit context-field mutants, expired unaccepted
  proposals, mandatory fixture verification, compact reorder/tag/missing/ref/limit negatives.
  No focused run on changing draft; no quality verdict yet.
- Four004A1 packet files present (~264KB including ~207KB retained fixture at first inspection).
  Ordinary reviewer completed initial tests-first/code read with no confirmed blocker; execution
  and independent probes await writer's stable-candidate signal. Brain source reconciliation agrees
  synthetic scope/typed mappings/continuity are explicit; final acceptance still pending.
-004A1 final source/test reconciliation reports all four prior gaps closed. Read-only fixture
  inspection:20pins match,18audience traces/146cuts/61literal observations, all61 lengths/digests
  agree, largest observation3686bytes, fixture207039bytes. No residual source-evidence gap in A1.
- Brain `just check` running outside sandbox to /private/tmp/sandtable-wave01-004a1-just-check.log.
  Local target check10files/491targets/0errors (anchors/external excluded); all72 canonical AC IDs
  retained uniquely; no tracked src/tests/scenarios changes. Candidate hashes retained in
  evidence/004a1-candidate-sha256.txt pending final writer/reviewer/gate reconciliation.

## Hard stop — clock/privacy conflict

- Ordinary reviewer independent probe: both genuine seal-order histories at cuts1/2 expose equal
  waiting-side bytes; identical proposal with trusted time3500 accepts before hidden seal(HWM3000)
  but system-cancels/rejects after private seal4000(HWM4000). Deadline33000 unchanged.
- Source agent independently confirms no explicit regression exception: DES002124–138/AC010274,
  DES002155–163, frozen round103–106, POL004/00625–27 conflict. Timing/traffic deferral is not a
  semantic-outcome waiver. Hard-stop condition applies; no new policy invented/authority changed.
- Brain `python3 -B .planning/2026-09-14-overnight-combat-wave-01/evidence/clock-high-water-counterexample.py`
  exit1, both orders reproduce equalObservationBytes=true, accepted→rejected, HWM3000→4000.
  Exact output evidence/clock-high-water-counterexample.log. This is failing acceptance evidence.
- Writer focused oracle exit0:14semantic groups,18traces/146cuts/63submissions/297mutations/
  180raw/21bindings. Reviewer independently reran same focused command and confirmed counts.
- Writer direct predecessor commands exit0:
  `python3 -B docs/specs/verify-combat-selection-steps-v1.py`5traces/41cuts/246mutations/164raw;
  `python3 -B docs/specs/verify-combat-sealed-round-v1.py`4traces/23cuts/276mutations/138raw;
  `python3 -B docs/specs/verify-combat-cycle-sequence-v1.py`112positions/1interrupt/6edges/
  38mutations/3996scopeidentities/896actor mappings.
- Brain story `just check` exit0:0warnings/errors,81boundary tests,1670full tests,0skipped,
  duration3m32.443s. Exact evidence/004a1-just-check.log. These passes do not waive failed privacy case.
- Reviewer verdict Request changes/P1 policy clarification. All children stopped; no A1 acceptance,
  A2/004B implementation or005/006 work. Writer handoff-only doc labels blocked candidate;
  schema/fixture/oracle remain unchanged from reviewed candidate hashes.
- Latest-main refresh remains ff6b5f60219afbb12d5e06bf836f06f94de32006; no existing overnight PR.
  Preparing one coherent blocked draft/handoff, not accepted contract delivery. No merge.
- Final readback: reviewed schema/fixture/oracle SHA256 unchanged; spec-only blocker status added.
  AST/JSON/diff checks pass;10Markdown files/498local targets/2new blocker anchors/0errors;
  external URLs and other anchors excluded. No further executable implementation after hard stop.
