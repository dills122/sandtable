# Private P01 Core opening journal cut v1

**Status:** private, unissuable one-step Core proof. It is not a campaign store,
owner authority path, public action, P01 admission, or full R03 predecessor
issuer. The bounded proof uses the independently pinned `r012-c01` Content7
CreationRequest from A1. The remaining 12 entries in its first predecessor
ledger and ten later ledgers remain caller-supplied evidence until separately
implemented and certified.

## Closed byte contract

[`p01-core-opening-journal-v1.schema.json`](p01-core-opening-journal-v1.schema.json)
describes the storage shape. The Core codec additionally requires exact UTF-8
JSON serialization with the listed property order, no duplicate/unknown keys,
and a maximum of 1,100,000 bytes. The complete CreationRequest bytes must hash
to `sha256:b8c96173e432f7f8f09b68cd13e2d1e1a2d49e1a2380815b49fe23dd5510a16c`.
The trusted context and request are supplied separately; the journal cannot
choose its own campaign, seed, setup, configuration, or content. `createdEventCanonicalUtf8`
is issued by Core's existing Created11 factory. It is never imported as an
authority claim.

The sole appendable record has sequence `1`. Core derives the System
`resolve-initiative` input from the replayed creation state, applies the
existing opening transition, and records exact canonical input, event, source
references, receipt ID, and predecessor/successor prefixes. The record is
accepted on read only if the entire journal matches Core's recomputation byte
for byte. Repeating the append returns identical bytes. A foreign request,
wrong actor, clock, source references, receipt, or altered framing rejects.
The first input must equal the first retained input in A1's authentic `r012-c01`
predecessor ledger.

The journal is an immutable byte value; Core does not persist or publish it.
A future host must durably compare-and-swap the head and keep the prior head to
detect rollback or concurrent writers. A caller presenting a valid earlier
zero-record value cannot be distinguished from a genuine unadvanced cut here.
This proof does not establish authority for a player input, host identity,
recovery after durable restart, or any of AC-003/006/007/008 in full.
