# Author Explanation — D2 Reserve completion codec

## Intent and plan
Task008 D2 constructs and validates frozen completion2 from complete creation-rooted D1 history.
D1 plus D2 supplies Reserve event codecs;019A must apply SAME completion event atomically to full
terminal ReserveState1 before E Movement. No added opening event, weaker contract or dropped gate.
This five-primary-file split preserves original dependency and exact bytes.

## Flow and components
CreateCommand/Create/ReadEvent accept trusted request, Created11, four preamble, one Weather, four
stage and zero/one designation events. Each calls D1 Replay; no supplied state/cache/base admission.
Private derivation retains actual request and predecessor. Input must equal exact owner command,
base hash, prior version and position. Generate derives canonical Movement position and ordinal1
cycle from retained context/order/version/prefix/configuration. ReadEvent extracts input then compares
entire bytes with independently reconstructed event. Models carry typed immutable evidence; EventBytes
serializes fresh arrays. Codec supplies OpeningBase, command/input/event, length-framed cycle identity
and receipt. Existing D1 codec only exposes guarded World writer and extracts unchanged member writer.

## Choices and invariants
Frozen OpeningBase profile literal is compatibility spelling, not isolated admission authority.
Advanced Weather RNG, current D1 World/members and actual prefix are preserved. Cycle string hashes
(rules/setup/content) remain U32-length UTF8; only openingPrefix/policy digest are raw32. Event receipt
uses rc. plus domain-separated digest of event omitting receipt alone. Canonical byte equality rejects
unknown, duplicate, reordered, alternate-number or re-signed inconsistent fields. Input event bounded
1MiB/depth32. Core metadata is trusted caller input, not remote actor authentication.

## Verification and dev review
Worker TDD red build then44/44 focused (21 D2+23 D1), all16 traces and48 frozen base/input/event
fingerprints. Independent test-side cycle/receipt framing, deliberately wrong hash representation,
coherently re-signed cycle forks, actor/occurrence/foreign/partial/reordered predecessor and raw/bounds
rejects, immutable buffers, legacy and D1 reader rejection. Lead source/test dev review found no
remaining actionable issue. See d2-evidence.md for exact executed integration and review evidence.
Unchanged oracle baseline broader than implemented scope; no claim it proves019A runtime behavior.

## Limits and challenge points
No terminal projection, completion retry ledger, postcompletion readback, Movement handoff, generic
Snapshot12, public ingress or durable publication. These remain explicit later gates. Evidence type
is internal, but later019A must consume validated complete history rather than trusting arbitrary
constructed internal records. Canonical reconstruction costs repeated bounded predecessor replay;
no cache optimization introduced. Inspect exact framing, real predecessor provenance, defensive
buffers, D1 serializer parity and preservation of original atomic opening requirement.
