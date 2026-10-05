# N4 integrated verification evidence

Date:2026-10-05 America/Toronto. Author verification record, not an independent readiness verdict.
Exact review head is supplied by coordinator dispatch after commit; base
`e90eef556bde6bbde4fd6b3e17064ba613868ee6`, branch `codex/overnight-core-sync`.
N4 modifies documentation only. Fresh reviewer must assess integrated plan/handoff separately.

## Combined-state identity and retained gate

Compared final tested/reviewed N3 `5c847895bc269d06e9d4e8d86bf90f9acd473f31`,
final published N3 `1fbc4ced8377155f327b4ab27b36bc1a88e7abbe`, merged main
`e90eef556bde6bbde4fd6b3e17064ba613868ee6` and coordinator checkpoint
`4660093ebad34b3e8b806fe62a71f34f9c24fcf7`. Each named content tree/blob is identical:

| Path | Git content identity at all four commits |
| --- | --- |
| src | `54da2cf5d14fdd2336224c0d1adf87bf53cbf828` |
| tests | `1f6c62ea0b6add15417cbdedb6d5ac892c9361b3` |
| docs/specs | `c870e5ccf3ac76f8857836e152e44a561ecc6d08` |
| docs/research | `4fe3cce799355a02114a7ff13ebf808e6d25b821` |
| Sandtable.slnx | `0c8ae21675b13fddb5a243a9ce5e85c1c64ef047` |
| global.json | `3f1eb32e402b5b999f69388e1ec0aee32a34e0f5` |
| Directory.Build.props | `3c2b131678633d47b34761e56d54191caa76426b` |
| Directory.Packages.props | `01c41899efa6e29c120a8c5721498dfab9c70ef2` |
| justfile | `cde08a73ae1c0fa20968fa7dab99943529cedfad` |

Exact command patterns: `git rev-parse <commit>:<path>` for each table cell,
`git diff --name-status 5c847895bc269d06e9d4e8d86bf90f9acd473f31 HEAD`, and
`git merge-base --is-ancestor <each merged SHA> origin/main`. The complete tested-head
comparison contains only Markdown administration; no other build/runtime inputs differ.
All five R1/R2/N1/N2/N3 merged SHAs are ancestors of origin/main. Local implementation
branch tips equal their origin remote-tracking refs, recorded in `equivalence.json`.
This is local Git evidence; hosted CI/merge success is attributed to the coordinator's live
observations in primary `.planning/combat-overnight/run-state.md`, not a new N4 network read.

All17N3 final source/project/log/binlog SHA256 hashes from the child handoff independently
matched. Full inventory `/private/tmp/n4-gates/retained-n3-hashes.json`. In particular final
`/private/tmp/n3-gates/route-just-check.log` SHA256 is
`9dfe0348c08dbffc69c10dfae789a1c0c41d4646917d7be5ba7e2ff37c0e5b9c`.
It records actual `just check`:2533solution/81Boundary, zero failures/skips, clean
restore/build/format and zero build warnings/errors. Core8m09s943ms; solution8m10s116ms.
The original N3 wrapper used `PATH=/private/tmp/n3-dotnet:$PATH just check` and appended
unique route-gate binlogs to each restore/build/test invocation of `/opt/homebrew/bin/dotnet`.
N4 **did not rerun the full solution suite**. This exact final corrected gate is reused
because complete executable inputs match and no interacting changes occurred. Historical
2530/2531gates are not substituted. N3's final native bytes already include preceding
merged R1/R2/N1/N2 work. Fresh combined-worktree checks below supplement the retained gate.

## Fresh executed combined checks

Exact cwd `/Users/dsteele/.codex/worktrees/overnight-core-sync/sandtable`; SDK10.0.400,
SDK-style projects, native MTP selected in global.json, xUnit v3 (`xunit.v3.mtp-v2` package4.0.1). No adjacent
packages.config in the test project. Detection inspected global.json, test project,
Directory.Build.props and Directory.Packages.props.

