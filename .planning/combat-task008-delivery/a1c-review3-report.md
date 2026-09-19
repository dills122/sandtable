# A1c independent review

Review instance: 3 of 3.

## Preliminary ledger — before author explanation

Scope verified: branch `codex/combat-task008-creation-binding`, HEAD/base `e64bed9`; four untracked source/test files and five tracked documentation changes match bootstrap. Additional untracked `docs/specs/__pycache__/` is generated verifier output, outside implementation scope. No author packet or prior review report was opened before this ledger. CCE search incidentally returned author intent and prior-report heading/scope snippets; neither contained findings or substantive rationale. Focused file reads followed incomplete CCE symbol retrieval.

Canonical creation envelope, Task008 execution index, new tests and implementation inspected, followed by Setup7/configuration, World7 initial codec, Content7 validation and manifest/ID guards. Preliminary concerns checked: trusted-context substitution, input bounds, unsigned seed handling, exact JSON spelling, disabled-admission retry order, accidental runtime activation, and overclaiming persistence. No actionable defect established. Exact comparison is sufficient here because entire accepted Created11 state derives from independently retained immutable request; it does not establish later-history recovery.

Independent checks: focused C# run passed 52/52, zero skipped; creation-envelope oracle passed; `git diff --check` passed. Initial sandboxed test attempt failed before test execution on local IPC permission; same no-build command passed with escalation.

## Findings

No actionable findings. Setup/configuration 128-character envelope bound is enforced before trusted-context reconstruction; four boundary cases pass. No further blocker identified after inspecting surrounding predecessor validation.

## Plan Review

A1c scope matches execution row: Request1/Created11 literal parity, independent trusted input binding, immutable request identity, canonical rejection, and pure retry selection. New production types remain internal and only reference each other/predecessor Core values; no host, public registration, prior reader or transport change. Navigation/design updates preserve dormant status and pending A2/B–H gates.

Actual publication uniqueness, ambiguous acknowledgments and durable recovery remain parent008 obligations. Plan explicitly requires publication evidence seam before A2 acceptance. That deferred decision does not invalidate this bounded codec slice and must not be erased when closing A1c.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Request and event match frozen 741/7,520-byte literals | `RequestAndCreated11MatchFrozenBytesAndBinding`, schema object inventory, focused test pass | Confirmed | Exact versioned byte parity established |
| Imported nested authority cannot replace trusted inputs | Context constructor, Setup/config strict readers, request reconstruction, Created11 whole-byte comparison, trusted-fork tests | Confirmed | Same-ID rehashed evidence fails against original retained request |
| RNG uses explicit full unsigned seed with cursor0 | Request constructor; `FullUnsignedSeedRangeRoundTripsWithoutDraws` | Confirmed | No draw or signed narrowing |
| Retry reads retained bytes before admission, preserving original | `CampaignCombatCreationCut.Decide`, retry/identity tests | Confirmed | Pure retry behavior covered; no publication proof inferred |
| Setup/config IDs accept128 and reject129 | Context guard and four `TrustedInputIdsRespectEnvelopeBounds` cases | Confirmed | Reported prior blocker closed in current target |
| No public/historical activation | Git boundary and production reference search | Confirmed | Dormant addition, unchanged existing readers |
| Current full integration green | Author packet says post-fix refresh underway; lead confirmed build zero warnings/errors | Partly verified | Reviewer independently ran focused tests; final full-suite/format evidence remains lead closeout |

## Verification Performed

- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatCreationBindingTests' '-bl:/tmp/a1c-review3-{}.binlog'`: 52 passed, zero failed/skipped, 1.079s. Successful binlog `/tmp/a1c-review3-20260919-210605--42106--b9voHQ-dotnet-test.binlog`. Initial sandbox attempt exited134 on IPC permission; escalated retry passed.
- `PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-authority-envelope-v1.py`: exit0; four goldens, 67 mutations, 36 raw rejects, nine recovery boundaries, 12 identity/context forks, two turn boundaries, 693 nested type rejects. Contract oracle only.
- `git diff --check`: exit0.
- Reviewed working-tree delta, canonical contract/schema, frozen test inputs, surrounding validators and new-symbol references. No build or full-suite run performed concurrently with lead.

## Open Questions And Residual Risks

No open A1c correctness question. This review provides no evidence for Snapshot12, noninitial recovery, atomic campaign-ID uniqueness, concurrency or actual durable restart. Lead owns current full integration completion. Generated Python cache is outside intended deliverable.

## Verdict

**Ready** for bounded A1c scope. No remaining blocker found; review instance3 completes configured review loop. No additional review dispatched.

## Recommended Next Actions

Lead finish post-fix integration closeout and record exact evidence before packaging A1c. Preserve Task008 publication seam decision and A2/full-history exclusions in subsequent work.
