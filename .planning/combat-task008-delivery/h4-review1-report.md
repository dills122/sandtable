**Task008 H4 independent review — round 1 of 3**

**Verdict: Ready**, scoped to H4 implementation review. Final acceptance still requires root-owned integration gates and remaining review rounds. No actionable P0–P3 implementation or plan findings.

**Scope and independence**

Verified repository `/Users/dsteele/repos/sandtable`, branch `codex/combat-task008-reaction-lifecycle`, HEAD/base `ca43442edaebd21a812733207f49dd5f435fd4a9`.

All five hashes in `h4-source.sha256` matched before and after review. Scope comprises two untracked files, three modified history-test helpers, and four supporting documentation changes.

Read requirements, schema, tests and implementation before author packet. Preliminary ledger recorded in conversation before reading `h4-author.md`, `h4-checks.md`, or attempting session recall. No files modified, dotnet invoked, agents spawned, fixes applied, or additional rounds started.

Independence limitation: bootstrap-designated `execution.md` and canonical plan contain historical acceptance summaries. Those were visible but supplied no evidence for this verdict. No prior review reports or separate aggregate-evidence files opened.

**Preliminary ledger**

| Blind-pass concern | Preliminary conclusion |
|---|---|
| Review target stability | Branch, HEAD, explicit paths and five source hashes matched. |
| Snapshot authority | Root reconstructed through causal replay; supplied snapshot cannot confer authority. |
| Retained creation and admission | Missing Created11 rejects; actual creation-policy seam receives admission flag. |
| Contract coverage | Tests cover all 368 roots, 286 histories and 62 shared-history groups. Independent fixture audit confirms inventory and prefix completeness. |
| Bounds and ownership | Structural bounds precede replay; retained history validates capacity before payload copies; exported buffers copied. |
| Plan alignment | H4 remains bounded Initial H work; later gameplay and publication obligations stay open. |
| Remaining uncertainty | Runtime checks not independently executed; JSON mapping depends on predecessor writer shapes. |

**Implementation findings**

No actionable defects found.

- **Trust boundary:** [Restore](/Users/dsteele/repos/sandtable/src/Cna.Core/Campaigns/CampaignCombatInheritedSnapshotV12Codec.cs:13) checks root bounds, requires independently retained creation, captures owned history, invokes `CampaignCombatCreationCut.Decide`, rejects publication, replays history, then compares entire canonical root. This enforces commitment to supplied trusted history rather than merely self-consistent snapshot hashes.
- **Root composition:** [Compose](/Users/dsteele/repos/sandtable/src/Cna.Core/Campaigns/CampaignCombatInheritedSnapshotV12Codec.cs:28) starts with C2 creation root, verifies configuration/creation bindings, identity, receipt versions/event hashes and Chronicle prefix, then updates fields without changing root order.
- **Causal state:** Mapping preserves Reserve membership, inherited cycle evidence, actual versus suspended positions, closed-stop continuation and resolved inactive Reaction windows. Idle/default handling carries explicit absence checks.
- **Compatibility:** Creation-only codec and inspected predecessor/router files remain unchanged. New tests require exact C2 creation bytes and rejection of noninitial roots by creation-only reader.
- **Bounds:** [ValidateBounds](/Users/dsteele/repos/sandtable/src/Cna.Core/Campaigns/CampaignCombatInheritedSnapshotV12Codec.cs:107) applies one-MiB, depth32 and ordinary-array512 limits. Array4096 exception follows actual root `world.cohesionCauses` structure; dotted or empty property names cannot acquire exception.
- **Tests:** [CombatInheritedSnapshotTests](/Users/dsteele/repos/sandtable/tests/Cna.Core.Tests/Campaigns/CombatInheritedSnapshotTests.cs:12) exercises actual retained transcripts, exact frozen bytes, disabled admission at every selected cut, lawful foreign forks, shorter heads, malformed histories, recomputed Weather forgery, causal-field loss and defensive ownership. Expected root bytes come from frozen fixtures rather than implementation-generated expectations.

