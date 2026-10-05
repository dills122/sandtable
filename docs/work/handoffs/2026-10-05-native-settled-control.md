# Handoff: native Result2 settled control

## Objective And Boundary

CMB-019D3 / overnight N1 implements the private native019D2 settled-control profile.
Earlier Movement remains synthetic-pre-combat. No actual positive-history entry,
ordinary repeated Movement or Convoy execution/cost, later-II/consumed history,
public/Snapshot/transport activation or parent017–019 closure is claimed.

## Canonical Sources

- `docs/specs/combat-settled-control-v1.md`, ordered schema, frozen fixture and oracle.
- Unchanged settled-continuation019D0 contract and native019D1 admission.
- `docs/design/combat-cycle-implementation-plan.md`, Task019D3 frozen manifest.
- Primary `.planning/combat-overnight/session-plan.md` N1 execution limits.
- Separate review packets: `docs/work/reviews/2026-10-05-native-settled-control-bootstrap.md`
  and `docs/work/reviews/2026-10-05-native-settled-control-author.md`.

## Current Repository State

Managed worktree `/Users/dsteele/.codex/worktrees/native-settled-control/sandtable`.
Branch `codex/native-settled-control`, review base
`907f41403ed159d65024f193f3e1f730a23b9bbb` (merged150/151).
Review used an explicit dirty boundary at907f414. Implementation/evidence are now
committed as `a95b21166aaef18c754c60cee2ac6bb8eee89a17`; publication-status metadata follows.
Coordinator pushed `ac00bed4d98046243b10cfb04e63c6a0401793fd` and opened
[draft PR154](https://github.com/dills122/sandtable/pull/154), targeting main.
Publication blocker is resolved; this metadata correction follows that published head.
Five primary files: new engine/models, codec, focused tests, one fixture-link project
change and canonical implementation plan. README, tech design, naming, roadmap and
Movement delivery plan have administrative capability updates. Review packets and
this handoff are retained administrative evidence. `.serena/` is tool-generated,
untracked and excluded from review/publication. Primary checkout was not edited.

## Completed Work And Evidence

Semantic RED1 failed for missing owner control opening after valid native proof
admission. GREEN1 passed, expanded focused8 passed, final focused16 passed0fail/skips
in22.165s (`/private/tmp/cmb019d3-final-focused.log`).

Native parity covers32+4 sources,80 traces,236 cuts and464 retained-command retries;
full original event/state bytes, pure World/RNG/members/attack-history preservation,
repeat resets and literal successor authority.1067 control event/state leaf mutations,
every proof leaf, altered source fields, fully re-signed native Result2/Release,
actor/foreign/stale/deadline/clock/timer/cache/canonical/capacity and ownership attacks
reject. Eight arithmetic theories cover all32 default contexts, commitment receipt
AND event hash, and literal CP/DP costs. Independent policy probes do not claim history.

New control oracle passed10semantic groups/80traces/32pins. All seven unchanged
oracles passed: settled-continuation-v1, result-settlement-v2, sealed-round-v2,
reserve-release-v1, ordinary-movement-v1, cycle-control-v1 and
inherited-reserve-movement-completion-v1. Logs `/private/tmp/cmb019d3-oracle-control.log`
and `/private/tmp/cmb019d3-upstream-oracles.log`.
All204 prior spec files byte-identical; changed-doc local paths and diff checks pass.

First `just check` passed2504solution tests/Boundary81,0fail/skips,0build warnings/errors.
It preceded final literal tests and source hardening and is NOT final-boundary evidence.
Final gate `/private/tmp/cmb019d3-final-check.log` completed2512solution tests/Boundary81,
0fail/skips,0build warnings/errors, format pass. Full-suite duration8m14.873s.

## Decisions And Rationale

ReadBase's factory runs complete native019D1 packet replay and exact regenerated proof
comparison before its private source constructor can retain immutable evidence.
Source/state/result outputs own their bytes; parsed projections are fresh.
Replay reconstructs complete suffix state; Apply compares supplied state caches and
returns original retained event bytes on exact command+actor retry after closure.
Actor/control/cycle binding precedes duplicate lookup. Supported empty witnesses force
finish; material progress and witnesses open the owner decision. Existing timeout,
unavailability/regression and opening-clock fallback policy is retained.
Repeat enters Movement with next ordinal, pre-repeat prefix and repeat result version;
finish enters Truck Convoy. Neither executes the successor or changes resources.

Graph project `sandtable-cmb019d3`, generation2026-10-05T02:55:07Z, predecessor paths
have no recorded coverage gaps; new untracked paths were not tracked and were checked
from exact source. Exact-worktree Serena activated; its new-worktree initial language
configuration was empty, so no Serena-diagnostic verification is claimed.

## Blockers And Limitations

Fresh GPT-6.1-sol medium independent review is Ready, set1/pass1,total1of9 (max3per set),
no findings; author Accept. Report retained in
`docs/work/reviews/2026-10-05-native-settled-control-review.md`. Draft PR154 is published;
exact-final-head CI and merge remain coordinator gates.
Earlier Runner aggregation failure did not recur in the first full gate; cause remains
unconfirmed. Retain any recurrence, do not weaken tests.

Coordinator milestone messaging was rejected by automatic approval review because it
would not accept delegated/transcript permission as trusted. A direct permission
question was superseded by the coordinator's authorized publication. No workaround or
unauthorized message was sent; status is returned through commentary/final retrieval.

Failures retained: CA1869 serializer-option allocation fixed with cached options;
strict canonical input mismatch fixed by ordered fixture-input encoding; descriptor
probe join fixed using proof sourceIdentity because probes lack top-level sourceId.
No test skip or assertion weakening.

## Immediate Next Actions

1. Local gate and independent review completed; no further tests or behavior changes.
2. Attach existing draft PR154 and push only administrative publication corrections.
3. Coordinator verifies exact-final-head CI and merges after required checks pass.

No new behavior after2026-10-05T08:47:28Z; hard end09:47:28Z. If closing out on time,
commit/push ALL retained work as explicit WIP with missing gates; no Ready/merge claim.

## Verification Commands

`dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-class '*CombatSettledControlTests' -bl:/private/tmp/cmb019d3-<unique>.binlog`

`python3 -B docs/specs/verify-combat-settled-control-v1.py`

`just check` only under coordinator full-suite lease. Author used a temporary PATH
wrapper adding unique `/private/tmp/cmb019d3-gate-<UUID>.binlog` to restore/build/test;
no repository tooling/config changed. Native MTP uses explicit --solution/--project.

## Delivery Metadata

Status: local gates and independent review passed; draft PR154 published, CI/merge pending.
Branch/review base above; latest tip is returned in final status and resolved by the PR head.
Only tool-generated `.serena/` remains untracked and excluded. Retained implementation commit `a95b21166aaef18c754c60cee2ac6bb8eee89a17`; publication-status
metadata commit follows. Latest local tip is the branch ref, returned in final status.
Full-suite lease RELEASED after complete final gate; coordinator may grant R2 its probe.
Use github-keychain-auth for each authorized Git/gh operation outside sandbox; unset
GH_TOKEN/GITHUB_TOKEN for gh; never extract credentials.

## Historical REVIEW_READY snapshot

Source/test bytes frozen after focused16cases/464retries pass22.165s. Current final
full suite is already running; lease remains held until completion. Earlier pre-retry
full2512pass is preserved separately. Coordinator may dispatch fresh GPT-6.1-sol medium
reviewer from bootstrap now; verdict held until final gate outcome. No new behavior or
coverage absent concrete failure/review finding. No PR/commit/independent pass yet.
Graph refreshed03:24:45Z,26302nodes/140015edges, no recorded issues for new source/codec/
tests/Campaigns scope; latest four assertion lines were read from exact source.

## Accepted Local Gate And Review

Final exact-byte gate complete2512solution tests/81Boundary tests,0fail/skips,0build
warnings/errors and format pass; log SHA256
593b01773adf44a8f2cdea7e53cddfc8ef94e12c98872258350bcb6fd79b2d33.
Independent Ready set1/pass1,total1of9,max3per set; no findings, author Accept.
FULLSUITE LEASE RELEASED. No more full gates or scope/test changes absent a material
change/new unresolved failure. Messaging-tool rejection is handled by ordinary
commentary/final status that coordinator retrieves; do not retry the denied tool.
Main868126d is docs-only PR152; no rebase or unrelated import into reviewed scope.

## Resolved Publication Blocker And Final Delivery

The original child push/PR attempt was rejected by automatic approval review before
execution because trusted destination/egress authorization was not established. Safe
read-only checks verified Keychain login `dills122` (id15662762), PUBLIC
`dills122/sandtable`, and ADMIN permission. The coordinator used direct human
authorization plus that live ownership evidence; automatic approval review then
approved its push of `ac00bed4d98046243b10cfb04e63c6a0401793fd` and creation of
[draft PR154](https://github.com/dills122/sandtable/pull/154). No further permission
is required; the pending child prompt is superseded. No rejected action was bypassed.

PR title: **Add native Result2 settled combat control**. Branch
`codex/native-settled-control`, target main, reviewed ancestry907f414 unchanged.
The PR is attached to this chat. Final publication-status correction changes only
this handoff and the author explanation; no source/test/project bytes, behavior,
review verdict or executed gate result change. No rebase, test rerun or additional
workstream. Coordinator waits exact-final-head CI and owns merge.

Reviewed hashes remain:

- Engine/models:8b167d88c07c8506e0b43e571c81117209747e0c2d7721b5135e0c91265793d1
- Codec:792707d1bf72214d209ab8a609e384ec02e123e9aeceaeae907246c9df827832
- Tests:799c02435c69c760b20d42243a31f54d33363b56ce2e6de6e481c45b4a5827e6
- Test project:37fa69bd8e923e356b27d5024c1c3769e185f086ea52aae1de71b95f1d6e926b

Gates remain16focused,2512solution and81Boundary tests passed0fail/skips; all eight
oracles pass; build0warnings/errors, format pass. Fresh independent Ready set1/pass1,
no findings; full-suite lease released. All retained work is committed/pushed; latest
metadata tip is returned in final status rather than embedding a self-referential hash.
