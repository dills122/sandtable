# Private P01 Core opening journal cut v1

**Status:** independent, disposable, private creation-stage proof artifact.
It is not the A1 R03 validation checkpoint, not the A2 sequence-zero
`r03-import-checkpoint` record or its predecessor/head/authority, and not a
campaign store, owner path, public action, P01 admission, or full R03
predecessor issuer. It borrows the independently pinned `r012-c01` Content7
CreationRequest from A1 solely as test input. The remaining 12 entries in its
first predecessor ledger and ten later ledgers remain caller-supplied evidence
until separately implemented and certified. Neither A1 validation nor A2
owner authority consumes this artifact.

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

The independently retained exact-byte SHA-256 vectors are:

| Cut | Canonical journal SHA-256 |
| --- | --- |
| Zero records, Created11 only | `fe42392dceb566faf641e7e5de0b32f482c3def94f840daceb719ee9f28a6f6c` |
| One record, System initiative | `ce7795a0993e5bb0fb35793a375cecc5dd8e9d8cada97d62fd66237b825d763a` |

Tests compare both complete canonical byte strings to these frozen digests,
in addition to authentic Created11/input/event parity. Altering any accepted
byte must fail readback and must not match the pinned digest for that cut.

The journal is an immutable proof byte value; Core does not persist or publish
it. It is deliberately disconnected from A2's future host journal. A future
host design must independently define durable compare-and-swap and head
retention to detect rollback or concurrent writers. A caller presenting a
valid earlier zero-record value cannot be distinguished from a genuine
unadvanced cut here.
This proof does not establish authority for a player input, host identity,
recovery after durable restart, or any of AC-003/006/007/008 in full.
