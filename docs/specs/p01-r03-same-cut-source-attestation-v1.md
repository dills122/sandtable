# Private P01 R03 same-cut source attestation v1

**Status:** private, unissuable Core attestation for the single authenticated
`r012-c01` R03 terminal cut. This does not admit P01, emit an owner-visible
field, authorize an action, append a journal record, or expose an API.

## Evidence boundary

The attestor first executes the [A1 checkpoint validator](p01-r03-validation-checkpoint-v1.md)
on the canonical import, independently trusted Rules10/Content7/Setup7/config
context, and all pinned runtime dependencies. A1 replays CreationRequest, ten
predecessor ledgers, and the R03 ledger, then recomputes the terminal Control
hash. The attestor accepts a separately retained terminal Control byte claim
only when its SHA-256 equals that A1 checkpoint hash. It reads source facts
from those matched Control bytes and the trusted Content7 definition. Thus
the campaign, cycle, position, representation, and location facts refer to
one authenticated terminal cut. A claim cannot choose a different World.

The retained physical archive is
`/Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence`.
[`verify-p01-r03-same-cut-source-v1.py`](verify-p01-r03-same-cut-source-v1.py)
first checks all A1 manifest, chunk, catalogue, source, event, Control, Proof,
and import pins, then checks that the CI-local retained after-Control fixture
is byte-identical to `r012-c01` in that pinned chunk. The compressed fixture
SHA-256 is `3c4eb5a37af03f5e07c03e38403fed371d802af51bdd0099552e5fc3f3037cf4`;
its decompressed Control SHA-256 is
`1ac7c737fd7f7fe38d13b741cdc92ef658d3e4e9e2b9d06d736207f24c50de7c`.
The fixture is parity evidence, not a replacement for the physical archive.

## Closed attestation

`P01R03SameCutSourceAttestationV1` is immutable private canonical JSON with
fields in this order: `contractVersion`, `a1CheckpointHash`,
`terminalControlHash`, `campaignId`, `creationBinding`, `cycleId`, `positionId`,
`gameTurn`, `operationStage`, `cycleOrdinal`, `actingSide`, `rulesetHash`,
`setupHash`, `contentHash`, `configurationHash`, `sourceElements`, `admitted`.
Each source element has `elementId`, `sideId`, `representationId`, `locationId`,
and a fixed `provenance` object naming the trusted Content7 side source and
authenticated R03 Control World representation/location paths.
The side comes from independently trusted Content7; the element,
representation, and current location come from the authenticated terminal
Control World. All elements must have one independent representation at the
same trusted Content7 location. The cycle identities, position, creation
binding, terminal version, prefix, and receipt must match the A1 checkpoint
and trusted context. The attestation contains no `pub.` reference, owner
opening, seat, legal-action claim, or journal sequence. `admitted` is always
`false`. A caller-supplied attestation is accepted only by full byte-for-byte
recomputation.

Control input cap is 65,536 bytes; attestation cap is 4,096 bytes. Generic
private rejects are `P01-A1S-001` for Control size, `P01-A1S-002` for a
Control hash mismatch, `P01-A1S-003` for same-cut content/World invariants,
and `P01-A1S-004` for claimed attestation readback. A1 and native replay
retain their own rejection codes. No partial attestation is returned.

This is one-root source evidence only. It does not attest all 300 roots,
current seat authority, owner-visible history, authentic owner decisions,
legal actions, host recovery, or any public/native admission. A2 and the full
AC-003/006/007/008 certificate remain separate gates.
