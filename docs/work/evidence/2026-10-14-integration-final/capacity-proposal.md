# N3 CI capacity diagnosis and bounded correction proposal

Read-only diagnosis at2026-10-14~23:02UTC (19:02Toronto), original reviewedhead972e0c513132afd5a92fcbbb5afdfe85f11732d0 remains frozen. No tests, code edits, branch, retry or merge launched. Brain owns approval and new exact-head/fresh-review gates. Raw evidence stays local.

## Root cause and evidence
Run38646785953 CoreB job116136681572 exceeded20m;22:22:04 start,22:23:23 execution start,22:42:20 cancel. Retained execution.log contains2608passed completions and zero failed/skipped lines, not a final XML/process receipt. N3 completed105/129 rows (6ordinarytests +99contracttests),24contract parity rows pending; eight old LandSequenceCache rows had not appeared. Last completed N3index97 passed3.633s at22:42:18.301, nextindex52 in-progress at22:42:18.302, cancellation2.14s later. Last20parity rows3.400–3.958s with continuous completion. This is direct workload/progress evidence, not a hang claim based only on local success. Diagnostic UID counts differ from console due deferred/provider reporting; do not use either as exhaustive CI identity acceptance.

Successful N2 run38630698865 CoreB job116082855576:2511exact XML rows; job20:20:06–20:39:24=19m18s, test step20:21:36–20:39:20=17m44s, assembly time1061.977s. Only42s nominal job headroom under20m. N3 added129complete rows to B without changing partition or budget. N3contract99completed row duration sum480.675s equals observed class wall span480.741s; remaining24parity at observed3.4–4.0s projects82–96s more. Full N3contract forecast563–577s (~9m23–9m37), six ordinary rows26.234s can parallelize as another collection. Full class/job expectation conservatively10–13m inclsetup/reporting;15m job cap leaves~2–5m headroom. Estimates are not a passing hosted gate.

CoreC is not sufficiently empty for a no-budget-change rebalance: prior N2 assembly130rows370.267s (~6m10); current same130class-family job22:22:04–22:32:18=10m14s, teststep22:23:40–22:32:15=8m35s. Simply moving N3 into C may exceed its15m cap depending collection scheduling/runner contention. A Ctimeout increase plus rebalance is viable but lengthens one leaf and reduces margin; dedicated N3 preserves existing policies and avoids depending on overlap.

## Recommended smallest robust fix
Add one required exact-row CoreD leaf owning exactly the TWO complete N3 classes:
- Cna.Core.Tests.Campaigns.CombatActualMovedCycleControlTests
- Cna.Core.Tests.Campaigns.CombatActualMovedCycleControlContractTests

Keep A's12method/eight-transferred-row policy, C'sfour existingcompleteclasses, B'sremainingwholeinventory semantics and all budgets unchanged (B20; A/C/D/Exercise/Contracts15). BexcludesDclasses;Dincludesonlyexactclasses and requiresbothpresent. Future methods in either selectedclass followD; similar/extra/unrelatedclassnames stayB. Expected atcurrent product: A71/B2511/C130/D129/Exercise469/Contracts10, total3320, inferenceuntilfreshactualdiscovery and XML/process verification. No skipped rows, wildcard slicing, test changes, native/product edits, timeout-only increase, attemptreuse or gate bypass.

Need explicit CORE_SHARDS=(core-a,core-b,core-c,core-d) enumeration forpartitionload/build/discovery/execution and MODULES map6leaves; preserve verify_coverage's fail-closed exactset/union/disjoint identity/SHA/attempt/SDK/Release/process0/singleXML/pass-only checks. New leaf downloads same prepared currentattempt inventory and emits itsownstatus/XML/diagnostics. Workflow matrix and aggregate coverD; no independent new service or product stream.

