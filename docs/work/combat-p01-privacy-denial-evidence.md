# P01 AC-007: private denial projection evidence

**Scope:** This proof covers only the current provisional P01 assessor's unsupported result. It
does not define or activate an outward Combat candidate, observation, selection, or hosted
transport. `P01-AC-007` remains open.

The current `combat-actual-moved-repeat-position-v1` schema freezes the internal `p01.*`
candidate preimage and golden vector. Its assessment contains private source, World and random
hashes, a private candidate ID for positive shapes, cycle and position data, and an empty
`publicActions` array. The contract does not yet define a typed audience-visible P01 candidate
or mapping from the owner's authorized observation and decision revision. Copying the private
candidate ID into an outward action would violate the contract's privacy boundary.

`verify-combat-p01-privacy-gates-v1.py` authenticates the retained evidence pins, eleven chunks,
the original CreationRequest plus eleven distinct R03 ledgers, and each exact terminal row before
using the existing provisional assessor. It requires all 300 row keys and the 44/256 provisional
partition; every assessment remains `unsupported`, `admitted=false`, with no public action. The
private denial encoder accepts the exact current assessment field set and emits only:

```json
{"contractVersion":1,"status":"unsupported","admitted":false,"publicActions":[]}
```

All authentic rows must emit those exact bytes. Focused tests compare both acting orientations
and an empty row, then mutate the private ID, source/World/random hashes, partition, cycle and
position without changing denial bytes. A nonempty action, changed status/admission/version, or
an extra private-ID field rejects. These mutations exercise the projection boundary after replay;
they are **not** authenticated equal-authorized histories and do not prove outward positive
candidate privacy. This denial encoder is a proof instrument, not a public serializer.

Verified on the retained evidence: 300 authenticated rows, 44 provisional positive, 256
provisional empty, zero admitted, with one denial encoding SHA-256
`fe1565fc181a86fd7d374e912d3698c840c8020fac991078d74bc8844b47e91c`. Three focused
tests passed with `python3 docs/specs/test-combat-p01-privacy-gates-v1.py -v`; the full source-pin
and row proof passed with `python3 docs/specs/verify-combat-p01-privacy-gates-v1.py`.

## Remaining AC-007 gate

Freeze the typed owner-visible candidate, decision revision, apparent target and public identity
preimage without private Source/Proof hashes, hidden opponent IDs/counts, raw CP/TOE/ammo, RNG,
event count or blocker reason. Construct two fully authenticated R03 histories with equal
authorized owner observations but differing hidden opponent facts. Verify identical visible
bytes, candidate IDs, and accept/reject behavior through a typed P01/P02 boundary, including
stale and forged submissions. Until that is reviewed, P01 remains unsupported and P02/P03 stay
inactive.
