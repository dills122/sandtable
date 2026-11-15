# Scheduled arrival index v1 — bounded contract

**Status:** implemented and tested for synthetic rules-laboratory inputs only.
This contract defines an immutable, internal lookup index. It does not admit the
Graziani scenario, create a content format, change existing setup hashes, advance
a phase, or authorize a player-facing action. [Source handoff](../work/plans/2026-11-12-graziani-arrival-authority-handoff.md)
records the remaining source, scenario, command/event, state and replay gates.

## Data and invariants

An arrival group has a stable identifier, owner, game turn, operation stage,
nonempty stable unit identifiers and nonempty source references. IDs use the
existing content stable-ID grammar. The index defensively copies input and
canonicalizes groups by turn, stage, owner and ordinal group ID; unit IDs and
source references are ordinally sorted. It accepts only defined sides, turns
1–111 and stages 1–3. These numeric bounds validate a data shape, not playability
of every turn. Group IDs are unique. A unit cannot appear twice within one group
or in two groups at the same turn/stage; a later entry can refer to the same unit
because withdrawal and return may exist. No assumption is made about its legal
presence, control or actual later return.

A lookup takes a turn, stage and owner and returns only that owner's groups in
canonical order. This is an internal filter, not an authorization check: caller
must derive the side from an authenticated authority. Empty results mean only
that this index contains no matching groups. They do **not** prove a scenario
has no reinforcements, supply, naval convoy, or other phase obligations.

## Boundaries and evidence

The index is a pure value-level helper. Historical or public activation requires
source-verified content and rights, admitted configuration and hashes, side-safe
commands, unit identity validation, exact event/snapshot/receipt schemas,
completed-state and duplicate protection, legality of deployment, stage ordering,
and deterministic replay. It must never be wired into existing `ExplicitNone`
authority as evidence of emptiness. Test only synthetic groups here.

Acceptance checks: immutable copied inputs; canonical order independent of input;
exact time/owner selection; valid later recurrence; reject malformed IDs, invalid
sides/times, empty groups/sources and duplicate IDs/units within the same slot.
Negative checks must demonstrate that a returned collection cannot be mutated.
These tests do not certify six-turn play or the source correctness of any record.
