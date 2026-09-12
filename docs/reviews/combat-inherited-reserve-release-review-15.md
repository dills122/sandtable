# Inherited Reserve Release Review 15

**Review target:** `CMB-TASK-003D2c.3i` working-tree packet based on `fbfd849`

**Final implementation commit:** `719ea0d`

**Verdict:** Ready with non-blocking follow-ups; the sole P3 was corrected before the final commit.

## Scope

The review covered the creation-rooted inherited Reserve Release specification, schema, fixture,
Python oracle, and the four navigation/status updates. It verified that exact D2c.3h histories for
both owners can open Release, accept owner `release-I`, and complete at the same sequence position
without claiming guarded repeat, ordinal-2 Movement, full Snapshot composition, C# runtime, public
actions, or simulator support.

Pre-existing `.gitignore`, `AGENTS.md`, `.claude/`, `CLAUDE.md`, and
`.github/copilot-instructions.md` changes in the author's checkout were excluded and not committed.

## Finding and resolution

### P3 — Retained negative/recovery evidence did not match the full specification wording

The initial oracle relied on parent coverage or ad hoc reviewer probes for several cases that the
new specification described as retained 3i evidence. Current behavior was correct, but a later
wrapper regression could have escaped the new packet while its traceability table still claimed
complete coverage.

The author accepted the finding and added failure-sensitive both-owner probes for:

- out-of-order and duplicate events;
- unknown owner choice;
- post-open clock regression and clock unavailability;
- excluded `complete-release`, repeat, Movement, Snapshot, and runtime command families; and
- exact deterministic fallback reasons.

No gameplay policy, runtime code, or dependent capability changed.

## Confirmed invariants

- Exact D2c.3h Normal-Weather Reserve-I history is reconstructed; a supplied hash is never trusted
  without replay and byte comparison.
- System owns open, completion, and recovery; the actual cycle owner alone owns `release-I`.
- The positive path advances authority 22→25 and remains at same-slot Reserve Release.
- Only the matched own member changes from Reserve I to none. World otherwise, RNG, Weather,
  locations, CP0, designation history, and prior Chronicle prefix remain bound.
- The disposition alone records material progress and creates the same-scope ordinal-2 Movement
  exception with CPA basis and voluntary ceiling 10.
- Mandatory fallback remains deterministic and does not depend on a model or remote service.
- The packet exposes no opposing hidden state and activates no production capability.

## Reproduced verification

- Focused 3i oracle: 2 traces, 6 events, 8 cuts, 6 retries, 658 mutations, 24 malformed-byte
  rejections, 31 boundaries, 10 recovery paths, and 7 source pins.
- D2c.3h predecessor: 2 traces/20 events, 22 cuts, 20 retries, 1,103 mutations, 46 raw rejections,
  196 boundaries, and 13 source pins.
- D2b.1 Release control: 13 cases/48 traces, 188 cuts, 2,368 mutations, 840 raw rejections,
  20 timing checks, and 27 boundaries.
- D2b.2 cycle control: 19 cases/64 traces, 164 cuts, 1,748 mutations, 700 raw rejections,
  43 boundaries, and 216 cost coordinates.
- Python AST, JSON parsing, and `git diff --check`: pass.
- Local Markdown targets/anchors: 170 files, 1,390 targets, and 27 anchors passed the inspected
  fallback checker. Pinned Lychee 0.24.2 was unavailable, so external-link verification was not run.

No .NET or simulator command was required for this documentation/Python-only feature boundary.
The repository-wide documentation audit that retained this report separately ran all 26 contract
oracles on the final parent branch; build/test evidence is reported with that audit and is not
retroactively attributed to this review.

## Residual boundary

Guarded repeat/finish, positive released-Reserve Movement and exception expiry, alternate owner
conversion profiles, broader Reaction/vehicle families, armed Combat continuation, D2c.4,
Task004, checkpoint B, and runtime/public/simulator work remain open. The explicitly authorized
Combat review sequence is exhausted at 15 of 15; another independent pass requires new owner
authorization.
