# Snapshot evidence reconciliation

Status: focused validation complete; independent review **Ready**, no actionable findings. Base `38bf158dde122af8d54eeefba2d6c845e6da80a0`, branch `codex/rc-snapshot-evidence`.
Ruflo task `task-1793144652408-bzjsxa`; namespace `sandtable-rc-20261023`.

## Intent and plan

Reconcile the two stale Snapshot runtime-source pins against actual source changes,
retain original evidence, rebuild dependent provenance, and validate affected
readers. User approved expanded dependent refresh/native compatibility and one
additional independent review (instance9/9). No new gameplay, public admission,
certificate, historical-store migration or playable RC claim.

## Source changes and compatibility

Both original source hashes resolve exactly to `5c16fdb6`. Current event routing
adds native Combat arms; the Snapshot serializer/projector suffix is unchanged.
Movement extracts a World-based overload and adds a fail-closed stacking guard;
current authority remains one battalion organization at stacking value1.

The owning inherited-successor oracle also assumed the entire router contained
only historical arms. It now checks exact equality against the historical set
plus28 explicitly named native extensions. The20 inherited declarations, versions,
owners and opening histories are unchanged. Tests reject all38 missing router
names, an unknown name and an inherited/native overlap. A subset test or arbitrary
new-event acceptance was rejected as insufficient.

Actual Selection previously checked only original fixed dependency hashes. It now
uses existing whole-revision owned acquisition, without changing selection
transitions, event contracts or proof construction. `CombatEvidenceRevisions`
retains all27 previous registrations and adds a separate `snapshot` set with23
explicit overrides. Old and new bytes cannot be mixed into an accepted set; nested
readers retain the root revision. No mutable ambient revision or unchecked fallback.

## Evidence preservation and regeneration

The two original ZIPs, original P01 fixtures and original300 native catalogue are
byte-identical to base (22 protected files). A separate161-file PR334 ZIP contains
exact `git show 38bf158d:path` bytes, including the prior refreshed300 catalogue.
SHA256: `d96e0fc8d0afc59a6a9849a88bb37ac49cc25bd0ddccd119e99b7bbe47408040`.
The reviewer independently checked every entry. Original archive hashes and old
runtime registrations remain unchanged. Current native parity uses the separate
refreshed catalogue; no original root is promoted.

Pins were propagated only through the declared dependency closure. Owning oracle
functions regenerate proof-bearing fixtures in dependency order. JSON comparisons
recursively decode embedded canonical JSON, require identical shapes/counts/types,
and permit scalar changes only where replacing64-hex digest substrings makes old
and new strings identical. This distinguishes provenance-derived receipt, decision,
cycle, commitment and capability IDs from gameplay values. Side-projection payloads
are identical after removing only provenance maps; retained subset digest checks
remain active with recomputed values. Original tests are retained; new compatibility
and dispatch-negative checks are additive.

Outward's prerequisite correction updates24 stale requirement line references to
unique existing IDs and two design-source hashes. Two further stale Outward pins were exposed by
its original check: Content7 documentation changed only implementation/status prose
in806d2d60; the cycle-sequence fixture changed only a source hash in PR334.
Their exact old commits and hashes are retained in the ledger. Requirement text, status, planned
tests and owners remain identical. Outward contract evidence does not assert those
planned runtime acceptance tests are implemented.

## Validation and limits

Retained logs and receipts: `artifacts/rc-snapshot/`. The committed evidence ledger
summarizes exact commands, counts, exits, hashes and reviewer disposition.
Original Snapshot failure and intermediate pin/dispatch failures remain retained.
A turn interruption left early Selection/generation runs incomplete; those are not
claimed passing. A controlled generator exit75 paused between stages for the native
dependency update; no healthy test was killed. Final runs use corrected inputs.

Completed checks (process exit0, no failed/skipped selected native tests):

- Release solution build; format verification for all changed C# files.
- Selection60; Snapshot24; retained PR334 compatibility1 using final161-file archive;
  legacy33; Core P0110; Host P0146; authority boundary81.
- RoundEntry177 and current R03 parity300, each split into four exact-display chunks.
- Original Snapshot:368 cuts,6992 mutations,6992 omissions,2576 raw negatives.
- All21 inherited owning oracles, including strict successor dispatch negatives.
- Both runtime-source tamper probes rejected; side-projection retained subsets passed.

Counts describe selected runs, not a deduplicated solution total. Original occurrence oracle passed8 groups, including2576 continuation cuts and
67 rejection checks. Parent main passed all behavior and fixture checks (12 children,
10 parents). Its wrapper then failed on the two stale Outward pins described above;
that exit1 is retained separately from the corrected Outward run. Corrected Outward run exited0:72 requirements,99 pins,12 profiles,51 readbacks,
2440 capacity records and4 fixture mutation rejects. Its comparison permits only
24 source-line corrections,3 verified file-byte-count changes and26 digests.
All43 other changed fixtures have identical shapes/counts/types and only digest
changes. Final pin refresh made zero changes; final source hashes match validation.
Independent review9/9: **Ready**, no actionable findings.
[Review report](2026-10-27-snapshot-independent-review.md).

Full solution tests, hosted exact-head CI, full B2 registry execution, Maproom,
external physical P01 archive admission and broad release validation remain outside
this focused packet. Source certification and playable-product gates remain open.