```sh
dotnet --version
dotnet restore Sandtable.slnx '/bl:/private/tmp/n4-gates/restore-{}.binlog'
dotnet build Sandtable.slnx --no-restore '/bl:/private/tmp/n4-gates/build-{}.binlog'
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatPositiveEntryTests' --filter-class '*CombatSettledControlTests' '/bl:/private/tmp/n4-gates/focused-{}.binlog'
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-trait 'Boundary=UserSpace' '/bl:/private/tmp/n4-gates/boundary-{}.binlog'
dotnet format Sandtable.slnx --verify-no-changes --no-restore
git diff --check
```

Restore and build exited0; zero build warnings/errors. Focused37/37 passed with zero
failures/skips (25.657s); Boundary81/81 passed with zero failures/skips (6.331s).
Format exited0 with empty log; diff whitespace check passed. Each MSBuild command has a
retained unique binlog. Logs are `restore.log`, `build.log`, `focused.log`, `boundary.log`,
`format.log` in `/private/tmp/n4-gates`.

## Contract and predecessor oracle results

Fresh commands execute unchanged oracles with `python3 -B`; no pins, fixtures or
dependency code are edited. Exact commands and exit status follow.

| Exact command | Exit/result |
| --- | --- |
| `python3 -B docs/specs/verify-combat-positive-entry-v1.py` | 0; PASS |
| `python3 -B docs/specs/verify-combat-settled-control-v1.py` | 0; PASS |
| `python3 -B docs/specs/verify-combat-reserve-designation-v1.py` | 0; PASS |
| `python3 -B docs/specs/verify-combat-stage-entry-v1.py` | 0; PASS |
| `python3 -B docs/specs/verify-combat-inherited-movement-lifecycle-v1.py` | 0; PASS |
| `python3 -B docs/specs/verify-combat-inherited-selection-v1.py` | 0; PASS |
| `python3 -B docs/specs/verify-combat-selection-steps-v1.py` | 0; PASS |
| `python3 -B docs/specs/verify-combat-sealed-round-v2.py` | 0; PASS |
| `python3 -B docs/specs/verify-combat-result-settlement-v2.py` | 0; PASS |
| `python3 -B docs/specs/verify-combat-inherited-breakdown-completion-v1.py` | 1; FAILED, unchanged sequence source pin |

Nine commands pass; the original Breakdown command exits1 before semantic checks with
`AssertionError: source drift: src/Cna.Core/Rules/Cna1979LandSequence.cs`. No failed
command is counted as passing. New positive entry retains3966rejections/6cuts/6retries;
settled control retains80traces/32source pins/236cuts/464retries. Raw outputs and statuses
are in `/private/tmp/n4-gates/<name>.log` and `oracles.json`.


Original Breakdown source drift is retained as failure, not counted as a passing oracle.
Exact N2 baseline18e8f99 reproduced it independently; pre146sequence pin vs PR146immutable
catalog caching. Separate N3 supplement `/private/tmp/n3-gates/breakdown-supplement.log`
passed8retained semantic/golden traces,16cuts/8retries/426mutations/92raw/298boundary probes.
N4 reuses that supplement through unchanged spec/source identity; it did not run a patched
or skipped version of the original command. Separate pin maintenance remains coordinator-owned.

## Scope, documentation and user-state validation

Final target is exactly eight Markdown paths named in neutral bootstrap. `git diff --check`
and16local Markdown target existence checks passed before freeze; anchors/network are not
validated. README/tech-design/naming summaries already match candidate-only/private behavior,
so no product/architecture/naming rewrite. Historical N0 report is labeled, its verdict preserved.
Primary status was inspected read-only: main18e8f99 with four tracked user configuration edits,
untracked `.serena/` and September20handoff. N4 performed no primary writes/staging/cleanup.
Coordinator merge/N4 commits need preservation push; existing five implementation branches are
published at their listed final heads. N4 author performs no PR/push/merge and no outbound messages.

## Evidence inventory

Temporary evidence is local and may be cleaned later; these committed hashes preserve identity,
not availability. Missing files must be reported. SHA256 inventory finalized at freeze below.

