# Independent Review 1: Reserve Designation v1 Planning

**Review instance:** 1 of 3

**Date:** 2026-08-24

**Mode:** Fresh-task, blind-first, read-only review of the delivered Stage Entry harness adoption
and owner-approved Reserve planning target

**Verdict:** Changes requested; harness adoption accepted, Reserve Task 002 blocked

## Findings and author responses

### IR1-001 — P1: Capability Point terminology

The packet incorrectly described Section 18.26 as concerning Command Points. The primary rule and
the repository vocabulary use Capability Points.

**Author response:** Accept. Research, specification, design, scope, rules-artifact data, and
adjudication invariants now say that Reserve designation/release costs no Capability Points and that
v1 performs no Capability Point mutation.

### IR1-002 — P1: Reserve controller did not fit the current controller seam

The current Exercise controller is stateless and receives opaque action IDs, while the proposal
required kind/subject inspection and an unstated notion of one completed designation.

**Author response:** Accept. The design now freezes a runtime semantic candidate view containing
action ID, kind, and optional element ID. The exact policy is
`designate-all-reserves-then-first-by-action-id`: it selects the ordinally first currently offered
designation until none remain, then selects completion. Progress comes only from regenerated current
legal actions; no state/count is retained. Exercise Manifest, Maneuver Manifest, and controller
configuration identity clean-cut to v2. Tasks 010-013 now inventory the controller, executor,
runtime, codecs, identities, evidence consumers, and focused tests.

### IR1-003 — P1: Local and context-authoritative validation were conflated

The owner-specific bound requires `CampaignContentContext`, while context-free snapshot parsing
currently calls local validation. Movement constraints and historical preservation were also
underspecified.

**Author response:** Accept. The specification/design now separate strict structural/local decoding
from context-authoritative admission, name which consumers use each layer, make the local world-count
formula and authoritative ownership formula distinct, freeze exact Reserve/Movement status
constraints, and assign malformed/forged tests. Completion-world preservation is proven by event
recomputation/projection rather than claimed by a stateless validator.

### IR1-004 — P2: Public observation status type was ambiguous

The public `ObservedOwnElement` property could not remain described as either an observation type or
formatter choice.

**Author response:** Accept. The contract now freezes public enum
`CampaignObservationReserveStatus` with ordinals `None=0`, `ReserveI=1`, and `ReserveII=2`, canonical
strings, construction/unknown-value behavior, equality participation, owner projection, and
Campaign Observation v3 ownership. The authoritative enum remains internal.

## Confirmed by reviewer

- Incremental designation, explicit completion, distinct events, owner-only projection, and opaque
  submissions are directionally sound.
- State arithmetic is correct for the current two-element-per-side fixtures once validation layers
  are explicit.
- Setup/content identity can remain unchanged; Reserve is mutable campaign state.
- Candidate contract v1 is defensible as a new discriminated variant if consumers are inventoried.
- The delivered Stage Entry Exercise/Maneuver adoption is narrow and internally consistent; no
  actionable implementation defect was found.
- `git diff --check` and relative Markdown-link checks passed in the reviewer worktree.

## Re-review decision

The three P1 corrections materially change validation and Exercise integration detail, so review
instance 2 of 3 is required before `RES-TASK-002`. Production Reserve code remains blocked.
