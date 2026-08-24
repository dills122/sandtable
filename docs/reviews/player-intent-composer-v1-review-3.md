# Independent Review 3: Player Intent Composer v1 Roadmap Integration

**Review instance:** 3 of 3 (final)

**Verdict:** Ready with follow-up

**Scope:** complete documentation-only research, specification, technical delivery plan, project-map
updates, and pre-alpha roadmap placement

## Findings

### P2 — Prototype work item was not yet execution-sized

`INTENT-PLAN-002` combined draft synchronization, clarification, stale state, dual confirmations,
hot-seat isolation, browser/worker/navigation tests, and accessibility walkthroughs. The original
`INTENT-PLAN-008` wording likewise anticipated later separation without naming the child slices.

**Impact:** implementing either umbrella item directly could produce an oversized change or hide
dependencies between interaction behavior, privacy cleanup, and accessibility.

**Disposition:** accepted. The design now labels the task list as delivery work items, prohibits
direct execution of umbrella items, and requires `INTENT-PLAN-002` to split into interaction-state,
confirmation/staleness, and hot-seat-isolation slices. `INTENT-PLAN-008` must split deterministic
onboarding from accessibility hardening. Each executable child remains capped at five material files
with independent acceptance criteria and evidence.

### P3 — Requirement-to-task coverage was implicit

The specification defines functional, non-functional, and acceptance obligations, while the task
list did not explicitly map each requirement group to its delivery task/checkpoint and evidence.

**Disposition:** accepted. Phase 0 and `INTENT-PLAN-001` now require a compact
requirement-group → task/checkpoint → evidence matrix before implementation authorization.

No P0 or P1 findings remain.

## Confirmed Planning And Architecture

- Current work remains untouched; the capability is future, unauthorized, and outside the current
  sprint.
- The representative decision is selected only after the Sprint 5 combat/replay skeleton.
- The no-model prototype runs alongside Sprints 6-7 without blocking rules/content work.
- Sprint 8 integrates only one deterministic slice and waits for an approved Legal Actions
  plan/batch binding.
- The six-turn MVP remains model-independent and includes hot-seat isolation as an acceptance gate.
- Needle remains optional and strictly post-MVP under `INTENT-PARSER-EVAL-001`.
- Command → Staff → Umpire ownership, side-safe projection, trusted audience derivation,
  deterministic validation/planning, exact-audience legal revalidation, and zero parser authority
  edges are preserved.
- The hot-seat boundary covers epochs, late responses, workers, navigation, DOM/live regions,
  storage, cache, and diagnostics.

## Verification Evidence

- Frozen HEAD and merge-base: `a7b91d1e47c74493ed0ebc4b3980979b500339e4`.
- Exact tracked/untracked scope matched the review bootstrap.
- `git diff --check` passed.
- New-file `git diff --no-index --check` emitted no whitespace diagnostics.
- All repository-relative Markdown targets existed.
- Requirement, acceptance, and task definitions were unique.
- No .NET, frontend, model, or accessibility test was run or claimed because the target is
  documentation-only.
- The review task remained read-only and did not modify repository files.

## Residual Gates

The representative decision, Command/Staff field boundary, starter precedence, and future
multi-order Legal Actions binding remain deliberate Phase 0/Sprint 8 gates. The two-question cap
still requires playtesting, every future Maproom storage/worker surface must join the hot-seat
inventory, and Needle performance remains unmeasured until the post-MVP evaluation.

The final reviewer concluded that the architecture, roadmap placement, dependency order,
deterministic MVP, authority/fog boundaries, and privacy gates are ready for a documentation pull
request. This is the final allowed review instance; no further review task will be created.
