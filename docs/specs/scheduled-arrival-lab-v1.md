# Scheduled arrival laboratory v1

**Status:** implemented bounded synthetic transition model; not a published-scenario ruleset, campaign
admission, save schema, public action or a replacement for the existing
`ExplicitNone` policy. The [scenario draft](graziani-setup-draft.md) and
[arrival handoff](../work/plans/2026-11-12-graziani-arrival-authority-handoff.md)
record unresolved source, rights, destination, timing and integration work.

## Purpose and scope

Exercise obligation ordering and replay using explicit synthetic inputs before
changing campaign serializers or historical setup identities. Each admitted lab
scenario supplies a stable scenario identity, immutable arrival index, and an
explicit independently enumerated set of non-land obligations for one turn and
stage. The lab does not infer that set from the land index. It models resolution
of land groups and acknowledgment of synthetic non-land obligations; it does not
create real campaign units or decide the rules for naval convoys and supply.

A lab position is `(turn, stage)`. A group must have exactly one owner and one
or more stable unit IDs. Its synthetic allowed destinations are explicit stable
IDs. Construction rejects missing group/destination mappings, extra mappings,
duplicate identities and overlap with unit IDs already present at creation. An
invalid UTF-16 source reference is rejected before JSON hashing, preventing
distinct malformed strings from collapsing to the same replacement character. An
arrival group is pending until a valid owner command resolves it. Resolution
places all constituent units atomically at one allowed destination. This is a
synthetic constraint for this lab, not a claim about historical arrival choices.
The scenario data and starting state are trusted fixture inputs, never a side's
command. The immutable admission binding must include the entire scenario,
destinations and explicit other-obligation list; a bare identifier is not a
cryptographic proof of content identity.

## Commands and transitions

Every command carries contract version 1, expected state version, exact turn and
stage, and a stable command ID. An authenticated actor is supplied separately;
its identity must not be inferred from a client-provided owner field. Commands:

- `ResolveLand(groupId, destinationId)`: only the group's owner may resolve a
  group at its exact slot. Destination must be in the admitted lab allowlist;
  no constituent unit may already exist. One event creates all units and marks
  the group complete, with unchanged slot. The lab performs no random draw and
does not model a campaign RNG stream; a future integration must prove that
stream is preserved.
- `ResolveOther(obligationId)`: only the trusted system actor may resolve an
  explicitly enumerated non-land lab obligation. This models its completion,
  not its domain effects.
- `CompleteArrival`: only the trusted system actor may complete the slot, and
  only once **both** pending sets are empty. Land completion by itself cannot
  erase unsupported or non-land obligations. This changes the lab phase to
  complete; it does not advance the actual campaign sequence.

A successful command increments the state version exactly once. Stale version,
wrong actor/slot, unknown or completed group, invalid destination, duplicate unit,
unresolved obligation, or a new command after completion rejects without an event,
world mutation, random draw or receipt. A successful retry using the same command
ID and same canonical command and actor returns its original receipt and event
without incrementing state; a changed payload or actor under that ID rejects.
The command ID namespace is per campaign and includes rejected commands only if
the actual host's accepted-receipt policy explicitly says so; this lab retains
accepted receipts only. Expected version is part of the canonical payload, so a
successful lost-response retry must preserve its original expected version.

The lab event must carry schema version, scenario binding, command ID, actor,
prior and resulting versions, exact slot, event kind and exact effect: group and
destination and canonical constituent list; or other-obligation ID; or completion.
A receipt binds the accepted canonical command, actor and emitted event. The
serialization format and cryptographic hash algorithm are **not frozen** by this
prose; no persisted/public wire compatibility claim follows. Events must be
immutable and must not expose other groups to a player-facing interface.

## Projection and replay

Replay starts from the same admitted fixture and initial state. For each event,
reconstruct the expected command effect from prior state and trusted fixture,
verify event identity and all fields, and apply only on exact match. Reject
missing interior, reordered, duplicate, inconsistent or cross-fixture events
before returning a result. A valid prefix cannot reveal a missing suffix without
an independently authenticated expected head. Likewise, a different but legal
history cannot be detected as forged without a trusted journal or signature. Replay may rebuild accepted-retry receipts from events only if the event
retains the complete canonical command identity and actor; otherwise receipts
must be included in a separately authenticated checkpoint. A checkpoint must not
be treated as proof of history merely because its version looks plausible.
Receipt durability and atomic event/receipt commit remain Host integration work.

The lab's actor filtering is not a complete outward privacy design. Public
adapters must prevent side channels in legal actions, errors, logs, event streams,
snapshots and timing, including future/opponent groups and hidden choices.

## Acceptance and integration gates

Synthetic tests cover exact slot and owner; allowed and rejected destinations;
atomic all-unit placement; duplicate units; two land groups plus a non-land
obligation; every completion ordering; early completion rejection; stale and
wrong-side commands; successful retry and conflicting command ID; no change on
rejection; deterministic full replay; inconsistently altered, reordered, duplicate and
cross-fixture events; and post-completion rejection. Authentication of an entire
legal history and missing-suffix detection require an independent trusted head.

Before connecting this design to `CampaignEngine`, define and implement compatible
setup/content identity, world element and representation creation, current-phase
and all-category obligation admission, versioned command/event/snapshot codecs,
strict projection, side-safe legal actions and host atomic receipts. Existing
`NoObligationNavalConvoyArrivalResolved` remains valid only for its original
admitted policy and versions; it must never be reused to prove a new schedule is
empty. The current stage-entry factory is explicitly scoped to stage 1 and fixed
initial version; later stages require a separately admitted path. Historical
scenario rulings, legal destinations and phase placement require their own review.

## Current campaign integration constraints

The laboratory uses its own `ArrivalLabState`, not `CampaignSnapshot`. The
following observed implementation constraints are why replay in this laboratory
is not an actual campaign replay:

- [Stage-entry event constructors](../../src/Cna.Core/Campaigns/StageEntryEvents.cs)
  bind the current initial arrival to state version 8 and operation stage 1.
  [The factory](../../src/Cna.Core/Campaigns/CampaignEngine.cs) builds that event
  only for the catalog's exact `ExplicitNone` policy.
- [Snapshot validation](../../src/Cna.Core/Campaigns/CampaignSnapshotValidator.cs)
  ties early positions to state versions and validates pre-movement World as
  initial. The newer [Snapshot11 admission](../../src/Cna.Core/Campaigns/CampaignSnapshotV11Admission.cs)
  similarly ties its initial positions and World to its bounded profile. An
  inserted event changes these relationships and cannot be made compatible by
  ignoring version or accepting any World.
- [World validation](../../src/Cna.Core/Campaigns/CampaignWorldFactory.cs) derives
  expected elements and representations from initial placements. A real arrival
  needs a versioned admitted unit lifecycle and representation identities, plus
  validators for both pre- and post-arrival states. Adding a location string to
  the lab dictionary does not create one of those campaign elements.
- Current event/snapshot codecs, projections and Host journal routes have their
  own contracts and prior-version readers. Their compatibility, atomic receipt
  persistence and eventual player observations must be designed and tested as
  one explicitly admitted profile; this lab changes none of them.

The lab's fixture hash is an internal SHA-256 over its deterministic JSON fixture
encoding. It binds the tested fixture inputs for in-process replay, but is not a
published canonical content hash or a trusted journal head. In particular, this
laboratory does not authenticate an otherwise legal alternate history, detect a
missing final suffix without an external head, persist receipts, enforce real map
stacking, or protect a public API.
