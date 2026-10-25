# Combat evidence dependency revisions v1

Status: implemented with bounded local validation and independent review; private evidence only. Full release gates remain open. No public admission or RC certification.

A dependency revision is an immutable, complete path-to-SHA256 set. `legacy` retains the pre-reconciliation bytes and their existing manifest identities. `refreshed` denotes one reviewed replacement set; it must not be inferred from arbitrary caller-supplied schemas or manifest fields.

Acquisition must copy caller-owned bytes before use, enforce each family's existing size bounds, authenticate every required path (including delivery artifacts), and select exactly one complete registered revision. Mixing individually valid files from different revisions is rejected. Unknown hashes, missing files, malformed bytes, and unsupported revisions fail before replay or cache access. Returned byte arrays are independent copies.

Replay uses the selected revision's runtime pin map to derive existing domain-separated manifest hashes. Delivery pins are authenticated but are not added to runtime manifest preimages. No ambient mutable revision, global per-request state, fallback to unchecked bytes, or automatic rewriting of old command/event identities is permitted. Nested readers must agree with the selected root revision, including when their own dependency subset is unchanged between revisions.

Old evidence remains readable with its exact legacy dependencies and must reproduce identical controls, proofs, events and retry results. Refreshed dependencies may produce new provenance-derived identities; identity-bound old commands/events presented with the refreshed set must not be silently translated. Some predecessor sources and commands contain no dependency-manifest binding and remain byte-identical across revisions; those may replay under either complete set, but a retained proof claim must match the selected manifest exactly. A reader never rewrites a submitted proof claim. Wire contract versions and authoritative rules remain unchanged; a manifest digest is the existing evidence identity binding. Storage adapters must retain the matching dependency revision when preserving evidence; this contract does not assert that arbitrary older external stores have already done so.

Acceptance: legacy replay parity; refreshed oracle/native parity; mixed-revision, unknown-byte, missing-path and post-acquisition mutation rejection; cold/warm cache checks; concurrent revisions without cross-talk; owned input/output buffers; explicit classification of every changed authoritative/provenance field; focused tests for affected consumers. Independent final review and exact-head CI remain required before merge.

Outward and unrelated Snapshot source drift (`CampaignCurrentEventRuntime.cs` and `CampaignElementMovedV3Factory.cs`) remain separate gates. Historical migration manifests and dated review evidence retain original bytes.