**Plan review**

[H4 and Initial H requirements](/Users/dsteele/repos/sandtable/docs/design/combat-cycle-implementation-plan.md:822) align with implementation:

- H1–H3 causal routing precedes H4 composition and restore.
- Five primary-file boundary maintained.
- Existing nineteen-field contract preserved; no migration or public registration introduced.
- README, technical design and naming documentation consistently describe H4 as active.
- Later 28-trace runtime closure, Tasks009–019, public admission and `HOST-PUB-001` remain explicitly excluded.

No dependency inversion, unsupported completion claim, or missing H4 acceptance requirement found. Final acceptance bookkeeping belongs after remaining gates.

**Author-claim reconciliation**

| Author claim | Evidence | Status / consequence |
|---|---|---|
| Exact roots at every selected cut | Full-inventory test, frozen fixture audit, retained focused-test log | Confirmed by source and retained execution evidence; not independently rerun. |
| Actual disabled-admission restore | `Restore` calls existing `CreationCut.Decide`; all-cut test passes `false`; fresh-creation negative included | Confirmed. |
| No supplied family/cache authority | Public inputs limited to request, creation and events; private composition consumes replay result | Confirmed. |
| Complete receipts, prefix and inherited evidence retained | Composition checks, exact golden comparisons and targeted mutations | Confirmed. |
| Capacity and defensive ownership preserved | Structural traversal, retained-history capture, boundary sentries and mutation test | Confirmed. |
| Focused suite passes 11 tests | `/tmp/h4-focused-freeze.log` | Confirmed: 11 passed, zero failed/skipped, 14.930 seconds. |
| Root build passes | `/tmp/h4-build.log` | Confirmed: zero warnings/errors, 5.10 seconds. |
| Full format check exited successfully | Checks packet; empty `/tmp/h4-format-full.log` | Exit status remains author-reported; empty log alone cannot establish success. |
| Historical synthetic compatibility protected by full suite | No relevant codec change identified; full-suite completion not consumed | Runtime regression claim remains unverified here. |
| Durable publication and later gameplay remain open | Code boundary and canonical plan | Confirmed. |

**Verification performed**

- `git rev-parse --show-toplevel HEAD --abbrev-ref HEAD`: expected repository, HEAD and branch.
- `shasum -a 256 -c .planning/combat-task008-delivery/h4-source.sha256`: five matches, repeated at review end.
- Path-restricted `git status`, `git diff`, and `git diff --check ca43442 -- <review paths>`: expected changes; no tracked-diff whitespace errors. Git diff does not cover untracked additions; those were inspected directly.
- Path-restricted `git diff --name-only ca43442 -- <compatibility paths>`: no changes to inspected creation codec, creation-policy implementation, retained-history/router, inherited specification/schema/fixture or test project.
- Read-only `python3 -B -c …` fixture audit: all 368 rows passed nineteen-field order, byte length, SHA-256, state-version, receipt-count, receipt-hash and receipt-version checks. Confirmed 286 unique histories, 62 shared groups, identical shared roots, every prefix present; maximum root 20,242 bytes.
- Inspected retained focused-test, build and format logs. Did not run or claim completion of full suite or boundary gate.

Tool limitations: CCE search and post-ledger session recall were blocked by approval policy; local source supplied fallback. Initial Python heredoc failed because shell required temporary-file creation; equivalent inline invocation passed. Read-only Git commands succeeded despite denied macOS cache-write warnings.

**Residual risks and next actions**

JSON-based composition introduces coupling to thirteen predecessor writer shapes and bounded parse/copy overhead. Exact-root tests provide strong protection for selected profile; future family extensions must update mapping and contract explicitly.

Trusted committed-history provenance remains caller responsibility. This review establishes neither archive authentication nor durable restart/publication behavior.

Root owner should retain completed full-suite, boundary and format-exit evidence, then conduct rounds 2 and 3 under existing policy. No code correction requested from round 1.