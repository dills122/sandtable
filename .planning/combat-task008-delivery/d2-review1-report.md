# D2 independent review

Review instance: 1 of 3. Preliminary ledger recorded before opening author explanation/evidence.

## Preliminary ledger
- Scope verified: branch codex/combat-task008-reserve-completion, HEAD/base 073423f35c8be6bdb43e4b1579a705285408b934; five primary source hashes match d2-source.sha256. Four new source/test files explicitly included alongside tracked codec extraction and six documentation/status changes.
- Canonical Reserve and inherited-successor specs separate codec ownership from 019A projection; current plan preserves this distinction, same completion2 event, and Movement dependency. No plan blocker identified.
- Full source inspection: Create/CreateCommand/ReadEvent privately replay D1; no external OpeningBase admitted. Exact input equality and regenerated-event byte equality bind actor/occurrence/full predecessor. No actionable defect identified yet.
- Framing matches verifier AUTH_FIELDS: U32 contract version, U64 numeric authority, length-framed UTF-8 strings including rules/setup/content hashes, raw digest bytes only for openingPrefix/configuration digest.
- Tests cover 16 frozen fixture cases, alternate decoded-string-hash identity, re-signed cycle mutations, full-chain corruption, strict raw and scalar changes, unchanged predecessor and copied buffers. Execution pending; terminal state/readback/retry deliberately absent under 019A.
- Independence note: CCE search incidentally surfaced short d2-evidence heading/scope snippet while locating code; no author packet or prior review reports read. Session recall returned prior D1 acceptance and historical contract checkpoints, not D2 review judgments.

## Findings

No actionable findings. Reviewed five primary files, actual tracked diff and untracked additions; no unrelated source, fixture, project, generated-client, legacy-dispatch, or public-registration changes. Frozen source hashes matched both before and after review.

## Plan Review

Ready for bounded D2 scope. Canonical `combat-reserve-designation-v1.md` requires actual Created11 → four preamble → Weather2 → four stage events → optional designation provenance. `CampaignCombatReserveCompletion.DeriveBase` delegates that reconstruction to D1 Replay and retains replay-owned request, predecessor World/RNG, resolved first owner, prefix and member history. ReadEvent independently regenerates complete expected bytes, so internally consistent re-signing cannot confer authority.

Original atomic completion requirement remains intact: completion2 contains exact Movement position and ordinal1 authority in one event. No extra opening command/event introduced. `combat-cycle-implementation-plan.md:801–816` explicitly sequences D1 → D2 → 019A → E, leaves Reserve parent open and assigns terminal state11/12, complete receipt ledger, designation retry after completion, readback and Movement handoff to 019A. This is bounded implementation decomposition, not weakened acceptance. Five-primary-file cap honored; README, technical design, naming and roadmap distinguish evidence from terminal projection. HOST-PUB-001 remains open.

Exact wire review checked schema field order and verifier implementations `_base`, `_completion_event`, `AUTH_FIELDS`, `tuple_bytes` and receipt generation. Cycle contract version uses big-endian U32; gameTurn, stage, ordinal and opened version use U64; rules/setup/content hashes are length-framed UTF-8 strings; only openingPrefix and admittedPolicyBundleDigest are raw digests. Domain includes terminating NUL. Sources retain 18.11 and 5.2.reserve-designation. OpeningBase historical profile literal stays unchanged. Movement keeps sequence5 and null activeSide, with actual owner bound in cycle.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Full-history consumer, no supplied base/cache admission | Completion CreateCommand/Create/ReadEvent/DeriveBase; D1 Replay/Initial | Confirmed | Provenance established before generation/read acceptance |
| Exact frozen base/input/event compatibility | Canonical schemas and Python construction; 16-case C# golden theory | Confirmed | 48 frozen fingerprints exercised across four Weather outcomes, both orders, none/I |
| String-hash and raw-digest framing differ | Codec CycleId, canonical AUTH_FIELDS/tuple_bytes, independent test Identity and deliberately wrong encoding | Confirmed | Identity matches canonical framing rather than plausible alternative |
| Canonical input/event rejection and bounds | DeserializeInput/Parse, Generate, full byte equality; raw/scalar/history and signed-cycle rejection tests | Confirmed | Unknown, duplicate, alternate encoding and altered authority cannot enter through reviewed API |
| Immutable evidence/unchanged predecessor | Read-only predecessor member/receipt collections, fresh EventBytes; buffer-mutation and serialized-predecessor assertions | Confirmed | No retained mutable input byte arrays observed |
| D1 change only serializer reuse | Tracked codec diff: WriteMembers extraction, WriteWorld accessibility; D1 golden tests | Confirmed | Existing 23 designation tests pass |
| 44 focused tests pass | Independent no-build run below | Confirmed | 21 D2 + 23 D1 executed successfully |
| Build/format integration pass | `/tmp/d2-build.log`, `/tmp/d2-format.log`, author evidence | Build log confirmed; format reported with empty successful-output log | Reviewer did not rebuild or rerun format |
| Broader oracle proves contract, not 019A runtime | Canonical oracle source and `/tmp/d-reserve-baseline.log` | Confirmed scope distinction | No terminal C# behavior inferred from Python results |
| Full suite | Evidence said running when read | Unverified by this reviewer | Lead retains integration ownership; no full-suite pass claimed here |

## Verification Performed

- `git status --short`, `git branch --show-current`, `git rev-parse HEAD`, `git diff --stat` and scoped diff inspection: expected branch/base and dirty scope verified.
- `shasum -a 256 -c .planning/combat-task008-delivery/d2-source.sha256`: all five OK, twice.
- `git diff --check`: exit 0.
- Runtime selection checked: SDK 10.0.400; global.json native MTP; SDK-style net10.0 test executable with xUnit v3 MTP package.
- Exact test command: `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatReserveCompletionTests' --filter-class '*CombatReserveDesignationTests' '-bl:/tmp/d2-review1-{}.binlog'`.
- First sandbox attempt exited134 before test execution: MTP local named-pipe SocketException13 Permission denied. Same command with approved outside-sandbox execution passed44/44, failed0, skipped0, duration4.268s. Successful binlog `/tmp/d2-review1-20260919-230508--61116--EZlQ__-dotnet-test.binlog` exists. Failure binlog also retained.
- Existing oracle baseline inspected: PASS16 traces/40 cuts/2342 leaf/733 raw/1578 boundary-retry/4 frozen-kernel parity/14 source pins. Not rerun; canonical sources unchanged and no concern warranted duplicate execution.
- No builds, code edits, commits, delegation or prior-review-report reads performed. Only this report written.

## Open Questions And Residual Risks

- Ready verdict covers dormant codec/evidence only. No terminal replay/retry/readback, general restoration, public activation, durable publication or playable Movement is certified.
- 019A must derive/validate complete history when admitting evidence; internal constructors are not an external trust boundary and must not become one accidentally.
- Bounded repeated predecessor replay is acceptable for finite state10/11 profile; no general campaign-capacity or performance claim.
- Full solution integration remains lead-owned. Existing build log shows zero warnings/errors; focused independent run uses that build.

## Verdict

**Ready** for Task008 D2 codec scope. Implementation and plan both preserve canonical provenance, exact bytes and same-event atomic-opening obligation; no blocker found.

## Recommended Next Actions

Lead reconcile this report and integration evidence, complete configured remaining fresh reviews, then hand validated D2 event plus full predecessor history to 019A. Keep Task008/Reserve parent, terminal projection and HOST-PUB-001 open until their respective evidence exists. No replacement reviewer or further workstream started by this review.
