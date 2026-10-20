# Private P01 R03 validation checkpoint v1 (A1/P03a)

**Status:** implemented as a private, unissuable Core validator for the single
retained `r012-c01` root. This does not admit P01, append an event, create an
owner opening, promise host recovery, or enable a native/public action. The
complete AC-003/006/007/008 certificate remains open. The [owner-approved A1
plan](../design/p01-private-authority-journal-proposal.md) defines this boundary.

## Trusted selection and physical source

Core's A1 validator accepts a canonical import byte string, an independently
constructed `CampaignCombatCreationContext`, and a trusted dependency byte
reader. The import cannot select its own expected row, Rules10/Content7 setup,
configuration, or expected terminal vector. The validator requires the exact
reviewed Rules10 hash, Content7 hash, setup hash, configuration hash, campaign
identity, dependency-manifest hash, and final Source hash. It then calls the
existing native R03 replay with the CreationRequest, ten predecessor ledgers,
one R03 ledger, and 108 pinned runtime dependency bytes. Replay recomputes
the System event, Control, Proof, World, RNG, version, prefix and receipt. The
result must match the independently frozen r012-c01 terminal vector.

The physical source is the original private archive at
`/Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence`. Its
`manifest.json` SHA-256 is
`39cae77c405b83a793b3d806fcf4a67860b9ff8514e59c9bfa83384d7dfc2b35`.
`rows-036-067.jsonl.gz` has SHA-256
`9f79444d6efe3577f7739b4e1dac363f20bb589e7ad0661e58d7293d3006f5cb`.
The independent 300-row native hash catalogue has SHA-256
`cf3f8ecfe9e4040d5678a6e0e9cc2441525e1e8fe3a3f35cf316207a9f45ec4e`.
The CI-local selected import fixture is
[`p01-r03-a1-r012-c01-import-v1.json.gz`](fixtures/p01-r03-a1-r012-c01-import-v1.json.gz),
SHA-256 `5019978589938979d7010416f6e3424e07615532475f5633a60db66414db14aa`.
The fixture is parity evidence, not permission to skip physical archive
authentication. Run `python3 docs/specs/verify-p01-r03-a1-physical-v1.py
/Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence` at the
archive gate.

## Closed canonical byte contracts

`P01R03ImportV1` is exact UTF-8 JSON with these fields in order:

1. `contractVersion`: integer `1`.
2. `creationRequestCanonicalUtf8`: ASCII string containing canonical original
   CreationRequest JSON, maximum 4,096 bytes.
3. `predecessorInputLedgersCanonicalUtf8`: ten ordered arrays of canonical
   ASCII input JSON strings. At most 128 inputs total; each at most 4,096 bytes.
4. `r03InputLedgerCanonicalUtf8`: one-element array of the original canonical
   R03 System input JSON string.
5. `r03SourceCanonicalUtf8`: original canonical R03 Source JSON ASCII string,
   maximum 1,048,576 bytes.
6. `dependencyManifestHash`: exact literal
   `sha256:0052acb77da04ecbbee792a0cb330fd4d8189cca1513d5d450fbb716d265cfeb`.

Total import cap is 2,097,152 bytes; combined input cap is 32,768 bytes.
Unknown, repeated, reordered, noncanonical, oversized, or non-ASCII fields
reject. Strings are written with the .NET relaxed JSON encoder only for the
canonical outer framing; each contained JSON value is parsed and replayed by
the existing native closed codecs. The supplier provides no claimed Control,
Proof, event, receipt, admission, or row label.

`P01ValidatedCheckpointV1` is exact UTF-8 JSON with these fields in order:
`contractVersion`, `campaignId`, `creationBinding`, `creationRequestHash`,
`orderedLedgerHashesAndLengths`, `dependencyManifestHash`, `r03SourceHash`,
`r03ControlHash`, `r03ProofHash`, `terminalStateVersion`, `terminalPrefix`,
`terminalReceiptId`, `positionId`, `activeCycleId`, `admitted`. Each of the
eleven ordered ledger descriptors contains `bytes` then `hash` of the exact
canonical JSON ledger array. Plain `sha256:` hashes cover exact bytes; no
digest authenticates a supplier by itself. Checkpoint cap is 4,096 bytes.
`admitted` is always `false`. The checkpoint has no append sequence, outward
identifier, owner-visible fields, World, or journal record. Repeated validation
returns byte-identical output and makes no write. `ReadCheckpoint` recomputes
the import and compares the entire claimed checkpoint byte for byte.

Rejections are `P01-A1-001` for closed import/cap/canonical errors,
`P01-A1-002` for trusted selection/source mismatch, `P01-A1-003` for a
recomputed terminal-vector mismatch, and `P01-A1-004` for claimed checkpoint
readback mismatch. Native R03 and ancestor rejection codes remain intact.
No A1 rejection returns a partial checkpoint.

This version is immutable. New fields or accepted roots require a separate
version, reviewed pins, and migration rules. A2 host seat/clock provenance,
conditional journal append, durable readback, owner decisions, and eventual
public/native admission require separate owner approval and proof.