```text
3f0a5c45d2a31fd18c365df40243da50c35f1fadd2007a3996e502339518c8c2  /private/tmp/n4-gates/boundary-20261005-075614--63404--Pn3MOO-dotnet-test.binlog
9d1163ac064936a9de298b66426aaefa8ba93d36ef238f6e8391b6d7814eda49  /private/tmp/n4-gates/boundary.log
c9cafc5819b1047f77ff7921cb6f0e64cae140eabd5e4039398c5b41ca8ed68e  /private/tmp/n4-gates/build-20261005-075506--62012--Jlsixe.binlog
4256ac8c2faa7450ac0041d92ed3aec3ff2c17af39f273b611d99d516c4b1dc7  /private/tmp/n4-gates/build.log
ba6c24134b2ec0c306c5122f0a7a34617da65b81cffda7a46bd934168b1d59b3  /private/tmp/n4-gates/docs-validation.json
03478edc75ea6a8b4e9ddcaaf7ee9c1d695815713f6bc59ae87dc8ea57c4f887  /private/tmp/n4-gates/equivalence.json
926fb221331dee7a26591cdc3a27ddb85ccf3499f4d5d87fc026b3d7f05b5cdb  /private/tmp/n4-gates/focused-20261005-075548--62857--zm5JIh-dotnet-test.binlog
34a0825f7113bda7daace224a15d1fc3f350787db3ce39ba4948bac29ce79281  /private/tmp/n4-gates/focused.log
e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855  /private/tmp/n4-gates/format.log
29301e961dfe7f27f21bca40dbabfb45435508f7c16ae4535fe19fd89f5dc714  /private/tmp/n4-gates/inherited-breakdown-completion.log
7bee4ba5162e44fc17a89b1ab441a46234b5ff75097e7b5ac606a4eb34befe30  /private/tmp/n4-gates/inherited-movement-lifecycle.log
c7bfdf0c23a008bedaa208eaeadc6675f8e95f60f4214990821a2f6de1fb58a9  /private/tmp/n4-gates/inherited-selection.log
8ebe21bd1e781fa7b049de8feed8247644d23d2857c44b9dc5ab910d5133dea4  /private/tmp/n4-gates/oracles.json
37988e248972aed56c4b2f9112a3cb8b55e94c023add34a1635e4e31b4359199  /private/tmp/n4-gates/positive-entry.log
241de7c01e68847d2ebc50c8dd6129d077e4fa871893ca49a462c60965a10df5  /private/tmp/n4-gates/reserve-designation.log
77347593f5e08ec1a9be5c52b81950e56965d2df5a616f69728a6a8803fcfa50  /private/tmp/n4-gates/restore-20261005-075441--61544--AHdmB+.binlog
40d104602671164ab3958225da3e1c8cabd55f0251d6df6bc911d9d5a24f44ce  /private/tmp/n4-gates/restore.log
2a034604581df3937100289a18e23f5b3270ba57d6ff345cb0f5c2e9f3b400bb  /private/tmp/n4-gates/result-settlement.log
483f1e344b792bf9369354e8270e7200455b887a25383b13b1c53520a8c32724  /private/tmp/n4-gates/retained-n3-hashes.json
7497cdcac2a85a116e5b2eaaf43be2313d90522fddc1da852312b388eedd527c  /private/tmp/n4-gates/sealed-round.log
a6d1fb28ec2d5487bfd23e5cd0c4dc208c4bda1147d027ad9ded9c0639777691  /private/tmp/n4-gates/selection-steps.log
20566545f49dffe0c5408d06e32ad0f05448be8197fd629c444dbef00da96af0  /private/tmp/n4-gates/settled-control.log
e372e19cdc54d3802cff035842151cca20a2695a1c2d9106592e3b29436d5305  /private/tmp/n4-gates/stage-entry.log
fe89c0621921a369bbe7a28e5c6c58b22b4466221923ab91165401824a1a34ec  /private/tmp/n4-gates/state.json
9dfe0348c08dbffc69c10dfae789a1c0c41d4646917d7be5ba7e2ff37c0e5b9c  /private/tmp/n3-gates/route-just-check.log
1640401235a3c3ef8fd0563efe1fceca7677a731353f5902499dddea9daf7222  /private/tmp/n3-gates/breakdown-supplement.log
```