## Exact proposed edit allocation, after Brain approval only
1..github/scripts/test_partition.py: Dexactclass predicate/filter;fourCore iteration;6leaf map; unchangedacceptancevalidatorsemantics.
2..github/scripts/test_partition_test.py: genuine newDpolicy regression tests, fixtureupdates for sixthrequiredleaf and exact counts, failure controls.
3..github/workflows/ci.yml: addcore-dtoexistingmatrix, existing budgetexpressionunchanged, currentattemptartifact/needsvalidationunchanged.
4.README.md: currentfive-shardmap ->six, exactDresponsibility.
5.docs/design/combat-cycle-implementation-plan.md: serializedcapacitycorrection and exactnewheadgates, historicalfailedrunretained.
6.docs/work/plans/2026-10-14-n3-ci-capacity-implementation.md: pre-edit plan/TDD/evidence/time.
7.docs/research/2026-10-14-n3-ci-capacity-diagnosis.md: summary/derivedtimings only, no rawlogs/XML/binlogs.
Separate neutralbootstrap/author/selfreview/evidence text for boundedCIreview2 of9(pass2of3,recovery0of2) afterimplementation;oldN3packets/source/schema/pins/fixtures remainread-only. Suggested branchcodex/n3-ci-capacity fromexact972e, preservingoriginalN3branch. Primarycheckout/otherauthorsunrelatedwork preserved. Noedituntilapproved.

## Genuine TDD and regression approach
Before helper implementation, add tests requiring Dcomplete-class transfer and bothrequiredclasses tocurrent complete-row fixture; observe genuine assertionfailure oncurrent policy, not syntax/import/empty suite. Then implementminimalclasspredicateandfixturechanges. Run full fastPython coveragehelpersuite (no .NET/heavyexecution). Cases: exact exhaustive/disjoint4Coreunion, A/eightoldtransferunchanged, oldCunchanged, all129realN3identities moveB->D, futureN3methods enterD, similarclassremainsB, missingselectedDclass/leaf fails; source/attempt/SDK/config/process/nonzero/stale/missingXML/duplicate/wrongassignment/faileddependency stillreject. Workflowtests assert6exactmatrixleaves/no timeoutchange/artifactretention and actualexecutionargv matchesdiscoveryfilters. SimulatepreparedDmissing/wrongassignment and deliberatelybadproofs. Validate actual Release discovery for all4Core filters against singlefull inventory usingunchangedexpandedproviderproof and source/attempt binding; this is metadataenumeration, notheavytestexecution. No duplicate product tests or full local suite unless separatelyleased.

Author selfreview ->fresh independent CI-scope review2 exacthead ->newPR190publicationhead/CI run, no rerun of failedhead. Brain reconcilesactual3320disjointXMLrows, process0, attempt1,Release, exacttestedSHA, thenmerge/treematch/verifiedmainCI/CodeQL/links. ExistingN3native review1 accepted, its129native/referenceproofsremainbaseline; CIchange mustnotpresentold972CI asnewheadpass. No actualRepeat/public/full72/downstreamGO.

## Timing and stop decision
Atproposal~23:02UTC:closeout00:37:35 (~1h35),hardstop01:37:35 (~2h35). AuthorhelperTDD+actualdiscovery+packets15–25m; freshCIreview10–15m andhostedPRCI22–28m mayoverlap; merge/mainanother22–28m. Earliestverifiedmain~00:10–00:30,medium-lowconfidence, lateness/failure mayleave labelledunverifiedWIP. Bstillhas historicaltight20m budget; newDremovesmeasuredgrowthbutcloudvariationcouldstillfailanoldleaf. Do notpreemptivelyweakenlimits or promisecomplete. Reestimateatfreshdiscovery or15minafterGO. No newfeatureaftercloseout/productafterhardstop; Brain owns hold/retention ifgatesdon'tfit.

## Evidence locality and publication boundary
Raw /private/tmp/n3-core-b-ci.log, /private/tmp/n3-core-b-timeout and /private/tmp/n2-ci-final remainlocal; diagnostic/sourceextractionrecords summarizedcounts/timestamps only. No rawexport/27d8293 ancestryretry. PreserveoldN3 head972eandreview1report/disposition. This proposal is nofixapproval, no productchange or freshreviewcounterreset.
