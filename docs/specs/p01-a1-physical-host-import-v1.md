# Private P01 A1 physical host import v1

**Status:** private, unregistered, unissuable proof for the single authenticated
`r012-c01` R03 terminal cut. It creates no seat grant, owner opening, action,
Chronicle event, public endpoint, receipt, or admitted Combat state.

## Selection and Core validation

`P01A1PhysicalHostImportV1.ValidatePhysical` accepts only host-selected absolute
paths to the original physical evidence archive and trusted repository source.
It pins the manifest, selected `rows-036-067.jsonl.gz` chunk, native 300-row
hash catalogue, and the one `r012-c01` row. It reconstructs the canonical A1
import from that row's CreationRequest, ten predecessor ledgers, R03 input and
Source, and compares the complete import to the pinned CI-local import fixture.
The retained terminal Control bytes likewise must match the pinned same-cut
fixture. A row label or supplied hash alone cannot authorize import.

Core reconstructs independent Rules10/Content7/Setup7/configuration context
from the hash-pinned authority-envelope source, replays all eleven ledgers via
the [A1 validator](p01-r03-validation-checkpoint-v1.md), and recomputes the
[same-cut source attestation](p01-r03-same-cut-source-attestation-v1.md).
The façade returns copies of the canonical import, Control, checkpoint and
attestation bytes plus the campaign ID derived from the validated original
CreationRequest. Callers cannot construct a validation result through its
public API. `ValidateRetained` checks the pinned import and Control hashes and
recomputes both Core results for cold readback. It is not physical archive
authentication; the initial import must use `ValidatePhysical`.

Physical manifest cap is 8,192 bytes, compressed chunk cap 4,000,000 bytes,
selected row line cap 2,500,000 characters, catalogue cap 300,000 bytes,
import cap 2,097,152 bytes, and Control cap 65,536 bytes. Incorrect path,
byte, pin, selected row, context, or canonical result fails closed with
`P01-A1H-001` or the existing A1/same-cut rejection codes.

## Host transaction and readback

The internal `P01PrivateHostFoundation.ImportPhysicalA1` validates the physical
archive before opening an immediate SQLite write transaction. The current
operator secret is checked, then one transaction inserts the campaign binding
and the exact import, Control, checkpoint and attestation blobs into the new
`a1_imports` table. The `checkpoint_hash` in `campaigns` is SHA-256 of the
validated checkpoint bytes; the initial head uses the existing domain-bound
campaign binding. A failed validation or transaction leaves neither row.

This is store schema version 3; older versions have no automatic migration. The
campaign row stores a physical-import origin and domain-bound origin digest in
the same transaction as the evidence. Deleting the evidence row leaves the
origin marker intact: cold readback fails and activation and grant issue remain
closed. Exact DDL, column, foreign-key, index and unexpected-object checks cover
`a1_imports`. A cold readback verifies campaign/head/fence binding and reruns
Core's retained-byte validation before returning any evidence. Imported rows
cannot acquire an activation fence or issue a seat grant. The older test-only
seed path remains for storage primitive tests and is not a physical import.

The original archive is external to the repository. Portable CI tests cover
retained-byte Core validation and malformed host storage. The local physical
gate sets `SANDTABLE_P01_PHYSICAL_ARCHIVE` to the pinned archive and runs the
focused OrleansHost test class; it covers physical selection, atomic commit,
cold reopen and unissuable behavior. The full P01 certificate, authentic owner
observations and submissions, and public/native admission remain open.

The fixed original r012-c01 adapter selects the authenticated original evidence
dependency archive during retained validation and cold readback. Current working-tree
dependency revisions cannot silently replace that original root's manifest identity.
See [evidence revisions](combat-evidence-revisions-v1.md#original-p01-proof-consumers).
