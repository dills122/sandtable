# Optional OpenCodeReview scrutiny

**Status:** opt-in workflow is merged, and the owner has configured the Sandtable Actions secret.
The owner approved OpenRouter code egress for this gated pilot. Use it only on selected PRs after
the paid rerun guard is merged and checked.

## Purpose and selection

OpenCodeReview (OCR) is a paid, advisory review of a selected pull-request diff. Use it when a
change crosses Umpire authority, replay/source pins, fog or seat boundaries, event/persistence
contracts, or a substantial Maproom interaction path. Use ordinary deterministic tests and fresh
independent engineering review as the merge gates. OCR findings need reproduction and human
disposition; green OCR output does not certify a PR, and a zero exit code can contain partial
coverage. This pilot does not run a full-repository scan.

The tracked [project rule](../../.opencodereview/rule.json) admits changed C# tests, Maproom unit
and Playwright tests, Python verifier tests, and Combat schema/spec files that OCR's defaults may
otherwise skip. It excludes generated Angular API clients, large fixture/golden outputs, work
notes, reviews, research, and lockfiles. Review contract documentation and source pin effects
separately when a diff exceeds the OCR budget or its selection filter omits a relevant file.

## GitHub activation

The [trusted workflow](../../.github/workflows/open-code-review-pilot.yml) adapts Reef's [pinned opt-in pilot](https://github.com/dills122/reef/blob/c2dc8358a52c80edf2ac0c09a2279119ab224915/.github/workflows/open-code-review-pilot.yml)
and [operating notes](https://github.com/dills122/reef/blob/c2dc8358a52c80edf2ac0c09a2279119ab224915/docs/CI_OPERATIONS.md#opt-in-opencodereview-pilot)
with these safeguards:

- `pull_request_target` uses the trusted `main` workflow and rule; restrict to same-repository,
  non-draft PRs. Each paid run requires the repository owner to apply the `ocr-scrutiny` label to
  a ready PR. No draft/ready toggle, push, reopen, comment, fork, scheduled trigger, or Actions
  rerun can spend tokens. The workflow requires first attempt of a fresh label event.
- Job token starts with no permissions and grants only `contents: read` and
  `pull-requests: write` to the review job. Use the official action at the full Reef-tested
  `bccbc15f785269400735d5255540c231e6c02b6d` commit with OCR CLI `1.12.9` and
  `OCR_NO_UPDATE=1`. No separate checkout or shell step may execute PR files.
- The model/provider matches Reef's pilot: `openai/gpt-5.4-mini` through OpenRouter, with a
  Sandtable Actions secret named `OCR_OPENROUTER_API_KEY`. Selected PR content and review context
  are sent to that provider. Configure a Sandtable-specific secret through GitHub repository
  settings before applying the label; GitHub repository secrets cannot be copied from Reef.
- Begin with medium effort, one review group at a time, a 750,000-token *soft* run budget and a
  45-minute job timeout. OCR checks budget between rounds and may spend beyond the threshold in
  its last round. Keep artifacts off; use sticky summary, incremental comments and checkpoint
  ranges. A missing, invalid, or stale checkpoint must widen to a full review.

Before activation, inspect the repository's Actions event-policy insights and confirm that
`pull_request_target` is permitted. [GitHub's public-repository default policy](https://docs.github.com/en/actions/reference/security/securely-using-pull_request_target#default-policy-for-pull_request_target)
currently evaluates affected runs and is scheduled for enforcement on November 2, 2026. If the
policy would block this workflow, arrange a narrowly applicable policy only after reviewing its
privileged-event safety; do not weaken the workflow's trust boundary to make it run.

The repository owner should apply the label only after focused CI is green and the PR head is
stable. A later push or draft/ready toggle does not run OCR. To request another paid review of a
**new head**, the owner must remove and re-add the label to the ready PR; each re-application is a
new cost decision. Rerunning an existing Actions run is blocked. Re-applying the label to the
**same head** reaches OCR's checkpoint and is a no-op, not a second paid review. A fresh review of
an unchanged head needs a separately reviewed checkpoint reset procedure; this pilot has none.
Before using a result, verify the reviewed head SHA, range mode, selected/completed/failed file
counts and summary. A partial review or stale head is evidence only for the files actually
completed. Remove the label when the pilot is finished.

## Value check

Record each run's model, selected/completed/failed files, input/output tokens or provider charge,
actionable findings accepted, false positives, and maintainer time spent. Compare those findings
with CI and the independent review for the *same head*. Continue paying only if OCR finds distinct
material issues or measurably reduces review time. Stop the pilot if costs rise without that value.

Do not make OCR a required branch-protection check: optional invocation and partial-success
semantics cannot prove merge readiness. Never give it an authority role in the game.
